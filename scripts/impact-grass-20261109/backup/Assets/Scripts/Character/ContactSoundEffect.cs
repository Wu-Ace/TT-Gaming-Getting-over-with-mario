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

            // Tangential scraping must not sound like a direct impact.
            float speed = 0f;
            foreach (var contact in collision.contacts)
                speed = Mathf.Max(speed, Mathf.Abs(Vector2.Dot(collision.relativeVelocity, contact.normal)));
            bool body = GetComponent<Death>() != null;
            float threshold = body ? 2.8f : 1.1f;
            if (speed < threshold) return;
            float gain = Mathf.Lerp(body ? .025f : .07f, body ? .13f : .32f,
                Mathf.InverseLerp(threshold, body ? 12f : 9f, speed));
            GameplayAudio.Play(body ? "body_hit" : "hammer_hit", gain, 1f);

            SetRecentlyPlayed();
        }

        private void SetRecentlyPlayed()
        {
            _recentlyPlayed = true;
            this.Invoke(GetComponent<Death>() != null ? .45f : .16f, () => _recentlyPlayed = false);
        }
    }
}