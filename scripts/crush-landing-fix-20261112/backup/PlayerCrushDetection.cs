using System.Collections.Generic;
using UnityEngine;

namespace Character.Lifecycle
{
    // Only the cauldron rigidbody's solid colliders count, never the paddle or triggers.
    [DefaultExecutionOrder(250)]
    public sealed class PlayerCrushDetection : MonoBehaviour
    {
        public float penetrationTolerance = .06f;
        public float contactTolerance = .02f;
        public float minimumClosingSpeed = .05f;
        Rigidbody2D body;
        Death death;
        readonly List<Contact> contacts = new List<Contact>();
        struct Contact { public Vector2 normal; public float closing; }

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
                    // A genuine deep embedding now kills immediately instead of rewinding.
                    if (distance.distance < -penetrationTolerance) return true;
                    if (distance.distance > contactTolerance) continue;
                    var normal = distance.pointA - distance.pointB;
                    if (distance.distance < 0) normal = -normal;
                    if (normal.sqrMagnitude < .00000001f) normal = body.worldCenterOfMass - (Vector2)other.bounds.center;
                    normal.Normalize();
                    var otherBody = other.attachedRigidbody;
                    var velocity = otherBody != null ? otherBody.GetPointVelocity(distance.pointB) : Vector2.zero;
                    contacts.Add(new Contact { normal = normal, closing = Vector2.Dot(velocity - body.GetPointVelocity(distance.pointA), normal) });
                }
            }
            // Catch a closing squeeze even when the solver prevents a large penetration.
            for (int i = 0; i < contacts.Count; i++)
                for (int j = i + 1; j < contacts.Count; j++)
                    if (Vector2.Dot(contacts[i].normal, contacts[j].normal) < -.65f &&
                        contacts[i].closing + contacts[j].closing > minimumClosingSpeed) return true;
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
