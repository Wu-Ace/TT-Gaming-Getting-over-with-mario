using UnityEditor;
using UnityEngine;
using System.IO;
[InitializeOnLoad] public static class ForegroundMountainReview {
 static ForegroundMountainReview(){ EditorApplication.delayCall += Review; }
 static void Review(){
  const string path="scripts/foreground-20261111/preview.png";
  if(File.Exists(path))return;
  var camera=Camera.main;if(camera==null)return;
  var landscape=camera.GetComponent<KitLandscape>();if(landscape==null)return;
  landscape.Rebuild();
  var old=camera.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(1280,720,24);
  var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);
  try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());}
  finally{camera.targetTexture=old;RenderTexture.active=active;Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);}
 }
}
