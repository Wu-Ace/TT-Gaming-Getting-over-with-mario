using Character.Lifecycle;
using UnityEngine;

namespace Character
{
    // Collision approach speed, rather than sliding speed, controls impact feedback.
    public class ContactSoundEffect : MonoBehaviour
    {
        public const float HammerThreshold = 2.2f;
        public const float BodyThreshold = 2.8f;
        Death lifecycle;
        bool body;
        float nextImpact;
        void Awake()
        {
            lifecycle = FindObjectOfType<Death>();
            body = GetComponent<Death>() != null;
            nextImpact = Time.unscaledTime + .25f;
        }
        public static float ImpactGain(bool body, float normalSpeed)
        {
            float threshold = body ? BodyThreshold : HammerThreshold;
            if(float.IsNaN(normalSpeed) || float.IsInfinity(normalSpeed) || normalSpeed < threshold) return 0;
            return Mathf.Lerp(body ? .025f : .07f, body ? .13f : .32f,
                Mathf.InverseLerp(threshold, body ? 12f : 9f, normalSpeed));
        }
        public bool TryPlayImpact(float normalSpeed)
        {
            if((lifecycle != null && lifecycle.Dead) || Time.unscaledTime < nextImpact) return false;
            float gain = ImpactGain(body, normalSpeed);
            if(gain <= 0) return false;
            GameplayAudio.Play(body ? "body_hit" : "hammer_hit", gain);
            nextImpact = Time.unscaledTime + (body ? .45f : .16f);
            return true;
        }
        void OnCollisionEnter2D(Collision2D collision)
        {
            if(collision.gameObject.layer != LayerMask.NameToLayer("Collider")) return;
            float speed = 0;
            foreach(var contact in collision.contacts)
                speed = Mathf.Max(speed, Mathf.Abs(Vector2.Dot(collision.relativeVelocity, contact.normal)));
            TryPlayImpact(speed);
        }
    }
}
