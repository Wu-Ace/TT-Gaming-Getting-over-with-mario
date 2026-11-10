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
            var brick = Resources.Load<GameObject>("PlainBrick");
            var source = brick != null ? brick.GetComponentInChildren<SpriteRenderer>() : null;
            var renderer = GetComponent<SpriteRenderer>();
            if (source != null && renderer != null)
            {
                var width = renderer.bounds.size.x;
                renderer.sprite = source.sprite;
                renderer.drawMode = SpriteDrawMode.Simple;
                // Scale only the visual child; keep the original collider untouched.
                var visual = new GameObject("Plain brick visual");
                visual.transform.SetParent(transform, false);
                var copy = visual.AddComponent<SpriteRenderer>();
                copy.sprite = source.sprite;
                copy.sortingLayerID = renderer.sortingLayerID;
                copy.sortingOrder = renderer.sortingOrder;
                var k = width / (source.sprite.bounds.size.x * Mathf.Abs(transform.lossyScale.x));
                visual.transform.localScale = Vector3.one * k;
                renderer.enabled = false;
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