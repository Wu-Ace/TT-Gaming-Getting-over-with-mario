using System;
using UnityEngine;

namespace Winning
{
    public class CoinPurse : MonoBehaviour
    {
        private int _count;

        public event Action<int> OnCoinCountChanged;

        public void Increment()
        {
            _count++;
            OnCoinCountChanged?.Invoke(_count);
        }
    }
}