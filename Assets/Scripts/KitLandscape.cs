using UnityEngine;
using UnityEngine.SceneManagement;

// Fit the kit landscape behind the active gameplay camera, including aspect changes.
public sealed class KitLandscape : MonoBehaviour
{
    Camera view;
    SpriteRenderer landscape;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Attach();
    }
    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) { Attach(); }
    static void Attach()
    {
        if (SceneManager.GetActiveScene().name != "OneOneScene" && FindObjectOfType<CampaignLevel>() == null) return;
        var camera = Camera.main;
        if (camera != null && camera.GetComponent<KitLandscape>() == null)
            camera.gameObject.AddComponent<KitLandscape>();
    }
    void Awake()
    {
        view = GetComponent<Camera>();
        var level=FindObjectOfType<CampaignLevel>();
        var sprite = level != null && level.background != null ? level.background : Resources.Load<Sprite>("KitBackground");
        if (sprite == null) return;
        var child = new GameObject("Kit Landscape");
        child.transform.SetParent(transform, false);
        child.transform.localPosition = new Vector3(0, 0, view.farClipPlane * 0.9f);
        landscape = child.AddComponent<SpriteRenderer>();
        landscape.sprite = sprite;
        landscape.sortingOrder = -32000;
    }
    void LateUpdate()
    {
        if (landscape == null || !view.orthographic) return;
        var size = landscape.sprite.bounds.size;
        float scale = Mathf.Max(view.orthographicSize * 2 / size.y,
            view.orthographicSize * 2 * view.aspect / size.x);
        landscape.transform.localScale = new Vector3(scale, scale, 1);
    }
}
