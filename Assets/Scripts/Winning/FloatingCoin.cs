using UnityEngine;
namespace Winning
{
    // Retain the component identity for existing scene references.
    public class FloatingCoin : MonoBehaviour
    {
        private void Awake() { gameObject.SetActive(false); }
    }
}
