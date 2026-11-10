using System.Linq;
using Block;
using TMPro;
using UnityEngine;
using Winning;

namespace Ui
{
    public class CoinPurseUi : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private TextMeshProUGUI totalLabel;

        private void Awake()
        {
            FindObjectOfType<CoinPurse>().OnCoinCountChanged +=
                newValue => label.text = newValue > 9 ? $"{newValue}" : $"0{newValue}";

            var floatingCoins = FindObjectsOfType<FloatingCoin>().ToList().Count;
            var coinBlocks = FindObjectsOfType<CoinBlock>().ToList().Count;
            totalLabel.text = $"{floatingCoins + coinBlocks}";
        }
    }
}