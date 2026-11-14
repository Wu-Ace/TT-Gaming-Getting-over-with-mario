using System.Linq;
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/Level05_Cave.unity"||UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Unexpected scene");
if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,"scripts/level05-clearance-fix-20261114/compressed.before.unity",true))throw new System.Exception("Backup failed"); System.IO.File.Copy("scripts/level05-lower-platforms-20261114/before.unity","Assets/Scenes/Level05_ClearanceWorking.unity",true);UnityEditor.AssetDatabase.ImportAsset("Assets/Scenes/Level05_ClearanceWorking.unity");scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Level05_ClearanceWorking.unity"); var rows=Codely.Newtonsoft.Json.JsonConvert.DeserializeObject<int[]>(System.IO.File.ReadAllText("scripts/level05-clearance-fix-20261114/row-map.json")); int Row(int y)=>y<0?y:y>=rows.Length?rows[rows.Length-1]+y-rows.Length+1:rows[y];
var map=UnityEngine.Object.FindObjectsOfType<UnityEngine.Tilemaps.Tilemap>().Single(m=>m.name.StartsWith("Jump King"));var level=UnityEngine.Object.FindObjectOfType<CampaignLevel>();
var added=UnityEngine.GameObject.Find("Level05 climbing footholds");if(added)UnityEditor.Undo.DestroyObjectImmediate(added);
var list=new System.Collections.Generic.List<UnityEngine.Vector3Int>();foreach(var cell in map.cellBounds.allPositionsWithin)if(map.HasTile(cell))list.Add(cell);var cells=list.OrderBy(p=>p.y).ToArray();var tiles=cells.Select(p=>map.GetTile(p)).ToArray();
UnityEditor.Undo.RegisterCompleteObjectUndo(map,"Lower Level05 platforms");map.ClearAllTiles();for(int i=0;i<cells.Length;i++){var p=cells[i];p.y=Row(p.y);map.SetTile(p,tiles[i]);}map.RefreshAllTiles();map.GetComponent<UnityEngine.Tilemaps.TilemapCollider2D>().ProcessTilemapChanges();
float Y(float y){float v=(y+3.6f)/.4f;int k=UnityEngine.Mathf.FloorToInt(v);return (Row(k)+(Row(k+1)-Row(k))*(v-k))*.4f-3.6f;}
var moved=new System.Collections.Generic.HashSet<UnityEngine.Transform>();
void Move(UnityEngine.Transform t){if(!t||!moved.Add(t))return;UnityEditor.Undo.RecordObject(t,"Lower with Level05 platform");var p=t.position;p.y=Y(p.y);t.position=p;UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(t);}
foreach(UnityEngine.Transform child in level.transform){if(child==map.transform.parent)continue;if(child.name=="Interactions"){foreach(UnityEngine.Transform t in child)Move(t);}else Move(child);}
foreach(var root in scene.GetRootGameObjects()){if(root.name.StartsWith("DEC"))foreach(UnityEngine.Transform t in root.transform)Move(t);}
UnityEditor.Undo.RecordObject(level,"Lower Level05 route");for(int i=0;i<level.route.Length;i++){var p=level.route[i];p.y=Y(p.y);level.route[i]=p;}
var spawn=level.initialSpawn;int cx=map.WorldToCell(spawn).x;int floor=map.cellBounds.yMin;for(int y=map.cellBounds.yMin;y<3;y++)if(map.HasTile(new UnityEngine.Vector3Int(cx,y,0)))floor=y;spawn.y=map.CellToWorld(new UnityEngine.Vector3Int(cx,floor+1,0)).y+.85f;level.initialSpawn=spawn;UnityEditor.EditorUtility.SetDirty(level);
var death=UnityEngine.Object.FindObjectOfType<Character.Lifecycle.Death>();var rig=death.transform.parent;UnityEditor.Undo.RecordObject(rig,"Align entry to lowered terrain");rig.position+=(UnityEngine.Vector3)spawn-death.transform.position;UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(rig);
UnityEngine.Physics2D.SyncTransforms();UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,"Assets/Scenes/Level05_Cave.unity");
System.IO.File.WriteAllText("scripts/level05-clearance-fix-20261114/result.txt","Existing terrain rows remapped clearance constrained row mapping, native .4 cells and horizontal positions preserved; no added footholds. Spawn="+spawn+" cellsBefore="+cells.Length+" bounds="+map.cellBounds);
UnityEngine.Debug.Log("Saved lowered Level05; spawn="+spawn+" cell bounds="+map.cellBounds);



