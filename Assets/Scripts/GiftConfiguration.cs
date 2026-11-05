using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LiveGifts
{
    public enum GiftEffect { Grow, Shrink, LaunchUp, LaunchLeft, LaunchRight, LaunchDown }
    [Serializable] public class GiftBinding
    {
        public string giftId;
        public GiftEffect effect;
        [Range(0.5f, 2f)] public float scale = 1.5f;
        [Range(0.1f, 30f)] public float duration = 10f;
        [Range(0f, 15f)] public float speed = 5f;
        public Key testKey;
    }
    [CreateAssetMenu(menuName = "Live Gifts/Configuration")]
    public class GiftConfiguration : ScriptableObject
    {
        public GiftBinding[] gifts = Defaults();
        public static GiftBinding[] Defaults()
        {
            var result = new GiftBinding[20];
            var keys = new[] { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6,
                Key.Digit7, Key.Digit8, Key.Digit9, Key.Digit0, Key.F1, Key.F2, Key.F3, Key.F4,
                Key.F5, Key.F6, Key.F7, Key.F8, Key.F9, Key.F10 };
            for (int i = 0; i < result.Length; i++) result[i] = new GiftBinding {
                giftId = "test-" + (i + 1), effect = (GiftEffect)(i % 6),
                scale = i % 6 == 1 ? 0.65f : 1.5f, testKey = keys[i] };
            return result;
        }
    }
}
