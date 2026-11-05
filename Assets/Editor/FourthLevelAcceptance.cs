using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Character.Lifecycle;

[InitializeOnLoad]
public static class FourthLevelAcceptance
{
    static DonutFallPlatform donut; static Vector2 donutOrigin; static bool donutChecked;
    static int levelIndex, stage;
    static double due;
    static string log;
    static string[] paths;
    static CampaignLevel level;
    static Death body;
    static CampaignInteractable[] stars;
    static FourthLevelAcceptance(){EditorApplication.delayCall+=Requested;EditorApplication.playModeStateChanged+=Changed;}
    static void Requested()
    {
        if(!File.Exists("scripts/fourth-level-test.request") || EditorApplication.isPlayingOrWillChangePlaymode)return;
        File.Delete("scripts/fourth-level-test.request");EditorSceneManager.OpenScene("Assets/Scenes/Level04_Town.unity");SessionState.SetBool("FourthLevelAcceptance",true);EditorApplication.EnterPlaymode();
    }
    static void Changed(PlayModeStateChange change)
    {
        if(change==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("FourthLevelAcceptance",false))
        {
            SessionState.SetBool("FourthLevelAcceptance",false);GameMenuUI.Testing=true;GameMenuUI.PreferencePrefix="Goombas.Campaign.TEST."+Guid.NewGuid()+".";paths=EditorBuildSettings.scenes.Where(s=>s.enabled && s.path.EndsWith("/Level04_Town.unity")).Select(s=>s.path).ToArray();
            log="UTC "+DateTime.UtcNow.ToString("O")+"\nAutomated Play trigger/state smoke; teleportation is NOT a hammer traversal.\n";
            SceneManager.sceneLoaded+=Loaded;
            donutChecked=false;levelIndex=0;stage=0;due=EditorApplication.timeSinceStartup+2;EditorApplication.update+=Tick;
        }
        if(change==PlayModeStateChange.EnteredEditMode)EditorApplication.update-=Tick;
    }
    static void Loaded(Scene scene,LoadSceneMode mode) { Freeze(); }
    static void Check(bool pass,string message){log+=(pass?"PASS ":"FAIL ")+message+"\n";File.WriteAllText("scripts/level04-designed-evidence/play-results.txt",log);if(!pass)throw new Exception(message);}
    static void Freeze()
    {
        body=UnityEngine.Object.FindObjectOfType<Death>();
        var tester=UnityEngine.Object.FindObjectsOfType<MonoBehaviour>().FirstOrDefault(m=>m.GetType().Name=="GiftKeyboardTester");
        if(tester!=null)tester.GetType().GetField("visible",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)?.SetValue(tester,false);
        UnityEngine.Object.FindObjectOfType<Character.Control.ArmsSimulation>().enabled=false;
        foreach(var rb in body.transform.parent.GetComponentsInChildren<Rigidbody2D>()){rb.gravityScale=0;rb.velocity=Vector2.zero;rb.angularVelocity=0;}
    }
    static void Move(Vector2 target)
    {
        Vector2 delta=target-(Vector2)body.transform.position;
        foreach(var rb in body.transform.parent.GetComponentsInChildren<Rigidbody2D>()){rb.position+=delta;rb.velocity=Vector2.zero;rb.angularVelocity=0;}
        Physics2D.SyncTransforms();
        foreach(var camera in UnityEngine.Object.FindObjectsOfType<Cinemachine.CinemachineVirtualCamera>())camera.PreviousStateIsValid=false;
    }
    static void Capture(string suffix)
    {ScreenCapture.CaptureScreenshot(Path.GetFullPath($"scripts/level04-designed-evidence/L04-{suffix}.png"));}
    static void Tick()
    {
        if(!EditorApplication.isPlaying || EditorApplication.timeSinceStartup<due)return;
        due=EditorApplication.timeSinceStartup+.55;
        try
        {
            if(stage==0)
            {
                level=CampaignLevel.Current;Check(level!=null && level.number==4,"Scene loads level 3");
                Freeze();
                if(!donutChecked){
                    var all=UnityEngine.Object.FindObjectsOfType<DonutFallPlatform>();Check(all.Length==4,"Four falling platform groups");
                    donut=all.OrderBy(d=>d.transform.position.y).First();donutOrigin=donut.GetComponent<Rigidbody2D>().position;
                    var hit=body.GetComponentsInChildren<Collider2D>().First(c=>CampaignInteractable.Body(c)==body);
                    float foot=body.transform.position.y-hit.bounds.min.y;
                    Move(new Vector2(donutOrigin.x,donut.GetComponent<BoxCollider2D>().bounds.max.y+foot+.08f));
                    body.GetComponent<Rigidbody2D>().gravityScale=1;stage=30;due=EditorApplication.timeSinceStartup+.6;return;
                }
                stars=UnityEngine.Object.FindObjectsOfType<CampaignInteractable>().Where(s=>s.kind==CampaignInteractable.Kind.Star).OrderBy(s=>s.starId).ToArray();
                Check(stars.Length==3 && level.StarCount==0,"Exactly three fresh stars");
                var hammer=body.transform.parent.GetComponentsInChildren<Collider2D>().First(c=>c.attachedRigidbody!=null && c.attachedRigidbody.name=="Hammer");
                stars[0].Touch(hammer);Check(level.StarCount==0,"Hammer cannot collect stars");
                var door=UnityEngine.Object.FindObjectsOfType<CampaignInteractable>().Single(s=>s.kind==CampaignInteractable.Kind.Door);
                Move(door.transform.position);stage=1;
            }
            else if(stage==30){Check(donut.Triggered,"Actual body landing triggers delayed fall");Check(donut.GetComponent<Rigidbody2D>().position.y>donutOrigin.y-.1f,"Platform holds during warning delay");Move(new Vector2(1,-1f));Freeze();stage=31;due=EditorApplication.timeSinceStartup+1;}
            else if(stage==31){Check(donut.Falling && donut.GetComponent<Rigidbody2D>().position.y<donutOrigin.y-1,"Platform falls after delay");stage=32;due=EditorApplication.timeSinceStartup+4;}
            else if(stage==32){Check(!donut.Triggered && Vector2.Distance(donutOrigin,donut.GetComponent<Rigidbody2D>().position)<.01f,"Platform restores origin and rearms");donutChecked=true;stage=0;}
            else if(stage==1)
            {Check(!level.Won,"Locked exit rejects body");Move(stars[0].transform.position);stage=2;}
            else if(stage==2)
            {Check(level.StarCount==1,"Body trigger collects first star");Check(!level.Collect(0),"Repeated star collection ignored");Capture("star");stage=20;}
            else if(stage==20){Move(stars[1].transform.position);stage=3;}
            else if(stage==3)
            {Check(level.StarCount==2,"Body trigger collects second star");Move(stars[2].transform.position);stage=4;}
            else if(stage==4)
            {
                Check(level.StarCount==3 && !level.Won,"Third star opens exit without remote completion");
                var checkpoint=UnityEngine.Object.FindObjectsOfType<CampaignInteractable>().First(s=>s.kind==CampaignInteractable.Kind.Checkpoint);Move(checkpoint.transform.position);stage=5;
            }
            else if(stage==5)
            {
                var checkpoint=UnityEngine.Object.FindObjectsOfType<CampaignInteractable>().First(s=>s.kind==CampaignInteractable.Kind.Checkpoint);
                Check(level.Activated(checkpoint.link),"Checkpoint activates on body contact");
                var switches=UnityEngine.Object.FindObjectsOfType<CampaignInteractable>().Where(s=>s.kind==CampaignInteractable.Kind.Switch || s.kind==CampaignInteractable.Kind.Key).ToArray();
                var c=body.GetComponentsInChildren<Collider2D>().First(hit=>CampaignInteractable.Body(hit)==body);foreach(var item in switches){item.Touch(c);Check(level.Activated(item.link),"Local shortcut activates "+item.link);}
                body.Kill();stage=6;due=EditorApplication.timeSinceStartup+4;
            }
            else if(stage==6)
            {
                level=CampaignLevel.Current;Freeze();Check(!body.Dead && level.StarCount==3,"Actual death/reload retains collected stars");
                var checkpoint=UnityEngine.Object.FindObjectsOfType<CampaignInteractable>().First(s=>s.kind==CampaignInteractable.Kind.Checkpoint);
                Check(Vector2.Distance(level.SpawnPosition,checkpoint.respawn)<.01f && Vector2.Distance(body.transform.position,checkpoint.respawn)<2f,"Death respawns safely at checkpoint: actual="+body.transform.position+" target="+checkpoint.respawn);
                var door=UnityEngine.Object.FindObjectsOfType<CampaignInteractable>().Single(s=>s.kind==CampaignInteractable.Kind.Door);Move(door.transform.position);stage=7;
            }
            else if(stage==7)
            {
                Check(level.Won,"Body enters unlocked door to complete level");Capture("exit");stage=8;
            }
            else if(stage==8)
            {
                CampaignLevel.RestartFresh();stage=9;due=EditorApplication.timeSinceStartup+1.5;
            }
            else if(stage==9)
            {
                Check(CampaignLevel.Current.StarCount==0 && !CampaignLevel.Current.Won,"Manual restart clears star/completion state");
                var spike=UnityEngine.Object.FindObjectsOfType<CampaignInteractable>().FirstOrDefault(s=>s.kind==CampaignInteractable.Kind.Spikes);
                if(spike!=null){Freeze();Move(spike.transform.position);stage=21;return;}
                Advance();
            }
            else if(stage==21)
            {
                Check(body.Dead,"Physical body contact with spikes kills");Advance();
            }
        }
        catch(Exception e){Finish(e);}
    }
    static void Advance()
    {

                levelIndex++;
                if(levelIndex>=paths.Length){Finish(null);return;}
                SceneManager.LoadScene(paths[levelIndex]);stage=0;due=EditorApplication.timeSinceStartup+1.5;
    }
    static void Finish(Exception error)
    {
        EditorApplication.update-=Tick;SceneManager.sceneLoaded-=Loaded;
        log+=error==null?"PASS third scene; full physical hammer playthrough, device/export NOT RUN.\n":"FAIL "+error+"\n";
        File.WriteAllText("scripts/level04-designed-evidence/play-results.txt",log);EditorApplication.ExitPlaymode();
    }
}
// Refresh test request.
// Updated campaign regression.
// Run against Mario reference layouts.
// Final supplied-map regression.
// Final climbing layout regression.
// Run first-two regression on authored horizontal revision.
// Recheck final footholds and grounded enemies.
// Final patrol positions verification.
// Regression after grounded patrol placement and real shell contact check.



// Final aerial regression.
// Loop reference physical falling bridge regression.
// Request topology repair after final native grid layout.
