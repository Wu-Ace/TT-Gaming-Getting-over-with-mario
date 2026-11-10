using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class TerrainTilemapCollisionMigration {
 const string Out="scripts/tilemap-collision-20261110";
 class Cell {public Tilemap map;public Vector3Int cell;public Vector2 center;public float scale;}
 static Tile Solid(Cell c) {
  var source=c.map.GetSprite(c.cell); string guid;long id;AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source,out guid,out id);
  string stem=guid+"-"+id+"-"+c.scale.ToString("F4",System.Globalization.CultureInfo.InvariantCulture);
  string png="Assets/Tiles/SolidTerrain/"+stem+".png", path="Assets/Tiles/SolidTerrain/"+stem+".asset";
  var tile=AssetDatabase.LoadAssetAtPath<Tile>(path);if(tile!=null)return tile;
  var original=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(source.texture));
  if(original.spriteImportMode!=SpriteImportMode.Single)throw new Exception("Unsupported multi-sprite terrain "+source.name);
  File.Copy(AssetDatabase.GetAssetPath(source.texture),png,true);AssetDatabase.ImportAsset(png);
  var importer=(TextureImporter)AssetImporter.GetAtPath(png);var settings=new TextureImporterSettings();original.ReadTextureSettings(settings);settings.spritePixelsPerUnit=original.spritePixelsPerUnit/c.scale;importer.SetTextureSettings(settings);
  importer.textureCompression=TextureImporterCompression.Uncompressed;importer.filterMode=original.filterMode;importer.wrapMode=TextureWrapMode.Clamp;importer.mipmapEnabled=false;importer.SaveAndReimport();
  tile=ScriptableObject.CreateInstance<Tile>();tile.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(png);tile.colliderType=Tile.ColliderType.Grid;tile.flags=TileFlags.None;AssetDatabase.CreateAsset(tile,path);return tile;
 }
 [MenuItem("Tools/Levels/Configure Grid Terrain Collisions")]
 public static void Run() {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");
  for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new Exception("Unsaved scene changes");
  Directory.CreateDirectory(Out+"/backup");Directory.CreateDirectory("Assets/Tiles/SolidTerrain");AssetDatabase.Refresh();
  var setup=EditorSceneManager.GetSceneManagerSetup();var report=new List<string>();
  try {foreach(var entry in EditorBuildSettings.scenes.Where(s=>s.enabled&&!s.path.Contains("MainMenu"))) {
   var scene=EditorSceneManager.OpenScene(entry.path);var roots=scene.GetRootGameObjects();
   var cells=new List<Cell>();
   foreach(var map in roots.SelectMany(g=>g.GetComponentsInChildren<Tilemap>()))foreach(var pos in map.cellBounds.allPositionsWithin){
    if(!map.HasTile(pos)||map.GetColliderType(pos)!=Tile.ColliderType.None)continue;
    var sprite=map.GetSprite(pos);string asset=AssetDatabase.GetAssetPath(sprite);if(!asset.Contains("Terrain")&&!asset.Contains("terrain-"))continue;
    var m=map.GetTransformMatrix(pos);float scale=m.m00;
    if(scale<=0||Mathf.Abs(m.m11-scale)>.0001f||Mathf.Abs(m.m01)+Mathf.Abs(m.m10)+Mathf.Abs(m.m03)+Mathf.Abs(m.m13)>.0001f)continue;
    if(Mathf.Abs(sprite.bounds.size.x*scale-.8f)>.04f||Mathf.Abs(sprite.bounds.size.y*scale-.8f)>.04f)continue;
    cells.Add(new Cell{map=map,cell=pos,center=map.GetCellCenterWorld(pos),scale=scale});
   }
   var selected=new Dictionary<Cell,BoxCollider2D>();var boxes=new List<BoxCollider2D>();
   foreach(var box in roots.SelectMany(g=>g.GetComponentsInChildren<BoxCollider2D>()).Where(b=>b.enabled&&!b.isTrigger&&b.GetComponentInParent<Rigidbody2D>()==null)) {
    if(box.name!="Terrain"&&box.name!="Solid row"&&!box.name.StartsWith("GroundTile")&&!box.GetComponentsInParent<Transform>().Any(t=>t.name=="Terrain"))continue;
    if(Quaternion.Angle(box.transform.rotation,Quaternion.identity)>.001f||box.edgeRadius>0||box.usedByEffector)continue;
    var b=box.bounds;int nx=Mathf.RoundToInt(b.size.x/.8f),ny=Mathf.RoundToInt(b.size.y/.8f);
    if(nx<1||ny<1||Mathf.Abs(nx*.8f-b.size.x)>.00015f||Mathf.Abs(ny*.8f-b.size.y)>.00015f)continue;
    var matches=new List<Cell>();bool valid=true;
    for(int x=0;x<nx&&valid;x++)for(int y=0;y<ny;y++) {
     Vector2 center=new Vector2(b.min.x+(x+.5f)*.8f,b.min.y+(y+.5f)*.8f);
     var candidates=cells.Where(c=>(c.center-center).sqrMagnitude<.00000002f).ToArray();
     if(candidates.Length!=1||selected.ContainsKey(candidates[0])){valid=false;break;}matches.Add(candidates[0]);
    }
    if(!valid)continue;
    foreach(var c in matches)selected.Add(c,box);boxes.Add(box);
   }
   // One Tilemap must keep one collision layer/material. Reject mixed-property maps.
   foreach(var group in selected.Keys.GroupBy(c=>c.map).ToArray())if(group.Select(c=>selected[c].gameObject.layer).Distinct().Count()!=1||group.Select(c=>selected[c].sharedMaterial).Distinct().Count()!=1) {
    foreach(var box in group.Select(c=>selected[c]).Distinct().ToArray()){foreach(var c in selected.Where(k=>k.Value==box).Select(k=>k.Key).ToArray())selected.Remove(c);boxes.Remove(box);}
   }
   if(selected.Count==0){report.Add(scene.name+": unchanged (irregular terrain retained)");continue;}
   string backup=Out+"/backup/"+Path.GetFileName(entry.path);if(!File.Exists(backup))File.Copy(entry.path,backup);
   foreach(var group in selected.Keys.GroupBy(c=>c.map)) {
    var map=group.Key;var old=selected[group.First()];map.gameObject.layer=old.gameObject.layer;
    foreach(var c in group){var color=map.GetColor(c.cell);map.SetTile(c.cell,Solid(c));map.SetTileFlags(c.cell,TileFlags.None);map.SetTransformMatrix(c.cell,Matrix4x4.identity);map.SetColor(c.cell,color);}
    var rb=map.gameObject.AddComponent<Rigidbody2D>();rb.bodyType=RigidbodyType2D.Static;
    var composite=map.gameObject.AddComponent<CompositeCollider2D>();composite.geometryType=CompositeCollider2D.GeometryType.Polygons;composite.vertexDistance=.001f;composite.offsetDistance=.0005f;composite.sharedMaterial=old.sharedMaterial;
    var tc=map.gameObject.AddComponent<TilemapCollider2D>();tc.compositeOperation=Collider2D.CompositeOperation.Merge;tc.extrusionFactor=.001f;tc.ProcessTilemapChanges();composite.GenerateGeometry();
   }
   Physics2D.SyncTransforms();
   foreach(var group in selected.Keys.GroupBy(c=>c.map)) {
    var map=group.Key;var composite=map.GetComponent<CompositeCollider2D>();
    if(group.Any(c=>!composite.OverlapPoint(c.center))) {
     var tileCollider=map.GetComponent<TilemapCollider2D>();tileCollider.compositeOperation=Collider2D.CompositeOperation.None;
     UnityEngine.Object.DestroyImmediate(composite);tileCollider.ProcessTilemapChanges();Physics2D.SyncTransforms();
     if(group.Any(c=>!tileCollider.OverlapPoint(c.center)))throw new Exception("Unmerged coverage also failed: "+map.name);
     report.Add(scene.name+" "+map.name+": merge coverage failed; validated TilemapCollider2D without Composite");
    }
   }
   foreach(var box in boxes)UnityEngine.Object.DestroyImmediate(box);
   EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);report.Add(scene.name+": solidCells="+selected.Count+" replacedBoxes="+boxes.Count+" maps="+selected.Keys.Select(c=>c.map).Distinct().Count());
  }AssetDatabase.SaveAssets();}finally{EditorSceneManager.RestoreSceneManagerSetup(setup);File.WriteAllLines(Out+"/migration.txt",report);}
 }
}




