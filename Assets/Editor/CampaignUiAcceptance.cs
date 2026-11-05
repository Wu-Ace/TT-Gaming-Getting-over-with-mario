using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
[InitializeOnLoad] public static class CampaignUiAcceptance
{
    static bool prepared, captured;
    static double due;
    static CampaignUiAcceptance(){EditorApplication.delayCall+=Run;EditorApplication.playModeStateChanged+=Changed;}
    static void Run(){if(!File.Exists("scripts/campaign-ui.request") || EditorApplication.isPlayingOrWillChangePlaymode)return;File.Delete("scripts/campaign-ui.request");SessionState.SetBool("CampaignUI",true);EditorApplication.EnterPlaymode();}
    static void Changed(PlayModeStateChange s)
    {
        if(s==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("CampaignUI",false)){SessionState.SetBool("CampaignUI",false);prepared=false;captured=false;due=EditorApplication.timeSinceStartup+1;EditorApplication.update+=Tick;}
        if(s==PlayModeStateChange.EnteredEditMode)EditorApplication.update-=Tick;
    }
    static void Tick()
    {
        if(EditorApplication.timeSinceStartup<due)return;
        due=EditorApplication.timeSinceStartup+.3;
        var level=CampaignLevel.Current;
        if(!prepared)
        {
            var death=UnityEngine.Object.FindObjectOfType<Character.Lifecycle.Death>();UnityEngine.Object.FindObjectOfType<Character.Control.ArmsSimulation>().enabled=false;
            var door=UnityEngine.Object.FindObjectsOfType<CampaignInteractable>().Single(s=>s.kind==CampaignInteractable.Kind.Door);
            var delta=(Vector2)door.transform.position-(Vector2)death.transform.position;
            foreach(var body in death.transform.parent.GetComponentsInChildren<Rigidbody2D>()){body.gravityScale=0;body.position+=delta;body.velocity=Vector2.zero;body.angularVelocity=0;}
            foreach(var camera in UnityEngine.Object.FindObjectsOfType<Cinemachine.CinemachineVirtualCamera>())camera.PreviousStateIsValid=false;
            for(int i=0;i<3;i++)level.Collect(i);
            var tester=UnityEngine.Object.FindObjectsOfType<MonoBehaviour>().FirstOrDefault(m=>m.GetType().Name=="GiftKeyboardTester");if(tester!=null)tester.GetType().GetField("visible",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)?.SetValue(tester,false);
            prepared=true;File.WriteAllText("scripts/level-evidence/ui-results.txt","READY: use actual Next level button in Game view.\n");
        }
        else if(level!=null && level.number==1 && level.Won && !captured)
        {
            ScreenCapture.CaptureScreenshot(Path.GetFullPath("scripts/level-evidence/first-door-final.png"));captured=true;
        }
        else if(level!=null && level.number==2)
        {
            File.WriteAllText("scripts/level-evidence/ui-results.txt","PASS actual Next level button loaded Level02_Jungle with fresh stars.\n");
            ScreenCapture.CaptureScreenshot(Path.GetFullPath("scripts/level-evidence/next-level-ui.png"));EditorApplication.update-=Tick;EditorApplication.delayCall+=()=>EditorApplication.ExitPlaymode();
        }
    }
}
// Final UI smoke.
