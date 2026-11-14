using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;

// Explicit, repeatable original-only conversion. No import-time scene changes.
public static class InstallJumpKingFifth {
 const string Out="scripts/jk-original-20261111";
 const string Folder="Assets/Tiles/JumpKingFifth";
 const string ScenePath="Assets/Scenes/Level05_Cave.unity";
 const float Cell=.4f, Base=-3.6f;
 [Serializable] public class CellData { public int x,y,kind; public string sourceSlope; }
 [Serializable] public class Plan {public CellData[] cells,extra;}
 static Vector2 Center(Vector3Int c)=>new Vector2((c.x+.5f)*Cell,Base+(c.y+.5f)*Cell);
 static Vector2 Sample(CellData c,float x,float y)=>new Vector2((c.x+x)*Cell,Base+(c.y+y)*Cell);
 static Vector2 Landing(Vector2 desired,Dictionary<Vector3Int,int> cells) {
  var choices=cells.Where(kv=>kv.Value==0&&kv.Key.x>=2&&kv.Key.x<=57&&kv.Key.y<628)
   .Select(kv=>kv.Key).Where(c=>Enumerable.Range(-1,3).All(dx=>cells.TryGetValue(c+new Vector3Int(dx,0,0),out int kind)&&kind==0)
    &&Enumerable.Range(-1,3).All(dx=>Enumerable.Range(1,5).All(dy=>!cells.ContainsKey(c+new Vector3Int(dx,dy,0))))).ToArray();
  if(choices.Length==0)throw new Exception("No original landing candidates");
  return choices.Select(c=>Center(c)+Vector2.up*(Cell*.5f+.85f))
   .OrderBy(p=>(p.x-desired.x)*(p.x-desired.x)+2*(p.y-desired.y)*(p.y-desired.y)).First();
 }
 [MenuItem("Tools/Levels/Restore Exact Original Jump King Fifth")]
 public static void Apply() {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");
  for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
   if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Preserving unsaved scene edits");
  var plan=JsonUtility.FromJson<Plan>(File.ReadAllText(Out+"/plan.json"));
  if(plan.cells.Length!=7565||plan.extra.Length!=0)throw new Exception("Original-only plan required");
  var scene=EditorSceneManager.OpenScene(ScenePath);
  var level=UnityEngine.Object.FindObjectOfType<CampaignLevel>();
  var map=UnityEngine.Object.FindObjectsOfType<Tilemap>().Single(m=>m.name.StartsWith("Jump King"));
  var grid=map.GetComponentInParent<Grid>();
  if(Vector2.Distance(map.GetCellCenterWorld(Vector3Int.zero),new Vector2(.2f,-3.4f))>.0001f)
   throw new Exception("Unexpected authored Grid transform");
  var rule=map.GetTile(Vector3Int.zero) as RuleTile;
  if(!rule)rule=AssetDatabase.LoadAssetAtPath<JumpKingRockTile>(Folder+"/Terrain.asset");
  var tiles=new TileBase[]{rule,
   AssetDatabase.LoadAssetAtPath<Tile>(Folder+"/Slope1.asset"),
   AssetDatabase.LoadAssetAtPath<Tile>(Folder+"/Slope2.asset"),
   AssetDatabase.LoadAssetAtPath<Tile>(Folder+"/Slope3.asset"),
   AssetDatabase.LoadAssetAtPath<Tile>(Folder+"/Slope4.asset")};
  if(tiles.Any(t=>t==null))throw new Exception("Missing authored terrain tiles");
  map.ClearAllTiles();
  var expected=new Dictionary<Vector3Int,int>();
  foreach(var c in plan.cells){var p=new Vector3Int(c.x,c.y,0);map.SetTile(p,tiles[c.kind]);expected.Add(p,c.kind);}
  map.RefreshAllTiles();map.CompressBounds();
  var col=map.GetComponent<TilemapCollider2D>();col.ProcessTilemapChanges();Physics2D.SyncTransforms();
  level.initialSpawn=Landing(new Vector2(12,-1.15f),expected);
  // Retain the game's existing finish flag, placing it over an original tower landing.
  var exit=UnityEngine.Object.FindObjectsOfType<CampaignInteractable>().Single(i=>i.kind==CampaignInteractable.Kind.Door);
  exit.transform.position=(Vector3)Landing(new Vector2(9.4f,246.05f),expected);
  level.route=new[]{level.initialSpawn,(Vector2)exit.transform.position};
  level.title="Jump King · Original Forest to Tower";
  var body=UnityEngine.Object.FindObjectOfType<Character.Lifecycle.Death>();
  body.transform.parent.position+=(Vector3)level.initialSpawn-body.transform.position;
  int tileMismatches=0,samples=0,physicsMismatches=0;
  for(int y=0;y<630;y++)for(int x=0;x<60;x++) {
   var p=new Vector3Int(x,y,0);bool solid=expected.TryGetValue(p,out int kind);
   if(map.GetTile(p)!=(solid?tiles[kind]:null))tileMismatches++;
   foreach(float sx in new[]{.2f,.5f,.8f})foreach(float sy in new[]{.2f,.5f,.8f}) {
    if(solid&&kind>0&&(Mathf.Abs(sx-sy)<.0001f||Mathf.Abs(sx+sy-1)<.0001f))continue;
    bool inside=solid&&(kind==0||(kind==1?sx+sy<1:kind==2?sy<sx:kind==3?sy>sx:sx+sy>1));
    bool actual=col.OverlapPoint(new Vector2((x+sx)*Cell,Base+(y+sy)*Cell));samples++;if(actual!=inside)physicsMismatches++;
   }
  }
  File.WriteAllText(Out+"/applied.txt","Original-only cells="+expected.Count+"; extra cells=0; original slope cells="+plan.cells.Count(c=>c.kind>0)+"; original tile differences="+tileMismatches+"; physics subcell samples="+samples+"; physics differences="+physicsMismatches+"; spawn="+level.initialSpawn+"; finish="+exit.transform.position);
  if(tileMismatches!=0||physicsMismatches!=0)throw new Exception("Original comparison failed; scene not saved");
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  Selection.activeGameObject=map.gameObject;
  if(SceneView.lastActiveSceneView){var v=SceneView.lastActiveSceneView;v.in2DMode=true;v.LookAt(new Vector3(12,118,0),Quaternion.identity,10,true,true);v.Focus();v.Repaint();}
 }
}
