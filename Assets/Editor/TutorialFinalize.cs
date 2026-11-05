using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
[InitializeOnLoad] public static class TutorialFinalize {
 static TutorialFinalize(){EditorApplication.delayCall+=Run;}
 static void Run(){if(!File.Exists("scripts/tutorial-finalize.request")||EditorApplication.isPlayingOrWillChangePlaymode)return;File.Delete("scripts/tutorial-finalize.request");EditorSceneManager.OpenScene("Assets/Scenes/Tutorial.unity");var level=UnityEngine.Object.FindObjectOfType<CampaignLevel>();
 if(UnityEngine.Object.FindObjectOfType<Winning.WinThresholdDetection>()==null){var win=new GameObject("Tutorial Win Events");win.transform.SetParent(level.transform);win.AddComponent<Winning.WinThresholdDetection>();}
 var threshold=new GameObject("Tutorial Death Threshold");threshold.transform.SetParent(level.transform);threshold.transform.position=new Vector3(0,-12,0);
 var body=UnityEngine.Object.FindObjectOfType<Character.Lifecycle.Death>();var fall=new SerializedObject(body.GetComponent<Character.Lifecycle.FallDetection>());fall.FindProperty("deathThreshold").objectReferenceValue=threshold.transform;fall.ApplyModifiedPropertiesWithoutUndo();
 var native=new System.Collections.Generic.Dictionary<string,int>{{"tl",27},{"tr",58},{"top",24},{"bl",26},{"br",11},{"bottom",43}};
 foreach(var r in level.transform.Find("Terrain").GetComponentsInChildren<SpriteRenderer>()) {var role=r.name.Split(' ').Last();var sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/PlatformerKit/Terrain/grass-"+native[role]+".png");r.sprite=(Sprite)typeof(KitTerrainRepair).GetMethod("SeamSafeSprite",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{sprite});r.transform.localScale=Vector3.one*5;}
 // Place the edit-time rig at the same spawn used in Play.
 body.transform.parent.position+=(Vector3)level.initialSpawn-body.transform.position;
 foreach(var test in UnityEngine.Object.FindObjectsOfType<LiveGifts.GiftKeyboardTester>()){var so=new SerializedObject(test);var prop=so.FindProperty("visible");if(prop!=null){prop.boolValue=false;so.ApplyModifiedPropertiesWithoutUndo();}}
 Selection.activeGameObject=level.gameObject;if(SceneView.lastActiveSceneView!=null)SceneView.lastActiveSceneView.LookAt(new Vector3(0,-1,0),Quaternion.identity,8,true);
 EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());File.WriteAllText("scripts/tutorial-evidence/finalize.txt","Fall threshold restored; native topology and extruded gutter; spawn aligned.");
 }
}

