using System;

namespace Block
{
    public class StarBlock : QuestionMarkBlock
    {
        public event Action OnStarPickUp;

        protected override void OnTouched()
        {
            OnStarPickUp?.Invoke();
        }
    }
}