using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
[InitializeOnLoad] public static class ManualLevel02Study {
 static ManualLevel02Study(){EditorApplication.delayCall+=Run;EditorApplication.playModeStateChanged+=Change;}
 static void Run(){if(!File.Exists("scripts/manual-level02.request")||EditorApplication.isPlayingOrWillChangePlaymode)return;if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)return;File.Delete("scripts/manual-level02.request");EditorSceneManager.OpenScene("Assets/Scenes/Level02_Jungle.unity");SessionState.SetBool("ManualLevel02Study",true);EditorApplication.EnterPlaymode();}
 static void Change(PlayModeStateChange s){if(s!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool("ManualLevel02Study",false))return;SessionState.SetBool("ManualLevel02Study",false);GameMenuUI.Testing=true;GameMenuUI.PreferencePrefix="Goombas.Manual.TEST."+Guid.NewGuid()+".";EditorApplication.delayCall+=()=>{foreach(var m in UnityEngine.Object.FindObjectsOfType<MonoBehaviour>())if(m.GetType().Name=="GiftKeyboardTester")m.GetType().GetField("visible",BindingFlags.Instance|BindingFlags.NonPublic)?.SetValue(m,false);};}
}
// Start isolated mouse sample.
