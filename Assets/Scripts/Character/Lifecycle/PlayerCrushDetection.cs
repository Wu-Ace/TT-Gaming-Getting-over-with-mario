using System.Collections.Generic;
using UnityEngine;

namespace Character.Lifecycle
{
    // Only the cauldron rigidbody's solid colliders count, never the paddle or triggers.
    [DefaultExecutionOrder(250)]
    public sealed class PlayerCrushDetection : MonoBehaviour
    {
        public float contactTolerance = .02f;
        public float minimumClosingSpeed = .05f;
        Rigidbody2D body;
        Death death;
        readonly List<Contact> contacts = new List<Contact>();
        readonly Dictionary<Collider2D, Motion> motion = new Dictionary<Collider2D, Motion>();
        struct Contact { public Vector2 normal, velocity; }
        sealed class Motion
        {
            public Vector2 position, velocity;
            public float angle, angularSpeed, fixedTime;
        }

        Vector2 ObstacleVelocity(Collider2D collider, Vector2 point)
        {
            Vector2 position = collider.transform.position;
            float angle = collider.transform.eulerAngles.z;
            if (!motion.TryGetValue(collider, out var sample))
            {
                sample = new Motion { position = position, angle = angle, fixedTime = Time.fixedTime };
                motion.Add(collider, sample);
            }
            Vector2 delta = position - sample.position;
            float turn = Mathf.DeltaAngle(sample.angle, angle);
            if (delta.sqrMagnitude > .00000001f || Mathf.Abs(turn) > .0001f)
            {
                sample.velocity = delta / Time.fixedDeltaTime;
                sample.angularSpeed = turn * Mathf.Deg2Rad / Time.fixedDeltaTime;
                sample.position = position;
                sample.angle = angle;
                sample.fixedTime = Time.fixedTime;
            }
            else if (sample.fixedTime != Time.fixedTime)
            {
                sample.velocity = Vector2.zero;
                sample.angularSpeed = 0;
                sample.fixedTime = Time.fixedTime;
            }
            var rb = collider.attachedRigidbody;
            var reported = rb != null ? rb.GetPointVelocity(point) : Vector2.zero;
            // MovePosition may report zero velocity after the solver; use observed motion then.
            if (reported.sqrMagnitude > .00000001f) return reported;
            Vector2 radius = point - position;
            return sample.velocity + new Vector2(-radius.y, radius.x) * sample.angularSpeed;
        }

        public bool IsCrushed()
        {
            if (body == null) body = GetComponent<Rigidbody2D>();
            if (body == null || !body.simulated) return false;
            contacts.Clear();
            var rig = transform.parent != null ? transform.parent : transform;
            foreach (var c in body.GetComponentsInChildren<Collider2D>())
            {
                if (!c.enabled || !c.gameObject.activeInHierarchy || c.isTrigger || c.attachedRigidbody != body) continue;
                var bounds = c.bounds;
                foreach (var other in Physics2D.OverlapBoxAll(bounds.center, bounds.size + Vector3.one * (contactTolerance * 2), 0))
                {
                    if (other.isTrigger || other.transform.IsChildOf(rig) ||
                        Physics2D.GetIgnoreLayerCollision(c.gameObject.layer, other.gameObject.layer) ||
                        Physics2D.GetIgnoreCollision(c, other)) continue;
                    var distance = c.Distance(other);
                    if (!distance.isValid) continue;
                    // A single impact may briefly penetrate the floor. That is not crushing.
                    if (distance.distance > contactTolerance) continue;
                    var normal = distance.pointA - distance.pointB;
                    if (distance.distance < 0) normal = -normal;
                    if (normal.sqrMagnitude < .00000001f) normal = body.worldCenterOfMass - (Vector2)other.bounds.center;
                    normal.Normalize();
                    var velocity = ObstacleVelocity(other, distance.pointB);
                    contacts.Add(new Contact { normal = normal, velocity = velocity });
                }
            }
            // Catch a closing squeeze even when the solver prevents a large penetration.
            for (int i = 0; i < contacts.Count; i++)
                for (int j = i + 1; j < contacts.Count; j++)
                {
                    if (Vector2.Dot(contacts[i].normal, contacts[j].normal) >= -.65f) continue;
                    // Measure the obstacles closing their gap, not the player's falling speed.
                    // Equal platform velocities cancel, even on angled contact surfaces.
                    var axis = (contacts[i].normal - contacts[j].normal).normalized;
                    if (Vector2.Dot(contacts[i].velocity - contacts[j].velocity, axis) > minimumClosingSpeed) return true;
                }
            return false;
        }

        void Check()
        {
            if (death == null) death = GetComponent<Death>();
            if (death == null || death.Dead || (CampaignLevel.Current != null && CampaignLevel.Current.Won)) return;
            if (IsCrushed()) death.Kill();
        }
        void FixedUpdate() { Check(); }
        void OnCollisionEnter2D(Collision2D collision) { Check(); }
        void OnCollisionStay2D(Collision2D collision) { Check(); }
    }
}
