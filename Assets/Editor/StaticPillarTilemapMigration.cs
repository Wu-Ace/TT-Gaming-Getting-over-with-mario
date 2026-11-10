using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;

public static class StaticPillarTilemapMigration {
 const string Out="scripts/tilemap-pillars-20261110";
 [MenuItem("Tools/Levels/Migrate Static Pillars And Stairs")]
 public static void Run(){
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play before converting terrain");
  Directory.CreateDirectory(Out+"/backup");
  // Preserve any live scene edits before opening another scene.
  for(int i=0;i<SceneManager.sceneCount;i++){var sc=SceneManager.GetSceneAt(i);if(!sc.isDirty)continue;if(string.IsNullOrEmpty(sc.path))throw new Exception("Unnamed unsaved scene");string path=Out+"/backup/"+Path.GetFileName(sc.path)+".before-live-save";if(!File.Exists(path))File.Copy(sc.path,path);EditorSceneManager.SaveScene(sc);}
  var setup=EditorSceneManager.GetSceneManagerSetup();var report=new List<string>();var footprint=new List<string>();
  try{foreach(var entry in EditorBuildSettings.scenes.Where(e=>e.enabled&&!e.path.Contains("MainMenu"))){
   var scene=EditorSceneManager.OpenScene(entry.path);var renderers=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<SpriteRenderer>(true)).Where(r=>r.name.StartsWith("Tube")||r.name.StartsWith("StairsBlock")).ToArray();
   if(renderers.Length==0){report.Add(scene.name+": no static pillar/stair objects");continue;}
   var map=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Tilemap>()).Single(m=>m.name=="Ground — paint here");
   var rule=map.GetTilesBlock(map.cellBounds).OfType<RuleTile>().First();
   string backup=Out+"/backup/"+Path.GetFileName(entry.path);if(File.Exists(backup))throw new Exception("Existing migration backup: inspect before rerun");File.Copy(entry.path,backup);
   var targets=new List<GameObject>();var cells=new HashSet<Vector3Int>();int pillars=0,stairs=0,skipped=0;float maxShift=0;
   foreach(var renderer in renderers){
    var go=renderer.gameObject;var body=go.GetComponent<Rigidbody2D>();var colliders=go.GetComponents<Collider2D>();
    bool supported=go.activeInHierarchy&&renderer.enabled&&go.transform.childCount==0&&body!=null&&body.bodyType==RigidbodyType2D.Static&&colliders.Length==1&&!colliders[0].isTrigger&&!colliders[0].usedByEffector&&Quaternion.Angle(go.transform.rotation,Quaternion.identity)<.001f;
    supported &= go.GetComponents<Component>().All(c=>c is Transform||c is SpriteRenderer||c is Rigidbody2D||c is BoxCollider2D||c is PolygonCollider2D);
    if(!supported){report.Add("KEEP special/unsupported "+renderer.name);skipped++;continue;}
    var bounds=colliders[0].bounds;
    if(colliders[0] is PolygonCollider2D polygon){
     if(polygon.pathCount!=1){skipped++;continue;}var points=polygon.GetPath(0);bool diagonal=false;
     for(int i=0;i<points.Length;i++){var d=go.transform.TransformVector(points[i]-points[(i+1)%points.Length]);if(Mathf.Abs(d.x)>.12f&&Mathf.Abs(d.y)>.12f)diagonal=true;}
     if(diagonal){report.Add("KEEP sloped "+renderer.name);skipped++;continue;}
    }
    int x0=Mathf.RoundToInt(bounds.min.x/.8f),x1=Mathf.RoundToInt(bounds.max.x/.8f),y0=Mathf.RoundToInt((bounds.min.y+3.6f)/.8f),y1=Mathf.RoundToInt((bounds.max.y+3.6f)/.8f);
    if(x1<=x0||y1<=y0){skipped++;continue;}
    var center=new Vector3((x0+x1)*.4f,(y0+y1)*.4f-3.6f,0);maxShift=Mathf.Max(maxShift,Vector2.Distance(bounds.center,center));
    for(int x=x0;x<x1;x++)for(int y=y0;y<y1;y++){var c=new Vector3Int(x,y,0);map.SetTile(c,rule);map.SetTransformMatrix(c,Matrix4x4.identity);cells.Add(c);}
    footprint.Add(scene.name+"|"+renderer.name+"|old="+bounds.ToString("F4")+"|cells="+x0+","+y0+".."+x1+","+y1);
    targets.Add(go);if(renderer.name.StartsWith("Tube"))pillars++;else stairs++;
   }
   map.RefreshAllTiles();var tc=map.GetComponent<TilemapCollider2D>();tc.ProcessTilemapChanges();var composite=map.GetComponent<CompositeCollider2D>();if(composite!=null)composite.GenerateGeometry();Physics2D.SyncTransforms();
   Collider2D collision=composite;if(collision==null)collision=tc;
   if(cells.Any(c=>!collision.OverlapPoint(map.GetCellCenterWorld(c)))){
    if(composite==null)throw new Exception("Missing tile collision");tc.compositeOperation=Collider2D.CompositeOperation.None;UnityEngine.Object.DestroyImmediate(composite);tc.ProcessTilemapChanges();Physics2D.SyncTransforms();collision=tc;
    if(cells.Any(c=>!collision.OverlapPoint(map.GetCellCenterWorld(c))))throw new Exception("Missing unmerged tile collision");report.Add(scene.name+": Composite coverage failed, using unmerged TilemapCollider2D");
   }
   foreach(var go in targets)UnityEngine.Object.DestroyImmediate(go);
   if(skipped==0&&scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<SpriteRenderer>(true)).Any(r=>r.name.StartsWith("Tube")||r.name.StartsWith("StairsBlock")))throw new Exception("Unexpected remaining legacy renderers");
   EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
   report.Add(scene.name+": pillars="+pillars+" stairs="+stairs+" cellsTouched="+cells.Count+" skipped="+skipped+" maximumCenterShift="+maxShift.ToString("F4"));
  }}finally{EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorSceneManager.RestoreSceneManagerSetup(setup);File.WriteAllLines(Out+"/migration.txt",report);File.WriteAllLines(Out+"/footprints.txt",footprint);}
 }
}
