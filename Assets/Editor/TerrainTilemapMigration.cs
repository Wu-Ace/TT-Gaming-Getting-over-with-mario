using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class TerrainTilemapMigration
{
    const string Output = "scripts/tilemap-20261110";
    [MenuItem("Tools/Levels/Migrate Static Terrain To Tilemap")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play before migration.");
        for(int i=0;i<SceneManager.sceneCount;i++) if(SceneManager.GetSceneAt(i).isDirty) throw new Exception("Save unsaved scene edits before migration.");
        Directory.CreateDirectory(Output+"/backup");
        Directory.CreateDirectory("Assets/Tiles/MigratedTerrain");
        AssetDatabase.Refresh();
        var setup=EditorSceneManager.GetSceneManagerSetup();
        var report=new List<string>();
        try {
            foreach(var entry in EditorBuildSettings.scenes.Where(s=>s.enabled && !s.path.Contains("MainMenu"))) {
                var scene=EditorSceneManager.OpenScene(entry.path);
                var sources=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<SpriteRenderer>(true))
                    .Where(r=>r.sprite!=null && r.enabled && r.gameObject.activeInHierarchy && r.drawMode==SpriteDrawMode.Simple)
                    .Where(r=>r.name.StartsWith("GroundTile") || r.GetComponentsInParent<Transform>(true).Any(t=>t.name=="Terrain"))
                    .Where(r=>r.GetComponentInParent<Rigidbody2D>()==null && r.GetComponentInParent<Animator>()==null && r.GetComponents<MonoBehaviour>().Length==0)
                    .ToArray();
                if(sources.Length==0){report.Add(scene.name+": no eligible terrain");continue;}
                string backup=Output+"/backup/"+Path.GetFileName(entry.path);
                if(!File.Exists(backup))File.Copy(entry.path,backup);
                var gridObject=new GameObject("Terrain Grid");var grid=gridObject.AddComponent<Grid>();grid.cellSize=new Vector3(.8f,.8f,1);
                int count=0,maps=0;
                var remaining=new List<SpriteRenderer>(sources);
                while(remaining.Count>0) {
                    var first=remaining[0];var origin=first.transform.position-new Vector3(.4f,.4f,0);
                    var go=new GameObject("Terrain "+maps);go.layer=first.gameObject.layer;go.transform.SetParent(grid.transform,false);go.transform.position=origin;
                    var map=go.AddComponent<Tilemap>();map.tileAnchor=new Vector3(.5f,.5f,0);
                    var renderer=go.AddComponent<TilemapRenderer>();renderer.sortingLayerID=first.sortingLayerID;renderer.sortingOrder=first.sortingOrder;renderer.sharedMaterial=first.sharedMaterial;
                    foreach(var r in remaining.ToArray()) {
                        if(r.sortingLayerID!=first.sortingLayerID || r.sortingOrder!=first.sortingOrder || r.sharedMaterial!=first.sharedMaterial || r.gameObject.layer!=first.gameObject.layer)continue;
                        var delta=r.transform.position-first.transform.position;
                        var cell=new Vector3Int(Mathf.RoundToInt(delta.x/.8f),Mathf.RoundToInt(delta.y/.8f),0);
                        if(Mathf.Abs(delta.x-cell.x*.8f)>.0001f || Mathf.Abs(delta.y-cell.y*.8f)>.0001f || Mathf.Abs(delta.z)>.0001f || map.HasTile(cell))continue;
                        string guid;long localId;AssetDatabase.TryGetGUIDAndLocalFileIdentifier(r.sprite,out guid,out localId);
                        string path="Assets/Tiles/MigratedTerrain/"+guid+"-"+localId+".asset";
                        var tile=AssetDatabase.LoadAssetAtPath<Tile>(path);
                        if(tile==null){tile=ScriptableObject.CreateInstance<Tile>();tile.sprite=r.sprite;tile.colliderType=Tile.ColliderType.None;tile.flags=TileFlags.None;AssetDatabase.CreateAsset(tile,path);}
                        map.SetTile(cell,tile);map.SetTileFlags(cell,TileFlags.None);map.SetColor(cell,r.color);
                        var scale=r.transform.lossyScale;scale.x*=r.flipX?-1:1;scale.y*=r.flipY?-1:1;
                        map.SetTransformMatrix(cell,Matrix4x4.TRS(Vector3.zero,r.transform.rotation,scale));
                        // Keep authored collider objects and all serialized references intact.
                        r.enabled=false;
                        remaining.Remove(r);count++;
                    }
                    foreach(var old in sources.Where(s=>s!=null && !s.enabled).ToArray()) UnityEngine.Object.DestroyImmediate(old);
                    maps++;
                }
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                report.Add(scene.name+": migrated="+count+" maps="+maps+"; original colliders retained");
            }
            AssetDatabase.SaveAssets();
        } finally {EditorSceneManager.RestoreSceneManagerSetup(setup);File.WriteAllLines(Output+"/migration.txt",report);}
    }
}

