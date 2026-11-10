using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
public static class AudioFeedbackValidation
{
 public static void Run() {
  var route=typeof(GameplayAudio).GetMethod("NaturalClip",BindingFlags.Static|BindingFlags.NonPublic);
  string[] cues={"hammer_hit","body_hit","star","stars_complete","checkpoint","key","switch","locked","platform_warn","platform_fall","enemy_bee","enemy_plant","enemy_hit","death_hit","coin","ui_click","ui_back","gate","growth_start","growth_end","strength_start","strength_end","warning","enemy_ghost","spawn","pause","resume","complete","fall_wind"};
  foreach(var cue in cues) {var path=(string)route.Invoke(null,new object[]{cue});var clip=Resources.Load<AudioClip>("NaturalAudio/"+path);if(clip==null || clip.length<=0)throw new Exception("Missing cue: "+cue);}
  for(int i=0;i<5;i++)if(Resources.Load<AudioClip>("NaturalAudio/KenneyImpact/footstep_grass_"+i.ToString("000"))==null)throw new Exception("Missing grass variant");
  if(Character.ContactSoundEffect.ImpactGain(false,2.19f)!=0 || Character.ContactSoundEffect.ImpactGain(true,2.79f)!=0)throw new Exception("Light collision must be silent");
  if(Character.ContactSoundEffect.ImpactGain(false,4f)<=0 || Character.ContactSoundEffect.ImpactGain(true,5f)<=0)throw new Exception("Heavy collision must play");
  Debug.Log("IMPACT_GRASS_STATIC_PASS: 29 cue routes,5 grass variants,light-hit thresholds.");
 }
}
