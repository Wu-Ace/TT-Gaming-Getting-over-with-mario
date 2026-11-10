using System.Linq;
using Block;
using Character.Control;
using Extension;
using UnityEngine;
using UnityEngine.UI;

namespace Character.PowerUp
{
    public class TemporaryStrength : MonoBehaviour
    {
        [SerializeField] private int forceMultiplier = 2;
        [SerializeField] private int torqueMultiplier = 2;
        [SerializeField] private float effectDuration = 10f;
        [SerializeField] private float transitionDuration = 1f;
        [SerializeField] private AudioClip startSfx;
        [SerializeField] private AudioClip endSfx;
        [SerializeField] private Color color;
        [SerializeField] private SpriteRenderer hammer;
        [SerializeField] private SpriteRenderer hammerHandleExtension;
        [SerializeField] private Slider timeLeftUi;

        private ArmsSimulation _armsSimulation;
        private int _baseForce;
        private int _baseTorque;
        private AudioSource _audioSource;
        private Coroutine _delayedWeakness;
        private Color _baseHammerColor;
        private Color _baseHammerHandleExtensionColor;
        private bool _gettingStrong;
        private bool _gettingWeak;
        private float _transitionTimeElapsed;
        private float _timeLeft;
        private int _lastWarning;

        private void Awake()
        {
            _armsSimulation = FindObjectOfType<ArmsSimulation>();
            _baseForce = _armsSimulation.MaxBodyForce;
            _baseTorque = _armsSimulation.MaxTorque;

            _baseHammerColor = hammer.color;
            _baseHammerHandleExtensionColor = hammerHandleExtension.color;

            _audioSource = GetComponent<AudioSource>();

            FindObjectsOfType<StarBlock>().ToList().ForEach(block => block.OnStarPickUp += GetStrong);
        }

        private void GetStrong()
        {
            _armsSimulation.MaxBodyForce = _baseForce * forceMultiplier;
            _armsSimulation.MaxTorque = _baseTorque * torqueMultiplier;

            _transitionTimeElapsed = 0;
            _gettingStrong = true;

            GameplayAudio.Play("strength_start");

            if (_delayedWeakness != null)
            {
                this.CancelInvoke(_delayedWeakness);
            }

            _timeLeft = effectDuration;
            _delayedWeakness = this.Invoke(effectDuration, () =>
            {
                _armsSimulation.MaxBodyForce = _baseForce;
                _armsSimulation.MaxTorque = _baseTorque;

                _gettingWeak = true;

                GameplayAudio.Play("strength_end");
            });
        }

        private void Update()
        {
            if (_gettingStrong)
            {
                _transitionTimeElapsed += Time.deltaTime;
                hammer.color = Color.Lerp(_baseHammerColor, color,
                    _transitionTimeElapsed / transitionDuration);
                hammerHandleExtension.color = Color.Lerp(_baseHammerHandleExtensionColor, color,
                    _transitionTimeElapsed / transitionDuration);
                if (hammer.color == color || hammerHandleExtension.color == color)
                {
                    hammer.color = color;
                    hammerHandleExtension.color = color;
                    _gettingStrong = false;
                }
            }
            else if (_gettingWeak)
            {
                _transitionTimeElapsed += Time.deltaTime;
                hammer.color = Color.Lerp(color, _baseHammerColor,
                    _transitionTimeElapsed / transitionDuration);
                hammerHandleExtension.color = Color.Lerp(color, _baseHammerHandleExtensionColor,
                    _transitionTimeElapsed / transitionDuration);
                if (hammer.color == _baseHammerColor || hammerHandleExtension.color == _baseHammerHandleExtensionColor)
                {
                    hammer.color = _baseHammerColor;
                    hammerHandleExtension.color = _baseHammerHandleExtensionColor;
                    _gettingWeak = false;
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
        }
    }
}