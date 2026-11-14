using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

// Menu art is generated from the native Kit; gameplay geometry stays in its own scenes.
public sealed class GameMenuUI : MonoBehaviour
{
    public static GameMenuUI Instance { get; private set; }
    public static bool IsPaused => Instance != null && Instance.settings && !Instance.IsMenu;
    public const string MenuScene = "MainMenu";
    public const string Pref = "Goombas.UI.v1.";
    public static string PreferencePrefix = Pref;
    public static bool Testing;
    public const int PlayableLevelCount = 5;
    public static readonly string[] Scenes = {"Level01","Level02_Jungle","Level03_Beach","Level04_Town","Level05_Cave","Level06_Grassland_Winter","Level07_Ice","Level08_Castle","Level09_Volcano","Level10_Sky"};
    static readonly string[] Names = {"草地起点","丛林穿行","海滨冒险","小镇高台","地下探险","冬日草地","冰雪之路","城堡挑战","火山之旅","天空终点"};
    public bool IsMenu => SceneManager.GetActiveScene().name == MenuScene;
    public bool settings;
    public int page, world;
    Texture2D landscape, diamond, star;
    Texture2D[] plates, worlds;
    GUIStyle label, title, button, greyButton;
    Font font;
    AudioSource clicks;
    AudioClip click, back;
    float music=1, effects=1, previousTimeScale=1;
    int resolutionIndex, monitorW, monitorH;
    Resolution[] resolutions;
    Vector2 scroll;
    bool dropdown;
    string error;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if(Instance!=null)return;
        var go=new GameObject("Game Menu UI"); DontDestroyOnLoad(go); go.AddComponent<GameMenuUI>();
    }
    void Awake()
    {
        Instance=this;
        landscape=Load("landscape");diamond=Load("diamond");star=Load("star");
        plates=Enumerable.Range(0,3).Select(i=>Load("plate"+i)).ToArray();worlds=Enumerable.Range(0,3).Select(i=>Load("world"+i)).ToArray();
        font=GameFont.Main;
        clicks=gameObject.AddComponent<AudioSource>();clicks.playOnAwake=false;
        click=Resources.Load<AudioClip>("MenuUI/click");back=Resources.Load<AudioClip>("MenuUI/back");
        music=PlayerPrefs.GetFloat(PreferencePrefix+"music",1);effects=PlayerPrefs.GetFloat(PreferencePrefix+"effects",1);
        RefreshResolutions(); SceneManager.sceneLoaded+=Loaded;
    }
    Texture2D Load(string s)=>Resources.Load<Texture2D>("MenuUI/"+s);
    bool showLevelsOnLoad;
    void Loaded(Scene scene,LoadSceneMode mode){CloseSettings();page=showLevelsOnLoad && scene.name==MenuScene ? 1 : 0;showLevelsOnLoad=false;error=null;ApplyVolumes();}
    void OnDestroy(){SceneManager.sceneLoaded-=Loaded;if(Instance==this)Instance=null;}
    void RefreshResolutions()
    {
        monitorW=Screen.currentResolution.width;monitorH=Screen.currentResolution.height;
        resolutions=Screen.resolutions.Where(r=>r.width<=monitorW && r.height<=monitorH).GroupBy(r=>r.width+"x"+r.height).Select(g=>g.First()).OrderByDescending(r=>r.width*r.height).ToArray();
        string saved=PlayerPrefs.GetString(PreferencePrefix+"resolution","auto");
        resolutionIndex=Array.FindIndex(resolutions,r=>r.width+"x"+r.height==saved)+1;
        ApplyResolution(false);
    }
    void ApplyResolution(bool save)
    {
        string value="auto";int w=monitorW,h=monitorH;
        if(resolutionIndex>0 && resolutionIndex<=resolutions.Length){var r=resolutions[resolutionIndex-1];w=r.width;h=r.height;value=w+"x"+h;}
        if(save){PlayerPrefs.SetString(PreferencePrefix+"resolution",value);PlayerPrefs.Save();}
        // The editor Game view belongs to the editor; do not resize the user's editor.
        if(!Application.isEditor && w>0 && h>0)Screen.SetResolution(w,h,Screen.fullScreenMode);
    }
    void Update()
    {
        if(Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) { if(settings)CloseSettings();else if(!IsMenu)OpenSettings();else if(page>0)page--; }
        if(Screen.currentResolution.width!=monitorW || Screen.currentResolution.height!=monitorH)RefreshResolutions();
    }
    public void OpenSettings(){if(settings || Character.Lifecycle.DeathFeedback.Active)return;GameplayAudio.Play("pause",.35f);settings=true;dropdown=false;previousTimeScale=Time.timeScale;if(!IsMenu)Time.timeScale=0;}
    public void CloseSettings(){if(settings)GameplayAudio.Play("resume",.35f);if(settings && !IsMenu)Time.timeScale=previousTimeScale;settings=false;dropdown=false;}
    public void LoadLevel(int i)
    {
        if(i < -1 || i>=PlayableLevelCount)return;
        string targetScene = i == -1 ? "Tutorial" : Scenes[i];
        if(!Application.CanStreamedLevelBeLoaded(targetScene)){error="关卡未加入构建，请返回重试。";GameplayAudio.Play("locked",.4f);return;}
        CloseSettings();Time.timeScale=1;SceneManager.LoadScene(targetScene);
    }
    void ApplyVolumes()
    {
        // Keep SFX routing scoped to sources. Do not mute UI with a global AudioListener volume.
        foreach(var source in FindObjectsOfType<AudioSource>())
        {
            if(source==clicks || source.GetComponent<GameplayAudio>()!=null)continue;
            var gain=source.GetComponent<MenuAudioGain>();if(gain==null)gain=source.gameObject.AddComponent<MenuAudioGain>();
            gain.Apply(source,source.loop?music:effects);
        }
        clicks.volume=effects;
    }
    void SaveVolumes(){PlayerPrefs.SetFloat(PreferencePrefix+"music",music);PlayerPrefs.SetFloat(PreferencePrefix+"effects",effects);PlayerPrefs.Save();ApplyVolumes();}
    void Sound(bool goBack=false){GameplayAudio.Play(goBack?"ui_back":"ui_click",.16f);}
    void Styles()
    {
        if(label!=null)return;
        label=new GUIStyle(GUI.skin.label){font=font,fontSize=28,alignment=TextAnchor.MiddleLeft};label.normal.textColor=new Color32(40,72,61,255);
        title=new GUIStyle(label){fontSize=48};title.normal.textColor=new Color32(255,248,223,255);
        button=new GUIStyle(GUI.skin.button){font=font,fontSize=28,border=new RectOffset(12,12,10,10),alignment=TextAnchor.MiddleCenter};
        foreach(var state in new[]{button.normal,button.hover,button.active,button.focused}){state.background=Load("button");state.textColor=Color.white;}
        greyButton=new GUIStyle(button);foreach(var state in new[]{greyButton.normal,greyButton.hover,greyButton.active,greyButton.focused}){state.background=Load("buttonGrey");state.textColor=new Color32(40,72,61,255);}
    }
    void Text(Rect r,string value,int size=28,Color? color=null,bool center=false)
    {
        var s=new GUIStyle(label){fontSize=size,alignment=center?TextAnchor.MiddleCenter:TextAnchor.MiddleLeft};s.normal.textColor=color??label.normal.textColor;GUI.Label(r,value,s);
    }
    bool Button(Rect r,string value,Action action,bool green=true)
    {
        if(!GUI.Button(r,value,green?button:greyButton))return false;if(action!=OpenSettings && action!=CloseSettings)Sound();action();return true;
    }
    void Fill(Rect r,Color c){var old=GUI.color;GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
    void Background(){GUI.DrawTexture(new Rect(0,0,1600,900),landscape,ScaleMode.ScaleAndCrop);}
    void OnGUI()
    {
        Styles();GUI.depth=-20;
        var old=GUI.matrix;float scale=Mathf.Min(Screen.width/1600f,Screen.height/900f);
        // Letterboxed reference layout keeps circles round and never stretches source art.
        GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1600*scale)/2,(Screen.height-900*scale)/2,0),Quaternion.identity,new Vector3(scale,scale,1));
        if(IsMenu){Fill(new Rect(-4000,-4000,9600,8900),new Color32(35,63,56,255));Background();if(page==0)Home();else Levels();}
        else if(!settings)
        {
            // HUD anchors follow the actual display, independently of the menu reference frame.
            float hudScale=Mathf.Clamp(Screen.height/900f,.65f,2f);
            GUI.matrix=Matrix4x4.Scale(new Vector3(hudScale,hudScale,1));
            GUI.DrawTexture(new Rect(40,75,32,32),CampaignLevel.Current!=null && CampaignLevel.Current.number<=3?diamond:star,ScaleMode.ScaleToFit);
            Text(new Rect(80,65,150,55),"×"+(CampaignLevel.Current!=null?CampaignLevel.Current.StarCount:0).ToString("00"),32,Color.white);
            if (CampaignLevel.Current != null)
            {
                float width = Mathf.Min(380, Screen.width/hudScale - 420);
                if (width > 80)
                {
                    float x = (Screen.width/hudScale-width)/2;
                    float progress = CampaignLevel.Current.RouteProgress;
                    Fill(new Rect(x-12,20,width+24,70),new Color32(24,48,43,225));
                    Text(new Rect(x,22,width,30),"关卡进度  " + Mathf.FloorToInt(progress*100) + "%",20,Color.white,true);
                    Fill(new Rect(x,60,width,12),new Color32(76,96,88,255));
                    Fill(new Rect(x,60,width*progress,12),new Color32(70,218,156,255));
                    Fill(new Rect(x+Mathf.Clamp(width*progress-3,0,width-6),56,6,20),new Color32(255,225,115,255));
                }
            }
            Button(new Rect(Screen.width/hudScale-180,25,160,60),"设置",OpenSettings,false);
        }
        GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1600*scale)/2,(Screen.height-900*scale)/2,0),Quaternion.identity,new Vector3(scale,scale,1));
        if(settings)Settings();
        if(!string.IsNullOrEmpty(error))Text(new Rect(450,800,800,65),error,24,Color.white,true);
        GUI.matrix=old;
    }
    void Home()
    {
        Text(new Rect(95,95,650,85),"GETTING OVER",55,new Color32(255,248,223,255));Text(new Rect(90,165,650,120),"GOOMBAS",90,new Color32(255,248,223,255));
        Text(new Rect(98,285,500,45),"越过障碍，继续向前。",25);
        Fill(new Rect(82,350,438,340),new Color32(255,246,220,255));
        Button(new Rect(115,380,370,76),"开始",()=>page=1);Button(new Rect(115,480,370,76),"设置",OpenSettings);Button(new Rect(115,580,370,76),"退出",()=>{if(!Testing)Application.Quit();});
    }
    void Levels()
    {
        Text(new Rect(85,50,1000,80),"选择关卡",48,Color.white);
        int start=0,end=PlayableLevelCount+1;
        for(int i=start;i<end;i++)
        {
            int n=i-1;float x=40+i*255;
            Fill(new Rect(x,300,245,340),new Color32(35,63,56,240));Text(new Rect(x,330,245,70),(n<0 ? "教程" : (n+1).ToString("00")),52,Color.white,true);
            Text(new Rect(x,420,245,60),(n<0 ? "新手教程" : Names[n]),28,Color.white,true);
            int best=PlayerPrefs.GetInt(PreferencePrefix+"stars."+(n<0 ? "Tutorial" : Scenes[n]),0);Text(new Rect(x,490,245,55),(n<3?"钻石 ":"★ ")+best+" / 3",24,new Color32(255,225,115,255),true);
            Button(new Rect(x+15,560,215,60),"进入关卡",()=>LoadLevel(n));
        }
        Button(new Rect(85,800,190,62),"← 返回",()=>page=0,false);
    }
    void ReturnToLevelMenu()
    {
        CloseSettings();
        Time.timeScale=1;
        if(IsMenu){page=1;return;}
        showLevelsOnLoad=true;
        SceneManager.LoadScene(MenuScene);
    }
    void Settings()
    {
        Fill(new Rect(-4000,-4000,9600,8900),new Color(0.05f,.13f,.1f,.7f));
        Fill(new Rect(415,130,770,635),new Color32(227,231,232,255));Fill(new Rect(415,130,770,525),new Color32(20,184,123,255));
        Text(new Rect(465,155,400,75),"设置",40,Color.white);Button(new Rect(1103,146,60,60),"×",CloseSettings,false);
        float m=VolumeRow(290,"音乐音量",music),e=VolumeRow(405,"音效音量",effects);
        if(Mathf.Abs(m-music)>.001f || Mathf.Abs(e-effects)>.001f){music=m;effects=e;SaveVolumes();}
        Text(new Rect(470,525,230,65),"分辨率",29);
        string current=resolutionIndex==0?"自动（跟随屏幕）":resolutions[resolutionIndex-1].width+" × "+resolutions[resolutionIndex-1].height;
        Button(new Rect(715,522,410,65),current+"  v",()=>dropdown=!dropdown,false);
        Button(new Rect(635,684,330,60),"返回菜单",ReturnToLevelMenu,false);
        if(dropdown)
        {
            Fill(new Rect(710,220,430,375),new Color32(245,245,243,255));
            scroll=GUI.BeginScrollView(new Rect(715,225,420,360),scroll,new Rect(0,0,390,(resolutions.Length+1)*52));
            for(int i=0;i<=resolutions.Length;i++){int n=i;string name=i==0?"自动（跟随屏幕）":resolutions[i-1].width+" × "+resolutions[i-1].height;
                Button(new Rect(0,i*52,385,48),name,()=>{resolutionIndex=n;dropdown=false;ApplyResolution(true);},false);}
            GUI.EndScrollView();
        }
    }
    float VolumeRow(float y,string name,float value)
    {
        Text(new Rect(470,y-8,230,65),name,29);Fill(new Rect(715,y-8,410,64),new Color32(245,245,243,255));
        float v=GUI.HorizontalSlider(new Rect(740,y+14,358,30),value,0,1);Text(new Rect(1020,y+58,110,35),Mathf.RoundToInt(v*100)+"%",18,Color.white,true);return v;
    }
}

public sealed class MenuAudioGain : MonoBehaviour
{
    float original;bool captured;
    public void Apply(AudioSource source,float gain){if(!captured){original=source.volume;captured=true;}source.volume=original*gain;}
}


