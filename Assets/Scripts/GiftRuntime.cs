using System.Collections.Generic;
using Character.Lifecycle;
using Character.PowerUp;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LiveGifts
{
    public sealed class GiftRuntime : MonoBehaviour
    {
        public static GiftRuntime Instance { get; private set; }
        public GiftConfiguration Configuration { get; private set; }
        public string LastMessage { get; private set; } = "Ready";
        TemporaryGrowth growth;
        Death character;
        Rigidbody2D[] bodies;
        readonly HashSet<string> seen = new HashSet<string>();
        readonly Queue<string> history = new Queue<string>();
        readonly Queue<Vector2> impulses = new Queue<Vector2>();
        float scaleUntil, nextImpulse;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            SceneManager.sceneLoaded -= Loaded;
            SceneManager.sceneLoaded += Loaded;
            Attach();
        }
        static void Loaded(Scene scene, LoadSceneMode mode) { Attach(); }
        static void Attach()
        {
            if (FindObjectOfType<GiftRuntime>() != null) return;
            var target = FindObjectOfType<Death>();
            if (target != null) target.gameObject.AddComponent<GiftRuntime>();
        }
        void Awake()
        {
            Instance = this;
            character = GetComponent<Death>();
            growth = FindObjectOfType<TemporaryGrowth>();
            bodies = character.transform.parent.GetComponentsInChildren<Rigidbody2D>();
            Configuration = Resources.Load<GiftConfiguration>("GiftConfiguration");
            if (Configuration == null) Configuration = ScriptableObject.CreateInstance<GiftConfiguration>();
#if UNITY_EDITOR || GIFT_TESTING
            gameObject.AddComponent<GiftKeyboardTester>();
#endif
        }
        // Call on Unity's main thread. The bridge must convert cumulative streaks to deltas.
        // One unique eventId per delivered delta; quantities are bounded per message.
        public bool ReceiveGift(string eventId, string giftId, int quantity = 1, string viewer = "Viewer")
        {
            if (character == null || character.Dead || Time.timeScale == 0 || string.IsNullOrEmpty(eventId)
                || seen.Contains(eventId) || quantity < 1) return false;
            GiftBinding binding = null;
            var entries = Configuration.gifts;
            if (entries == null) return false;
            for (int i = 0; i < Mathf.Min(20, entries.Length); i++)
                if (entries[i] != null && entries[i].giftId == giftId) { binding = entries[i]; break; }
            if (binding == null) return false;
            seen.Add(eventId); history.Enqueue(eventId);
            if (history.Count > 2048) seen.Remove(history.Dequeue());
            quantity = Mathf.Clamp(quantity, 1, 20);
            if (binding.effect == GiftEffect.Grow || binding.effect == GiftEffect.Shrink)
            {
                if (growth == null) return false;
                float scale = binding.effect == GiftEffect.Grow ? Mathf.Clamp(binding.scale, 1f, 2f)
                    : Mathf.Clamp(binding.scale, 0.5f, 1f);
                float remaining = Mathf.Approximately(growth.GiftScale, scale) ? Mathf.Max(0, scaleUntil - Time.time) : 0;
                growth.GiftScale = scale;
                scaleUntil = Time.time + Mathf.Min(30f, remaining + Mathf.Clamp(binding.duration, .1f, 30f) * quantity);
            }
            else
            {
                Vector2 direction = binding.effect == GiftEffect.LaunchLeft ? Vector2.left :
                    binding.effect == GiftEffect.LaunchRight ? Vector2.right :
                    binding.effect == GiftEffect.LaunchDown ? Vector2.down : Vector2.up;
                for (int i = 0; i < quantity && impulses.Count < 40; i++)
                    impulses.Enqueue(direction * Mathf.Clamp(binding.speed, 0, 15));
            }
            LastMessage = viewer + " / " + giftId + " / " + binding.effect + " x" + quantity;
            return true;
        }
        void Update()
        {
            if (character == null || character.Dead) { ClearEffects(); return; }
            if (growth != null && Time.time >= scaleUntil) growth.GiftScale = 1;
        }
        void FixedUpdate()
        {
            if (character == null || character.Dead || impulses.Count == 0 || Time.time < nextImpulse) return;
            var velocity = impulses.Dequeue();
            foreach (var body in bodies)
                if (body != null && body.bodyType == RigidbodyType2D.Dynamic)
                    body.AddForce(velocity * body.mass, ForceMode2D.Impulse);
            nextImpulse = Time.time + .25f;
        }
        public void ClearEffects()
        {
            impulses.Clear(); scaleUntil = 0;
            if (growth != null) growth.GiftScale = 1;
        }
        void OnDisable() { ClearEffects(); if (Instance == this) Instance = null; }
    }
}
