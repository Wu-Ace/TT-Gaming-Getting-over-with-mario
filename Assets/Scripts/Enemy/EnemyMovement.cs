using System.Linq;
using UnityEngine;

namespace Enemy
{
    public class EnemyMovement : MonoBehaviour
    {
        [SerializeField] private Transform leftLimit;
        [SerializeField] private Transform rightLimit;
        [SerializeField] private bool limitsEnabled; // Legacy serialized field retained for existing editor builders.
        [field: SerializeField] public float Speed { get; set; } = 1f;

        private Rigidbody2D _rigidbody;
        private Vector2 _direction;
        private Animator _animator;
        private Collider2D _collider;
        private static readonly int AnimationDirection = Animator.StringToHash("direction");

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _collider = GetComponentsInChildren<Collider2D>().FirstOrDefault(c => !c.isTrigger && c.attachedRigidbody == _rigidbody);
            // Keep gravity so patrol resumes naturally after landing.
            _rigidbody.constraints &= ~RigidbodyConstraints2D.FreezePositionY;
            if (_rigidbody.gravityScale <= 0f) _rigidbody.gravityScale = 1f;
            _animator = GetComponentInChildren<Animator>();
        }

        private void Start()
        {
            _direction = Random.value < 0.5 ? Vector2.right : Vector2.left;
            UpdateAnimationDirection();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!Obstacle(collision.collider)) return;
            // Only contacts facing our movement reverse us; floors and rear contacts do not.
            foreach (var contact in collision.contacts)
            {
                if (contact.normal.x * _direction.x < -0.5f)
                {
                    ChangeDirection();
                    break;
                }
            }
        }

        private void FixedUpdate()
        {
            if (_collider != null)
            {
                var bounds = _collider.bounds;
                float step = Mathf.Abs(Speed) * Time.fixedDeltaTime + .04f;
                float front = bounds.center.x + _direction.x * (bounds.extents.x + step);
                bool grounded = Supported(new Vector2(bounds.center.x, bounds.min.y + .05f));
                bool wall = Physics2D.OverlapBoxAll(
                    new Vector2(front, bounds.center.y + .04f),
                    new Vector2(.04f, Mathf.Max(.02f, bounds.size.y - .16f)), 0f).Any(Obstacle);
                bool edge = grounded && _rigidbody.velocity.y <= .1f &&
                    !Supported(new Vector2(front, bounds.min.y + .05f));
                if (wall || edge) ChangeDirection();
            }

            _rigidbody.velocity = new Vector2(Speed * _direction.x, _rigidbody.velocity.y);
        }

        private bool Obstacle(Collider2D other)
        {
            if (other == null || other == _collider || other.isTrigger ||
                other.attachedRigidbody == _rigidbody) return false;
            // Ignore the player's body, but keep the separately simulated hammer solid.
            if (CampaignInteractable.Body(other) != null) return false;
            return !Physics2D.GetIgnoreLayerCollision(_collider.gameObject.layer, other.gameObject.layer) &&
                !Physics2D.GetIgnoreCollision(_collider, other);
        }

        private bool Supported(Vector2 point)
        {
            return Physics2D.RaycastAll(point, Vector2.down, .16f)
                .Any(hit => hit.normal.y > .5f && Obstacle(hit.collider));
        }

        private void ChangeDirection()
        {
            _direction = _direction == Vector2.left ? Vector2.right : Vector2.left;
            UpdateAnimationDirection();
        }

        private void UpdateAnimationDirection()
        {
            if (_animator == null) return;
            var animationHasDirection =
                _animator
                    .parameters
                    .Select(parameter => parameter.nameHash)
                    .Contains(AnimationDirection);
            if (!animationHasDirection) return;

            _animator.SetFloat(AnimationDirection, _direction.x);
        }
    }
}
