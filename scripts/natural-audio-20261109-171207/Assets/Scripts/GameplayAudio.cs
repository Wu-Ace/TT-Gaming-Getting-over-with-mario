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
        var audio=Get();
        if(audio.last.TryGetValue(cue,out var time) && Time.unscaledTime-time<.12f)return;
        audio.last[cue]=Time.unscaledTime;
        if(!audio.clips.TryGetValue(cue,out var clip)) { clip=Resources.Load<AudioClip>("GameplayAudio/"+cue); audio.clips[cue]=clip; }
        Play(clip,volume,pitch);
    }
    public static void Play(AudioClip clip,float volume,float pitch=1f)
    {
        if(clip==null)return;
        var audio=Get(); var voice=audio.voices[audio.next++%audio.voices.Length];
        voice.Stop();voice.volume=Gain;voice.pitch=Mathf.Clamp(pitch,.5f,2f);voice.PlayOneShot(clip,Mathf.Clamp01(volume));
    }
}
