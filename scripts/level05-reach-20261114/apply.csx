using System.Linq;
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.path!="Assets/Scenes/Level05_Cave.unity"||UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Unexpected scene");
System.IO.File.Copy(scene.path,"scripts/level05-reach-20261114/Level05.disk.before",true);
if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,"scripts/level05-reach-20261114/Level05.live.before.unity",true))throw new System.Exception("Backup failed");
var source=UnityEngine.Object.FindObjectsOfType<UnityEngine.Tilemaps.Tilemap>().Single(m=>m.name.StartsWith("Jump King"));
if(UnityEngine.GameObject.Find("Level05 climbing footholds"))throw new System.Exception("Already installed");
var tile=source.GetTile(source.WorldToCell(new UnityEngine.Vector3(11.8f,-2.2f,0)));
if(!tile)throw new System.Exception("Missing native rock tile");
var json=Codely.Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText("scripts/level05-reach-20261114/footholds.json"));
var root=new UnityEngine.GameObject("Level05 climbing footholds");UnityEditor.Undo.RegisterCreatedObjectUndo(root,"Add Level05 climbing footholds");root.transform.SetParent(source.transform.parent,false);
var records=new System.Collections.Generic.List<string>();int count=0;
foreach(var p in json["points"]){
 float x=(float)p["x"],y=(float)p["y"];
 var cell=source.WorldToCell(new UnityEngine.Vector3(x,y,0));
 var top=source.CellToWorld(cell+UnityEngine.Vector3Int.up).y;
 // Leave the existing terrain and its clearance intact.
 bool clear=true;for(int dx=-2;dx<=2;dx++)for(int dy=0;dy<=5;dy++)if(source.HasTile(cell+new UnityEngine.Vector3Int(dx,dy,0)))clear=false;
 if(!clear)continue;
 // Existing ground within one step already provides support.
 bool supported=false;for(int dy=1;dy<=2;dy++)if(source.HasTile(cell+new UnityEngine.Vector3Int(0,-dy,0)))supported=true;
 if(supported)continue;
 var go=new UnityEngine.GameObject("Foothold "+(++count));go.layer=6;go.transform.SetParent(root.transform,false);
 var map=go.AddComponent<UnityEngine.Tilemaps.Tilemap>();var renderer=go.AddComponent<UnityEngine.Tilemaps.TilemapRenderer>();var original=source.GetComponent<UnityEngine.Tilemaps.TilemapRenderer>();renderer.sharedMaterial=original.sharedMaterial;renderer.sortingLayerID=original.sortingLayerID;renderer.sortingOrder=original.sortingOrder;
 for(int dx=-2;dx<2;dx++)map.SetTile(cell+new UnityEngine.Vector3Int(dx,0,0),tile);
 map.RefreshAllTiles();var col=go.AddComponent<UnityEngine.Tilemaps.TilemapCollider2D>();col.usedByEffector=true;var eff=go.AddComponent<UnityEngine.PlatformEffector2D>();eff.useOneWay=true;eff.useOneWayGrouping=true;eff.surfaceArc=160;eff.useSideFriction=false;eff.useSideBounce=false;col.ProcessTilemapChanges();
 records.Add(count+": center="+x+","+top+" width=1.6");
}
source.RefreshAllTiles();UnityEngine.Physics2D.SyncTransforms();UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);System.IO.File.WriteAllLines("scripts/level05-reach-20261114/added.txt",records);UnityEngine.Debug.Log("Saved native-cell one-way footholds: "+count);
