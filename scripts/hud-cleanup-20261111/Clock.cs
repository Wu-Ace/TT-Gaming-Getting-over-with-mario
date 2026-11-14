using System;
using System.Diagnostics;
using Character.Lifecycle;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Winning
{
    public class Clock : MonoBehaviour
    {
        [SerializeField] private int initialSecondsPassed;

        public event Action<int> OnSecondPassed;
        public event Action<TimeSpan> OnClockStopped;

        private Stopwatch _stopwatch;
        private int _elapsedSeconds;
        private bool _running;

        private void Awake()
        {
            FindObjectOfType<WinThresholdDetection>().OnGameWon += Stop;
            FindObjectOfType<Death>().OnCharacterDeath += Stop;
        }

        private void Start()
        {
            _stopwatch = new Stopwatch();
            _stopwatch.Start();
            _running = true;
        }

        private void Stop()
        {
            _running = false;
            var time = _stopwatch.Elapsed + new TimeSpan(0, 0, initialSecondsPassed);
            OnClockStopped?.Invoke(time);
            Debug.Log(time.ToString(@"mm\:ss\.fff"));
        }

        private void Update()
        {
            if (!_running) return;
            if (GameMenuUI.IsPaused) { _stopwatch.Stop(); return; }
            if (!_stopwatch.IsRunning) _stopwatch.Start();

            if (_stopwatch.ElapsedMilliseconds / 1000 > _elapsedSeconds)
            {
                _elapsedSeconds++;
                OnSecondPassed?.Invoke(_elapsedSeconds + initialSecondsPassed);
            }
        }
    }
}