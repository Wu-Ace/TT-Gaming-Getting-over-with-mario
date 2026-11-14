using System;
using Extension;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Character.Control
{
    public class TargetMovement : MonoBehaviour
    {
        [SerializeField] private Transform bodyCenter;
        [SerializeField] private float minRadius = 0f;
        [SerializeField] private float maxRadius = 2f;
        [SerializeField] private float reactionTimeMultiplier = 10f;
        [SerializeField] private bool autoAdjustmentEnabled;
        [SerializeField] private Rigidbody2D followedBody;
        [SerializeField] private float adjustmentDurationSeconds = 0.25f;
        [SerializeField] private float adjustmentWaitSeconds = 0.5f;

        private Vector2 _mousePosition;
        private Camera _camera;
        private bool _recentMovement;
        private float _adjustmentTimeSpent;
        private bool _readyToReceiveInput;

        public event Action<Vector2> OnPositionChanged;

        private void Awake()
        {
            _camera = Camera.main;
        }

        private void Start()
        {
            _readyToReceiveInput = true;
        }

        [UsedImplicitly]
        public void OnMousePositionChanged(InputAction.CallbackContext context)
        {
            if (!_readyToReceiveInput) return;

            var input = context.ReadValue<Vector2>();
            if (input == Vector2.zero) return; // to avoid weird behaviour when clicking outside the game window
            _mousePosition = input;

            _recentMovement = true;
            this.Invoke(adjustmentWaitSeconds, () =>
            {
                _recentMovement = false;
                _adjustmentTimeSpent = 0;
            });
        }

        private void Update()
        {
            if (!_readyToReceiveInput) return;

            if (_recentMovement || !autoAdjustmentEnabled)
            {
                var mouseWorldPosition = _camera.ScreenToWorldPoint(_mousePosition);
                var mousePlanePosition = new Vector2(mouseWorldPosition.x, mouseWorldPosition.y);
                var center = (Vector2) bodyCenter.position;
                var difference = mousePlanePosition - center;
                var distanceToCenter = Mathf.Clamp(difference.magnitude, minRadius, maxRadius);
                var targetPosition = center + difference.normalized * distanceToCenter;
                transform.position = Vector2.Lerp(
                    transform.position,
                    targetPosition,
                    Time.deltaTime * reactionTimeMultiplier);
                OnPositionChanged?.Invoke(transform.position);
            }
            else
            {
                _adjustmentTimeSpent += Time.deltaTime;
                transform.position = Vector2.Lerp(
                    transform.position,
                    followedBody.position,
                    _adjustmentTimeSpent / adjustmentDurationSeconds);
            }
        }
    }
}