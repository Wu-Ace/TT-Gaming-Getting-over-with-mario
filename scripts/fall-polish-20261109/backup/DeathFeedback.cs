using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Character.Lifecycle
{
    // Scene-local presentation: no global time-scale changes, progress writes or voice clips.
    [DefaultExecutionOrder(10000)]
    public sealed class DeathFeedback : MonoBehaviour
    {
        [SerializeField, Range(.6f, 2f)] float impactDuration = 1.05f;
        [SerializeField, Range(.6f, 2f)] float fallDuration = .85f;
        [SerializeField, Range(0, .12f)] float impactHold = .06f;
        [SerializeField, Range(0, .015f)] float shakeHeight = .005f;
        public static bool Active { get; private set; }
        public bool Falling { get; private set; }
        public float Elapsed => Time.unscaledTime - started;
        float started, duration, originalSize;
        bool released, loading;
        Camera view;
        CinemachineBrain brain;
        bool brainWasEnabled;
        Vector3 cameraPosition;
        Quaternion cameraRotation;
        Rigidbody2D[] bodies;
        Vector2 launch;
        Canvas canvas;
        Image fade, flash, marker;
        readonly List<Image> sparks = new List<Image>();
        readonly List<Image> fallTrails = new List<Image>();
        readonly List<Vector2> directions = new List<Vector2>();
        readonly List<Sprite> ownedSprites = new List<Sprite>();
        Vector3 contactPoint;
        float markerX;
        bool showMarker;
        AudioClip cue;
        readonly Dictionary<AudioSource, float> musicVolumes = new Dictionary<AudioSource, float>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetState() { Active = false; }

        public void Play(Transform root, Rigidbody2D body, bool falling, Vector2 point, Vector2 normal)
        {
            Active = true;
            Falling = falling;
            started = Time.unscaledTime;
            duration = falling ? fallDuration : impactDuration;
            contactPoint = point;
            view = Camera.main;
            if (view != null)
            {
                cameraPosition = view.transform.position;
                cameraRotation = view.transform.rotation;
                originalSize = view.orthographicSize;
                brain = view.GetComponent<CinemachineBrain>();
                if (brain != null) { brainWasEnabled = brain.enabled; brain.enabled = false; }
                var viewport = view.WorldToViewportPoint(transform.position);
                markerX = Mathf.Clamp01(viewport.x);
                showMarker = falling && (viewport.y < .08f || viewport.y > 1 || viewport.x < 0 || viewport.x > 1);
            }
            launch = falling ? new Vector2(0, -5) : new Vector2(Mathf.Abs(normal.x) > .15f ? Mathf.Sign(normal.x) * 3.5f : 0, 7);
            // Release the complete rig so the hammer cannot pin or pull the corpse.
            foreach (var joint in root.GetComponentsInChildren<Joint2D>()) joint.enabled = false;
            foreach (var collider in root.GetComponentsInChildren<Collider2D>()) collider.enabled = false;
            foreach (var animator in root.GetComponentsInChildren<Animator>()) animator.enabled = false;
            bodies = root.GetComponentsInChildren<Rigidbody2D>();
            foreach (var rb in bodies) rb.simulated = false;
            var sorting = root.GetComponent<SortingGroup>();
            if (sorting == null) sorting = root.gameObject.AddComponent<SortingGroup>();
            sorting.sortingLayerID = SortingLayer.layers[SortingLayer.layers.Length - 1].id;
            sorting.sortingOrder = 30000;
            foreach (var source in FindObjectsOfType<AudioSource>())
                if (source.loop) musicVolumes[source] = source.volume;
            BuildOverlay();
            cue = MakeCue(falling);
            GameplayAudio.Play(cue, falling ? .5f : .65f);
            StartCoroutine(Sequence());
        }

        void BuildOverlay()
        {
            var go = new GameObject("Death feedback overlay", typeof(Canvas), typeof(CanvasScaler));
            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.matchWidthOrHeight = .5f;
            if (!Falling && view != null)
            {
                var star = MakeShape(false);
                for (int i = 0; i < 9; i++)
                {
                    sparks.Add(MakeImage("Impact star", star, new Color(1, .82f, .22f), new Vector2(i == 0 ? 105 : 26, i == 0 ? 105 : 26)));
                    float a = (i - 1) * Mathf.PI / 4;
                    directions.Add(new Vector2(Mathf.Cos(a), Mathf.Sin(a)));
                }
            }
            if (Falling)
                for (int i = 0; i < 5; i++)
                    fallTrails.Add(MakeImage("Fall streak", null, Color.clear, new Vector2(3, 24 + i * 6)));
            if (showMarker)
            {
                marker = MakeImage("Fall direction", MakeShape(true), new Color(1, .85f, .3f), new Vector2(48, 48));
                marker.rectTransform.anchorMin = marker.rectTransform.anchorMax = new Vector2(markerX, 0);
            }
            flash = MakeImage("Impact flash", null, Color.clear, Vector2.zero);
            Stretch(flash);
            fade = MakeImage("Death fade", null, Color.clear, Vector2.zero);
            Stretch(fade);
        }

        Image MakeImage(string label, Sprite sprite, Color color, Vector2 size)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(canvas.transform, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            image.preserveAspect = true;
            image.rectTransform.sizeDelta = size;
            return image;
        }
        static void Stretch(Image image)
        {
            image.rectTransform.anchorMin = Vector2.zero;
            image.rectTransform.anchorMax = Vector2.one;
            image.rectTransform.offsetMin = image.rectTransform.offsetMax = Vector2.zero;
        }
        Sprite MakeShape(bool arrow)
        {
            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Point;
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float px = (x + .5f) / size * 2 - 1, py = (y + .5f) / size * 2 - 1;
                bool inside;
                if (arrow) inside = (Mathf.Abs(px) < .22f && py > -.05f && py < .8f) || (py < .05f && py > -.85f && Mathf.Abs(px) < (py + .85f) * .85f);
                else
                {
                    inside = false;
                    for (int a = 0, b = 9; a < 10; b = a++)
                    {
                        float aa = Mathf.PI * .5f + a * Mathf.PI / 5, ba = Mathf.PI * .5f + b * Mathf.PI / 5;
                        float ar = a % 2 == 0 ? .95f : .4f, br = b % 2 == 0 ? .95f : .4f;
                        Vector2 av = new Vector2(Mathf.Cos(aa), Mathf.Sin(aa)) * ar, bv = new Vector2(Mathf.Cos(ba), Mathf.Sin(ba)) * br;
                        if ((av.y > py) != (bv.y > py) && px < (bv.x - av.x) * (py - av.y) / (bv.y - av.y) + av.x) inside = !inside;
                    }
                }
                pixels[y * size + x] = inside ? Color.white : Color.clear;
            }
            texture.SetPixels(pixels); texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * .5f, size);
            ownedSprites.Add(sprite);
            return sprite;
        }

        IEnumerator Sequence()
        {
            while (Elapsed < duration && !loading)
            {
                if (!GameMenuUI.IsPaused && Elapsed > .45f && RestartPressed()) break;
                yield return null;
            }
            loading = true;
            float from = fade.color.a;
            float start = Time.unscaledTime;
            while (Time.unscaledTime - start < .15f)
            {
                fade.color = new Color(0, 0, 0, Mathf.Lerp(from, 1, (Time.unscaledTime - start) / .15f));
                yield return null;
            }
            fade.color = Color.black;
            // Only the blackout survives loading; world effects remain scene-local.
            var arrival = canvas.gameObject.AddComponent<DeathArrivalFade>();
            arrival.Initialize(fade);
            DontDestroyOnLoad(canvas.gameObject);
            canvas = null;
            marker = null;
            SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
        }
        static bool RestartPressed()
        {
            return (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame))
                || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                || (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame);
        }
        void LateUpdate()
        {
            float t = Elapsed;
            foreach (var pair in musicVolumes)
                if (pair.Key != null) pair.Key.volume = pair.Value * Mathf.Lerp(.35f, 1, Mathf.Clamp01(t / .4f));
            if (!released && (Falling || t >= impactHold))
            {
                released = true;
                foreach (var rb in bodies)
                {
                    if (rb == null) continue;
                    rb.constraints = RigidbodyConstraints2D.None;
                    rb.gravityScale = Mathf.Max(1, rb.gravityScale);
                    rb.simulated = true;
                    rb.velocity = launch;
                    rb.angularVelocity = Falling ? 75 : (launch.x < 0 ? 210 : -210);
                }
            }
            if (view != null)
            {
                float shake = !Falling && PlayerPrefs.GetInt(GameMenuUI.PreferencePrefix + "deathShake", 1) != 0 ? shakeHeight * originalSize * 2 * Mathf.Clamp01(1 - t / .22f) : 0;
                view.transform.SetPositionAndRotation(cameraPosition + new Vector3(Mathf.Sin(t * 145) * shake, Mathf.Sin(t * 113) * shake, 0)
                    + (Falling ? Vector3.down * (.25f * Mathf.SmoothStep(0, 1, t / .35f)) : Vector3.zero), cameraRotation);
                view.orthographicSize = originalSize * (1 - (!Falling ? .03f * Mathf.Sin(Mathf.Clamp01(t / .55f) * Mathf.PI) : 0));
            }
            if (flash != null)
                flash.color = new Color(1, .94f, .8f, !Falling && PlayerPrefs.GetInt(GameMenuUI.PreferencePrefix + "deathFlash", 1) != 0 ? .12f * Mathf.Clamp01(1 - t / .09f) : 0);
            for (int i = 0; i < sparks.Count; i++)
            {
                var star = sparks[i];
                if (star == null || view == null) continue;
                var screen = (Vector2)view.WorldToScreenPoint(contactPoint);
                RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)star.transform.parent, screen, null, out var position);
                star.rectTransform.anchoredPosition = position + (i == 0 ? Vector2.zero : directions[i] * (35 + 210 * t) + Vector2.down * (180 * t * t));
                star.rectTransform.localRotation = Quaternion.Euler(0, 0, i * 37 + t * 160);
                star.rectTransform.localScale = Vector3.one * (i == 0 ? Mathf.Lerp(.55f, 1.4f, Mathf.Clamp01(t / .18f)) : 1);
                star.color = new Color(1, i == 0 ? .95f : .78f, .25f, Mathf.Clamp01(1 - t / (i == 0 ? .2f : .5f)));
            }
            for (int i = 0; i < fallTrails.Count; i++)
            {
                var trail = fallTrails[i];
                if (trail == null || view == null) continue;
                RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)trail.transform.parent, view.WorldToScreenPoint(transform.position), null, out var position);
                trail.rectTransform.anchoredPosition = position + new Vector2((i - 2) * 18, 42 + Mathf.Repeat(t * 180 + i * 19, 65));
                trail.color = new Color(1, .93f, .7f, .65f * Mathf.Clamp01(1 - t / .6f));
            }
            if (marker != null)
            {
                // Clamp to the physical safe area, including tall phone screens.
                var rt = (RectTransform)canvas.transform;
                float units = rt.rect.height / Screen.height;
                float x = Mathf.Clamp(markerX * Screen.width, Screen.safeArea.xMin + 40, Screen.safeArea.xMax - 40);
                marker.rectTransform.anchorMin = marker.rectTransform.anchorMax = new Vector2(x / Screen.width, 0);
                marker.rectTransform.anchoredPosition = new Vector2(0, Screen.safeArea.yMin * units + Mathf.Lerp(110, 40, Mathf.Clamp01(t / .45f)));
                marker.color = new Color(1, .85f, .3f, Mathf.Clamp01(1 - Mathf.Max(0, t - .35f) / .25f));
            }
            if (!loading && fade != null) fade.color = new Color(0, 0, 0, .12f * Mathf.Clamp01(t / duration));
        }
        static AudioClip MakeCue(bool falling)
        {
            // Pure synthesized tones/noise; no vocal samples or calls.
            const int rate = 22050;
            float length = falling ? .42f : .18f;
            var samples = new float[(int)(rate * length)];
            float phase = 0;
            uint noise = 12345;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / samples.Length;
                phase += 2 * Mathf.PI * Mathf.Lerp(falling ? 650 : 220, falling ? 120 : 65, t) / rate;
                noise = noise * 1664525u + 1013904223u;
                float n = ((noise >> 8) / 16777215f * 2 - 1);
                samples[i] = (Mathf.Sin(phase) * .55f + n * (falling ? .05f : .35f)) * Mathf.Min(1, t * 80) * Mathf.Pow(1 - t, 2) * .6f;
            }
            var clip = AudioClip.Create(falling ? "Death fall tone" : "Death impact", samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }
        void OnDestroy()
        {
            Active = false;
            foreach (var pair in musicVolumes) if (pair.Key != null) pair.Key.volume = pair.Value;
            if (view != null)
            {
                view.transform.SetPositionAndRotation(cameraPosition, cameraRotation);
                view.orthographicSize = originalSize;
                if (brain != null) brain.enabled = brainWasEnabled;
            }
            if (canvas != null) Destroy(canvas.gameObject);
            foreach (var sprite in ownedSprites) if (sprite != null) { Destroy(sprite.texture); Destroy(sprite); }
            if (cue != null) Destroy(cue);
        }
    }
}
