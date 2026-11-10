using System.Collections.Generic;
using UnityEngine;

// Dedicated persistent voices preserve cues through scene reloads and object cleanup.
public sealed class GameplayAudio : MonoBehaviour
{
    static GameplayAudio instance;
    readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    readonly Dictionary<string, float> last = new Dictionary<string, float>();
    AudioSource[] voices;
    int next;
    int lastGrass = -1;
    // Only physical recordings are allowed; no electronic/synthesized cues.
    static string NaturalClip(string cue)
    {
        switch(cue)
        {
            case "hammer_hit": return "KenneyImpact/footstep_grass_000";
            case "body_hit": return "KenneyImpact/footstep_grass_000";
            case "star": return "KenneyImpact/impactGlass_light_000";
            case "stars_complete": return "KenneyImpact/impactGlass_medium_000";
            case "checkpoint": return "KenneyImpact/impactWood_medium_000";
            case "key": return "KenneyImpact/impactMetal_light_000";
            case "switch": return "KenneyImpact/impactWood_light_000";
            case "locked": return "locked";
            case "platform_warn": return "KenneyImpact/impactWood_light_000";
            case "platform_fall": return "KenneyImpact/impactPlank_medium_000";
            case "enemy_bee": return "KenneyImpact/impactSoft_medium_000";
            case "enemy_plant": return "KenneyImpact/impactSoft_medium_000";
            case "enemy_hit": return "KenneyImpact/impactSoft_medium_000";
            case "death_hit": return "KenneyImpact/impactSoft_medium_000";
            case "coin": return "KenneyImpact/impactMetal_light_000";
            case "ui_click": return "KenneyImpact/impactWood_light_000";
            case "ui_back": return "KenneyImpact/impactGeneric_light_000";
            case "gate": return "gate";
            case "growth_start": return "growth_start";
            case "growth_end": return "growth_end";
            case "strength_start": return "strength_start";
            case "strength_end": return "strength_end";
            case "warning": return "KenneyImpact/impactWood_light_000";
            case "enemy_ghost": return "enemy_ghost";
            case "spawn": return "spawn";
            case "pause": return "KenneyImpact/impactWood_light_000";
            case "resume": return "KenneyImpact/impactGeneric_light_000";
            case "complete": return "complete";
            case "fall_wind": return "fall_wind";
            default: return null;
        }
    }
    public string LastClipName { get; private set; }
    public int PlayCount { get; private set; }
    public static float Gain => Mathf.Clamp01(PlayerPrefs.GetFloat(GameMenuUI.PreferencePrefix + "effects", 1));
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset() { instance = null; }
    static GameplayAudio Get()
    {
        if (instance != null) return instance;
        var go = new GameObject("Gameplay Audio"); DontDestroyOnLoad(go);
        instance = go.AddComponent<GameplayAudio>();
        instance.voices = new AudioSource[12];
        for(int i=0;i<12;i++) { var v=go.AddComponent<AudioSource>(); v.playOnAwake=false; v.spatialBlend=0; instance.voices[i]=v; }
        return instance;
    }
    void Update() { foreach(var v in voices) v.volume=Gain; }
    public static void Play(string cue, float volume=.65f, float pitch=1f)
    {
        string physical = NaturalClip(cue);
        if(physical == null) return;
        var audio=Get();
        if(audio.last.TryGetValue(cue,out var time) && Time.unscaledTime-time<.12f)return;
        audio.last[cue]=Time.unscaledTime;
        if(cue == "hammer_hit" || cue == "body_hit")
        {
            int variant = Random.Range(0, audio.lastGrass < 0 ? 5 : 4);
            if(audio.lastGrass >= 0 && variant >= audio.lastGrass) variant++;
            audio.lastGrass = variant;
            physical = "KenneyImpact/footstep_grass_" + variant.ToString("000");
        }
        if(!audio.clips.TryGetValue(physical,out var clip)) { clip=Resources.Load<AudioClip>("NaturalAudio/"+physical); audio.clips[physical]=clip; }
        audio.LastClipName = clip == null ? null : clip.name;
        audio.PlayCount++;
        Play(clip,volume * .45f,1f);
    }
    public static void Play(AudioClip clip,float volume,float pitch=1f)
    {
        if(clip==null)return;
        var audio=Get(); var voice=audio.voices[audio.next++%audio.voices.Length];
        voice.Stop();voice.volume=Gain;voice.pitch=1f;voice.PlayOneShot(clip,Mathf.Clamp01(volume));
    }
}
