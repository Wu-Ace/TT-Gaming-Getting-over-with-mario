using System;
using Character.Control;
using Character.PowerUp;
using Extension;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

namespace Character.Lifecycle
{
    public class Death : MonoBehaviour
    {
        [SerializeField] private float animationForce = 10f;
        [SerializeField] private float animationMaxAngle = 10f;
        [SerializeField] private float animationTorque = 10f;
        [SerializeField] private AudioClip sfx;
        [SerializeField] private float sfxVolume = 1f;
        [SerializeField] private float sfxPitch = 1f;

        public bool Dead { get; private set; }
        public event Action OnCharacterDeath;

        private Rigidbody2D _rigidbody;
        private ArmsSimulation _armsSimulation;
        private AudioSource _audioSource;
        private bool _heavy;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            if (GetComponent<PlayerUnstuck>() == null) gameObject.AddComponent<PlayerUnstuck>();
            _armsSimulation = transform.parent.GetComponentInChildren<ArmsSimulation>();
            _audioSource = GetComponent<AudioSource>();

            GetComponent<FallDetection>().OnFallDetected += () => Die(RigidbodyConstraints2D.FreezeAll);
            GetComponent<EnemyContactDetection>().OnEnemyDeathContactDetected += () => Die(RigidbodyConstraints2D.None);

            var temporaryGrowth = FindObjectOfType<TemporaryGrowth>();
            temporaryGrowth.OnMushroomEffectStarted += () => _heavy = true;
            temporaryGrowth.OnMushroomEffectEnded += () => _heavy = false;
        }

        public void Kill() { Die(RigidbodyConstraints2D.None); }

        private void Die(RigidbodyConstraints2D rigidbodyConstraints)
        {
            if (Dead) return;
            Dead = true;

            OnCharacterDeath?.Invoke();

            _rigidbody.constraints = rigidbodyConstraints;
            _armsSimulation.enabled = false;

            var direction = Quaternion.Euler(0, 0, Random.Range(-1f, 1f) * animationMaxAngle) * Vector3.up;
            _rigidbody.AddForce(direction * (animationForce * (_heavy ? 2 : 1)), ForceMode2D.Impulse);
            _rigidbody.AddTorque(animationTorque * (_heavy ? 2 : 1), ForceMode2D.Impulse);

            PlaySoundEffect();

            this.Invoke(3f, () => SceneManager.LoadScene(SceneManager.GetActiveScene().name));
        }

        private void PlaySoundEffect()
        {
            _audioSource.clip = sfx;
            _audioSource.pitch = sfxPitch;
            _audioSource.volume = sfxVolume;
            GameplayAudio.Play(sfx, sfxVolume, sfxPitch);
        }
    }
}