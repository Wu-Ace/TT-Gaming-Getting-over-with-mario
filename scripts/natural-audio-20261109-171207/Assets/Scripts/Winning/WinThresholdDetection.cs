using System;
using Character.Control;
using Character.Lifecycle;
using UnityEngine;

namespace Winning
{
    public class WinThresholdDetection : MonoBehaviour
    {
        [SerializeField] private Animator flagAnimation;

        public event Action OnGameWon;

        private ArmsSimulation _armsSimulation;
        private bool _won;
        private Death _characterLifecycle;
        private AudioSource _audioSource;
        private static readonly int Touched = Animator.StringToHash("Touched");

        private void Awake()
        {
            _armsSimulation = FindObjectOfType<ArmsSimulation>();
            _characterLifecycle = FindObjectOfType<Death>();
            _audioSource = GetComponent<AudioSource>();
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.gameObject.layer != LayerMask.NameToLayer("Character")) return;
            if (_characterLifecycle.Dead) return;
            if (_won) return;

            if (CampaignLevel.Current != null) return; // Campaign exits are owned by the three-star door.
            TryWin();
        }

        public void TryWin()
        {
            if (_won || _characterLifecycle.Dead) return;
            if (CampaignLevel.Current != null && CampaignLevel.Current.StarCount != 3) return;
            _won = true;

            OnGameWon?.Invoke();
            if (_audioSource != null) _audioSource.Play();
            if (flagAnimation != null) flagAnimation.SetTrigger(Touched);

            _armsSimulation.enabled = false;
            enabled = false;
        }
    }
}