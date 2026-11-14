using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using System.IO;
public static class BakeRemakeArms {
 static void Put(SpriteRenderer r,Vector2 anchor,Vector2 pixel,Vector2 end,float length,Vector2 target){
  float scale=length/Vector2.Distance(pixel,end);Vector2 source=new Vector2(end.x-pixel.x,pixel.y-end.y);Vector2 dir=target-anchor;
  var q=Quaternion.Euler(0,0,(Mathf.Atan2(dir.y,dir.x)-Mathf.Atan2(source.y,source.x))*Mathf.Rad2Deg);
  var local=new Vector3(pixel.x-r.sprite.pivot.x,r.sprite.rect.height-pixel.y-r.sprite.pivot.y,0);
  r.transform.SetPositionAndRotation((Vector3)anchor-q*(local*scale),q);r.transform.localScale=Vector3.one*scale*r.sprite.pixelsPerUnit;
 }
 public static void Apply(){
  string folder="Assets/Art/PlayerArmPoses";Directory.CreateDirectory(folder);
  var old=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
  var root=new GameObject("Arm frame baking");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
  var cam=new GameObject("Bake camera").AddComponent<Camera>();cam.transform.SetParent(root.transform);cam.transform.position=new Vector3(0,-.6f,-10);cam.orthographic=true;cam.orthographicSize=.8f;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=Color.clear;cam.cullingMask=1<<31;
  var rt=new RenderTexture(320,320,24,RenderTextureFormat.ARGB32);cam.targetTexture=rt;
  var up=new GameObject("Upper").AddComponent<SpriteRenderer>();up.transform.SetParent(root.transform);up.gameObject.layer=31;
  var low=new GameObject("Lower").AddComponent<SpriteRenderer>();low.transform.SetParent(root.transform);low.gameObject.layer=31;low.sortingOrder=1;
  var mat=new Material(Shader.Find("Sprites/Default"));up.sharedMaterial=low.sharedMaterial=mat;
  float[] distances={.20f,.32f,.44f,.56f,.68f,.80f,.90f,.98f};
  try{
   for(int side=0;side<2;side++){
    string name=side==0?"left":"right";up.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/11.10/Player_"+name+"arm1.png");low.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/11.10/Player_"+name+"arm2.png");
    for(int i=0;i<distances.Length;i++){
     float d=distances[i];var elbow=new Vector2(Mathf.Sqrt(Mathf.Max(0,.49f*.49f-d*d*.25f)),-d*.5f);var hand=new Vector2(0,-d);
     Put(up,Vector2.zero,side==0?new Vector2(16,16):new Vector2(66,16),side==0?new Vector2(68,70):new Vector2(12,74),.49f,elbow);
     Put(low,elbow,side==0?new Vector2(13,17):new Vector2(12,28),side==0?new Vector2(94,16):new Vector2(99,15),.49f,hand);
     cam.Render();var prev=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(320,320,TextureFormat.RGBA32,false);tex.ReadPixels(new Rect(0,0,320,320),0,0);tex.Apply();RenderTexture.active=prev;
     string path=folder+"/"+name+"_"+i.ToString("00")+".png";File.WriteAllBytes(path,tex.EncodeToPNG());Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(path);
     var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Single;imp.spritePixelsPerUnit=200;var settings=new TextureImporterSettings();imp.ReadTextureSettings(settings);settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spritePivot=new Vector2(.5f,.875f);imp.SetTextureSettings(settings);imp.textureCompression=TextureImporterCompression.Uncompressed;imp.mipmapEnabled=false;imp.alphaIsTransparency=true;imp.SaveAndReimport();
    }
   }
  }finally{cam.targetTexture=null;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(mat);EditorSceneManager.CloseScene(scene,true);UnityEngine.SceneManagement.SceneManager.SetActiveScene(old);}
  var prefab=PrefabUtility.LoadPrefabContents("Assets/Prefabs/Character.prefab");try{
   var rig=prefab.GetComponent<Character.Control.PaddleCharacterVisual>();rig.leftPoses=new Sprite[8];rig.rightPoses=new Sprite[8];
   for(int i=0;i<8;i++){rig.leftPoses[i]=AssetDatabase.LoadAssetAtPath<Sprite>(folder+"/left_"+i.ToString("00")+".png");rig.rightPoses[i]=AssetDatabase.LoadAssetAtPath<Sprite>(folder+"/right_"+i.ToString("00")+".png");}
   PrefabUtility.SaveAsPrefabAsset(prefab,"Assets/Prefabs/Character.prefab");
  }finally{PrefabUtility.UnloadPrefabContents(prefab);}
 }
}