using System.Linq;
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.path!="Assets/Scenes/Level02_Jungle.unity"||scene.isDirty)throw new System.Exception("Unexpected scene or unsaved work");
var roots=scene.GetRootGameObjects();
var map=roots.SelectMany(r=>r.GetComponentsInChildren<UnityEngine.Tilemaps.Tilemap>(true)).Single(t=>t.GetUsedTilesCount()>0);
var list=new System.Collections.Generic.List<UnityEngine.Vector3Int>(); foreach(var p in map.cellBounds.allPositionsWithin)if(map.HasTile(p))list.Add(p); var cells=list.ToArray();
var columns=cells.Select(p=>p.x).Distinct().OrderBy(x=>x).ToArray();
var cuts=columns.Skip(1).Where((x,i)=>x-columns[i]==2).Select(x=>x-1).ToArray();
if(cuts.Length!=5)throw new System.Exception("Expected five single-column chasms");
var tiles=cells.Select(p=>map.GetTile(p)).ToArray();
var destinations=cells.Select(p=>new UnityEngine.Vector3Int(p.x+cuts.Count(c=>c<p.x),p.y,p.z)).ToArray();
float Shift(float x)=>cuts.Count(c=>map.CellToWorld(new UnityEngine.Vector3Int(c+1,0,0)).x<=x+.0001f)*.8f;
var objects=new System.Collections.Generic.List<UnityEngine.Transform>();
var level=roots.Single(r=>r.name=="Level").transform;
foreach(UnityEngine.Transform t in level){if(t.name=="Interactions"){foreach(UnityEngine.Transform child in t)objects.Add(child);}else if(t.name=="WinThreshold")objects.Add(t);}
foreach(UnityEngine.Transform t in roots.Single(r=>r.name=="DEC platform dressing").transform)objects.Add(t);
UnityEditor.Undo.RegisterCompleteObjectUndo(map,"Widen Level02 chasms");
foreach(var t in objects){UnityEditor.Undo.RecordObject(t,"Move with platform");var p=t.position;p.x+=Shift(p.x);t.position=p;UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(t);}
var campaign=roots.SelectMany(r=>r.GetComponentsInChildren<CampaignLevel>(true)).Single();
UnityEditor.Undo.RecordObject(campaign,"Move progress route");
for(int i=0;i<campaign.route.Length;i++){var p=campaign.route[i];p.x+=Shift(p.x);campaign.route[i]=p;}
UnityEditor.EditorUtility.SetDirty(campaign);
map.ClearAllTiles();map.SetTiles(destinations,tiles);map.RefreshAllTiles();
var collider=map.GetComponent<UnityEngine.Tilemaps.TilemapCollider2D>();if(collider)collider.ProcessTilemapChanges();
foreach(var p in destinations)if(!map.HasTile(p))throw new System.Exception("Missing relocated cell");
int count=0;foreach(var p in map.cellBounds.allPositionsWithin)if(map.HasTile(p))count++;if(count!=cells.Length)throw new System.Exception("Tile count changed");
for(int i=0;i<cuts.Length;i++){int gap=cuts[i]+i;foreach(var y in cells.Select(p=>p.y).Distinct())if(map.HasTile(new UnityEngine.Vector3Int(gap,y,0))||map.HasTile(new UnityEngine.Vector3Int(gap+1,y,0)))throw new System.Exception("Gap not clear");}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
UnityEditor.Selection.activeGameObject=map.gameObject;UnityEditor.SceneView.lastActiveSceneView.FrameSelected();
var report=new{cells=cells.Length,cuts=cuts,grid=.8f,gapsAfter=2,totalAddedWidth=4f,movedObjects=objects.Count};
System.IO.File.WriteAllText("scripts/level02-gaps-20261114/result.json",Codely.Newtonsoft.Json.JsonConvert.SerializeObject(report));
UnityEngine.Debug.Log(Codely.Newtonsoft.Json.JsonConvert.SerializeObject(report));

