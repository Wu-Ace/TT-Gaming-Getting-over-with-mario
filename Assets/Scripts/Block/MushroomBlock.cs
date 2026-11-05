using System;

namespace Block
{
    public class MushroomBlock : QuestionMarkBlock
    {
        public event Action OnMushroomPickUp;

        protected override void OnTouched()
        {
            OnMushroomPickUp?.Invoke();
        }
    }
}