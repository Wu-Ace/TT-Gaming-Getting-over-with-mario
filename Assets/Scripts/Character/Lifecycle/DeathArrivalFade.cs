using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Character.Lifecycle
{
    public sealed class DeathArrivalFade : MonoBehaviour
    {
        Image image;
        float loadedAt = -1;
        public void Initialize(Image blackout)
        {
            image = blackout;
            SceneManager.sceneLoaded += Loaded;
        }
        void Loaded(Scene scene, LoadSceneMode mode) { loadedAt = Time.unscaledTime; }
        void Update()
        {
            if (loadedAt < 0) return;
            float t = (Time.unscaledTime - loadedAt) / .18f;
            if (image != null) image.color = new Color(0, 0, 0, 1 - Mathf.Clamp01(t));
            if (t >= 1) Destroy(gameObject);
        }
        void OnDestroy() { SceneManager.sceneLoaded -= Loaded; }
    }
}
