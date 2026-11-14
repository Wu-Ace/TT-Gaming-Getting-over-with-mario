using UnityEngine;

namespace Character.Control
{
    public class ArmsSimulation : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D hammer;
        [SerializeField] private Transform hammerHead;
        [SerializeField] private Transform center;
        [SerializeField] private bool torqueEnabled = true;
        [field: SerializeField] public int MaxTorque { get; set; } = 1000;
        [SerializeField] private int torqueDampFactor = 1;
        [SerializeField] private ForceMode2D torqueMode = ForceMode2D.Force;
        [SerializeField] private bool forceEnabled = true;
        [SerializeField] private int maxHammerForce = 10;
        [SerializeField] private ForceMode2D hammerForceMode = ForceMode2D.Force;
        [field: SerializeField] public int MaxBodyForce { get; set; } = 50;
        [SerializeField] private ForceMode2D bodyForceMode = ForceMode2D.Force;

        private Rigidbody2D _rigidbody;
        private Vector2 _targetPosition;
        private bool _firstPositionChange;
        private bool _hammerHeadInContact;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            FindObjectOfType<TargetMovement>().OnPositionChanged += newPosition =>
            {
                _targetPosition = newPosition;
                _firstPositionChange = true;
            };
            var hammerHeadCollisionDetection = hammerHead.gameObject.GetComponentInChildren<CollisionDetection>();
            hammerHeadCollisionDetection.OnCollisionEnter += () => _hammerHeadInContact = true;
            hammerHeadCollisionDetection.OnCollisionExit += () => _hammerHeadInContact = false;
        }

        private void FixedUpdate()
        {
            if (!_firstPositionChange) return;

            var cauldronPosition = (Vector2) center.position;
            var hammerHeadPosition = (Vector2) hammerHead.position;
            var centerToHammerHead = cauldronPosition - hammerHeadPosition;
            var centerToTarget = cauldronPosition - _targetPosition;

            var angleDifference = Vector2.SignedAngle(centerToHammerHead, centerToTarget);
            if (Mathf.Abs(angleDifference) > 0.01 && torqueEnabled)
            {
                var torque = angleDifference * MaxTorque / 180f - _rigidbody.angularVelocity * torqueDampFactor;
                _rigidbody.AddTorque(torque, torqueMode);
            }

            var radialDifference = centerToHammerHead.magnitude - centerToTarget.magnitude;
            if (Mathf.Abs(radialDifference) > 0.01 && forceEnabled)
            {
                var direction = centerToHammerHead.normalized;
                if (_hammerHeadInContact)
                {
                    var bodyForce = -direction * (Mathf.Pow(radialDifference, 3) * MaxBodyForce);
                    _rigidbody.AddForce(bodyForce * Time.fixedDeltaTime, bodyForceMode);
                }
                else
                {
                    var hammerForce = direction * (Mathf.Pow(radialDifference, 3) * maxHammerForce) - hammer.velocity;
                    hammer.AddForce(hammerForce * Time.fixedDeltaTime, hammerForceMode);
                }
            }
        }
    }
}