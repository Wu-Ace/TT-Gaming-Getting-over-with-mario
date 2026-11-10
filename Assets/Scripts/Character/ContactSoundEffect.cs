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
        Rigidbody2D physicsBody;
        Vector2 beforePhysicsVelocity;
        void Awake()
        {
            lifecycle = GetComponent<Death>();
            if(lifecycle == null) lifecycle = FindObjectOfType<Death>();
            body = GetComponent<Death>() != null;
            physicsBody = GetComponent<Rigidbody2D>();
            nextImpact = Time.unscaledTime + .25f;
        }
        void FixedUpdate()
        {
            if(physicsBody != null) beforePhysicsVelocity = physicsBody.velocity;
        }
        public static float ImpactGain(bool body, float normalSpeed)
        {
            float threshold = body ? BodyThreshold : HammerThreshold;
            if(float.IsNaN(normalSpeed) || float.IsInfinity(normalSpeed) || normalSpeed < threshold) return 0;
            return Mathf.Lerp(body ? .025f : .07f, body ? .13f : .32f,
                Mathf.InverseLerp(threshold, body ? 12f : 9f, normalSpeed));
        }
        public bool TryPlayImpact(float normalSpeed, bool landing = false)
        {
            if((lifecycle != null && lifecycle.Dead) || Time.unscaledTime < nextImpact) return false;
            float gain = ImpactGain(body, normalSpeed);
            if(gain <= 0) return false;
            GameplayAudio.Play(body ? (landing ? "body_land" : "body_hit") : "hammer_hit", gain);
            nextImpact = Time.unscaledTime + (body ? .45f : .16f);
            return true;
        }
        void OnCollisionEnter2D(Collision2D collision)
        {
            // Moving/falling platforms may use Default rather than the terrain layer.
            if(!body && collision.gameObject.layer != LayerMask.NameToLayer("Collider")) return;
            if(collision.transform.root == transform.root) return;
            float speed = 0;
            bool landing = false;
            Vector2 otherVelocity = collision.rigidbody == null ? Vector2.zero : collision.rigidbody.velocity;
            foreach(var contact in collision.contacts)
            {
                speed = Mathf.Max(speed, Mathf.Abs(Vector2.Dot(collision.relativeVelocity, contact.normal)));
                if(body && contact.normal.y > .5f && beforePhysicsVelocity.y < -BodyThreshold)
                {
                    landing = true;
                    // The solver may already have stopped a fast-falling body by callback time.
                    speed = Mathf.Max(speed, Mathf.Max(0,-Vector2.Dot(beforePhysicsVelocity-otherVelocity,contact.normal)));
                }
            }
            // A genuine hard landing must not be swallowed by a recent wall-bump cooldown.
            if(landing && speed >= 8f) nextImpact = Mathf.Min(nextImpact, Time.unscaledTime);
            TryPlayImpact(speed, landing);
        }
    }
}
