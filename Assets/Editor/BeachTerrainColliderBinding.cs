using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class BeachTerrainColliderBinding
{
    const string Request = "scripts/beach-collider-binding.request";
    static BeachTerrainColliderBinding() { EditorApplication.update += Poll; }
    static void Poll()
    {
        if (!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode || File.Exists("Library/PlayTerrainSave.pending.json")) return;
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (!scene.path.EndsWith("/Level03_Beach.unity")) return;
        File.Delete(Request);
        try { Repair(); } catch (Exception e) { Debug.LogException(e); }
    }
    [MenuItem("Tools/Levels/Bind Beach terrain collision to tiles")]
    public static void Repair()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || File.Exists("Library/PlayTerrainSave.pending.json")) return;
        var level = UnityEngine.Object.FindObjectOfType<CampaignLevel>();
        if (level == null || !level.gameObject.scene.path.EndsWith("/Level03_Beach.unity")) return;
        var terrain = level.transform.Find("Terrain");
        if (terrain == null) return;
        var tiles = terrain.GetComponentsInChildren<SpriteRenderer>(true)
            .Where(s => s.name.StartsWith("Beach ") && s.sprite != null).ToArray();
        var rows = terrain.GetComponentsInChildren<BoxCollider2D>(true)
            .Where(c => c.name == "Solid row" && !c.isTrigger).ToArray();
        if (tiles.Length == 0 || rows.Length == 0) return;
        string backup = "art-backup/beach-collider-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
        Directory.CreateDirectory(backup);
        EditorSceneManager.SaveScene(level.gameObject.scene, backup + "/loaded-scene.unity", true);
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Bind Beach tile collision");
        foreach (var tile in tiles)
        {
            if (tile.GetComponent<Collider2D>() != null) continue;
            Undo.RecordObject(tile.gameObject, "Terrain collision layer");
            tile.gameObject.layer = rows[0].gameObject.layer;
            var box = Undo.AddComponent<BoxCollider2D>(tile.gameObject);
            // Retain the authored 0.8 world-unit cell, independent of decorative sprite bounds.
            var scale = tile.transform.lossyScale;
            box.size = new Vector2(.8f / Mathf.Abs(scale.x), .8f / Mathf.Abs(scale.y));
            box.sharedMaterial = rows[0].sharedMaterial;
        }
        foreach (var row in rows) Undo.DestroyObjectImmediate(row);
        Undo.CollapseUndoOperations(group);
        Physics2D.SyncTransforms();
        EditorSceneManager.MarkSceneDirty(level.gameObject.scene);
        EditorSceneManager.SaveScene(level.gameObject.scene);
        File.WriteAllText(backup + "/result.txt", "Bound tiles=" + tiles.Length + "; removed detached rows=" + rows.Length);
        Debug.Log("Beach terrain collision now follows each tile. Backup: " + backup);
    }
}
