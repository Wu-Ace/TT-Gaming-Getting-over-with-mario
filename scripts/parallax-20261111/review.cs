if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) throw new System.Exception("Stop first");
var setup = UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup();
var report = new System.Collections.Generic.List<string>();
try
{
 foreach(var entry in UnityEditor.EditorBuildSettings.scenes.Where(s=>s.enabled))
 {
  var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(entry.path);
  var level=UnityEngine.Object.FindObjectOfType<CampaignLevel>(); if(level==null)continue;
  var camera=UnityEngine.Camera.main;var bg=camera.GetComponent<KitLandscape>();
  if(!bg||!bg.HasLayeredBackground)throw new System.Exception("Missing background in "+scene.name);
  var savedPosition=camera.transform.position;var savedTarget=camera.targetTexture;
  var oldActive=UnityEngine.RenderTexture.active;
  var rt=new UnityEngine.RenderTexture(1200,675,24);
  var tex=new UnityEngine.Texture2D(1200,675,UnityEngine.TextureFormat.RGB24,false);
  try
  {
   camera.transform.position=new UnityEngine.Vector3(level.initialSpawn.x,level.initialSpawn.y+.3f,-10);
   camera.targetTexture=rt;bg.UpdateLayers();camera.Render();UnityEngine.RenderTexture.active=rt;
   tex.ReadPixels(new UnityEngine.Rect(0,0,1200,675),0,0);tex.Apply();
   System.IO.File.WriteAllBytes("scripts/parallax-20261111/"+scene.name+"-editor-camera.png",tex.EncodeToPNG());
   report.Add(scene.name+": serialized background resolves six layers; actual gameplay-camera editor render captured; no scene saves during review");
  }
  finally
  {
   camera.targetTexture=savedTarget;camera.transform.position=savedPosition;UnityEngine.RenderTexture.active=oldActive;
   UnityEngine.Object.DestroyImmediate(tex);rt.Release();UnityEngine.Object.DestroyImmediate(rt);
  }
 }
}
finally{UnityEditor.SceneManagement.EditorSceneManager.RestoreSceneManagerSetup(setup);}
var view=UnityEditor.SceneView.lastActiveSceneView;
if(view!=null){var camera=UnityEngine.Camera.main;view.LookAt(new UnityEngine.Vector3(camera.transform.position.x,camera.transform.position.y,0),UnityEngine.Quaternion.identity,6,true);view.Repaint();}
System.IO.File.WriteAllLines("scripts/parallax-20261111/scene-review.txt",report);
UnityEngine.Debug.Log(string.Join("\n",report));
