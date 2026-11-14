using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;
public static class WaterAcceptance {
 public static void Run(){
  EditorSceneManager.OpenScene("Assets/Scenes/Level01.unity");
  var c=Camera.main;
  if(!c.GetComponent<KitLandscape>())c.gameObject.AddComponent<KitLandscape>();
  FirstLevelMoonlight.Attach(); FirstLevelWater.Attach();
  c.SendMessage("LateUpdate",SendMessageOptions.DontRequireReceiver);
  var reflection=GameObject.Find("Water reflection camera").GetComponent<Camera>(); reflection.Render();
  var rt=new RenderTexture(1280,720,24); c.targetTexture=rt;c.Render();RenderTexture.active=rt;
  var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();
  File.WriteAllBytes("scripts/water-20261111/preview.png",tex.EncodeToPNG());
  File.WriteAllText("scripts/water-20261111/result.txt","Level01 rendered in engine. Shader supported="+Shader.Find("Game/KingdomWater").isSupported+" camera="+c.transform.position);
  c.targetTexture=null;RenderTexture.active=null; Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);
 }
}
