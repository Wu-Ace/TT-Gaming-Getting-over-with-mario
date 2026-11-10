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

            float speed = collision.relativeVelocity.magnitude;
            if (speed < .65f) return;
            float pitch = Random.Range(Mathf.Clamp(_currentMinPitch,.75f,1.1f), Mathf.Clamp(_currentMaxPitch,1.1f,1.35f));
            GameplayAudio.Play(sfx, Mathf.Clamp01(volume) * Mathf.Lerp(.2f,.8f,Mathf.InverseLerp(.65f,8f,speed)),pitch);

            SetRecentlyPlayed();
        }

        private void SetRecentlyPlayed()
        {
            _recentlyPlayed = true;
            this.Invoke(Mathf.Clamp(interval,.1f,.18f), () => _recentlyPlayed = false);
        }
    }
}