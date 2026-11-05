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
    if(SceneManager.GetActiveScene().isDirty)throw new System.Exception("Unsaved active scene "+SceneManager.GetActiveScene().name+"; preserving edits, review blocked");
    AssetDatabase.Refresh();EditorSceneManager.OpenScene("Assets/Scenes/OneOneScene.unity");
    var depth=Camera.main.GetComponent<AncientForestDepth>();if(depth==null)depth=Camera.main.gameObject.AddComponent<AncientForestDepth>();
    depth.undergrowth=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/AncientForestLevel01/undergrowth-layer.png");depth.foreground=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/AncientForestLevel01/foreground-layer.png");EditorUtility.SetDirty(depth);
    var all=Object.FindObjectsOfType<SpriteRenderer>();var ground=all.Where(r=>r.name.StartsWith("GroundTile")).ToArray();
    var previous=GameObject.Find("Ancient Forest dressing");if(previous!=null)Object.DestroyImmediate(previous);
    all=Object.FindObjectsOfType<SpriteRenderer>();
    var dressing=new GameObject("Ancient Forest dressing");
    foreach(var r in all.Where(r=>r.name.StartsWith("Bush"))){var b=r.bounds;r.enabled=false;EditorUtility.SetDirty(r);if(PrefabUtility.IsPartOfPrefabInstance(r))PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
    var terrainBounds=ground.Select(r=>r.GetComponent<Collider2D>().bounds).ToArray();
    var tops=terrainBounds.Where(b=>!terrainBounds.Any(o=>Mathf.Abs(o.center.x-b.center.x)<.03f&&Mathf.Abs(o.min.y-b.max.y)<.03f)).OrderBy(b=>b.center.x).ToArray();
    for(int i=5;i<tops.Length;i+=11){var b=tops[i];string[] names={"fern","fern","rocks","lamp","fern","statue","ruins"};var name=names[(i/11)%names.Length];
     var go=new GameObject("Forest "+name);go.transform.SetParent(dressing.transform);var sr=go.AddComponent<SpriteRenderer>();sr.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/AncientForestLevel01/"+name+".png");
     float height=name=="lamp"?2.7f:name=="statue"?1.7f:name=="bush"?1.1f:name=="fern"?.4f:1.3f;float k=height/sr.sprite.bounds.size.y;go.transform.localScale=Vector3.one*k;
     go.transform.position=new Vector3(b.center.x,b.max.y+height*.5f-.05f,0);sr.sortingOrder=name=="bush"?20:-5;
    }
    var stairs=all.Where(r=>r.name.StartsWith("StairsBlock")).ToArray();var boxes=stairs.Select(r=>r.bounds).ToArray();
    foreach(var r in stairs){var b=r.bounds;System.Func<Vector3,bool> has=p=>boxes.Any(v=>Vector3.Distance(v.center,p)<.04f);
     bool above=has(b.center+Vector3.up*b.size.y),below=has(b.center-Vector3.up*b.size.y),left=has(b.center-Vector3.right*b.size.x),right=has(b.center+Vector3.right*b.size.x);
     int tile=!above?(!left?27:!right?58:24):!below?(!left?26:!right?11:43):(!left?44:!right?17:31);
     r.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/AncientForestLevel01/terrain-SeamSafe-Terrain-grass-"+tile+".png");
     r.drawMode=SpriteDrawMode.Simple;
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
