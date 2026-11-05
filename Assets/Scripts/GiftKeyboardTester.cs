#if UNITY_EDITOR || GIFT_TESTING
using System;
using UnityEngine;
using UnityEngine.InputSystem;
namespace LiveGifts
{
    public sealed class GiftKeyboardTester : MonoBehaviour
    {
        GiftRuntime runtime;
        bool visible = true;
        void Awake() { runtime = GetComponent<GiftRuntime>(); }
        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.f12Key.wasPressedThisFrame) visible = !visible;
            if (keyboard.backspaceKey.wasPressedThisFrame) runtime.ClearEffects();
            var gifts = runtime.Configuration.gifts;
            if (gifts == null) return;
            for (int i = 0; i < Mathf.Min(20, gifts.Length); i++)
                if (gifts[i] != null && gifts[i].testKey != Key.None && keyboard[gifts[i].testKey].wasPressedThisFrame)
                    runtime.ReceiveGift(Guid.NewGuid().ToString(), gifts[i].giftId, 1, "Keyboard");
        }
        void OnGUI()
        {
            if (!visible || runtime == null) return;
            GUILayout.BeginArea(new Rect(10, 10, 390, Mathf.Min(Screen.height - 20, 650)), GUI.skin.box);
            GUILayout.Label("GIFT TEST / F12: panel / Backspace: clear");
            GUILayout.Label(runtime.LastMessage);
            var gifts = runtime.Configuration.gifts;
            if (gifts != null) for (int i = 0; i < Mathf.Min(20, gifts.Length); i++)
                if (gifts[i] != null) GUILayout.Label(gifts[i].testKey + " : " + gifts[i].giftId + " / " + gifts[i].effect);
            GUILayout.EndArea();
        }
    }
}
#endif
