using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class InstallRockTerrain {
 const string Folder="Assets/Tiles/RockTerrain";
 const string Out="scripts/rock-terrain-20261111";
 public static void Apply() {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");
  for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Unsaved scene edits");
  Directory.CreateDirectory(Folder);Directory.CreateDirectory(Out+"/backup");
  var sprites=new System.Collections.Generic.Dictionary<string,Sprite>();
  foreach(string role in new[]{"tl","top","tr","left","fill","right","bl","bottom","br","single","thin","thinleft","thinright","column","coltop","colbottom"}) {
   string path=Folder+"/"+role+".png";AssetDatabase.ImportAsset(path);
   var ti=(TextureImporter)AssetImporter.GetAtPath(path);ti.textureType=TextureImporterType.Sprite;ti.spriteImportMode=SpriteImportMode.Single;ti.spritePixelsPerUnit=150;var settings=new TextureImporterSettings();ti.ReadTextureSettings(settings);settings.spriteAlignment=(int)SpriteAlignment.Center;settings.spritePivot=new Vector2(.5f,.5f);settings.spriteMeshType=SpriteMeshType.FullRect;ti.SetTextureSettings(settings);ti.mipmapEnabled=false;ti.alphaIsTransparency=true;ti.wrapMode=TextureWrapMode.Clamp;ti.filterMode=FilterMode.Bilinear;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.SaveAndReimport();sprites[role]=AssetDatabase.LoadAssetAtPath<Sprite>(path);
  }
  var rule=AssetDatabase.LoadAssetAtPath<RuleTile>(Folder+"/Rock.asset");if(!rule){rule=ScriptableObject.CreateInstance<RuleTile>();AssetDatabase.CreateAsset(rule,Folder+"/Rock.asset");}
  rule.m_DefaultSprite=sprites["fill"];rule.m_DefaultColliderType=Tile.ColliderType.Grid;rule.m_TilingRules.Clear();
  for(int mask=0;mask<16;mask++) {
   bool u=(mask&1)!=0,d=(mask&2)!=0,l=(mask&4)!=0,r=(mask&8)!=0;
   string role=!u&&!d?(!l&&!r?"single":!l?"thinleft":!r?"thinright":"thin"):!l&&!r?(!u?"coltop":!d?"colbottom":"column"):!u?(!l?"tl":!r?"tr":"top"):!d?(!l?"bl":!r?"br":"bottom"):!l?"left":!r?"right":"fill";
   var t=new RuleTile.TilingRule();t.m_NeighborPositions=new System.Collections.Generic.List<Vector3Int>{Vector3Int.up,Vector3Int.down,Vector3Int.left,Vector3Int.right};t.m_Neighbors=new System.Collections.Generic.List<int>{u?1:2,d?1:2,l?1:2,r?1:2};t.m_Sprites=new[]{sprites[role]};t.m_ColliderType=Tile.ColliderType.Grid;rule.m_TilingRules.Add(t);
  }
  EditorUtility.SetDirty(rule);AssetDatabase.SaveAssets();
  string lp=Folder+"/ledge.png";AssetDatabase.ImportAsset(lp);var li=(TextureImporter)AssetImporter.GetAtPath(lp);li.textureType=TextureImporterType.Sprite;li.spriteImportMode=SpriteImportMode.Single;li.spritePixelsPerUnit=160;li.textureCompression=TextureImporterCompression.Uncompressed;li.wrapMode=TextureWrapMode.Clamp;li.mipmapEnabled=false;li.SaveAndReimport();var ledge=AssetDatabase.LoadAssetAtPath<Sprite>(lp);
  var setup=EditorSceneManager.GetSceneManagerSetup();var report=new System.Collections.Generic.List<string>();
  try {foreach(string name in new[]{"Level01","Level02_Jungle","Level03_Beach","Level04_Town"}) {
   string path="Assets/Scenes/"+name+".unity";string backup=Out+"/backup/"+name+".unity";if(!File.Exists(backup))File.Copy(path,backup);
   var scene=EditorSceneManager.OpenScene(path);int count=0;
   foreach(var map in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Tilemap>(true)))foreach(var c in map.cellBounds.allPositionsWithin){var old=map.GetTile(c);if(old is RuleTile && AssetDatabase.GetAssetPath(old).StartsWith("Assets/Tiles/UnifiedTerrain/")){map.SetTile(c,rule);count++;}}
   int platforms=0;foreach(var r in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<SpriteRenderer>(true)).Where(r=>r.sprite&&(r.GetComponentInParent<DonutFallPlatform>()||r.GetComponentInParent<OrbitRidePlatform>()))) {
    string oldPath=AssetDatabase.GetAssetPath(r.sprite);if(oldPath.StartsWith(Folder))continue;
    var next=oldPath.Contains("donut-beach")?ledge:sprites["single"];float width=r.bounds.size.x;var parent=r.transform.parent;float parentScale=parent?Mathf.Abs(parent.lossyScale.x):1;r.sprite=next;float scale=width/next.bounds.size.x/parentScale;r.transform.localScale=new Vector3(scale,scale,r.transform.localScale.z);platforms++;
   }
   EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);report.Add(name+": "+count+" RuleTile cells replaced; moving/falling platform visuals="+platforms+"; collider components unchanged");
  }}finally{EditorSceneManager.RestoreSceneManagerSetup(setup);File.WriteAllLines(Out+"/applied.txt",report);}
 }
}

