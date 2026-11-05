using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
[InitializeOnLoad] public static class AncientForestBridge {
 static int frames;
 static AncientForestBridge(){EditorApplication.update+=Tick;}
 static void Capture(Camera camera,string name){
  var previous=camera.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(1400,800,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
  var tex=new Texture2D(1400,800,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1400,800),0,0);tex.Apply();File.WriteAllBytes("scripts/ancient-forest-evidence/"+name+".png",tex.EncodeToPNG());camera.targetTexture=previous;RenderTexture.active=active;Object.DestroyImmediate(tex);Object.DestroyImmediate(rt);
 }
 static void Tick(){
  if(EditorApplication.isPlaying&&File.Exists("scripts/ancient-forest-playing.request")){
   if(++frames>90){File.Delete("scripts/ancient-forest-playing.request");Capture(Camera.main,"game");
    var camera=Camera.main;var position=camera.transform.position;var size=camera.orthographicSize;
    var pillars=Object.FindObjectsOfType<SpriteRenderer>().Where(r=>r.name.StartsWith("Tube")).OrderBy(r=>r.bounds.size.y).ToArray();
    foreach(var r in pillars.GroupBy(r=>Mathf.Round(r.bounds.size.y*100)).Select(g=>g.First())){camera.transform.position=new Vector3(r.bounds.center.x,r.bounds.center.y,position.z);camera.orthographicSize=Mathf.Max(2,r.bounds.size.y);Capture(camera,"pillar-"+r.name);}
    camera.transform.position=position;camera.orthographicSize=size;
    File.WriteAllLines("scripts/ancient-forest-evidence/renderers.tsv",Object.FindObjectsOfType<SpriteRenderer>().Select(r=>r.name+"\t"+r.transform.position+"\t"+(r.sprite==null?"NULL":AssetDatabase.GetAssetPath(r.sprite))));
    File.WriteAllText("scripts/ancient-forest-evidence/play.txt","Actual Play camera and pillar heights captured; no traversal or collection test");EditorApplication.isPlaying=false;}return;
  }
  if(File.Exists("scripts/ancient-forest-review.request")&&!EditorApplication.isPlayingOrWillChangePlaymode){
   File.Delete("scripts/ancient-forest-review.request");
   try{
    if(SceneManager.GetActiveScene().isDirty)throw new System.Exception("Unsaved active scene; preserving edits, review blocked");
    AssetDatabase.Refresh();EditorSceneManager.OpenScene("Assets/Scenes/OneOneScene.unity");
    var all=Object.FindObjectsOfType<SpriteRenderer>();var ground=all.Where(r=>r.name.StartsWith("GroundTile")).ToArray();
    var stairs=all.Where(r=>r.name.StartsWith("StairsBlock")).ToArray();var boxes=stairs.Select(r=>r.bounds).ToArray();
    foreach(var r in stairs){var b=r.bounds;System.Func<Vector3,bool> has=p=>boxes.Any(v=>Vector3.Distance(v.center,p)<.04f);
     bool above=has(b.center+Vector3.up*b.size.y),below=has(b.center-Vector3.up*b.size.y),left=has(b.center-Vector3.right*b.size.x),right=has(b.center+Vector3.right*b.size.x);
     int tile=!above?(!left?27:!right?58:24):!below?(!left?26:!right?11:43):(!left?44:!right?17:31);
     r.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/AncientForestLevel01/terrain-SeamSafe-Terrain-grass-"+tile+".png");
     EditorUtility.SetDirty(r);if(PrefabUtility.IsPartOfPrefabInstance(r))PrefabUtility.RecordPrefabInstancePropertyModifications(r);
    }
    EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
    if(ground.Length==0)throw new System.Exception("No ground found");
    foreach(var r in ground)if(r.sprite==null||!AssetDatabase.GetAssetPath(r.sprite).StartsWith("Assets/Art/AncientForestLevel01/"))throw new System.Exception("Unreplaced ground "+r.name);
    var view=SceneView.lastActiveSceneView;
    if(view!=null){view.LookAt(new Vector3(10,-1,0),Quaternion.identity,8,true);view.Repaint();}
    var review=new GameObject("Temporary forest Scene review camera");var sceneCamera=review.AddComponent<Camera>();sceneCamera.orthographic=true;sceneCamera.orthographicSize=6;sceneCamera.transform.position=new Vector3(12,-2,-20);Capture(sceneCamera,"scene");Object.DestroyImmediate(review);
    File.WriteAllText("scripts/ancient-forest-evidence/editor.txt","Editor imported and compiled; ground renderers="+ground.Length+"; sprites resolve. "+System.DateTime.UtcNow.ToString("O"));
    File.WriteAllText("scripts/ancient-forest-playing.request","");frames=0;EditorApplication.isPlaying=true;
   }catch(System.Exception e){File.WriteAllText("scripts/ancient-forest-evidence/error.txt",e.ToString());}
  }
  if(File.Exists("scripts/ancient-forest-guids.request")){
   File.Delete("scripts/ancient-forest-guids.request");
   Directory.CreateDirectory("scripts/ancient-forest-evidence");
   File.WriteAllLines("scripts/ancient-forest-evidence/guids.tsv",AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/")).Select(p=>p+"\t"+AssetDatabase.AssetPathToGUID(p)));
  }
 }
}
