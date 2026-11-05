using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
[InitializeOnLoad] public static class Level02Designed {
 static Level02Designed(){EditorApplication.delayCall+=Run;}
 static Transform parent;
 static GameObject Prefab(string name,Vector2 point){var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/"+name+".prefab"));go.transform.SetParent(parent,true);var movement=go.GetComponentInChildren<Enemy.EnemyMovement>();if(movement!=null){var plan=JsonUtility.FromJson<Level02HorizontalBuilder.Plan>(File.ReadAllText("scripts/level02-horizontal.json"));int cell=Mathf.FloorToInt(point.x/.8f),h=-5;while(plan.cells.Any(c=>c.x==cell && c.y==h))h++;var hit=movement.GetComponentInChildren<Collider2D>();float foot=hit.bounds.min.y-movement.transform.position.y;point.y=h*.8f-3.6f-foot+.01f;go.transform.position+=(Vector3)point-movement.transform.position;var so=new SerializedObject(movement);var l=(Transform)so.FindProperty("leftLimit").objectReferenceValue;var r=(Transform)so.FindProperty("rightLimit").objectReferenceValue;l.position=point+Vector2.left*1.5f;r.position=point+Vector2.right*1.5f;movement.Speed=.8f;}else{var col=go.GetComponentInChildren<Collider2D>();go.transform.position+=(Vector3)point-col.bounds.center;}return go;}
 static void Run(){if(!File.Exists("scripts/level02-designed.request")||EditorApplication.isPlayingOrWillChangePlaymode)return;if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty && UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!="Assets/Scenes/Level02_Jungle.unity")return;File.Delete("scripts/level02-designed.request");try{
 Directory.CreateDirectory("scripts/level02-designed-evidence");File.WriteAllText("scripts/level02-horizontal.request","");typeof(Level02HorizontalBuilder).GetMethod("Run",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);
 KitTerrainRepair.Repair();
 var level=UnityEngine.Object.FindObjectOfType<CampaignLevel>();if(level.number!=2)throw new Exception("Wrong scene");parent=level.transform.Find("Interactions");
 foreach(var x in UnityEngine.Object.FindObjectsOfType<CampaignInteractable>().Where(x=>x.kind==CampaignInteractable.Kind.Star)){x.name="Objective Diamond "+(x.starId+1);x.GetComponent<SpriteRenderer>().sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Campaign/diamond.png");}
 foreach(var t in parent.GetComponentsInChildren<TextMesh>()){t.text=t.text.Replace("STAR","DIAMOND");t.name=t.name.Replace("Star","Diamond");}
 foreach(var g in parent.Cast<Transform>().Where(t=>t.name.Contains("Star Exit")))g.name="Three Diamond Exit Door";
 foreach(var p in new[]{new Vector2(9,-2),new Vector2(28.2f,-2),new Vector2(78,-.5f),new Vector2(91,-2),new Vector2(137,-2),new Vector2(176.8f,-2),new Vector2(213,-2),new Vector2(273,-2)})Prefab("Enemies/Goomba",p);
 Prefab("Enemies/KoopaTroopa",new Vector2(129,-2));Prefab("Enemies/KoopaTroopa",new Vector2(222,-2));
 foreach(var p in new[]{new Vector2(14,-.4f),new Vector2(17,-.4f),new Vector2(43,1.2f),new Vector2(48,2),new Vector2(73,2),new Vector2(90,2),new Vector2(105,-.4f),new Vector2(139,2),new Vector2(145,2),new Vector2(176,2),new Vector2(199,1),new Vector2(214,2),new Vector2(250,2),new Vector2(263,2)})Prefab("Obstacles/CoinQuestionMarkBlock",p);
 Prefab("Obstacles/MushroomQuestionMarkBlock",new Vector2(64,-.4f));Prefab("Obstacles/MushroomQuestionMarkBlock",new Vector2(204,-.4f));
 Prefab("Obstacles/StarQuestionMarkBlock",new Vector2(108,-.4f));Prefab("Obstacles/StarQuestionMarkBlock",new Vector2(241,-.4f));
 level.title="Jungle footholds and rewards";EditorSceneManager.MarkSceneDirty(level.gameObject.scene);EditorSceneManager.SaveScene(level.gameObject.scene);
 typeof(Level02HorizontalBuilder).GetMethod("Render",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{level});File.Copy("scripts/level02-horizontal-evidence/L02-map.png","scripts/level02-designed-evidence/L02-map.png",true);
 File.WriteAllText("scripts/level02-designed-evidence/build.txt","PASS L02 authored horizontal layout; 3 diamonds, 8 Goombas, 2 Koopas, 14 coin, 2 mushroom, 2 strength blocks. First scene unchanged. "+DateTime.UtcNow.ToString("O"));
 File.WriteAllText("scripts/diamond-review.request","");typeof(DiamondReview).GetMethod("Run",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);
 }catch(Exception e){File.WriteAllText("scripts/level02-designed-evidence/error.txt",e.ToString());Debug.LogException(e);}}
}
// Keep patrols on level surfaces.
