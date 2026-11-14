using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
var output="scripts/finish-flag-20261111";
var setup=EditorSceneManager.GetSceneManagerSetup();
if(setup.Any(s=>UnityEngine.SceneManagement.SceneManager.GetSceneByPath(s.path).isDirty))throw new Exception("Unsaved scene");
var sprites=new Sprite[2];
for(int i=0;i<2;i++){
 var path="Assets/Art/FinishFlag/"+(i==0?"Flag_down":"Flag_up")+".png";
 var t=(TextureImporter)AssetImporter.GetAtPath(path);t.textureType=TextureImporterType.Sprite;t.spriteImportMode=SpriteImportMode.Single;t.spritePixelsPerUnit=100;t.mipmapEnabled=false;t.alphaIsTransparency=true;t.textureCompression=TextureImporterCompression.Uncompressed;
 var settings=new TextureImporterSettings();t.ReadTextureSettings(settings);settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spritePivot=new Vector2(91f/345f,0);t.SetTextureSettings(settings);t.SaveAndReimport();sprites[i]=AssetDatabase.LoadAssetAtPath<Sprite>(path);
}
var report=new System.Collections.Generic.List<string>();
try{
foreach(var entry in EditorBuildSettings.scenes.Where(s=>s.enabled && !s.path.EndsWith("MainMenu.unity"))){
 var path=entry.path;File.Copy(path,output+"/backup/"+Path.GetFileName(path),true);
 var scene=EditorSceneManager.OpenScene(path);
 foreach(var goal in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<CampaignInteractable>(true)).Where(c=>c.kind==CampaignInteractable.Kind.Door)){
  var old=goal.GetComponent<SpriteRenderer>();var bounds=old.bounds;
  var colliders=goal.GetComponents<Collider2D>().Select(c=>EditorJsonUtility.ToJson(c)).ToArray();
  var go=new GameObject("Finish Flag Visual");go.transform.SetParent(goal.transform,false);go.transform.position=new Vector3(bounds.center.x,bounds.min.y,goal.transform.position.z);
  float scale=bounds.size.y/4.06f;var parentScale=goal.transform.lossyScale;go.transform.localScale=new Vector3(scale/parentScale.x,scale/parentScale.y,1);
  var visual=go.AddComponent<SpriteRenderer>();visual.sprite=sprites[0];visual.sortingLayerID=old.sortingLayerID;visual.sortingOrder=old.sortingOrder;visual.color=Color.white;
  old.enabled=false;goal.goalVisual=visual;goal.inactiveSprite=sprites[0];goal.activeSprite=sprites[1];EditorUtility.SetDirty(goal);
  if(!colliders.SequenceEqual(goal.GetComponents<Collider2D>().Select(c=>EditorJsonUtility.ToJson(c))))throw new Exception("Collider changed");
  report.Add(path+" | "+goal.name+" | height="+bounds.size.y+" | collider unchanged");
 }
 EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
}
}finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
AssetDatabase.SaveAssets();File.WriteAllLines(output+"/applied.txt",report);
var selected=UnityEngine.Object.FindObjectsOfType<CampaignInteractable>().First(c=>c.kind==CampaignInteractable.Kind.Door);Selection.activeGameObject=selected.goalVisual.gameObject;SceneView.lastActiveSceneView.FrameSelected();
return string.Join("\n",report);
