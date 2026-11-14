using System;
using UnityEngine;
namespace Winning
{
    public class CoinPurse : MonoBehaviour
    {
        private int _count;
        public int Count => CampaignLevel.Current != null ? CampaignLevel.Current.CoinCount : _count;
        public event Action<int> OnCoinCountChanged;
        private void Start() { OnCoinCountChanged?.Invoke(Count); }
        public void Increment()
        {
            if (CampaignLevel.Current != null)
            {
                if (!CampaignLevel.Current.CollectLegacyCoin()) return;
            }
            else _count++;
            OnCoinCountChanged?.Invoke(Count);
        }
    }
}
