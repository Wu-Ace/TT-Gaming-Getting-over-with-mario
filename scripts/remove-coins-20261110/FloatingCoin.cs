using Character.Lifecycle;
using Extension;
using UnityEngine;

namespace Winning
{
    public class FloatingCoin : MonoBehaviour
    {
        private CoinPurse _coinPurse;
        private Death _characterLifecycle;
        private AudioSource _audioSource;
        private SpriteRenderer _spriteRenderer;
        private bool _pickedUp;

        private void Awake()
        {
            _coinPurse = FindObjectOfType<CoinPurse>();
            _characterLifecycle = FindObjectOfType<Death>();
            _audioSource = GetComponent<AudioSource>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.gameObject.layer != LayerMask.NameToLayer("Character")) return;
            if (_characterLifecycle.Dead) return;
            if (_pickedUp) return;

            _pickedUp = true;

            _coinPurse.Increment();

            GameplayAudio.Play("coin", .08f);
            _spriteRenderer.enabled = false;
            this.Invoke(1f, () => Destroy(gameObject));
        }
    }
}