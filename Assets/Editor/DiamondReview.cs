using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
[InitializeOnLoad] public static class DiamondReview {
 static DiamondReview(){EditorApplication.delayCall+=Run;}
 static void Run(){if(!File.Exists("scripts/diamond-review.request")||EditorApplication.isPlayingOrWillChangePlaymode)return;File.Delete("scripts/diamond-review.request");
 try{if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Unsaved scene: review stopped to preserve edits.");
 string[] paths={"Assets/Scenes/OneOneScene.unity","Assets/Scenes/Level02_Jungle.unity"};
 foreach(var path in paths){EditorSceneManager.OpenScene(path);var items=UnityEngine.Object.FindObjectsOfType<CampaignInteractable>().Where(x=>x.kind==CampaignInteractable.Kind.Star).OrderBy(x=>x.starId).ToArray();if(items.Length!=3)throw new Exception("Expected three diamonds");
 foreach(var item in items){var sprite=item.GetComponent<SpriteRenderer>().sprite;if(AssetDatabase.GetAssetPath(sprite)!="Assets/Art/Campaign/diamond.png")throw new Exception("Wrong objective sprite");var go=new GameObject("Diamond review camera");var cam=go.AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=4;cam.transform.position=item.transform.position+new Vector3(0,-1,-20);var rt=new RenderTexture(1000,650,24);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var tex=new Texture2D(1000,650,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1000,650),0,0);tex.Apply();File.WriteAllBytes("scripts/diamond-evidence/"+Path.GetFileNameWithoutExtension(path)+"-"+item.starId+".png",tex.EncodeToPNG());RenderTexture.active=null;cam.targetTexture=null;UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);}}
 EditorSceneManager.OpenScene(paths[0]);var first=UnityEngine.Object.FindObjectsOfType<CampaignInteractable>().First(x=>x.kind==CampaignInteractable.Kind.Star);Selection.activeGameObject=first.gameObject;SceneView.lastActiveSceneView.LookAt(first.transform.position,Quaternion.identity,5,true);File.WriteAllText("scripts/diamond-evidence/editor.txt","PASS editor compiled; six objective sprites verified and rendered. Play collection not run. "+DateTime.UtcNow.ToString("O"));
 }catch(Exception e){File.WriteAllText("scripts/diamond-evidence/error.txt",e.ToString());Debug.LogException(e);}}
}
