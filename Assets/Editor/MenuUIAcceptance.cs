using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
[InitializeOnLoad] public static class MenuUIAcceptance
{
    static double due; static int phase;static readonly List<string> results=new List<string>();
    static MenuUIAcceptance(){EditorApplication.delayCall+=Run;EditorApplication.playModeStateChanged+=Changed;}
    static void Run()
    {
        if(!File.Exists("scripts/menu-ui-test.request") || EditorApplication.isPlayingOrWillChangePlaymode)return;
        if(SceneManager.GetActiveScene().isDirty)return;
        if(!File.Exists("Assets/Scenes/MainMenu.unity"))return;
        File.Delete("scripts/menu-ui-test.request");EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        SessionState.SetBool("MenuUIAcceptance",true);EditorApplication.EnterPlaymode();
    }
    static void Changed(PlayModeStateChange s)
    {
        if(s==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("MenuUIAcceptance",false))
        {phase=0;results.Clear();GameMenuUI.Testing=true;GameMenuUI.PreferencePrefix="Goombas.UI.TEST."+Guid.NewGuid()+".";due=EditorApplication.timeSinceStartup+2;EditorApplication.update+=Tick;}
        if(s==PlayModeStateChange.EnteredEditMode){EditorApplication.update-=Tick;if(SessionState.GetBool("MenuUIAcceptance",false)){SessionState.SetBool("MenuUIAcceptance",false);EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");}}
    }
    static void Shot(string name){ScreenCapture.CaptureScreenshot(Path.GetFullPath("scripts/ui-evidence/"+name+".png"));}
    static void Require(bool condition,string message){if(!condition)throw new Exception(message);results.Add("PASS "+message);}
    static void Tick()
    {
        if(EditorApplication.timeSinceStartup<due)return;due=EditorApplication.timeSinceStartup+1.5;
        try
        {
            var ui=GameMenuUI.Instance;
            switch(phase++)
            {
                case 0:Require(ui!=null && ui.IsMenu,"Menu bootstrap");Shot("01-home");break;
                case 1:ui.page=1;break;
                case 2:Shot("02-worlds");ui.OpenSettings();break;
                case 3:Shot("03-settings");ui.CloseSettings();ui.world=0;ui.page=2;break;
                case 4:Shot("04-levels");for(int i=0;i<GameMenuUI.Scenes.Length;i++)Require(Application.CanStreamedLevelBeLoaded(GameMenuUI.Scenes[i]),"Build scene "+GameMenuUI.Scenes[i]);ui.LoadLevel(0);break;
                case 5:Require(CampaignLevel.Current!=null && !ui.IsMenu,"Level load");ui.OpenSettings();Require(GameMenuUI.IsPaused && Time.timeScale==0,"Settings pauses gameplay");break;
                case 6:Shot("05-pause");ui.CloseSettings();Require(Time.timeScale==1,"Settings resumes gameplay");break;
                case 7:Shot("06-game");break;
                case 8:File.WriteAllLines("scripts/ui-evidence/smoke.txt",results);EditorApplication.ExitPlaymode();break;
            }
        }
        catch(Exception e){File.WriteAllText("scripts/ui-evidence/smoke-error.txt",e.ToString());EditorApplication.ExitPlaymode();}
    }
}
