using System;
using System.Linq;
using Block;
using Extension;
using UnityEngine;
using UnityEngine.UI;

namespace Character.PowerUp
{
    public class TemporaryGrowth : MonoBehaviour
    {
        [SerializeField] private float scaleMultiplier = 1.5f;
        [SerializeField] private float effectDuration = 10f;
        [SerializeField] private float transitionDuration = 1f;
        [SerializeField] private AudioClip startSfx;
        [SerializeField] private AudioClip endSfx;
        [SerializeField] private Transform cauldron;
        [SerializeField] private Transform hammer;
        [SerializeField] private float mass = 30f;
        [SerializeField] private Rigidbody2D bodyChangingMass;
        [SerializeField] private Slider timeLeftUi;

        public event Action OnMushroomEffectStarted;
        public event Action OnMushroomEffectEnded;

        private Vector3 _cauldronBaseScale;
        private Vector3 _cauldronTargetScale;
        private Vector3 _hammerBaseScale;
        private Vector3 _hammerTargetScale;
        private float _transitionTimeElapsed;
        private bool _growing;
        private bool _shrinking;
        private AudioSource _audioSource;
        private Coroutine _delayedShrinking;
        private float _baseMass;
        private float _timeLeft;
        private int _lastWarning;
        public float GiftScale { get; set; } = 1f;
        private float _appliedGiftScale = 1f;

        private void Awake()
        {
            _cauldronBaseScale = cauldron.localScale;
            _cauldronTargetScale = _cauldronBaseScale * scaleMultiplier;
            _hammerBaseScale = hammer.localScale;
            _hammerTargetScale = _hammerBaseScale * scaleMultiplier;

            _baseMass = bodyChangingMass.mass;

            _audioSource = GetComponent<AudioSource>();

            FindObjectsOfType<MushroomBlock>().ToList().ForEach(block => block.OnMushroomPickUp += Grow);
        }

        private void Grow()
        {
            _transitionTimeElapsed = 0;
            bodyChangingMass.mass = mass;
            _growing = true;

            GameplayAudio.Play("growth_start");

            if (_delayedShrinking != null)
            {
                this.CancelInvoke(_delayedShrinking);
            }

            OnMushroomEffectStarted?.Invoke();
        }

        private void Update()
        {
            cauldron.localScale /= _appliedGiftScale;
            hammer.localScale /= _appliedGiftScale;
            if (_growing)
            {
                _transitionTimeElapsed += Time.deltaTime;
                cauldron.localScale =
                    Vector2.Lerp(_cauldronBaseScale, _cauldronTargetScale, _transitionTimeElapsed / transitionDuration);
                hammer.localScale =
                    Vector2.Lerp(_hammerBaseScale, _hammerTargetScale, _transitionTimeElapsed / transitionDuration);
                if (cauldron.localScale.x >= _cauldronTargetScale.x || hammer.localScale.x >= _hammerTargetScale.x)
                {
                    cauldron.localScale = _cauldronTargetScale;
                    hammer.localScale = _hammerTargetScale;
                    _growing = false;
                    _timeLeft = effectDuration;
                    _delayedShrinking = this.Invoke(effectDuration, () =>
                    {
                        _transitionTimeElapsed = 0;
                        bodyChangingMass.mass = _baseMass;
                        _shrinking = true;

                        GameplayAudio.Play("growth_end");

                        OnMushroomEffectEnded?.Invoke();
                    });
                }
            }
            else if (_shrinking)
            {
                _transitionTimeElapsed += Time.deltaTime;
                cauldron.localScale =
                    Vector2.Lerp(_cauldronTargetScale, _cauldronBaseScale, _transitionTimeElapsed / transitionDuration);
                hammer.localScale =
                    Vector2.Lerp(_hammerTargetScale, _hammerBaseScale, _transitionTimeElapsed / transitionDuration);
                if (cauldron.localScale.x <= _cauldronBaseScale.x || hammer.localScale.x <= _hammerBaseScale.x)
                {
                    cauldron.localScale = _cauldronBaseScale;
                    hammer.localScale = _hammerBaseScale;
                    _shrinking = false;
                }
            }

            if (_timeLeft > 0)
            {
                _timeLeft = Mathf.Max(_timeLeft - Time.deltaTime, 0f);
            }
            int warning = Mathf.CeilToInt(_timeLeft);
            if (warning > 0 && warning <= 3 && warning != _lastWarning) GameplayAudio.Play("warning", .3f);
            _lastWarning = warning;
            timeLeftUi.value = _timeLeft / effectDuration;
            _appliedGiftScale = Mathf.Clamp(GiftScale, 0.5f, 2f);
            cauldron.localScale *= _appliedGiftScale;
            hammer.localScale *= _appliedGiftScale;
        }
    }
}