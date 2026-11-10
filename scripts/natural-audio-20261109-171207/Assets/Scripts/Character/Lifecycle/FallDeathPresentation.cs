using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Character.Lifecycle
{
    // Screen-space memory of the real rig, not a replacement or teleport of its physics body.
    public sealed class FallDeathPresentation : MonoBehaviour
    {
        sealed class Pose { public Image image; public Vector2 offset; public float angle; public Color color; public int echo; }
        readonly List<Pose> poses = new List<Pose>();
        readonly List<Image> wind = new List<Image>();
        readonly List<Image> rings = new List<Image>();
        readonly List<Texture2D> textures = new List<Texture2D>();
        readonly List<Sprite> sprites = new List<Sprite>();
        Canvas canvas;
        Camera view;
        RectTransform frame;
        Image vignette;
        FallIrisGraphic iris;
        Vector2 exit;
        float started;
        bool closing;
        public int SnapshotCount => poses.Count;
        public bool Offscreen { get; private set; }

        public void Initialize(Canvas owner, Camera camera, Transform rig, Transform body)
        {
            canvas = owner; view = camera; frame = (RectTransform)canvas.transform;
            started = Time.unscaledTime;
            // Snapshot coordinates must use the same scale from the very first frame,
            // before CanvasScaler receives its next Update (including high-DPI displays).
            var scaler = owner.GetComponent<CanvasScaler>();
            if (scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
                canvas.scaleFactor = Mathf.Pow(Screen.width / scaler.referenceResolution.x, 1 - scaler.matchWidthOrHeight)
                    * Mathf.Pow(Screen.height / scaler.referenceResolution.y, scaler.matchWidthOrHeight);
            Canvas.ForceUpdateCanvases();
            var viewport = view.WorldToViewportPoint(body.position);
            Offscreen = viewport.y < .12f || viewport.y > 1 || viewport.x < 0 || viewport.x > 1;
            float screenX = Mathf.Clamp(viewport.x * Screen.width, Screen.safeArea.xMin + 100, Screen.safeArea.xMax - 100);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(frame, new Vector2(screenX, Screen.safeArea.yMin + Screen.safeArea.height * .2f), null, out exit);
            vignette = Image("Fall depth vignette", Radial(false), Vector2.zero);
            Stretch(vignette.rectTransform);
            vignette.preserveAspect = false;
            // Short, sparse upward strokes at the sides imply a downward rush without covering play.
            for (int i = 0; i < 24; i++) wind.Add(Image("Fall wind streak", null, new Vector2(i % 3 == 0 ? 3 : 2, 32 + i % 5 * 19)));
            Sprite ring = Radial(true);
            for (int i = 0; i < 2; i++) { var image = Image("Fall air ring", ring, new Vector2(110, 42)); image.preserveAspect = false; rings.Add(image); }
            if (Offscreen)
            {
                var renderers = rig.GetComponentsInChildren<SpriteRenderer>();
                System.Array.Sort(renderers, (a, b) => a.sortingOrder.CompareTo(b.sortingOrder));
                Vector2 bodyScreen = view.WorldToScreenPoint(body.position);
                float pixelsPerUnit = Screen.height / (view.orthographicSize * 2 * canvas.scaleFactor);
                // Earlier silhouettes trail the fully colored lead pose. Every source frame keeps its ratio/pivot.
                for (int echo = 2; echo >= 0; echo--)
                    foreach (var renderer in renderers)
                    {
                        if (!renderer.enabled || renderer.sprite == null || !renderer.gameObject.activeInHierarchy) continue;
                        Sprite source = renderer.sprite;
                        Vector2 size = source.rect.size / source.pixelsPerUnit * Mathf.Abs(renderer.transform.lossyScale.x) * pixelsPerUnit;
                        var image = Image(echo == 0 ? "Fall silhouette" : "Fall afterimage", source, size);
                        image.rectTransform.pivot = source.pivot / source.rect.size;
                        var scale = renderer.transform.lossyScale;
                        image.rectTransform.localScale = new Vector3((renderer.flipX ? -1 : 1) * Mathf.Sign(scale.x), (renderer.flipY ? -1 : 1) * Mathf.Sign(scale.y), 1);
                        Vector2 offset = ((Vector2)view.WorldToScreenPoint(renderer.transform.position) - bodyScreen) / canvas.scaleFactor;
                        poses.Add(new Pose { image = image, offset = offset, angle = renderer.transform.eulerAngles.z - view.transform.eulerAngles.z, color = renderer.color, echo = echo });
                    }
            }
            var go = new GameObject("Fall iris closure", typeof(RectTransform), typeof(CanvasRenderer), typeof(FallIrisGraphic));
            go.transform.SetParent(transform, false);
            iris = go.GetComponent<FallIrisGraphic>();
            iris.raycastTarget = false;
            Stretch(iris.rectTransform);
            iris.Set(0, exit);
            Animate(0);
        }
        Image Image(string label, Sprite sprite, Vector2 size)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);
            var image = go.GetComponent<Image>(); image.sprite = sprite; image.raycastTarget = false; image.preserveAspect = true;
            image.rectTransform.sizeDelta = size; image.color = Color.clear;
            return image;
        }
        static void Stretch(RectTransform rt)
        { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero; }
        Sprite Radial(bool ring)
        {
            const int n = 128;
            var texture = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[n * n];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float radius = new Vector2((x + .5f) / n * 2 - 1, (y + .5f) / n * 2 - 1).magnitude;
                float alpha = ring ? Mathf.Clamp01(1 - Mathf.Abs(radius - .83f) / .045f) : Mathf.SmoothStep(0, 1, Mathf.Clamp01((radius - .3f) / .85f));
                pixels[y * n + x] = new Color(1, 1, 1, alpha);
            }
            texture.SetPixels(pixels); texture.Apply(); textures.Add(texture);
            var sprite = Sprite.Create(texture, new Rect(0, 0, n, n), Vector2.one * .5f, n);
            sprites.Add(sprite); return sprite;
        }
        void Update() { Animate(Time.unscaledTime - started); }
        void Animate(float t)
        {
            if (vignette == null) return;
            vignette.color = new Color(.025f, .06f, .1f, Mathf.Lerp(.12f, .75f, Mathf.SmoothStep(0, 1, t / .85f)));
            float energy = Mathf.Sin(Mathf.Clamp01(t / .9f) * Mathf.PI);
            for (int i = 0; i < wind.Count; i++)
            {
                var line = wind[i];
                float side = i % 2 == 0 ? -1 : 1;
                float x = side * frame.rect.width * (.32f + (i % 7) * .025f);
                float y = Mathf.Repeat(t * (650 + i % 4 * 100) + i * 97, frame.rect.height + 180) - frame.rect.height * .5f - 90;
                line.rectTransform.anchoredPosition = new Vector2(x, y);
                line.rectTransform.localRotation = Quaternion.Euler(0, 0, -side * 5);
                line.color = new Color(.82f, .91f, 1, energy * (i % 3 == 0 ? .38f : .19f));
            }
            for (int i = 0; i < rings.Count; i++)
            {
                float phase = Mathf.Clamp01((t - i * .1f) / .45f);
                rings[i].rectTransform.anchoredPosition = exit + Vector2.down * 22;
                rings[i].rectTransform.localScale = Vector3.one * Mathf.Lerp(.7f, 3.5f, phase);
                rings[i].color = new Color(.9f, .95f, 1, t < i * .1f ? 0 : .5f * (1 - phase));
            }
            foreach (var pose in poses)
            {
                float p = Mathf.Clamp01((t - pose.echo * .055f) / .62f);
                float angle = -14 + 110 * p * p;
                Quaternion rotation = Quaternion.Euler(0, 0, angle);
                pose.image.rectTransform.anchoredPosition = exit + (Vector2)(rotation * pose.offset) + new Vector2(20 * p, 28 - (frame.rect.height * .32f + 100) * p * p);
                pose.image.rectTransform.localRotation = Quaternion.Euler(0, 0, pose.angle + angle);
                float alpha = (pose.echo == 0 ? .9f : .16f / pose.echo) * (1 - Mathf.SmoothStep(0, 1, p));
                pose.image.color = pose.echo == 0 ? new Color(pose.color.r, pose.color.g, pose.color.b, pose.color.a * alpha) : new Color(.6f, .8f, 1, alpha);
            }
            if (!closing) iris.Set(0, exit);
        }
        public void Hide()
        {
            if (vignette != null) vignette.enabled = false;
            foreach (var image in wind) image.enabled = false;
            foreach (var image in rings) image.enabled = false;
            foreach (var pose in poses) pose.image.enabled = false;
            if (iris != null) iris.enabled = false;
        }
        public void Close(float amount) { closing = true; iris.Set(Mathf.SmoothStep(0, 1, amount), exit); }
        public static AudioClip CreateWindCue()
        {
            const int rate = 22050;
            var data = new float[(int)(rate * .7f)];
            uint noise = 7341; float phase = 0, filtered = 0;
            for (int i = 0; i < data.Length; i++)
            {
                float t = (float)i / data.Length;
                noise = noise * 1664525u + 1013904223u;
                filtered = Mathf.Lerp(filtered, (noise >> 8) / 16777215f * 2 - 1, .12f);
                phase += 2 * Mathf.PI * Mathf.Lerp(520, 85, t * t) / rate;
                data[i] = (filtered * .8f + Mathf.Sin(phase) * .22f) * Mathf.Sin(Mathf.PI * t) * .5f;
            }
            var clip = AudioClip.Create("Fall wind and descending tone", data.Length, 1, rate, false);
            clip.SetData(data, 0); return clip;
        }
        void OnDestroy()
        {
            foreach (var sprite in sprites) if (sprite != null) Destroy(sprite);
            foreach (var texture in textures) if (texture != null) Destroy(texture);
        }
    }
}
