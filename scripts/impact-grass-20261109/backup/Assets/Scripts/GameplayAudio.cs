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
    // Only physical recordings are allowed; no electronic/synthesized cues.
    static string NaturalClip(string cue)
    {
        switch(cue)
        {
            case "hammer_hit": return "table";
            case "body_hit": return "canBounce";
            case "star": return "can";
            case "stars_complete": return "can";
            case "checkpoint": return "table";
            case "key": return "can";
            case "switch": return "table";
            case "locked": return "locked";
            case "platform_warn": return "platform_warn";
            case "platform_fall": return "platform_fall";
            case "enemy_bee": return "bump";
            case "enemy_plant": return "bump";
            case "enemy_hit": return "bump";
            case "death_hit": return "bump";
            case "coin": return "can";
            case "ui_click": return "table";
            case "ui_back": return "table";
            case "gate": return "gate";
            case "growth_start": return "growth_start";
            case "growth_end": return "growth_end";
            case "strength_start": return "strength_start";
            case "strength_end": return "strength_end";
            case "warning": return "warning";
            case "enemy_ghost": return "enemy_ghost";
            case "spawn": return "spawn";
            case "pause": return "pause";
            case "resume": return "resume";
            case "complete": return "complete";
            case "fall_wind": return "fall_wind";
            default: return null;
        }
    }
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
        if(!audio.clips.TryGetValue(cue,out var clip)) { clip=Resources.Load<AudioClip>("NaturalAudio/"+physical); audio.clips[cue]=clip; }
        Play(clip,volume * .45f,1f);
    }
    public static void Play(AudioClip clip,float volume,float pitch=1f)
    {
        if(clip==null)return;
        var audio=Get(); var voice=audio.voices[audio.next++%audio.voices.Length];
        voice.Stop();voice.volume=Gain;voice.pitch=1f;voice.PlayOneShot(clip,Mathf.Clamp01(volume));
    }
}
