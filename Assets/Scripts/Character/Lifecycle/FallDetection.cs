using System;
using UnityEngine;

namespace Character.Lifecycle
{
    public class FallDetection : MonoBehaviour
    {
        [SerializeField] private Transform deathThreshold;

        public event Action OnFallDetected;
        private bool _detected;

        private void Update()
        {
            if (!_detected && transform.position.y < deathThreshold.position.y)
            {
                OnFallDetected?.Invoke();
                _detected = true;
            }
        }
    }
}