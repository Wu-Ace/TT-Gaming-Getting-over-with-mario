using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;

// Explicit menu action only: subsequent scene edits are never regenerated on import.
public static class InstallJumpKingFifth {
 const string Out="scripts/jk-tilemap-20261111", Folder="Assets/Tiles/JumpKingFifth", ScenePath="Assets/Scenes/Level05_Cave.unity";
 const float Cell=.4f, Base=-3.6f;
 [Serializable] public class CellData { public int x,y,kind; }
 [Serializable] public class Plan {public CellData[] cells,extra;}
 static Vector2 Center(Vector3Int c)=>new Vector2((c.x+.5f)*Cell,Base+(c.y+.5f)*Cell);
 static Vector2 Remap(Vector2 p)=>new Vector2(p.x*24/26.4f,Base+(p.y-Base)*252/115.36f);
 static Tile Slope(int kind) {
  string path=Folder+"/Slope"+kind+".asset";var tile=AssetDatabase.LoadAssetAtPath<Tile>(path);
  var raw=new Texture2D(2,2);raw.LoadImage(File.ReadAllBytes("Assets/Tiles/RockTerrain/fill.png"));
  const int N=120;var image=new Texture2D(N,N,TextureFormat.RGBA32,false);
  for(int y=0;y<N;y++)for(int x=0;x<N;x++) {bool inside=kind==1?x+y<N:kind==2?y<=x:kind==3?y>=x:x+y>=N-1;image.SetPixel(x,y,inside?raw.GetPixel(Mathf.Min(x+1,raw.width-1),Mathf.Min(y+1,raw.height-1)):Color.clear);}
  image.Apply();string png=Folder+"/Slope"+kind+".png";File.WriteAllBytes(png,image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(raw);UnityEngine.Object.DestroyImmediate(image);AssetDatabase.ImportAsset(png);
  var ti=(TextureImporter)AssetImporter.GetAtPath(png);ti.textureType=TextureImporterType.Sprite;ti.spriteImportMode=SpriteImportMode.Single;ti.spritePixelsPerUnit=150;ti.mipmapEnabled=false;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.filterMode=FilterMode.Bilinear;ti.wrapMode=TextureWrapMode.Clamp;var textureSettings=new TextureImporterSettings();ti.ReadTextureSettings(textureSettings);textureSettings.spriteMeshType=SpriteMeshType.FullRect;ti.SetTextureSettings(textureSettings);ti.SaveAndReimport();
  var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(png);Vector2 a=new Vector2(-.4f,-.4f),b=new Vector2(.4f,-.4f),c=new Vector2(-.4f,.4f),d=new Vector2(.4f,.4f);
  var exactPoints=kind==1?new[]{a,b,c}:kind==2?new[]{a,b,d}:kind==3?new[]{a,c,d}:new[]{b,c,d};
  // Persist the exact triangle in importer data as well as the loaded sprite.
  var so=new SerializedObject(ti);var shapes=so.FindProperty("m_SpriteSheet.m_PhysicsShape");if(shapes!=null){shapes.arraySize=1;var shape=shapes.GetArrayElementAtIndex(0);shape.arraySize=3;var points=exactPoints;for(int i=0;i<3;i++)shape.GetArrayElementAtIndex(i).vector2Value=points[i]*150;so.ApplyModifiedPropertiesWithoutUndo();ti.SaveAndReimport();sprite=AssetDatabase.LoadAssetAtPath<Sprite>(png);}
  if(!tile){tile=ScriptableObject.CreateInstance<Tile>();AssetDatabase.CreateAsset(tile,path);}tile.name="Slope"+kind;tile.sprite=sprite;tile.colliderType=Tile.ColliderType.Sprite;EditorUtility.SetDirty(tile);return tile;
 }
 static Vector2 Landing(Vector2 desired,HashSet<Vector3Int> occupied) {
  var choices=occupied.Where(c=>c.x>=2&&c.x<=57&&c.y<628&&!occupied.Contains(c+Vector3Int.up)&&Enumerable.Range(-1,3).All(dx=>Enumerable.Range(1,5).All(dy=>!occupied.Contains(c+new Vector3Int(dx,dy,0))))).ToArray();
  if(choices.Length==0)throw new Exception("No safe landing candidates");
  var best=choices.OrderBy(c=>{var p=Center(c)+Vector2.up*(Cell*.5f+.85f);float dy=p.y-desired.y;return (p.x-desired.x)*(p.x-desired.x)+dy*dy*2;}).First();
  return Center(best)+Vector2.up*(Cell*.5f+.85f);
 }
 [MenuItem("Tools/Levels/Install Original Jump King Fifth Tilemap")]
 public static void Apply() {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");
  for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Unsaved scene edits; preserving them");
  Directory.CreateDirectory(Out+"/backup");Directory.CreateDirectory(Folder);if(!File.Exists(Out+"/backup/Level05_Cave.unity"))File.Copy(ScenePath,Out+"/backup/Level05_Cave.unity");
  var scene=EditorSceneManager.OpenScene(ScenePath);var level=UnityEngine.Object.FindObjectOfType<CampaignLevel>();var plan=JsonUtility.FromJson<Plan>(File.ReadAllText(Out+"/plan.json"));
  var source=AssetDatabase.LoadAssetAtPath<RuleTile>("Assets/Tiles/RockTerrain/Rock.asset");if(!source)throw new Exception("Missing verified Rock RuleTile");
  string rulePath=Folder+"/Terrain.asset";var rule=AssetDatabase.LoadAssetAtPath<JumpKingRockTile>(rulePath);if(!rule){rule=ScriptableObject.CreateInstance<JumpKingRockTile>();rule.name="Terrain";rule.m_DefaultSprite=source.m_DefaultSprite;rule.m_DefaultColliderType=source.m_DefaultColliderType;rule.m_TilingRules=source.m_TilingRules;AssetDatabase.CreateAsset(rule,rulePath);}
  var tiles=new TileBase[]{rule,Slope(1),Slope(2),Slope(3),Slope(4)};rule.slopes=tiles.Skip(1).ToArray();EditorUtility.SetDirty(rule);
  // Remove the old static ground throughout the scene, including grids outside Terrain.
  var oldCols=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Collider2D>(true)).Where(c=>!c.isTrigger&&c.gameObject.layer==6&&(!c.attachedRigidbody||c.attachedRigidbody.bodyType==RigidbodyType2D.Static)).ToArray();
  foreach(var map in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Tilemap>(true)).Where(m=>m.gameObject.layer==6).ToArray())UnityEngine.Object.DestroyImmediate(map.gameObject);
  var old=level.transform.Find("Terrain");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
  foreach(var col in oldCols)if(col){var sr=col.GetComponent<SpriteRenderer>();if(sr)UnityEngine.Object.DestroyImmediate(sr);UnityEngine.Object.DestroyImmediate(col);}
  var root=new GameObject("Terrain");root.transform.SetParent(level.transform,false);root.transform.position=new Vector3(0,Base,0);root.transform.localScale=new Vector3(.5f,.5f,1);var grid=root.AddComponent<Grid>();grid.cellSize=new Vector3(.8f,.8f,1);
  var go=new GameObject("Jump King 01-14 - paint here");go.layer=6;go.transform.SetParent(root.transform,false);var mapNew=go.AddComponent<Tilemap>();go.AddComponent<TilemapRenderer>().sortingOrder=10;
  var occupied=new HashSet<Vector3Int>();foreach(var c in plan.cells.Concat(plan.extra)){var pos=new Vector3Int(c.x,c.y,0);mapNew.SetTile(pos,tiles[c.kind]);occupied.Add(pos);}mapNew.CompressBounds();
  var body=go.AddComponent<Rigidbody2D>();body.bodyType=RigidbodyType2D.Static;var composite=go.AddComponent<CompositeCollider2D>();composite.geometryType=CompositeCollider2D.GeometryType.Polygons;composite.vertexDistance=.001f;composite.offsetDistance=.0005f;
  var collider=go.AddComponent<TilemapCollider2D>();collider.compositeOperation=Collider2D.CompositeOperation.Merge;collider.extrusionFactor=.001f;collider.ProcessTilemapChanges();composite.GenerateGeometry();Physics2D.SyncTransforms();
  // Remap progression markers with the geometry, and settle them over clear landing surfaces.
  level.initialSpawn=Landing(new Vector2(12,Base+3),occupied);level.title="Jump King · Forest to Tower";
  if(level.route!=null)level.route=level.route.Select(Remap).ToArray();
  var placements=new List<string>();foreach(var item in UnityEngine.Object.FindObjectsOfType<CampaignInteractable>()) {
   var target=Landing(Remap(item.transform.position),occupied);if(item.kind==CampaignInteractable.Kind.Star)target+=Vector2.up*.65f;
   item.transform.position=new Vector3(target.x,target.y,item.transform.position.z);placements.Add(item.name+" "+target);
  }
  // Palette uses the same native sprite cell and uniform world scale as the scene.
  var palette=new GameObject("Jump King Fifth Palette");palette.AddComponent<Grid>().cellSize=new Vector3(.8f,.8f,1);var pg=new GameObject("Tiles");pg.transform.SetParent(palette.transform,false);var pm=pg.AddComponent<Tilemap>();pg.AddComponent<TilemapRenderer>();for(int i=0;i<tiles.Length;i++)pm.SetTile(new Vector3Int(i,0,0),tiles[i]);
  string palettePath=Folder+"/Jump King Fifth Palette.prefab";PrefabUtility.SaveAsPrefabAsset(palette,palettePath);UnityEngine.Object.DestroyImmediate(palette);
  if(!AssetDatabase.LoadAllAssetsAtPath(palettePath).Any(a=>a.GetType().Name=="GridPalette")){var settings=AssetDatabase.LoadAllAssetsAtPath("Assets/Tiles/RockTerrain/Rock Palette.prefab").FirstOrDefault(a=>a.GetType().Name=="GridPalette");if(settings)AssetDatabase.AddObjectToAsset(UnityEngine.Object.Instantiate(settings),palettePath);}
  var missing=new List<string>();int misses=0;foreach(var c in plan.cells.Concat(plan.extra)){var p=Center(new Vector3Int(c.x,c.y,0));if(c.kind>0)p+=new Vector2(c.kind==1||c.kind==3?-Cell/6:Cell/6,c.kind==1||c.kind==2?-Cell/6:Cell/6);if(!composite.OverlapPoint(p)){misses++;missing.Add(c.x+","+c.y+" kind="+c.kind+" p="+p+" actual="+mapNew.GetCellCenterWorld(new Vector3Int(c.x,c.y,0)));}}File.WriteAllLines(Out+"/composite-misses.txt",missing);
  bool fallback=misses>0;if(fallback){collider.compositeOperation=Collider2D.CompositeOperation.None;UnityEngine.Object.DestroyImmediate(composite);collider.ProcessTilemapChanges();Physics2D.SyncTransforms();misses=0;foreach(var c in plan.cells.Concat(plan.extra)){var p=Center(new Vector3Int(c.x,c.y,0));if(c.kind>0)p+=new Vector2(c.kind==1||c.kind==3?-Cell/6:Cell/6,c.kind==1||c.kind==2?-Cell/6:Cell/6);if(!collider.OverlapPoint(p))misses++;}if(misses>0)throw new Exception("Tilemap collision coverage missing="+misses);}
  File.WriteAllLines(Out+"/placements.txt",placements);File.WriteAllText(Out+"/applied.txt","Source cells="+plan.cells.Length+"; original slopes="+plan.cells.Count(c=>c.kind>0)+"; additional existing cells="+plan.extra.Length+"; total="+occupied.Count+"; collider center misses="+misses+"; composite fallback="+fallback+"; grid .4m square; native 60x45 screens; 14 screens; spawn="+level.initialSpawn);
  AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Selection.activeGameObject=go;
  if(SceneView.lastActiveSceneView){var v=SceneView.lastActiveSceneView;v.in2DMode=true;v.LookAt(new Vector3(12,3,0),Quaternion.identity,10,true,true);v.Repaint();}
 }
}

