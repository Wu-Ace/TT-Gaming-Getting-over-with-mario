using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
[InitializeOnLoad] public static class Level02Designed {
 static Level02Designed(){EditorApplication.delayCall+=Run;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredEditMode)EditorApplication.delayCall+=Run;};}
 static Transform parent;
 static GameObject Prefab(string name,Vector2 point){var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/"+name+".prefab"));go.transform.SetParent(parent,true);var movement=go.GetComponentInChildren<Enemy.EnemyMovement>();if(movement!=null){var plan=JsonUtility.FromJson<Level02HorizontalBuilder.Plan>(File.ReadAllText("scripts/level02-horizontal.json"));int cell=Mathf.FloorToInt(point.x/.8f),h=-5;while(plan.cells.Any(c=>c.x==cell && c.y==h))h++;var hit=movement.GetComponentInChildren<Collider2D>();float foot=hit.bounds.min.y-movement.transform.position.y;point.y=h*.8f-3.6f-foot+.01f;go.transform.position+=(Vector3)point-movement.transform.position;var so=new SerializedObject(movement);var l=(Transform)so.FindProperty("leftLimit").objectReferenceValue;var r=(Transform)so.FindProperty("rightLimit").objectReferenceValue;l.position=point+Vector2.left*1.5f;r.position=point+Vector2.right*1.5f;movement.Speed=.8f;}else{var col=go.GetComponentInChildren<Collider2D>();go.transform.position+=(Vector3)point-col.bounds.center;}return go;}
 static void Run(){if(!File.Exists("scripts/level02-designed.request")||EditorApplication.isPlayingOrWillChangePlaymode)return;if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty && UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!="Assets/Scenes/Level02_Jungle.unity")return;File.Delete("scripts/level02-designed.request");try{
 Directory.CreateDirectory("scripts/level02-designed-evidence");File.WriteAllText("scripts/level02-horizontal.request","");typeof(Level02HorizontalBuilder).GetMethod("Run",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);
 KitTerrainRepair.Repair();
 var level=UnityEngine.Object.FindObjectOfType<CampaignLevel>();if(level.number!=2)throw new Exception("Wrong scene");parent=level.transform.Find("Interactions");
 foreach(var x in UnityEngine.Object.FindObjectsOfType<CampaignInteractable>().Where(x=>x.kind==CampaignInteractable.Kind.Star)){x.name="Objective Diamond "+(x.starId+1);x.GetComponent<SpriteRenderer>().sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Campaign/diamond.png");}
 foreach(var t in parent.GetComponentsInChildren<TextMesh>()){t.text=t.text.Replace("STAR","DIAMOND");t.name=t.name.Replace("Star","Diamond");}
 foreach(var g in parent.Cast<Transform>().Where(t=>t.name.Contains("Star Exit")))g.name="Three Diamond Exit Door";
 // Place encounters on actual reference ledges, with bounded patrols.
 foreach(var p in new[]{new Vector2(6,-2),new Vector2(22,-2),new Vector2(35,-2),new Vector2(64,-2),new Vector2(89,-2),new Vector2(111,-2),new Vector2(132,-2)})Prefab("Enemies/Goomba",p);
 Prefab("Enemies/KoopaTroopa",new Vector2(10,-2));
 foreach(var p in new[]{new Vector2(10,-.4f),new Vector2(28,2),new Vector2(54,-.4f),new Vector2(110,-.4f),new Vector2(136,-.4f)})Prefab("Obstacles/CoinQuestionMarkBlock",p);
 // Repeated native terrain cells move together; colliders are authored separately.
 var flags=BindingFlags.NonPublic|BindingFlags.Static;
 foreach(var spec in new[]{new Vector3(57,3,0),new Vector3(89,1,.25f),new Vector3(97,0,.5f),new Vector3(131,3,.75f)}){
  var platform=new GameObject("Vertical moving platform");platform.layer=6;platform.transform.SetParent(parent,false);
  platform.transform.position=new Vector3((spec.x+1.5f)*.8f,(spec.y+.5f)*.8f-3.6f,0);
  for(int i=0;i<3;i++){
   var tileMethod=typeof(CampaignBuilder).GetMethod("Tile",flags);
   tileMethod.Invoke(null,new object[]{i==0?"tl":i==2?"tr":"top",(int)spec.x+i,(int)spec.y,platform.transform});
  }
  var col=platform.AddComponent<BoxCollider2D>();col.size=new Vector2(2.4f,.8f);
  var body=platform.AddComponent<Rigidbody2D>();body.bodyType=RigidbodyType2D.Kinematic;body.useFullKinematicContacts=true;body.interpolation=RigidbodyInterpolation2D.Interpolate;
  var ride=platform.AddComponent<VerticalRidePlatform>();ride.travel=3.2f;ride.period=6;ride.phase=spec.z;
 }
 foreach(var r in level.transform.Find("Terrain").GetComponentsInChildren<SpriteRenderer>().Concat(parent.GetComponentsInChildren<VerticalRidePlatform>().SelectMany(m=>m.GetComponentsInChildren<SpriteRenderer>()))) {
 r.sprite=(Sprite)typeof(KitTerrainRepair).GetMethod("SeamSafeSprite",flags).Invoke(null,new object[]{r.sprite});
 r.transform.localScale=Vector3.one*5;
 }
 level.initialSpawn=new Vector2(2,-2.9f);
 level.title="Jungle moving pillar crossing";EditorSceneManager.MarkSceneDirty(level.gameObject.scene);EditorSceneManager.SaveScene(level.gameObject.scene);
 typeof(Level02HorizontalBuilder).GetMethod("Render",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{level});File.Copy("scripts/level02-horizontal-evidence/L02-map.png","scripts/level02-designed-evidence/L02-map.png",true);
 File.WriteAllText("scripts/level02-designed-evidence/build.txt","L02 reference layout generated; 3 diamonds, four vertical platforms. Play validation pending. "+DateTime.UtcNow.ToString("O"));
 EditorSceneManager.OpenScene("Assets/Scenes/Level02_Jungle.unity");
 }catch(Exception e){File.WriteAllText("scripts/level02-designed-evidence/error.txt",e.ToString());Debug.LogException(e);}}
}
// Keep patrols on level surfaces.

// Reference revision final refresh.
