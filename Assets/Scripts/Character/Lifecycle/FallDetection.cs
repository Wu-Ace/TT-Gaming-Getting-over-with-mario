using System;
using UnityEngine;

namespace Character.Lifecycle
{
    public class FallDetection : MonoBehaviour
    {
        [SerializeField] private Transform deathThreshold;

        public event Action OnFallDetected;
        private bool _detected;
        private FirstLevelWater water;

        private void Update()
        {
            if (!water) water = FindObjectOfType<FirstLevelWater>();
            // Let the body cross the visible surface before drowning.
            float threshold = water && water.gameObject.scene == gameObject.scene
                ? water.waterHeight - 0.2f
                : deathThreshold ? deathThreshold.position.y : float.NegativeInfinity;
            if (!_detected && transform.position.y < threshold)
            {
                OnFallDetected?.Invoke();
                _detected = true;
            }
        }
    }
}
