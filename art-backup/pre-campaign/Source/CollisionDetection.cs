using System;
using UnityEngine;

namespace Character.Control
{
    public class CollisionDetection : MonoBehaviour
    {
        public event Action OnCollisionEnter;
        public event Action OnCollisionExit;

        private void OnTriggerEnter2D(Collider2D other)
        {
            OnCollisionEnter?.Invoke();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            OnCollisionExit?.Invoke();
        }
    }
}