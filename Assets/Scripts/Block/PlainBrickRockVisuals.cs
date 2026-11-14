using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class PlainBrickRockVisuals
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
        SceneManager.sceneLoaded -= Apply;
        SceneManager.sceneLoaded += Apply;
    }
    static void Apply(Scene scene, LoadSceneMode mode)
    {
        var prefab = Resources.Load<GameObject>("PlainBrick");
        var template = prefab != null ? prefab.GetComponentInChildren<SpriteRenderer>(true) : null;
        var blocks = new List<SpriteRenderer>();
        RockVariationTile rule = null;
        Material terrainMaterial = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var map in root.GetComponentsInChildren<Tilemap>(true))
            {
                var tiles = new TileBase[map.GetUsedTilesCount()];
                map.GetUsedTilesNonAlloc(tiles);
                foreach (var tile in tiles) if (tile is RockVariationTile rock) { rule = rock; terrainMaterial = map.GetComponent<TilemapRenderer>().sharedMaterial; break; }
            }
            foreach (var source in root.GetComponentsInChildren<SpriteRenderer>(true))
                if (source.enabled && ((template != null && source.sprite == template.sprite) || source.name.StartsWith("BrickBlock") || source.GetComponent<Block.QuestionMarkBlock>() != null)) blocks.Add(source);
        }
        if (rule == null) return;
        foreach (var source in blocks)
        {
            var bounds = source.bounds;
            int mask = 0;
            foreach (var other in blocks)
            {
                if (other == source) continue;
                var b = other.bounds;
                float dx = b.center.x - bounds.center.x, dy = b.center.y - bounds.center.y;
                if (Mathf.Abs(dy) < .05f && Mathf.Abs(Mathf.Abs(dx) - (b.size.x + bounds.size.x) * .5f) < .08f) mask |= dx < 0 ? 4 : 8;
                if (Mathf.Abs(dx) < .05f && Mathf.Abs(Mathf.Abs(dy) - (b.size.y + bounds.size.y) * .5f) < .08f) mask |= dy < 0 ? 2 : 1;
            }
            var cell = new Vector3Int(Mathf.RoundToInt(bounds.center.x / .8f), Mathf.RoundToInt(bounds.center.y / .8f), 0);
            var child = new GameObject("Platform terrain visual");
            child.transform.SetParent(source.transform, false);
            var renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = rule.Surface(mask, cell);
            renderer.sharedMaterial = terrainMaterial;
            renderer.sortingLayerID = source.sortingLayerID;
            renderer.sortingOrder = source.sortingOrder;
            // Terrain sprites have a 120px cell plus extrusion and an authored pivot.
            float scale = bounds.size.x / (.8f * Mathf.Abs(source.transform.lossyScale.x));
            child.transform.localScale = Vector3.one * scale;
            source.enabled = false;
        }
    }
}
