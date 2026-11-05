using System;
using UnityEngine;

namespace Enemy
{
    public class KoopaTroopaShellContactDetection : MonoBehaviour
    {
        public event Action<GameObject> OnShellContactDetected;

        private void OnCollisionEnter2D(Collision2D collision)
        {
            var collisionObject = collision.gameObject;
            if (collisionObject.CompareTag("Enemy"))
            {
                OnShellContactDetected?.Invoke(collisionObject);
            }
        }
    }
}