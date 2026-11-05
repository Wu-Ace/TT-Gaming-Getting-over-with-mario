using System;
using System.Linq;
using UnityEngine;

namespace Character.Lifecycle
{
    public class EnemyContactDetection : MonoBehaviour
    {
        [SerializeField] private bool weakspot = true;

        public event Action OnEnemyDeathContactDetected;
        public event Action<GameObject> OnEnemyTopContactDetected;

        private Death _lifecycle;

        private void Awake()
        {
            _lifecycle = FindObjectOfType<Death>();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (_lifecycle.Dead) return;

            var collisionObject = collision.gameObject;
            if (collisionObject.CompareTag("Enemy"))
            {
                const int angleTolerance = 20;
                var collidingFromAbove =
                    collision
                        .contacts.ToList()
                        .Select(contactPoint => Vector2.Angle(contactPoint.normal, Vector2.up))
                        .Any(angle => angle < angleTolerance);
                if (collidingFromAbove)
                {
                    OnEnemyTopContactDetected?.Invoke(collisionObject);
                }
                else if (weakspot)
                {
                    OnEnemyDeathContactDetected?.Invoke();
                }
            }
        }
    }
}