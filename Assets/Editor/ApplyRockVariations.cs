using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class ApplyRockVariations {
 const string Folder="Assets/Tiles/RockVariations",Out="scripts/rock-all-levels-20261111";
 [Serializable]class Bank {public string[] items;}
 static bool TerrainSprite(SpriteRenderer r) {
  if(!r.sprite||!r.enabled)return false;
  string p=AssetDatabase.GetAssetPath(r.sprite);
  string n=Path.GetFileNameWithoutExtension(p);
  return (r.name.StartsWith("GroundTile")||r.GetComponentsInParent<Transform>(true).Any(t=>t.name=="Terrain"))&&
   (new[]{"tl","top","tr","left","fill","right","bl","bottom","br"}.Contains(n)||p.StartsWith(Folder));
 }
 static Vector3Int Cell(Vector3 p)=>new Vector3Int(Mathf.RoundToInt(p.x/.8f-.5f),Mathf.RoundToInt((p.y+3.6f)/.8f-.5f),0);
 [MenuItem("Tools/Art/Apply Non-Repeating Rock To All Levels")]
 public static void Apply() {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");
  for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Unsaved scene edits preserved");
  Directory.CreateDirectory(Out+"/backup");
  var names=JsonUtility.FromJson<Bank>("{\"items\":"+File.ReadAllText(Out+"/bank.json")+"}").items;
  foreach(string path in Directory.GetFiles(Folder,"*.png")) {
   var ti=(TextureImporter)AssetImporter.GetAtPath(path);
   ti.textureType=TextureImporterType.Sprite;ti.spriteImportMode=SpriteImportMode.Single;ti.spritePixelsPerUnit=150;
   var s=new TextureImporterSettings();ti.ReadTextureSettings(s);s.spriteMeshType=SpriteMeshType.FullRect;s.spriteAlignment=(int)SpriteAlignment.Center;s.spritePivot=new Vector2(.5f,.5f);var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(path);if(tex&&tex.height==154){s.spriteAlignment=(int)SpriteAlignment.Custom;s.spritePivot=new Vector2(.5f,61f/154f);}ti.SetTextureSettings(s);
   ti.textureCompression=TextureImporterCompression.Uncompressed;ti.wrapMode=TextureWrapMode.Clamp;ti.filterMode=FilterMode.Bilinear;ti.mipmapEnabled=false;ti.alphaIsTransparency=true;ti.SaveAndReimport();
  }
  var cache=names.Distinct().ToDictionary(n=>n,n=>AssetDatabase.LoadAssetAtPath<Sprite>(Folder+"/"+n+".png"));var bank=names.Select(n=>cache[n]).ToArray();
  var setup=EditorSceneManager.GetSceneManagerSetup();var report=new List<string>();int sceneIndex=0;
  try {foreach(string path in Directory.GetFiles("Assets/Scenes","*.unity").Where(p=>!p.Contains("MainMenu")).OrderBy(p=>p)) {
   string backup=Out+"/backup/"+Path.GetFileName(path);if(!File.Exists(backup))File.Copy(path,backup);
   var scene=EditorSceneManager.OpenScene(path);var roots=scene.GetRootGameObjects();string asset=Folder+"/"+scene.name+".asset";
   var rule=AssetDatabase.LoadAssetAtPath<RockVariationTile>(asset);if(!rule){rule=ScriptableObject.CreateInstance<RockVariationTile>();AssetDatabase.CreateAsset(rule,asset);}
   rule.surfaces=bank;rule.seed=7381+(++sceneIndex)*104729;rule.m_DefaultSprite=bank[15*36];rule.m_DefaultColliderType=Tile.ColliderType.Grid;
   var maps=roots.SelectMany(g=>g.GetComponentsInChildren<Tilemap>(true)).ToArray();var slopes=new HashSet<TileBase>();int cells=0;
   foreach(var map in maps) {
    var used=new TileBase[map.GetUsedTilesCount()];map.GetUsedTilesNonAlloc(used);
    foreach(var t in used)if(AssetDatabase.GetAssetPath(t).Contains("/JumpKingFifth/Slope"))slopes.Add(t);
    foreach(var c in map.cellBounds.allPositionsWithin) {
     var old=map.GetTile(c);if(!old)continue;string p=AssetDatabase.GetAssetPath(old);
     if(old is RuleTile&&(p.Contains("/UnifiedTerrain/")||p.Contains("/RockTerrain/")||p.Contains("/JumpKingFifth/")||p.StartsWith(Folder))){map.SetTile(c,rule);cells++;}
    }
   }
   rule.slopes=slopes.ToArray();EditorUtility.SetDirty(rule);
   var renderers=roots.SelectMany(g=>g.GetComponentsInChildren<SpriteRenderer>(true)).Where(TerrainSprite).ToArray();var occupied=new HashSet<Vector3Int>(renderers.Select(r=>Cell(r.transform.position)));
   foreach(var r in renderers) {
    var c=Cell(r.transform.position);int mask=(occupied.Contains(c+Vector3Int.up)?1:0)|(occupied.Contains(c+Vector3Int.down)?2:0)|(occupied.Contains(c+Vector3Int.left)?4:0)|(occupied.Contains(c+Vector3Int.right)?8:0);
    r.sprite=rule.Surface(mask,c);
   }
   // Standalone full-cell moving platforms share the same deterministic family.
   int moving=0;foreach(var r in roots.SelectMany(g=>g.GetComponentsInChildren<SpriteRenderer>(true)).Where(r=>r.sprite&&(r.GetComponentInParent<VerticalRidePlatform>()||r.GetComponentInParent<DonutFallPlatform>()||r.GetComponentInParent<OrbitRidePlatform>()))) {
    string p=AssetDatabase.GetAssetPath(r.sprite);if(!(p.Contains("/RockTerrain/")||p.StartsWith(Folder)||p.Contains("/PlatformerKit/")))continue;
    var c=Cell(r.transform.position);if(r.GetComponentInParent<VerticalRidePlatform>()){var siblings=r.GetComponentInParent<VerticalRidePlatform>().GetComponentsInChildren<SpriteRenderer>();int mask=0;foreach(var n in siblings){var d=n.transform.position-r.transform.position;if(Mathf.Abs(d.y)<.05f){if(Mathf.Abs(d.x+.8f)<.05f)mask|=4;if(Mathf.Abs(d.x-.8f)<.05f)mask|=8;}}r.sprite=rule.Surface(mask,c);r.transform.localScale=Vector3.one;}else if(r.sprite.bounds.size.y>.5f)r.sprite=rule.Surface(0,c);
    else {int t=RockVariationTile.Variant(c,rule.seed,13),d=RockVariationTile.Variant(c,rule.seed,7919);r.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Folder+"/ledge-"+t+"-"+d+".png");}
    moving++;
   }
   foreach(var m in maps)m.RefreshAllTiles();
   // Scene-specific palettes preserve the fifth level's triangle paint tools.
   string oldPalette=scene.name=="Level05_Cave"?"Assets/Tiles/JumpKingFifth/Jump King Fifth Palette.prefab":"Assets/Tiles/RockTerrain/Rock Palette.prefab";
   var palette=PrefabUtility.LoadPrefabContents(oldPalette);try {foreach(var m in palette.GetComponentsInChildren<Tilemap>())foreach(var c in m.cellBounds.allPositionsWithin)if(m.GetTile(c) is RuleTile)m.SetTile(c,rule);PrefabUtility.SaveAsPrefabAsset(palette,Folder+"/"+scene.name+" Palette.prefab");}finally{PrefabUtility.UnloadPrefabContents(palette);}
   var paletteSettings=AssetDatabase.LoadAllAssetsAtPath(oldPalette).FirstOrDefault(a=>a.GetType().Name=="GridPalette");string pp=Folder+"/"+scene.name+" Palette.prefab";if(paletteSettings&&!AssetDatabase.LoadAllAssetsAtPath(pp).Any(a=>a.GetType().Name=="GridPalette"))AssetDatabase.AddObjectToAsset(UnityEngine.Object.Instantiate(paletteSettings),pp);
   EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
   report.Add(scene.name+": cells="+cells+" legacySprites="+renderers.Length+" movingSprites="+moving+" slopes="+slopes.Count);File.WriteAllLines(Out+"/applied.txt",report);
  }AssetDatabase.SaveAssets();}finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
 }
}



