using TMPro;
using UnityEngine;
namespace Ui
{
    public class CoinPurseUi : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private TextMeshProUGUI totalLabel;
        private void Awake() { gameObject.SetActive(false); }
    }
}
