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
    sealed class Progress { public int stars; public HashSet<string> switches = new HashSet<string>(); }
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
        Vector2 spawn=initialSpawn;
        SpawnPosition=spawn;
        var delta=(Vector3)spawn-death.transform.position;
        root.position+=delta;
        foreach(var rb in root.GetComponentsInChildren<Rigidbody2D>()) { rb.velocity=Vector2.zero; rb.angularVelocity=0; }
        Physics2D.SyncTransforms();
        GameplayAudio.Play("spawn", .4f);
        foreach(var camera in FindObjectsOfType<Cinemachine.CinemachineVirtualCamera>())camera.PreviousStateIsValid=false;
    }
    Transform progressPlayer;
    Vector2[] progressRoute;
    public float RouteProgress { get; private set; }
    void LateUpdate()
    {
        if (progressPlayer == null)
        {
            var player = FindObjectOfType<Death>();
            if (player == null) return;
            progressPlayer = player.transform;
        }
        if (progressRoute == null)
        {
            if (route != null && route.Length >= 2) progressRoute = route;
            else
            {
                var exit = FindObjectOfType<Winning.WinThresholdDetection>();
                if (exit == null) return;
                progressRoute = new[] { initialSpawn, (Vector2)exit.transform.position };
            }
        }
        float total = 0, bestDistance = float.PositiveInfinity, along = 0;
        Vector2 position = progressPlayer.position;
        for (int i = 1; i < progressRoute.Length; i++)
        {
            Vector2 start = progressRoute[i-1], segment = progressRoute[i] - start;
            float length = segment.magnitude;
            if (length < .001f) continue;
            float t = Mathf.Clamp01(Vector2.Dot(position-start, segment) / segment.sqrMagnitude);
            float distance = (position - (start + segment*t)).sqrMagnitude;
            if (distance < bestDistance) { bestDistance = distance; along = total + length*t; }
            total += length;
        }
        RouteProgress = Won ? 1f : (total > .001f ? Mathf.Clamp01(along/total) : 0f);
    }
    public bool HasStar(int id) => (state.stars & (1<<id))!=0;
    public bool Collect(int id) { if(Won || id<0 || id>2 || HasStar(id))return false; state.stars|=1<<id;GameplayAudio.Play(StarCount==3?"stars_complete":"star");return true; }
    public bool Activated(string id) => state.switches.Contains(id);
    public void Activate(string id) { state.switches.Add(id); }
    // Kept for legacy editor tools; checkpoints no longer affect respawning.
    public void Checkpoint(Vector2 position) { }
    public bool TryExit()
    {
        if(Won || FindObjectOfType<Death>().Dead)return false;
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
        if(!Won || GameMenuUI.Instance==null || GameMenuUI.Instance.settings)return;
        var ui=GameMenuUI.Instance;
        var old=GUI.matrix;
        float scale=Mathf.Min(Screen.width/1600f,Screen.height/900f);
        GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1600*scale)/2,(Screen.height-900*scale)/2,0),Quaternion.identity,Vector3.one*scale);
        ui.PanelArt(new Rect(380,180,840,570),"bg_2");
        ui.PanelArt(new Rect(480,125,640,155),"tab_1");
        ui.PanelText(new Rect(480,145,640,110),"LEVEL COMPLETE",60,-8);
        for(int i=0;i<3;i++)ui.PanelArt(new Rect(590+i*140,i==1?290:330,150,160),HasStar(i)?"icon_start_1":"icon_start_2");
        ui.PanelArt(new Rect(630,500,340,100),"bg_score");
        ui.PanelText(new Rect(630,500,340,100),StarCount+"/3",76,-5);
        ui.PanelButton(new Rect(620,640,150,150),"button_5","",RestartFresh);
        ui.PanelButton(new Rect(830,640,150,150),"button_6","",()=>{
            if(number<GameMenuUI.PlayableLevelCount && !string.IsNullOrEmpty(nextScene)){progress.Remove(nextScene);SceneManager.LoadScene(nextScene);}
            else SceneManager.LoadScene(GameMenuUI.MenuScene);
        });
        GUI.matrix=old;
    }
    void OnDestroy(){if(Current==this)Current=null;}
    void OnDrawGizmosSelected()
    {
        if(route==null)return;Gizmos.color=Color.cyan;
        for(int i=1;i<route.Length;i++)Gizmos.DrawLine(route[i-1],route[i]);
    }
}
