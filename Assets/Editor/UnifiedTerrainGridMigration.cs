using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class UnifiedTerrainGridMigration {
 const string Out="scripts/tilemap-grid-20261110";
 const string Folder="Assets/Tiles/UnifiedTerrain";
 static readonly Vector3 Origin=new Vector3(0,-3.6f,0);
 static readonly string[] Roles={"tl","top","tr","left","fill","right","bl","bottom","br"};
 static Vector3Int Cell(Vector3 p)=>new Vector3Int(Mathf.RoundToInt(p.x/.8f-.5f),Mathf.RoundToInt((p.y+3.6f)/.8f-.5f),0);
 static Vector3 Center(Vector3Int c)=>Origin+new Vector3((c.x+.5f)*.8f,(c.y+.5f)*.8f,0);
 static Sprite Art(string theme,string role){
  string source;
  if(theme=="Forest") source="Assets/Art/AncientForestLevel01/"+role+".png";
  else if(theme=="Grassland") {int[] ids={27,24,58,44,31,17,26,43,11};source="Assets/Art/PlatformerKit/Terrain/SeamSafe/grass-"+ids[Array.IndexOf(Roles,role)]+".png";}
  else source="Assets/Art/PlatformerKit/Terrain/SeamSafe/"+theme+"-"+role+".png";
  if(!File.Exists(source))source="Assets/Art/Campaign/"+theme+"/"+role+".png";
  var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(source);if(sprite==null)throw new Exception("Missing verified terrain art "+source);
  string path=Folder+"/"+theme+"-"+role+".png";if(!File.Exists(path)||theme=="Forest"){
   float ppu=150;
   if(source.Contains("SeamSafe"))File.Copy(source,path,true);
   else {var raw=new Texture2D(2,2,TextureFormat.RGBA32,false);raw.LoadImage(File.ReadAllBytes(source));ppu=raw.width/.8f;var padded=new Texture2D(raw.width+2,raw.height+2,TextureFormat.RGBA32,false);for(int x=0;x<padded.width;x++)for(int y=0;y<padded.height;y++)padded.SetPixel(x,y,raw.GetPixel(Mathf.Clamp(x-1,0,raw.width-1),Mathf.Clamp(y-1,0,raw.height-1)));padded.Apply();File.WriteAllBytes(path,padded.EncodeToPNG());UnityEngine.Object.DestroyImmediate(raw);UnityEngine.Object.DestroyImmediate(padded);}
   AssetDatabase.ImportAsset(path);
   var old=(TextureImporter)AssetImporter.GetAtPath(source);var ti=(TextureImporter)AssetImporter.GetAtPath(path);var settings=new TextureImporterSettings();old.ReadTextureSettings(settings);settings.spritePixelsPerUnit=ppu;settings.spriteAlignment=(int)SpriteAlignment.Center;settings.spritePivot=new Vector2(.5f,.5f);ti.SetTextureSettings(settings);ti.textureCompression=TextureImporterCompression.Uncompressed;ti.wrapMode=TextureWrapMode.Clamp;ti.filterMode=old.filterMode;ti.mipmapEnabled=false;ti.SaveAndReimport();
  }return AssetDatabase.LoadAssetAtPath<Sprite>(path);
 }
 static RuleTile Rule(string theme){string path=Folder+"/"+theme+".asset";var rule=AssetDatabase.LoadAssetAtPath<RuleTile>(path);if(rule!=null)return rule;
  var sprites=Roles.ToDictionary(r=>r,r=>Art(theme,r));rule=ScriptableObject.CreateInstance<RuleTile>();rule.m_DefaultSprite=sprites["fill"];rule.m_DefaultColliderType=Tile.ColliderType.Grid;
  for(int mask=0;mask<16;mask++){bool up=(mask&1)!=0,down=(mask&2)!=0,left=(mask&4)!=0,right=(mask&8)!=0;string role=!up?(!left?"tl":!right?"tr":"top"):!down?(!left?"bl":!right?"br":"bottom"):!left?"left":!right?"right":"fill";
   var tiling=new RuleTile.TilingRule();tiling.m_NeighborPositions=new List<Vector3Int>{Vector3Int.up,Vector3Int.down,Vector3Int.left,Vector3Int.right};tiling.m_Neighbors=new List<int>{up?1:2,down?1:2,left?1:2,right?1:2};tiling.m_Sprites=new[]{sprites[role]};tiling.m_ColliderType=Tile.ColliderType.Grid;rule.m_TilingRules.Add(tiling);
  }AssetDatabase.CreateAsset(rule,path);return rule;
 }
 static bool Terrain(Component c)=>c.name.StartsWith("GroundTile")||c.GetComponentsInParent<Transform>(true).Any(t=>t.name=="Terrain");
 static bool Flat(PolygonCollider2D p){if(p.pathCount!=1)return false;var points=p.GetPath(0);for(int i=0;i<points.Length;i++){var d=p.transform.TransformVector(points[i]-points[(i+1)%points.Length]);if(Mathf.Abs(d.x)>.12f&&Mathf.Abs(d.y)>.12f)return false;}return true;}
 [MenuItem("Tools/Levels/Unify Terrain On Native Grid")]
 public static void Run(){
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new Exception("Save unsaved edits first");
  Directory.CreateDirectory(Out+"/backup");Directory.CreateDirectory(Folder);AssetDatabase.Refresh();var setup=EditorSceneManager.GetSceneManagerSetup();var report=new List<string>();
  try{foreach(var entry in EditorBuildSettings.scenes.Where(s=>s.enabled&&!s.path.Contains("MainMenu"))){
   if(AssetDatabase.LoadAssetAtPath<SceneAsset>(entry.path)!=null&&File.ReadAllText(entry.path).Contains("m_Name: Unified Terrain Grid")){report.Add(Path.GetFileName(entry.path)+": already unified");continue;}
   string backup=Out+"/backup/"+Path.GetFileName(entry.path);if(!File.Exists(backup))File.Copy(entry.path,backup);else if(!File.ReadAllBytes(backup).SequenceEqual(File.ReadAllBytes(entry.path)))throw new Exception("Scene differs from backup, refusing repeated migration: "+entry.path);
   var scene=EditorSceneManager.OpenScene(entry.path);var roots=scene.GetRootGameObjects();var maps=roots.SelectMany(g=>g.GetComponentsInChildren<Tilemap>()).ToArray();var occupied=new HashSet<Vector3Int>();var removed=new List<Collider2D>();int moved=0,converted=0;float maxMove=0;
   foreach(var map in maps){foreach(var c in map.cellBounds.allPositionsWithin){if(!map.HasTile(c))continue;var sprite=map.GetSprite(c);var m=map.GetTransformMatrix(c);var size=sprite.bounds.size;float sx=Mathf.Abs(m.m00),sy=Mathf.Abs(m.m11);string path=AssetDatabase.GetAssetPath(sprite);
    bool standard=(path.Contains("Terrain")||path.Contains("terrain-"))&&Mathf.Abs(m.m01)+Mathf.Abs(m.m10)<.0001f&&Mathf.Abs(size.x*sx-.8f)<.04f&&Mathf.Abs(size.y*sy-.8f)<.04f;
    if(!standard)continue;var world=map.GetCellCenterWorld(c);var target=Cell(world);float distance=Vector3.Distance(world,Center(target));if(distance>.0001f)moved++;maxMove=Mathf.Max(maxMove,distance);occupied.Add(target);map.SetTile(c,null);converted++;
   }}
   // Remaining ordinary static collider geometry, including rectangular traced cave platforms.
   var colliders=roots.SelectMany(g=>g.GetComponentsInChildren<Collider2D>()).Where(c=>c.enabled&&!c.isTrigger&&c.GetComponentInParent<Rigidbody2D>()==null&&Terrain(c)&&c.GetComponents<MonoBehaviour>().Length==0).ToArray();
   foreach(var col in colliders){if(!(col is BoxCollider2D)&&!(col is PolygonCollider2D))continue;if(col is PolygonCollider2D poly&&!Flat(poly))continue;if(Quaternion.Angle(col.transform.rotation,Quaternion.identity)>.01f)continue;
    var b=col.bounds;if(b.size.x<.01f||b.size.y<.01f)continue;
    int x0=Mathf.RoundToInt(b.min.x/.8f),x1=Mathf.RoundToInt(b.max.x/.8f);int y0=Mathf.RoundToInt((b.min.y+3.6f)/.8f),y1=Mathf.RoundToInt((b.max.y+3.6f)/.8f);
    if(x1<=x0){x0=Cell(b.center).x;x1=x0+1;}if(y1<=y0){y1=Mathf.RoundToInt((b.max.y+3.6f)/.8f);y0=y1-1;}
    int rasterized=0;for(int x=x0;x<x1;x++)for(int y=y0;y<y1;y++){var c=new Vector3Int(x,y,0);bool inside=col is BoxCollider2D||col.OverlapPoint(Center(c));if(!inside){var point=Center(c);point.x=Mathf.Clamp(point.x,b.min.x+.001f,b.max.x-.001f);point.y=Mathf.Clamp(point.y,b.min.y+.001f,b.max.y-.001f);inside=col.OverlapPoint(point);}if(inside){occupied.Add(c);rasterized++;}}
    if(rasterized==0)continue;
    removed.Add(col);
    // Remove the old whole-platform image only when it belongs to this converted object.
    foreach(var map in maps)foreach(var c in map.cellBounds.allPositionsWithin){if(!map.HasTile(c))continue;var sp=map.GetSprite(c);if((map.GetCellCenterWorld(c)-col.transform.position).sqrMagnitude<.00001f&&col.name.StartsWith(sp.name))map.SetTile(c,null);}
   }
   if(occupied.Count==0)throw new Exception("No grid terrain in "+scene.name);
   string theme=scene.name=="Level01"?"Forest":scene.name.Contains("Jungle")?"Jungle":scene.name.Contains("Beach")?"Beach":scene.name.Contains("Town")?"Town":scene.name.Contains("Cave")?"Cave":"Grassland";var tile=Rule(theme);
   var gridObject=new GameObject("Unified Terrain Grid");gridObject.transform.position=Origin;var grid=gridObject.AddComponent<Grid>();grid.cellSize=new Vector3(.8f,.8f,1);
   var go=new GameObject("Ground — paint here");go.layer=6;go.transform.SetParent(grid.transform,false);var terrain=go.AddComponent<Tilemap>();terrain.tileAnchor=new Vector3(.5f,.5f,0);var renderer=go.AddComponent<TilemapRenderer>();renderer.sortingOrder=10;
   foreach(var c in occupied)terrain.SetTile(c,tile);terrain.RefreshAllTiles();
   var body=go.AddComponent<Rigidbody2D>();body.bodyType=RigidbodyType2D.Static;var composite=go.AddComponent<CompositeCollider2D>();composite.geometryType=CompositeCollider2D.GeometryType.Polygons;composite.vertexDistance=.001f;composite.offsetDistance=.0005f;
   var tc=go.AddComponent<TilemapCollider2D>();tc.compositeOperation=Collider2D.CompositeOperation.Merge;tc.extrusionFactor=.001f;tc.ProcessTilemapChanges();composite.GenerateGeometry();Physics2D.SyncTransforms();
   if(occupied.Any(c=>!composite.OverlapPoint(Center(c)))){tc.compositeOperation=Collider2D.CompositeOperation.None;UnityEngine.Object.DestroyImmediate(composite);tc.ProcessTilemapChanges();Physics2D.SyncTransforms();if(occupied.Any(c=>!tc.OverlapPoint(Center(c))))throw new Exception("Collision coverage failed "+scene.name);report.Add(scene.name+": unmerged fallback");}
   foreach(var col in removed)UnityEngine.Object.DestroyImmediate(col);
   foreach(var map in maps){map.CompressBounds();if(map.GetUsedTilesCount()==0)UnityEngine.Object.DestroyImmediate(map.gameObject);else map.name="Preserved irregular terrain";}
   // Delete only empty leaf holders inside old terrain, never scripts, colliders or referenced gameplay components.
   for(int pass=0;pass<8;pass++)foreach(var t in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Reverse().ToArray())if(t!=null&&t.childCount==0&&t.GetComponents<Component>().Length==1&&Terrain(t))UnityEngine.Object.DestroyImmediate(t.gameObject);
   // Palette is an ordinary Grid prefab containing the RuleTile, ready for Tile Palette import.
   var palette=new GameObject(theme+" Palette");palette.AddComponent<Grid>().cellSize=new Vector3(.8f,.8f,1);var pg=new GameObject("Tiles");pg.transform.SetParent(palette.transform,false);var pm=pg.AddComponent<Tilemap>();pg.AddComponent<TilemapRenderer>();pm.SetTile(Vector3Int.zero,tile);PrefabUtility.SaveAsPrefabAsset(palette,Folder+"/"+theme+" Palette.prefab");
   var paletteMethod=typeof(UnityEditor.Tilemaps.GridPaletteUtility).GetMethods(System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Single(m=>m.Name=="CreateGridPalette"&&m.GetParameters().Length==1);var settings=(ScriptableObject)paletteMethod.Invoke(null,new object[]{Enum.Parse(paletteMethod.GetParameters()[0].ParameterType,"Manual")});AssetDatabase.AddObjectToAsset(settings,Folder+"/"+theme+" Palette.prefab");UnityEngine.Object.DestroyImmediate(palette);
   EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);report.Add(scene.name+": gridCells="+occupied.Count+" sourceSprites="+converted+" movedSprites="+moved+" maximumShift="+maxMove.ToString("F4")+" convertedOldColliders="+removed.Count+" preservedMaps="+maps.Count(m=>m!=null));
  }AssetDatabase.SaveAssets();}finally{EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorSceneManager.RestoreSceneManagerSetup(setup);File.WriteAllLines(Out+"/migration.txt",report);}
 }
}
