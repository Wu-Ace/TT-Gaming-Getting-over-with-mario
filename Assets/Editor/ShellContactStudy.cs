using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Enemy;
using Character.Lifecycle;
[InitializeOnLoad] public static class ShellContactStudy {
 static bool hit;static int step;static double due;static string log;static GameObject probe,kroot,groot,shell;static EnemyDeath koopa,target;static float startX;
 static ShellContactStudy(){EditorApplication.delayCall+=Run;EditorApplication.playModeStateChanged+=Changed;}
 static void Run(){if(!File.Exists("scripts/shell-contact.request")||EditorApplication.isPlayingOrWillChangePlaymode)return;File.Delete("scripts/shell-contact.request");EditorSceneManager.OpenScene("Assets/Scenes/OneOneScene.unity");SessionState.SetBool("ShellContactStudy",true);EditorApplication.EnterPlaymode();}
 static void Changed(PlayModeStateChange s){if(s!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool("ShellContactStudy",false))return;SessionState.SetBool("ShellContactStudy",false);GameMenuUI.Testing=true;GameMenuUI.PreferencePrefix="Goombas.Shell.TEST."+Guid.NewGuid()+".";step=0;hit=false;log="Actual physics top-contact and shell-enemy collision; isolated contact probe, not manual character traversal.\n";due=EditorApplication.timeSinceStartup+2;EditorApplication.update+=Tick;}
 static void Check(bool b,string s){log+=(b?"PASS ":"FAIL ")+s+"\n";if(!b)throw new Exception(s);}
 static GameObject Spawn(string path,Vector2 p){var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/"+path+".prefab"));var m=root.GetComponentInChildren<EnemyMovement>();root.transform.position+=(Vector3)p-m.transform.position;m.Speed=0;return root;}
 static void Tick(){if(!EditorApplication.isPlaying||EditorApplication.timeSinceStartup<due)return;due=EditorApplication.timeSinceStartup+.8;try{switch(step++){
 case 0:UnityEngine.Object.FindObjectOfType<Character.Control.ArmsSimulation>().enabled=false;foreach(var rb in UnityEngine.Object.FindObjectOfType<Death>().transform.parent.GetComponentsInChildren<Rigidbody2D>()){rb.gravityScale=0;rb.constraints=RigidbodyConstraints2D.FreezeAll;}probe=new GameObject("Physics stomp contact probe");probe.layer=LayerMask.NameToLayer("Character");var detector=probe.AddComponent<EnemyContactDetection>();detector.OnEnemyTopContactDetected+=e=>log+="TOP CONTACT "+e.name+" rb="+e.GetComponent<Rigidbody2D>()+"\n";detector.OnEnemyDeathContactDetected+=()=>log+="SIDE CONTACT\n";probe.AddComponent<BoxCollider2D>().size=Vector2.one*.3f;var pb=probe.AddComponent<Rigidbody2D>();pb.constraints=RigidbodyConstraints2D.FreezeRotation;pb.gravityScale=1;
 kroot=Spawn("KoopaTroopa",new Vector2(0,20));koopa=kroot.GetComponentInChildren<EnemyDeath>();koopa.OnEnemyDeath+=g=>shell=g;groot=Spawn("Goomba",new Vector2(8,20));target=groot.GetComponentInChildren<EnemyDeath>();target.OnEnemyDeath+=_=>hit=true;Physics2D.SyncTransforms();var col=koopa.GetComponentInChildren<Collider2D>();probe.transform.position=new Vector3(col.bounds.center.x,col.bounds.max.y+1,0);Physics2D.SyncTransforms();foreach(var cam in UnityEngine.Object.FindObjectsOfType<Cinemachine.CinemachineVirtualCamera>())cam.enabled=false;Camera.main.transform.position=new Vector3(0,20,-10);due=EditorApplication.timeSinceStartup+3;break;
 case 1:log+="probe="+probe.transform.position+" enemy="+koopa.transform.position+" timeScale="+Time.timeScale+"\n";Check(shell!=null&&shell.activeInHierarchy,"Falling collider top contact creates Koopa shell through actual collision handler");UnityEngine.Object.Destroy(probe);startX=koopa.transform.position.x;break;
 case 2:float dx=koopa.transform.position.x-startX;Check(Mathf.Abs(dx)>.1f,"Shell moves after actual stomp");var km=koopa.GetComponent<EnemyMovement>();var gm=target.GetComponent<EnemyMovement>();gm.enabled=false;var kc=shell.GetComponent<Collider2D>();var gc=target.GetComponentInChildren<Collider2D>();var targetCenter=kc.bounds.center+new Vector3(Mathf.Sign(dx)*.65f,0,0);groot.transform.position+=targetCenter-gc.bounds.center;Physics2D.SyncTransforms();ScreenCapture.CaptureScreenshot(Path.GetFullPath("scripts/interaction-study/shell-collision.png"));break;
 case 3:Check(hit && target.GetComponentsInChildren<Collider2D>(true).All(c=>!c.enabled),"Moving shell defeats Goomba through actual physics contact");Finish(null);break;
 }}catch(Exception e){Finish(e);}}
 static void Finish(Exception e){EditorApplication.update-=Tick;if(e!=null)log+="FAIL "+e;File.WriteAllText("scripts/interaction-study/shell-contact.txt",log);EditorApplication.ExitPlaymode();}
}
