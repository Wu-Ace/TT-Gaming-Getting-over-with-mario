using TMPro;
using UnityEngine;
using Winning;

namespace Ui
{
    public class ClockUi : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI minutesUi;
        [SerializeField] private TextMeshProUGUI secondsUi;
        [SerializeField] private TextMeshProUGUI millisecondsUi;

        private void Awake()
        {
            var clock = FindObjectOfType<Clock>();
            clock.OnSecondPassed += elapsedSeconds =>
            {
                minutesUi.text = LeadingZeros(elapsedSeconds / 60, 2);
                secondsUi.text = LeadingZeros(elapsedSeconds % 60, 2);
            };
            clock.OnClockStopped += finalTime => millisecondsUi.text = LeadingZeros(finalTime.Milliseconds, 3);
        }

        private static string LeadingZeros(int number, int amount)
        {
            return number.ToString().PadLeft(amount, '0');
        }
    }
}