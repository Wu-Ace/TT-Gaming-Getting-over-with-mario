using Character.Lifecycle;
using UnityEngine;

namespace Block
{
    public abstract class QuestionMarkBlock : MonoBehaviour
    {
        [SerializeField] private GameObject inside;

        private Death _characterLifecycle;
        private Animator _animator;
        private static readonly int Touched = Animator.StringToHash("touched");
        private bool _touched;

        private void Awake()
        {
            OnAwake();
        }

        protected void OnAwake()
        {
            _touched = true;
            // Legacy question-block placements are now ordinary solid bricks.
            var animator = GetComponent<Animator>();
            if (animator != null) animator.enabled = false;
            if (inside != null) inside.SetActive(false);
            if (this is CoinBlock)
            {
                var prefab = Resources.Load<GameObject>("BlockPickups/Coin");
                if (prefab != null)
                {
                    var coin = Instantiate(prefab);
                    var bounds = GetComponent<Collider2D>().bounds;
                    coin.transform.position = new Vector3(bounds.center.x, bounds.max.y + 0.12f, transform.position.z);
                    var cr = coin.GetComponentInChildren<SpriteRenderer>();
                    if (cr != null) coin.transform.position += Vector3.up * cr.bounds.extents.y;
                }
            }
            enabled = false;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (_touched) return;
            if (collision.gameObject.layer != LayerMask.NameToLayer("Character")) return;
            if (_characterLifecycle.Dead) return;

            OnTouched();

            _animator.SetTrigger(Touched);
            inside.SetActive(true);

            _touched = true;
        }

        protected abstract void OnTouched();
    }
}
