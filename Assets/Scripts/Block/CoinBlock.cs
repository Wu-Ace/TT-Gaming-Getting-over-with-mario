using Winning;

namespace Block
{
    public class CoinBlock : QuestionMarkBlock
    {
        private CoinPurse _coinPurse;

        private void Awake()
        {
            OnAwake();
            _coinPurse = FindObjectOfType<CoinPurse>();
        }

        protected override void OnTouched()
        {
            _coinPurse.Increment();
        }
    }
}