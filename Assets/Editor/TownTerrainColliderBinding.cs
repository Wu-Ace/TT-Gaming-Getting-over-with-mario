using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class TownTerrainColliderBinding
{
    const string Request="scripts/town-collider-binding.request";
    static TownTerrainColliderBinding(){EditorApplication.update+=Poll;}
    static void Poll(){
        if(!File.Exists(Request)||EditorApplication.isPlayingOrWillChangePlaymode||File.Exists("Library/PlayTerrainSave.pending.json"))return;
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(!scene.path.EndsWith("/Level04_Town.unity"))return;
        try{Repair();File.Delete(Request);}catch(Exception e){File.Delete(Request);Debug.LogException(e);File.WriteAllText("scripts/town-collider-error.txt",e.ToString());}
    }
    [MenuItem("Tools/Levels/Bind Town terrain collision to tiles")]
    public static void Repair(){
        if(EditorApplication.isPlayingOrWillChangePlaymode||File.Exists("Library/PlayTerrainSave.pending.json"))return;
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(!scene.path.EndsWith("/Level04_Town.unity"))return;
        var level=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<CampaignLevel>(true)).Single();
        var root=level.transform.Find("Terrain");
        var tiles=root.GetComponentsInChildren<SpriteRenderer>(true).Where(s=>s.name.StartsWith("Town ")&&s.sprite!=null).ToArray();
        var rows=root.GetComponentsInChildren<BoxCollider2D>(true).Where(c=>c.name=="Solid row"&&!c.isTrigger).ToArray();
        if(tiles.Length==0)throw new Exception("No Town tiles found; terrain left unchanged.");
        if(rows.Select(c=>c.sharedMaterial).Distinct().Count()>1)throw new Exception("Mixed row materials; terrain left unchanged.");
        foreach(var tile in tiles){var scale=tile.transform.lossyScale;if(Mathf.Abs(scale.x)<.001f||Mathf.Abs(scale.y)<.001f)throw new Exception("Zero tile scale: "+tile.name);}
        string backup="art-backup/town-collider-"+DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");Directory.CreateDirectory(backup);
        if(!EditorSceneManager.SaveScene(scene,backup+"/loaded-scene.unity",true))throw new Exception("Backup failed");
        Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Bind Town collision to artwork");
        foreach(var tile in tiles){
            if(tile.GetComponent<Collider2D>()!=null)continue;
            Undo.RecordObject(tile.gameObject,"Town tile layer");tile.gameObject.layer=rows.Length>0?rows[0].gameObject.layer:6;
            var col=Undo.AddComponent<BoxCollider2D>(tile.gameObject);var scale=tile.transform.lossyScale;
            col.size=new Vector2(.8f/Mathf.Abs(scale.x),.8f/Mathf.Abs(scale.y));
            if(rows.Length>0)col.sharedMaterial=rows[0].sharedMaterial;
        }
        foreach(var row in rows)Undo.DestroyObjectImmediate(row);
        Undo.CollapseUndoOperations(group);Physics2D.SyncTransforms();
        // Exercise transform-to-physics synchronization, restoring the artwork afterwards.
        foreach(var tile in tiles){
            var col=tile.GetComponent<BoxCollider2D>();if(col==null)throw new Exception("Missing box on "+tile.name);
            Vector3 start=tile.transform.position;Vector3 before=col.bounds.center;Vector3 delta=new Vector3(.37f,.23f,0);
            try{tile.transform.position=start+delta;Physics2D.SyncTransforms();if(Vector3.Distance(col.bounds.center,before+delta)>.001f)throw new Exception("Collider failed movement check");}
            finally{tile.transform.position=start;Physics2D.SyncTransforms();}
        }
        EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Save failed");
        File.WriteAllText("scripts/town-collider-result.txt","PASS attached tiles="+tiles.Length+"; detached rows removed="+rows.Length+"; transform/physics move-and-restore checked for every tile. Backup="+backup+"\nFull traversal NOT RUN.");
        Debug.Log("Town terrain colliders bound to current artwork positions. Backup: "+backup);
    }
}
