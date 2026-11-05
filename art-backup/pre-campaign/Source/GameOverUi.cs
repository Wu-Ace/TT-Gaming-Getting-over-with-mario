using Extension;
using UnityEngine;
using Winning;

namespace Ui
{
    public class GameOverUi : MonoBehaviour
    {
        [SerializeField] private float delay = 3f;

        private static readonly int GameOver = Animator.StringToHash("GameOver");
        private Animator _animator;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            FindObjectOfType<WinThresholdDetection>().OnGameWon += () =>
            {
                this.Invoke(delay, () => _animator.SetTrigger(GameOver));
            };
        }
    }
}