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
            _animator = GetComponentInChildren<Animator>();
        }

        private void Start()
        {
            _direction = Random.value < 0.5 ? Vector2.right : Vector2.left;
            UpdateAnimationDirection();
        }

        private void OnCollisionEnter2D(Collision2D _)
        {
            ChangeDirection();
        }

        private void FixedUpdate()
        {
            if (limitsEnabled && transform.position.x < leftLimit.position.x && _direction == Vector2.left)
            {
                ChangeDirection();
            }

            if (limitsEnabled && transform.position.x > rightLimit.position.x && _direction == Vector2.right)
            {
                ChangeDirection();
            }

            _rigidbody.MovePosition(_rigidbody.position + Speed * Time.fixedDeltaTime * _direction);
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