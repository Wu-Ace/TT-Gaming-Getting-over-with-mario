using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// World coordinates and the shared shader clock keep every terrain chunk in phase.
public sealed class RockFlowTexture : MonoBehaviour
{
    Material material;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install() {
        SceneManager.sceneLoaded -= Loaded;
        SceneManager.sceneLoaded += Loaded;
        Attach(SceneManager.GetActiveScene());
    }
    static void Loaded(Scene scene, LoadSceneMode mode) { Attach(scene); }
    static void Attach(Scene scene) {
        if (!scene.IsValid() || scene.name.Contains("Menu")) return;
        foreach (var root in scene.GetRootGameObjects())
            if (root.GetComponentInChildren<RockFlowTexture>(true)) return;
        var go = new GameObject("Terrain flowing texture");
        SceneManager.MoveGameObjectToScene(go, scene);
        go.AddComponent<RockFlowTexture>();
    }
    void Start() {
        var shader = Resources.Load<Shader>("RockFlow/WorldRockFlow");
        var texture = Resources.Load<Texture2D>("RockFlow/Texture");
        if (!shader || !texture) return;
        texture.wrapMode = TextureWrapMode.Repeat;
        material = new Material(shader);
        material.SetTexture("_FlowTex", texture);
        foreach (var root in gameObject.scene.GetRootGameObjects()) {
            foreach (var sprite in root.GetComponentsInChildren<SpriteRenderer>(true))
                if (sprite.name == "Platform terrain visual") sprite.sharedMaterial = material;
            foreach (var map in root.GetComponentsInChildren<Tilemap>(true)) {
                var tiles = new TileBase[map.GetUsedTilesCount()];
                map.GetUsedTilesNonAlloc(tiles);
                foreach (var tile in tiles) if (tile is RockVariationTile || tile is JumpKingRockTile) {
                    var renderer = map.GetComponent<TilemapRenderer>();
                    if (renderer) renderer.sharedMaterial = material;
                    break;
                }
            }
        }
    }
    void OnDestroy() { if (material) Destroy(material); }
}
