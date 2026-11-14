using UnityEngine;
namespace Winning
{
    public class FloatingCoin : MonoBehaviour
    {
        private bool collected;
        private void Awake()
        {
            if (name != "Coin(Clone)") gameObject.SetActive(false);
        }
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (collected || other.GetComponentInParent<Character.Lifecycle.Death>() == null) return;
            collected = true;
            var purse = FindObjectOfType<CoinPurse>();
            if (purse != null) purse.Increment();
            Destroy(gameObject);
        }
    }
}
