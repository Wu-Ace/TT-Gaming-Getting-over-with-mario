using Character.Lifecycle;
using Character.PowerUp;
using Extension;
using UnityEngine;

namespace Character
{
    public class ContactSoundEffect : MonoBehaviour
    {
        [SerializeField] private AudioClip sfx;
        [SerializeField] private float minPitch = 1f;
        [SerializeField] private float maxPitch = 1f;
        [SerializeField] private float interval = 1f;
        [SerializeField] private float volume = 1f;
        [SerializeField] private bool changesWithMushroomEffect;
        [SerializeField] private float minPitchWithMushroomEffect = 0.5f;
        [SerializeField] private float maxPitchWithMushroomEffect = 0.5f;

        private AudioSource _audioSource;
        private bool _recentlyPlayed;
        private Death _lifecycle;
        private float _currentMinPitch;
        private float _currentMaxPitch;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _lifecycle = FindObjectOfType<Death>();

            _currentMinPitch = minPitch;
            _currentMaxPitch = maxPitch;

            FindObjectOfType<TemporaryGrowth>().OnMushroomEffectStarted += () =>
            {
                if (changesWithMushroomEffect)
                {
                    _currentMinPitch = minPitchWithMushroomEffect;
                    _currentMaxPitch = maxPitchWithMushroomEffect;
                }
            };
            FindObjectOfType<TemporaryGrowth>().OnMushroomEffectEnded += () =>
            {
                if (changesWithMushroomEffect)
                {
                    _currentMinPitch = minPitch;
                    _currentMaxPitch = maxPitch;
                }
            };
        }

        private void Start()
        {
            SetRecentlyPlayed();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (_lifecycle.Dead) return;
            if (_recentlyPlayed) return;
            if (collision.gameObject.layer != LayerMask.NameToLayer("Collider")) return;

            _audioSource.clip = sfx;
            _audioSource.pitch = Random.Range(_currentMinPitch, _currentMaxPitch);
            _audioSource.volume = volume;
            _audioSource.Play();

            SetRecentlyPlayed();
        }

        private void SetRecentlyPlayed()
        {
            _recentlyPlayed = true;
            this.Invoke(interval, () => _recentlyPlayed = false);
        }
    }
}