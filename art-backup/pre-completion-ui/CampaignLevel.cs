using System.Collections.Generic;
using System.Linq;
using Character.Lifecycle;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class CampaignLevel : MonoBehaviour
{
    public int number = 1;
    public string theme;
    public string title;
    public string nextScene;
    public Vector2 initialSpawn;
    public Sprite background;
    public Vector2[] route;
    public static CampaignLevel Current { get; private set; }
    sealed class Progress { public int stars; public Vector2? checkpoint; public HashSet<string> switches = new HashSet<string>(); }
    static readonly Dictionary<string, Progress> progress = new Dictionary<string, Progress>();
    Progress state;
    public int StarCount => state == null ? 0 : CountBits(state.stars);
    public bool Won { get; private set; }
    public Vector2 SpawnPosition { get; private set; }
    public static int CountBits(int mask) { int n=0; for(int i=0;i<3;i++) if((mask&(1<<i))!=0)n++; return n; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetSession() { progress.Clear(); Current=null; }
    void Awake()
    {
        Current=this;
        string key=SceneManager.GetActiveScene().path;
        if(!progress.TryGetValue(key,out state))progress[key]=state=new Progress();
    }
    void Start()
    {
        var death=FindObjectOfType<Death>();
        var root=death.transform.parent;
        Vector2 spawn=state.checkpoint ?? initialSpawn;
        SpawnPosition=spawn;
        var delta=(Vector3)spawn-death.transform.position;
        root.position+=delta;
        foreach(var rb in root.GetComponentsInChildren<Rigidbody2D>()) { rb.velocity=Vector2.zero; rb.angularVelocity=0; }
        Physics2D.SyncTransforms();
        foreach(var camera in FindObjectsOfType<Cinemachine.CinemachineVirtualCamera>())camera.PreviousStateIsValid=false;
    }
    public bool HasStar(int id) => (state.stars & (1<<id))!=0;
    public bool Collect(int id) { if(Won || id<0 || id>2 || HasStar(id))return false; state.stars|=1<<id;return true; }
    public bool Activated(string id) => state.switches.Contains(id);
    public void Activate(string id) { state.switches.Add(id); }
    public void Checkpoint(Vector2 position) { if(!Won)state.checkpoint=position; }
    public bool TryExit()
    {
        if(Won || StarCount!=3 || FindObjectOfType<Death>().Dead)return false;
        Won=true;
        string key=GameMenuUI.PreferencePrefix+"stars."+SceneManager.GetActiveScene().name;
        PlayerPrefs.SetInt(key,Mathf.Max(PlayerPrefs.GetInt(key,0),StarCount));PlayerPrefs.Save();
        if(LiveGifts.GiftRuntime.Instance!=null)LiveGifts.GiftRuntime.Instance.ClearEffects();
        FindObjectOfType<Winning.WinThresholdDetection>().TryWin();
        return true;
    }
    public static void RestartFresh()
    {
        progress.Remove(SceneManager.GetActiveScene().path);
        SceneManager.LoadScene(SceneManager.GetActiveScene().path);
    }
    void OnGUI()
    {
        var scale=Mathf.Clamp(Screen.height/900f,.65f,2f);
        var old=GUI.matrix; GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
        float w=Screen.width/scale;
        if(Won)
        {
            GUI.Box(new Rect(w/2-170,180,340,125),number==10?"ALL 10 LEVELS COMPLETE!":"LEVEL COMPLETE!");
            if(!string.IsNullOrEmpty(nextScene) && GUI.Button(new Rect(w/2-140,225,280,45),"Next level"))
            { progress.Remove(nextScene); SceneManager.LoadScene(nextScene); }
            if(number==10 && GUI.Button(new Rect(w/2-140,225,280,45),"Play again"))RestartFresh();
        }
        GUI.matrix=old;
    }
    void OnDestroy(){if(Current==this)Current=null;}
    void OnDrawGizmosSelected()
    {
        if(route==null)return;Gizmos.color=Color.cyan;
        for(int i=1;i<route.Length;i++)Gizmos.DrawLine(route[i-1],route[i]);
    }
}
