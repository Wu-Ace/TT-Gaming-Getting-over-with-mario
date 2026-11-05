using UnityEditor;
using UnityEngine;
using System.IO;
[InitializeOnLoad] public static class CompletionPreview {
 static double due; static bool captured;
 static CompletionPreview(){EditorApplication.delayCall+=Begin;EditorApplication.playModeStateChanged+=Changed;}
 static void Begin(){if(!File.Exists("scripts/completion-preview.request")||EditorApplication.isPlayingOrWillChangePlaymode)return;File.Delete("scripts/completion-preview.request");SessionState.SetBool("CompletionPreview",true);EditorApplication.EnterPlaymode();}
 static void Changed(PlayModeStateChange s){if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("CompletionPreview",false)){GameMenuUI.Testing=true;GameMenuUI.PreferencePrefix="Goombas.Completion.TEST.";due=EditorApplication.timeSinceStartup+3;captured=false;EditorApplication.update+=Tick;}if(s==PlayModeStateChange.EnteredEditMode){SessionState.SetBool("CompletionPreview",false);EditorApplication.update-=Tick;}}
 static void Tick(){if(EditorApplication.timeSinceStartup<due)return;var c=CampaignLevel.Current;if(c==null)return;if(!captured){for(int i=0;i<3;i++)c.Collect(i);typeof(CampaignLevel).GetProperty("Won").GetSetMethod(true).Invoke(c,new object[]{true});ScreenCapture.CaptureScreenshot(Path.GetFullPath("scripts/completion-ui-evidence/game.png"));captured=true;due=EditorApplication.timeSinceStartup+3;}else EditorApplication.ExitPlaymode();}
}

// refresh final
