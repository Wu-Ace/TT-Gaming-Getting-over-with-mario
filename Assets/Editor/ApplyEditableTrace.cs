using System;using System.IO;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;
public static class ApplyEditableTrace {
 [Serializable] public class PathData{public Vector2[] points;}
 [Serializable] public class Part{public string name;public int floor;public Vector2 origin,size;public PathData[] paths;}
 [Serializable] public class Plan{public Part[] parts;}
 [MenuItem("Tools/Levels/Apply editable fifth from source (replaces terrain)")]
 public static void Apply(){
 var scene=EditorSceneManager.OpenScene("Assets/Scenes/Level05_Cave.unity");var level=UnityEngine.Object.FindObjectOfType<CampaignLevel>();var plan=JsonUtility.FromJson<Plan>(File.ReadAllText("scripts/trace14-editable-plan.json"));
 var old=level.transform.Find("Terrain");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
 var root=new GameObject("Terrain");root.transform.SetParent(level.transform,false);
 for(int f=1;f<=14;f++){
 var floor=new GameObject("Floor "+f.ToString("00"));floor.transform.SetParent(root.transform,false);
 foreach(var part in plan.parts.Where(p=>p.floor==f)){
 var go=new GameObject(part.name);go.layer=6;go.transform.SetParent(floor.transform,false);go.transform.position=part.origin;
 var col=go.AddComponent<PolygonCollider2D>();col.pathCount=part.paths.Length;for(int i=0;i<part.paths.Length;i++)col.SetPath(i,part.paths[i].points);
 string art="Assets/Art/Campaign/Trace14Editable/"+part.name+".png";AssetDatabase.ImportAsset(art,ImportAssetOptions.ForceSynchronousImport);var ti=(TextureImporter)AssetImporter.GetAtPath(art);ti.textureType=TextureImporterType.Sprite;ti.spriteImportMode=SpriteImportMode.Single;ti.spritePixelsPerUnit=25;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.mipmapEnabled=false;ti.wrapMode=TextureWrapMode.Clamp;
 var settings=new TextureImporterSettings();ti.ReadTextureSettings(settings);settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spritePivot=Vector2.zero;ti.SetTextureSettings(settings);ti.SaveAndReimport();
 var sr=go.AddComponent<SpriteRenderer>();sr.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(art);sr.sortingOrder=10;
 }
 }
 Physics2D.SyncTransforms();int aligned=0;foreach(var sr in root.GetComponentsInChildren<SpriteRenderer>()){var c=sr.GetComponent<PolygonCollider2D>();if(Vector2.Distance(sr.bounds.min,c.bounds.min)>.12f || Vector2.Distance(sr.bounds.max,c.bounds.max)>.12f)throw new Exception("Visual collider bounds mismatch "+sr.name);aligned++;}
 var camObj=new GameObject("Review camera");var cam=camObj.AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=60;cam.transform.position=new Vector3(13.2f,54,-20);cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.12f,.16f,.22f);var rt=new RenderTexture(900,3500,24);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var image=new Texture2D(900,3500,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,900,3500),0,0);image.Apply();File.WriteAllBytes("scripts/editable-trace-render.png",image.EncodeToPNG());RenderTexture.active=null;cam.targetTexture=null;UnityEngine.Object.DestroyImmediate(camObj);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);File.WriteAllText("scripts/editable-trace-audit.txt","PASS independent sprite/collider alignment count="+aligned);
 EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);File.WriteAllText("scripts/editable-trace-result.txt","Saved independently editable terrain pieces="+plan.parts.Length+"; reachability is heuristic, full hammer play NOT RUN");
 }
}
