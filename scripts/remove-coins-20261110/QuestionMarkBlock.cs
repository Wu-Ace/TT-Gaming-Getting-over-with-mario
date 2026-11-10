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
            _characterLifecycle = FindObjectOfType<Death>();
            _animator = GetComponent<Animator>();
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