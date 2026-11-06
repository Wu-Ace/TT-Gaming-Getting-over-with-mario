using System.Linq;
using UnityEngine;

namespace Enemy
{
    public class EnemyMovement : MonoBehaviour
    {
        [SerializeField] private Transform leftLimit;
        [SerializeField] private Transform rightLimit;
        [SerializeField] private bool limitsEnabled = true;
        [field: SerializeField] public float Speed { get; set; } = 1f;

        private Rigidbody2D _rigidbody;
        private Vector2 _direction;
        private Animator _animator;
        private static readonly int AnimationDirection = Animator.StringToHash("direction");

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            // Ground enemies must fall when their patrol crosses a platform edge.
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
            // Landing on the floor must not reverse the patrol.
            foreach (var contact in collision.contacts)
            {
                if (Mathf.Abs(contact.normal.x) > 0.5f)
                {
                    ChangeDirection();
                    break;
                }
            }
        }

        private void FixedUpdate()
        {
            if (limitsEnabled && leftLimit != null && transform.position.x < leftLimit.position.x && _direction == Vector2.left)
            {
                ChangeDirection();
            }

            if (limitsEnabled && rightLimit != null && transform.position.x > rightLimit.position.x && _direction == Vector2.right)
            {
                ChangeDirection();
            }

            _rigidbody.velocity = new Vector2(Speed * _direction.x, _rigidbody.velocity.y);
        }

        private void ChangeDirection()
        {
            _direction = _direction == Vector2.left ? Vector2.right : Vector2.left;
            UpdateAnimationDirection();
        }

        private void UpdateAnimationDirection()
        {
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