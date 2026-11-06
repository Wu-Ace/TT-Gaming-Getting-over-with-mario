using System;using System.IO;using System.Linq;using System.Reflection;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;
public static class ApplyTrace14 {
 [Serializable] public class Poly{public Vector2[] points;} [Serializable] public class Plan{public Poly[] polygons;public Vector2[] stars,checkpoints;public Vector2 exit,spawn;}
 public static void Apply(){
  var scene=EditorSceneManager.OpenScene("Assets/Scenes/Level05_Cave.unity");var level=UnityEngine.Object.FindObjectOfType<CampaignLevel>();var plan=JsonUtility.FromJson<Plan>(File.ReadAllText("scripts/trace14-plan.json"));
  foreach(var t in level.transform.Cast<Transform>().Where(t=>t.name=="Terrain"||t.name=="Interactions").ToArray())UnityEngine.Object.DestroyImmediate(t.gameObject);
  var terrain=new GameObject("Terrain");terrain.transform.SetParent(level.transform,false);terrain.layer=6;
  var col=terrain.AddComponent<PolygonCollider2D>();col.pathCount=plan.polygons.Length;for(int i=0;i<plan.polygons.Length;i++)col.SetPath(i,plan.polygons[i].points);
  const string art="Assets/Art/Campaign/trace14-terrain.png";AssetDatabase.ImportAsset(art,ImportAssetOptions.ForceSynchronousImport);var ti=(TextureImporter)AssetImporter.GetAtPath(art);ti.textureType=TextureImporterType.Sprite;ti.spriteImportMode=SpriteImportMode.Single;ti.spritePixelsPerUnit=25;ti.maxTextureSize=4096;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.mipmapEnabled=false;ti.wrapMode=TextureWrapMode.Clamp;ti.SaveAndReimport();
  var visual=new GameObject("Traced terrain silhouette");visual.transform.SetParent(terrain.transform,false);visual.transform.position=new Vector3(13.2f,54.08f,0);var sr=visual.AddComponent<SpriteRenderer>();sr.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(art);sr.sortingOrder=10;
  var objects=new GameObject("Interactions");objects.transform.SetParent(level.transform,false);var flags=BindingFlags.NonPublic|BindingFlags.Static;
  typeof(CampaignBuilder).GetField("theme",flags).SetValue(null,level.theme);typeof(CampaignBuilder).GetField("objects",flags).SetValue(null,objects.transform);
  for(int i=0;i<3;i++)typeof(CampaignBuilder).GetMethod("Star",flags).Invoke(null,new object[]{i,plan.stars[i]});
  foreach(var star in objects.GetComponentsInChildren<CampaignInteractable>().Where(x=>x.kind==CampaignInteractable.Kind.Star))star.GetComponent<SpriteRenderer>().sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Campaign/diamond.png");
  for(int i=0;i<plan.checkpoints.Length;i++)typeof(CampaignBuilder).GetMethod("Checkpoint",flags).Invoke(null,new object[]{plan.checkpoints[i],"trace"+i});
  typeof(CampaignBuilder).GetMethod("Door",flags).Invoke(null,new object[]{plan.exit});level.initialSpawn=plan.spawn;level.title="Jump King floors 1-14";
  Physics2D.SyncTransforms();var pixels=new Texture2D(2,2);pixels.LoadImage(File.ReadAllBytes(art));int tested=0,wrong=0;
  for(int y=4;y<pixels.height-4;y+=8)for(int x=4;x<pixels.width-4;x+=8){bool expected=pixels.GetPixel(x,y).a>.5f;if((pixels.GetPixel(x-3,y).a>.5f)!=expected||(pixels.GetPixel(x+3,y).a>.5f)!=expected||(pixels.GetPixel(x,y-3).a>.5f)!=expected||(pixels.GetPixel(x,y+3).a>.5f)!=expected)continue;tested++;if(col.OverlapPoint(new Vector2(x/25f,y/25f-3.6f))!=expected)wrong++;}
  File.WriteAllText("scripts/trace14-collision-check.txt","Interior samples="+tested+" mismatch="+wrong);if(wrong>tested*.01f)throw new Exception("Traced collider differs from source mask");
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);File.WriteAllText("scripts/trace14-result.txt","Saved source-traced floors 1-14, polygon paths="+col.pathCount+"; Play NOT RUN");
 }
}
