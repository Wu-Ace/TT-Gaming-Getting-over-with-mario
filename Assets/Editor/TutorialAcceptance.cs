using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Character.Lifecycle;
[InitializeOnLoad] public static class TutorialAcceptance {
 static int errors;static int stage;static double due;static string log="";static Death body;static CampaignInteractable[] gems;static CampaignInteractable door;
 static TutorialAcceptance(){EditorApplication.delayCall+=Run;EditorApplication.playModeStateChanged+=Changed;}
 static void Run(){if(!File.Exists("scripts/tutorial-test.request")||!File.Exists("Assets/Scenes/Tutorial.unity")||EditorApplication.isPlayingOrWillChangePlaymode)return;File.Delete("scripts/tutorial-test.request");EditorSceneManager.OpenScene("Assets/Scenes/Tutorial.unity");SessionState.SetBool("TutorialTest",true);EditorApplication.EnterPlaymode();}
 static void Changed(PlayModeStateChange s){if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("TutorialTest",false)){SessionState.SetBool("TutorialTest",false);GameMenuUI.Testing=true;GameMenuUI.PreferencePrefix="Goombas.Tutorial.TEST."+Guid.NewGuid()+".";errors=0;Application.logMessageReceived+=(m,t,k)=>{if(k==LogType.Exception||k==LogType.Error)errors++;};stage=0;due=EditorApplication.timeSinceStartup+3;EditorApplication.update+=Tick;}if(s==PlayModeStateChange.EnteredEditMode){EditorApplication.update-=Tick;GameMenuUI.PreferencePrefix=GameMenuUI.Pref;GameMenuUI.Testing=false;}}
 static void Check(bool p,string m){log+=(p?"PASS ":"FAIL ")+m+"\n";File.WriteAllText("scripts/tutorial-evidence/play.txt",log);if(!p)throw new Exception(m);}
 static void Move(Vector2 p){var d=p-(Vector2)body.transform.position;foreach(var r in body.transform.parent.GetComponentsInChildren<Rigidbody2D>()){r.position+=d;r.velocity=Vector2.zero;r.angularVelocity=0;}Physics2D.SyncTransforms();}
 static void Tick(){if(!EditorApplication.isPlaying||EditorApplication.timeSinceStartup<due)return;due=EditorApplication.timeSinceStartup+.8;try{
 var l=CampaignLevel.Current;
 if(stage==0){Check(GameMenuUI.PreferencePrefix.Contains(".TEST."),"isolated save prefix");foreach(var t in UnityEngine.Object.FindObjectsOfType<LiveGifts.GiftKeyboardTester>())typeof(LiveGifts.GiftKeyboardTester).GetField("visible",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(t,false);body=UnityEngine.Object.FindObjectOfType<Death>();UnityEngine.Object.FindObjectOfType<Character.Control.ArmsSimulation>().enabled=false;foreach(var r in body.transform.parent.GetComponentsInChildren<Rigidbody2D>()){r.gravityScale=0;r.velocity=Vector2.zero;}gems=UnityEngine.Object.FindObjectsOfType<CampaignInteractable>().Where(i=>i.kind==CampaignInteractable.Kind.Star).OrderBy(i=>i.starId).ToArray();door=UnityEngine.Object.FindObjectsOfType<CampaignInteractable>().Single(i=>i.kind==CampaignInteractable.Kind.Door);Check(l.number==0&&gems.Length==3&&gems.All(g=>g.transform.position.x<l.initialSpawn.x)&&door.transform.position.x>l.initialSpawn.x,"three left gems and right door");ScreenCapture.CaptureScreenshot(System.IO.Path.GetFullPath("scripts/tutorial-evidence/game-start.png"));Move(door.transform.position);}
 else if(stage==1){Check(!l.Won&&door.blocker.activeSelf,"zero gems locked");Move(gems[0].transform.position);}
 else if(stage==2){Check(l.StarCount==1&&!l.Collect(0),"first gem and duplicate suppression");Move(door.transform.position);}
 else if(stage==3){Check(!l.Won&&door.blocker.activeSelf,"one gem locked");Move(gems[1].transform.position);}
 else if(stage==4){Check(l.StarCount==2,"second gem collected");Move(door.transform.position);}
 else if(stage==5){Check(!l.Won&&door.blocker.activeSelf,"two gems locked");Move(gems[2].transform.position);}
 else if(stage==6){Check(l.StarCount==3&&!l.Won&&!door.blocker.activeSelf,"three gems open door without remote win");Move(door.transform.position);}
 else if(stage==7){Check(l.Won,"body enters unlocked door and wins");Check(l.nextScene=="Assets/Scenes/OneOneScene.unity","next is original first level");ScreenCapture.CaptureScreenshot(System.IO.Path.GetFullPath("scripts/tutorial-evidence/game-win.png"));}
 else if(stage==8){SceneManager.LoadScene(l.nextScene);}
 else if(stage==9){Check(l.number==1,"original first level loads");Check(errors==0,"no runtime errors or exceptions");EditorApplication.ExitPlaymode();}
 stage++;
 }catch(Exception e){Debug.LogException(e);EditorApplication.ExitPlaymode();}}
}



