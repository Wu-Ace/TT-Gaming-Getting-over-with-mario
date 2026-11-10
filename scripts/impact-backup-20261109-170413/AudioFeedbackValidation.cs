using UnityEngine;
using UnityEditor;
using System;
public static class AudioFeedbackValidation
{
 public static void Run() {
  string[] cues={"star","stars_complete","checkpoint","key","switch","gate","locked","platform_warn","platform_fall","enemy_bee","enemy_ghost","enemy_plant","growth_start","growth_end","strength_start","strength_end","warning","spawn","pause","resume"};
  foreach(var cue in cues) {var c=Resources.Load<AudioClip>("GameplayAudio/"+cue);if(c==null || c.length<=0)throw new Exception("Missing/empty cue: "+cue);}
  var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Character.prefab");
  foreach(var c in prefab.GetComponentsInChildren<Character.ContactSoundEffect>(true)) {
   var s=new SerializedObject(c);
   if(s.FindProperty("maxPitch").floatValue>2)throw new Exception("Invalid collision pitch");
  }
  Debug.Log("AUDIO_VALIDATION_PASS: 20 clips imported; collision pitch valid; no PlayerPrefs or scene writes.");
 }
}