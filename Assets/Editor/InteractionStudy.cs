using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Block;
using Enemy;
using Character.Control;
using Character.Lifecycle;
using Character.PowerUp;
[InitializeOnLoad] public static class InteractionStudy {
 static int step;static double due;static string log;static QuestionMarkBlock block;static GameObject contact;static ArmsSimulation arms;static int force,torque,coins;static float baseScale;static Transform cauldron;static EnemyDeath koopa;static GameObject shell;static float shellX;
 static BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 static InteractionStudy(){EditorApplication.delayCall+=Request;EditorApplication.playModeStateChanged+=Change;}
 static void Request(){if(!File.Exists("scripts/interaction-study.request")||EditorApplication.isPlayingOrWillChangePlaymode)return;if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)return;File.Delete("scripts/interaction-study.request");EditorSceneManager.OpenScene("Assets/Scenes/OneOneScene.unity");SessionState.SetBool("InteractionStudy",true);EditorApplication.EnterPlaymode();}
 static void Change(PlayModeStateChange s){if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("InteractionStudy",false)){SessionState.SetBool("InteractionStudy",false);GameMenuUI.Testing=true;GameMenuUI.PreferencePrefix="Goombas.Campaign.TEST."+Guid.NewGuid()+".";step=0;log="Actual Play collision/state observation; isolated saves. Contact probe is Character-layer physics, not a mouse-controlled traversal.\n";due=EditorApplication.timeSinceStartup+2;EditorApplication.update+=Tick;}}
 static void Check(bool yes,string m){log+=(yes?"PASS ":"FAIL ")+m+"\n";File.WriteAllText("scripts/interaction-study/results.txt",log);if(!yes)throw new Exception(m);}
 static int Coins()=> (int)typeof(Winning.CoinPurse).GetField("_count",F).GetValue(UnityEngine.Object.FindObjectOfType<Winning.CoinPurse>());
 static void Contact(QuestionMarkBlock b){block=b;var col=b.GetComponent<Collider2D>();contact=new GameObject("Isolated Character layer contact probe");contact.layer=LayerMask.NameToLayer("Character");contact.transform.position=col.bounds.center;contact.AddComponent<BoxCollider2D>().size=Vector2.one*.15f;var rb=contact.AddComponent<Rigidbody2D>();rb.gravityScale=0;rb.constraints=RigidbodyConstraints2D.FreezeAll;Physics2D.SyncTransforms();foreach(var v in UnityEngine.Object.FindObjectsOfType<Cinemachine.CinemachineVirtualCamera>())v.enabled=false;Camera.main.transform.position=new Vector3(col.bounds.center.x,col.bounds.center.y,-10);}
 static bool Touched()=> (bool)typeof(QuestionMarkBlock).GetField("_touched",F).GetValue(block);
 static void Capture(string n)=>ScreenCapture.CaptureScreenshot(Path.GetFullPath("scripts/interaction-study/"+n+".png"));
 static void Tick(){if(!EditorApplication.isPlaying||EditorApplication.timeSinceStartup<due)return;due=EditorApplication.timeSinceStartup+.65;try{switch(step++){
 case 0:arms=UnityEngine.Object.FindObjectOfType<ArmsSimulation>();force=arms.MaxBodyForce;torque=arms.MaxTorque;arms.enabled=false;foreach(var b in UnityEngine.Object.FindObjectOfType<Death>().transform.parent.GetComponentsInChildren<Rigidbody2D>()){b.gravityScale=0;b.constraints=RigidbodyConstraints2D.FreezeAll;}coins=Coins();Contact(UnityEngine.Object.FindObjectsOfType<CoinBlock>().First());break;
 case 1:Check(Touched()&&Coins()==coins+1,"Physics contact activates coin block once");Capture("coin-contact");UnityEngine.Object.Destroy(contact);break;
 case 2:Contact(block);break;
 case 3:Check(Coins()==coins+1,"Repeated physical contact does not duplicate coin reward");UnityEngine.Object.Destroy(contact);Contact(UnityEngine.Object.FindObjectsOfType<StarBlock>().First());break;
 case 4:Check(Touched()&&arms.MaxBodyForce==force*3&&arms.MaxTorque==torque*3,"Star block immediately grants 3x force and torque");Capture("strength");UnityEngine.Object.Destroy(contact);due=EditorApplication.timeSinceStartup+11;break;
 case 5:Check(arms.MaxBodyForce==force&&arms.MaxTorque==torque,"Strength expires and restores base values");var growth=UnityEngine.Object.FindObjectOfType<TemporaryGrowth>();cauldron=(Transform)typeof(TemporaryGrowth).GetField("cauldron",F).GetValue(growth);baseScale=cauldron.localScale.x;Contact(UnityEngine.Object.FindObjectsOfType<MushroomBlock>().First());due=EditorApplication.timeSinceStartup+1.5;break;
 case 6:Check(Touched()&&Mathf.Abs(cauldron.localScale.x/baseScale-1.5f)<.02f,"Mushroom contact enlarges cauldron to 1.5x");Capture("growth");UnityEngine.Object.Destroy(contact);due=EditorApplication.timeSinceStartup+12;break;
 case 7:Check(Mathf.Abs(cauldron.localScale.x-baseScale)<.01f,"Growth expires and restores scale");koopa=UnityEngine.Object.FindObjectsOfType<EnemyDeath>().First(x=>x.GetComponentsInChildren<KoopaTroopaShellContactDetection>(true).Length>0);koopa.OnEnemyDeath+=g=>shell=g;koopa.SendMessage("DieIfSelf",koopa.gameObject);Check(shell!=null&&shell.activeInHierarchy,"Koopa top-contact handler creates active shell");shellX=koopa.transform.position.x;Camera.main.transform.position=new Vector3(shellX,koopa.transform.position.y,-10);break;
 case 8:Check(Mathf.Abs(koopa.transform.position.x-shellX)>.05f,"Shell actually moves during Play");Capture("shell-moving");koopa.Defeat();Check(!shell.GetComponent<KoopaTroopaShellMovement>().enabled,"True defeat disables shell movement");due=EditorApplication.timeSinceStartup+4.5;break;
 case 9:Check(koopa==null,"Defeated shell cleans up");log+="NOT RUN: manual mouse traversal, actual stomp entry and shell-victim collision.\n";Finish(null);break;
 }}catch(Exception e){Finish(e);}}
 static void Finish(Exception e){EditorApplication.update-=Tick;if(e!=null)log+="FAIL "+e;File.WriteAllText("scripts/interaction-study/results.txt",log);EditorApplication.ExitPlaymode();}
}
