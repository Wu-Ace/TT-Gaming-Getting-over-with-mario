using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Cinemachine;

[InitializeOnLoad]
public static class CampaignBuilder
{
    const string Art="Assets/Art/Campaign/";
    const string Kit="Assets/Art/2D Platformer Kit/";
    static readonly string[] Themes={"Grassland","Jungle","Beach","Town","Cave","Grassland_Winter","Ice","Castle","Volcano","Sky"};
    static readonly string[] Titles={"Grassland beginnings","Jungle slopes","Coastal crossing","Town rooftops","Haunted hollow","Snow ridge","Ice brakes","Twin castle towers","Volcanic descent","Sky summit"};
    static readonly string[] Backgrounds={"hills_day","forest_middle","ocean_and_islands_day","hills_day","cave_interior","mountains_day","mountains_night","castle_interior","mountains_dark_grey","clouds_dark_top"};
    static string theme;
    static Transform terrain, objects;
    static List<string> report;
    static CampaignBuilder(){EditorApplication.delayCall+=Requested;}
    static void Requested()
    {
        if(!File.Exists("scripts/campaign-build.request") || EditorApplication.isPlayingOrWillChangePlaymode)return;
        File.Delete("scripts/campaign-build.request");
        try { Build(); } catch(Exception e){File.WriteAllText("scripts/level-evidence/build-error.txt",e.ToString());Debug.LogException(e);}
    }
    static Sprite SpriteAt(string path)
    {
        var s=AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if(s==null)throw new Exception("Missing sprite: "+path);
        return s;
    }
    static void ImportArt()
    {
        foreach(var path in Directory.GetFiles(Art,"*.png",SearchOption.AllDirectories))
        {
            var ti=(TextureImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));
            if(ti.textureType==TextureImporterType.Sprite && Mathf.Approximately(ti.spritePixelsPerUnit,150) && ti.textureCompression==TextureImporterCompression.Uncompressed)continue;
            ti.textureType=TextureImporterType.Sprite;ti.spriteImportMode=SpriteImportMode.Single;
            ti.spritePixelsPerUnit=150;ti.filterMode=FilterMode.Bilinear;ti.wrapMode=TextureWrapMode.Clamp;
            ti.textureCompression=TextureImporterCompression.Uncompressed;ti.mipmapEnabled=false;ti.alphaIsTransparency=true;
            ti.SaveAndReimport();
        }
    }
    static GameObject Obj(string name,Vector2 p,Transform parent)
    {var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.position=p;return g;}
    static SpriteRenderer Visual(GameObject g,Sprite s,float scale=1)
    {var r=g.AddComponent<SpriteRenderer>();r.sprite=s;r.sortingLayerName="Default";r.sortingOrder=10;g.transform.localScale=Vector3.one*scale;return r;}
    static void Tile(string role,int x,int y,Transform parent)
    {
        var g=Obj(theme+" "+role,new Vector2((x+.5f)*.8f,(y+.5f)*.8f-3.6f),parent);
        Visual(g,SpriteAt(Art+theme+"/"+role+".png"));
    }
    static void Rect(int x,int y,int width,int height)
    {
        var g=Obj("Platform "+x+" "+y,Vector2.zero,terrain);
        var c=g.AddComponent<BoxCollider2D>();c.offset=new Vector2((x+width*.5f)*.8f,(y+height*.5f)*.8f-3.6f);c.size=new Vector2(width*.8f,height*.8f);
        for(int xx=0;xx<width;xx++)for(int yy=0;yy<height;yy++)
        {string role=yy==height-1?(xx==0?"tl":xx==width-1?"tr":"top"):yy==0?(xx==0?"bl":xx==width-1?"br":"bottom"):(xx==0?"left":xx==width-1?"right":"fill");Tile(role,x+xx,y+yy,g.transform);}
    }
    static CampaignInteractable Interactive(string name,CampaignInteractable.Kind kind,Vector2 p,string sprite,float size)
    {
        var g=Obj(name,p,objects);var s=SpriteAt(Kit+"Interactive/"+sprite+".png");
        Visual(g,s,size/s.bounds.size.y);
        // Trigger geometry is authored in world units, independent of the artwork's padding.
        var hit=g.AddComponent<BoxCollider2D>();hit.isTrigger=true;hit.size=new Vector2(size,size)/g.transform.localScale.x;
        var item=g.AddComponent<CampaignInteractable>();item.kind=kind;return item;
    }
    static void Star(int id,Vector2 p)
    {
        var item=Interactive("Objective Star "+(id+1),CampaignInteractable.Kind.Star,p,"star_0",1.1f);item.starId=id;
        var text=Obj("Star marker",p+Vector2.up*.9f,objects).AddComponent<TextMesh>();text.text="STAR "+(id+1);text.fontSize=45;text.characterSize=.035f;text.anchor=TextAnchor.MiddleCenter;text.color=new Color(1,.9f,.3f);
    }
    static void Checkpoint(Vector2 foot,string id)
    {
        var item=Interactive("Checkpoint "+id,CampaignInteractable.Kind.Checkpoint,foot+Vector2.up*.8f,"flag_green_down",1.6f);
        item.link=id;item.respawn=foot+Vector2.up*1.25f;
        item.inactiveSprite=SpriteAt(Kit+"Interactive/flag_green_down.png");item.activeSprite=SpriteAt(Kit+"Interactive/flag_green_raised.png");
    }
    static void Door(Vector2 floor)
    {
        var door=Obj("Three Star Exit Door",floor+Vector2.up*1.2f,objects);
        var doorSprite=SpriteAt(Art+theme+"/door.png");
        var r=Visual(door,doorSprite,2.4f/doorSprite.bounds.size.y);r.sortingOrder=5;
        var trigger=door.AddComponent<BoxCollider2D>();trigger.isTrigger=true;trigger.size=new Vector2(.6f,.8f);
        var item=door.AddComponent<CampaignInteractable>();item.kind=CampaignInteractable.Kind.Door;item.inactiveSprite=doorSprite;item.activeSprite=SpriteAt(Art+theme+"/door-open.png");
        var block=Obj("Closed door seal",floor+Vector2.up*1.2f,objects);
        Visual(block,SpriteAt(Kit+"Interactive/lock_gold.png"),1.2f);var collider=block.AddComponent<BoxCollider2D>();collider.size=new Vector2(.55f,1.8f);item.blocker=block;
        var label=Obj("Exit label",floor+Vector2.up*3,objects).AddComponent<TextMesh>();label.text="EXIT\n3 STARS";label.anchor=TextAnchor.MiddleCenter;label.fontSize=48;label.characterSize=.05f;
    }
    static void Spikes(float x,float floor,int count=2)
    {for(int j=0;j<count;j++){var s=Interactive("Spikes",CampaignInteractable.Kind.Spikes,new Vector2(x+j*.8f,floor+.23f),"spikes_dark",.6f);var c=s.GetComponent<BoxCollider2D>();c.size=new Vector2(.68f,.32f)/s.transform.localScale.x;}}
    static void Enemy(CampaignEnemy.Species type,Vector2 p,float range=2)
    {
        string prefix=type==CampaignEnemy.Species.Bee?"wasp":type==CampaignEnemy.Species.Ghost?"ghost":"plant";
        string animation=type==CampaignEnemy.Species.Ghost?"float":"idle";
        int count=type==CampaignEnemy.Species.Plant?4:2;
        var g=Obj(prefix+" patrol",p,objects);Visual(g,SpriteAt(Kit+"Enemies/"+prefix+"_"+animation+"_0.png"),.85f);
        var c=g.AddComponent<BoxCollider2D>();c.isTrigger=true;c.size=new Vector2(.72f,.65f)/g.transform.localScale.x;
        var rb=g.AddComponent<Rigidbody2D>();rb.bodyType=RigidbodyType2D.Kinematic;rb.useFullKinematicContacts=true;
        var e=g.AddComponent<CampaignEnemy>();e.species=type;e.range=range;e.speed=type==CampaignEnemy.Species.Ghost?.55f:.8f;e.frames=Enumerable.Range(0,count).Select(i=>SpriteAt(Kit+"Enemies/"+prefix+"_"+animation+"_"+i+".png")).ToArray();e.defeated=SpriteAt(Kit+"Enemies/"+prefix+"_defeated.png");
    }
    static void SwitchGate(Vector2 button,Vector2 gate,string id,bool key=false)
    {
        var item=Interactive(key?"Shortcut key":"Hammer switch",key?CampaignInteractable.Kind.Key:CampaignInteractable.Kind.Switch,button,key?"key_bronze":"switch_blue_off",.7f);item.link=id;
        item.inactiveSprite=SpriteAt(Kit+"Interactive/switch_blue_off.png");item.activeSprite=SpriteAt(Kit+"Interactive/switch_blue_on.png");
        var barrier=Interactive("Shortcut gate",CampaignInteractable.Kind.Barrier,gate,"lock_bronze",1.1f);barrier.link=id;
        var block=Obj("Gate collision",gate,objects);var hit=block.AddComponent<BoxCollider2D>();hit.size=new Vector2(.45f,2.4f);barrier.blocker=block;
    }
    static int Ramp(int x,int a,int b,int height)
    {if(x<a || x>b)return 0;return Mathf.Min(height,Mathf.Min(x-a,b-x));}
    static int Height(int level,int x)
    {
        switch(level)
        {
            case 2:return Ramp(x,25,78,4)+Ramp(x,110,175,6)+Ramp(x,215,270,5)+Ramp(x,295,345,7);
            case 3:return ((x/18)%3)-1;
            case 4:return Ramp(x,42,100,5)+Ramp(x,142,192,7)+Ramp(x,238,294,6);
            case 5:return -Ramp(x,40,115,3)-Ramp(x,165,240,4)+Ramp(x,285,345,4);
            case 6:return Ramp(x,20,108,6)+Ramp(x,130,235,8)+Ramp(x,260,350,5);
            case 7:return -Ramp(x,20,100,5)+Ramp(x,120,180,3)-Ramp(x,200,275,6)+Ramp(x,295,345,4);
            case 8:return Ramp(x,45,125,8)+Ramp(x,190,270,10);
            case 9:return -Ramp(x,30,110,5)-Ramp(x,115,235,8)+Ramp(x,275,345,6);
            case 10:return (x/30)%2==0?0:2;
            default:return 0;
        }
    }
    static bool Gap(int level,int x)
    {
        if(x<15 || x>350)return false;
        if(level==3)return x%18>=16;
        if(level==10)return x%24>=22;
        return false;
    }
    static float Floor(int level,int cell)=>Height(level,cell)*.8f-3.6f;
    static void Ground(int level)
    {
        // Flat runs share a collider. Slopes have a continuous polygon surface and native triangular art.
        for(int x=0;x<380;)
        {
            if(Gap(level,x)){x++;continue;}
            int h=Height(level,x);int change=x<379?Height(level,x+1)-h:0;
            bool slope=level!=3 && level!=10 && change!=0;
            if(slope)
            {
                int low=Mathf.Min(h,h+change);int high=Mathf.Max(h,h+change);
                var g=Obj("Slope "+x,Vector2.zero,terrain);var c=g.AddComponent<PolygonCollider2D>();
                c.points=new[]{new Vector2(x*.8f,-13.2f),new Vector2((x+1)*.8f,-13.2f),new Vector2((x+1)*.8f,(h+change)*.8f-3.6f),new Vector2(x*.8f,h*.8f-3.6f)};
                Tile(change>0?"up":"down",x,low,g.transform);
                for(int y=-12;y<low;y++)Tile(y==low-1?(change>0?"underup":"underdown"):"fill",x,y,g.transform);
                x++;continue;
            }
            int end=x+1;
            while(end<380 && !Gap(level,end) && Height(level,end)==h && (level==3 || level==10 || end==379 || Height(level,end+1)==h))end++;
            var run=Obj("Ground run "+x,Vector2.zero,terrain);var collider=run.AddComponent<BoxCollider2D>();collider.offset=new Vector2((x+end)*.4f,(h-12)*.4f-3.6f);collider.size=new Vector2((end-x)*.8f,(h+12)*.8f);
            if(level==7) {var ice=new PhysicsMaterial2D("Ice glide"){friction=.12f,bounciness=0};var path="Assets/Art/Campaign/IceGlide.physicsMaterial2D";var existing=AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);if(existing==null){AssetDatabase.CreateAsset(ice,path);existing=ice;}else UnityEngine.Object.DestroyImmediate(ice);collider.sharedMaterial=existing;}
            for(int xx=x;xx<end;xx++)for(int y=-12;y<h;y++)
            {
                bool left=Gap(level,xx-1) || xx==0;bool right=Gap(level,xx+1) || xx==379;
                string role=y==h-1?(left?"tl":right?"tr":"top"):(left?"left":right?"right":"fill");Tile(role,xx,y,run.transform);
            }
            x=end;
        }
    }
    static void MakeLayout(int level,CampaignLevel campaign)
    {
        Ground(level);
        var stars=new Vector2[3];int[] cells={70,185,305};
        for(int k=0;k<3;k++)stars[k]=new Vector2((cells[k]+.5f)*.8f,Floor(level,cells[k])+1.1f);
        switch(level)
        {
            case 2:
                Rect(66,6,9,2);stars[0]=new Vector2(56,3.9f);
                Rect(180,2,10,2);stars[1]=new Vector2(148, .7f);
                Rect(302,9,9,2);stars[2]=new Vector2(246,6.1f);
                SwitchGate(new Vector2(282,Floor(level,352)+.4f),new Vector2(289,Floor(level,361)+1.2f),"jungle-path");break;
            case 3:
                Rect(60,-4,10,2);stars[0]=new Vector2(52,-4.1f);
                Rect(180,1,10,2);stars[1]=new Vector2(148,-.1f);
                Rect(300,3,10,2);stars[2]=new Vector2(244,1.5f);
                Enemy(CampaignEnemy.Species.Bee,new Vector2(197, .2f),2.2f);
                Spikes(83,Floor(level,104),2);break;
            case 4:
                Rect(65,7,12,2);Rect(60,4,4,1);stars[0]=new Vector2(56,4.7f);
                Rect(177,9,12,2);Rect(171,6,4,1);stars[1]=new Vector2(146.5f,6.3f);
                Rect(300,2,12,2);stars[2]=new Vector2(244,.7f);
                SwitchGate(new Vector2(145,6.3f),new Vector2(155,Floor(level,194)+1.2f),"town-alley",true);break;
            case 5:
                Rect(35,3,35,2);Rect(85,1,28,2);Rect(150,2,30,2);Rect(212,2,28,2);
                Rect(64,-1,9,1);stars[0]=new Vector2(55,-2.5f);
                Rect(181,-2,10,1);stars[1]=new Vector2(148,-3.3f);
                Rect(300,7,10,2);Rect(293,4,4,1);stars[2]=new Vector2(244,4.7f);
                Enemy(CampaignEnemy.Species.Ghost,new Vector2(210,-3.5f),3);Spikes(108,-3.6f,2);break;
            case 6:
                Rect(66,8,10,2);stars[0]=new Vector2(56,5.5f);
                Rect(181,10,10,2);stars[1]=new Vector2(148.5f,7.1f);
                Rect(301,7,10,2);stars[2]=new Vector2(244,4.7f);
                SwitchGate(new Vector2(155,Floor(level,193)+.4f),new Vector2(164,Floor(level,205)+1.2f),"snow-shortcut");break;
            case 7:
                stars[0]=new Vector2(56,Floor(level,70)+1.1f);
                Rect(182,5,9,2);Rect(176,2,4,1);stars[1]=new Vector2(149,3.1f);
                Rect(301,6,10,2);stars[2]=new Vector2(244,3.9f);
                Spikes(85,-3.6f,2);Enemy(CampaignEnemy.Species.Bee,new Vector2(151,4.8f),2);break;
            case 8:
                Rect(66,10,10,2);stars[0]=new Vector2(56,7.1f);
                Rect(181,4,10,2);Rect(176,1,4,1);stars[1]=new Vector2(149,2.3f);
                Rect(301,5,10,2);Rect(295,2,4,1);stars[2]=new Vector2(244,3.1f);
                Rect(85,11,10,2);Rect(223,13,10,2);
                Enemy(CampaignEnemy.Species.Plant,new Vector2(227,Floor(level,283)+.6f));
                SwitchGate(new Vector2(148,2.3f),new Vector2(160,Floor(level,200)+1.2f),"castle-bridge");break;
            case 9:
                stars[0]=new Vector2(56,Floor(level,70)+1.1f);
                Rect(180,-6,12,2);stars[1]=new Vector2(149,-5.7f);
                Rect(300,8,10,2);stars[2]=new Vector2(244,5.5f);
                Enemy(CampaignEnemy.Species.Ghost,new Vector2(171,-8),2.5f);
                Spikes(104,Floor(level,130),2);
                SwitchGate(new Vector2(147,-5.7f),new Vector2(157,Floor(level,196)+1.2f),"volcano-pass");break;
            case 10:
                Rect(65,4,10,2);Rect(59,1,4,1);stars[0]=new Vector2(56,2.3f);
                Rect(180,6,12,2);Rect(174,3,4,1);stars[1]=new Vector2(149,3.9f);
                Rect(300,6,10,2);Rect(294,3,4,1);stars[2]=new Vector2(244,3.9f);
                Enemy(CampaignEnemy.Species.Bee,new Vector2(104,1),2.5f);Enemy(CampaignEnemy.Species.Plant,new Vector2(209,Floor(level,261)+.6f));
                Rect(315,8,10,2);SwitchGate(new Vector2(249,Floor(level,311)+.4f),new Vector2(261,Floor(level,326)+1.2f),"sky-pass");break;
        }
        for(int i=0;i<3;i++)
        {
            int cx=Mathf.RoundToInt(stars[i].x/.8f);int h=Height(level,cx);
            float rise=stars[i].y-1.1f-Floor(level,cx);
            if(rise>1.8f)Rect(cx-8,h+1,4,1);
            if(rise>3.4f)Rect(cx-4,h+3,3,1);
            Star(i,stars[i]);
        }
        Checkpoint(new Vector2(level==9?100:104,Floor(level,130)),"mid");
        if(level>=8)Checkpoint(new Vector2(224,Floor(level,280)),"late");
        Door(new Vector2(300,Floor(level,375)));
        var route=new List<Vector2>{campaign.initialSpawn};
        for(int x=8;x<=375;x+=4)route.Add(new Vector2(x*.8f,Floor(level,x)+1));
        campaign.route=route.ToArray();
        float distance=0;for(int i=1;i<route.Count;i++)distance+=Vector2.Distance(route[i-1],route[i]);
        // Star branches are explicitly included in the authored route budget.
        for(int i=0;i<3;i++)distance+=2*Mathf.Max(0,stars[i].y-(Floor(level,Mathf.RoundToInt(stars[i].x/.8f))+1));
        report.Add($"Level {level}: {theme}, estimated route with star detours {distance:F1}, width 304, stars {string.Join(";",stars.Select(s=>s.ToString()))}");
    }
    static void ConfigureCamera()
    {
        foreach(var ext in UnityEngine.Object.FindObjectsOfType<Extension.CinemachineLockYAxisExtension>())UnityEngine.Object.DestroyImmediate(ext);
        foreach(var confiner in UnityEngine.Object.FindObjectsOfType<CinemachineConfiner>())UnityEngine.Object.DestroyImmediate(confiner);
        foreach(var confiner in UnityEngine.Object.FindObjectsOfType<CinemachineConfiner2D>())UnityEngine.Object.DestroyImmediate(confiner);
    }
    [MenuItem("Tools/Campaign/Build Ten Levels")]
    public static void Build()
    {
        Directory.CreateDirectory("scripts/level-evidence");Directory.CreateDirectory("art-backup/pre-campaign");
        var original=SceneManager.GetActiveScene();
        if(original.path!="Assets/Scenes/OneOneScene.unity")throw new Exception("Open OneOneScene first. No scenes changed.");
        // Preserve both the previous disk file and all currently loaded edits before changing the scene.
        if(!File.Exists("art-backup/pre-campaign/OneOneScene.loaded.unity"))EditorSceneManager.SaveScene(original,"art-backup/pre-campaign/OneOneScene.loaded.unity",true);
        EditorSceneManager.SaveScene(original,"Assets/Editor/CampaignTemplate.unity",true);
        report=new List<string>{"UTC "+DateTime.UtcNow.ToString("O"),"No Git repository; disk and loaded-scene backups preserved."};
        ImportArt();
        theme=Themes[0];
        var old=UnityEngine.Object.FindObjectOfType<CampaignLevel>();if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
        var root=new GameObject("Campaign Objectives");objects=root.transform;
        var first=root.AddComponent<CampaignLevel>();first.number=1;first.theme=theme;first.title=Titles[0];first.nextScene="Assets/Scenes/Level02_Jungle.unity";first.initialSpawn=new Vector2(-28.01f,6.09f);first.background=SpriteAt(Kit+"Backgrounds/hills_day.png");
        // Locate existing exposed surfaces instead of moving first-level colliders.
        var candidates=UnityEngine.Object.FindObjectsOfType<Collider2D>().Where(c=>!c.isTrigger && c is BoxCollider2D && c.transform.root.name=="Level").ToArray();
        foreach(var pair in new[]{new Vector2(48,0),new Vector2(145,1),new Vector2(245,2)})
        {
            int id=(int)pair.y;float x=pair.x;
            var surface=candidates.Where(c=>c.bounds.max.y<10 && c.gameObject.layer==6)
                .OrderBy(c=>Mathf.Abs(c.bounds.center.x-x)).ThenByDescending(c=>c.bounds.max.y).First();
            x=surface.bounds.center.x;
            Star(id,new Vector2(x,surface.bounds.max.y+1.05f));
        }
        Checkpoint(new Vector2(123,-3.6f),"first-mid");
        Door(new Vector2(310,-3.6f));
        var win=UnityEngine.Object.FindObjectOfType<Winning.WinThresholdDetection>();
        foreach(var c in win.GetComponents<Collider2D>())c.enabled=false;
        var winSO=new SerializedObject(win);winSO.FindProperty("flagAnimation").objectReferenceValue=null;winSO.ApplyModifiedPropertiesWithoutUndo();
        var flag=GameObject.Find("Flag");if(flag!=null)flag.SetActive(false);
        ConfigureCamera();
        EditorSceneManager.SaveScene(original);
        report.Add("Level 1: existing Grassland geometry retained, three stars on exposed surfaces; new far-right exit x310; legacy win trigger disabled.");
        var scenes=new List<EditorBuildSettingsScene>{new EditorBuildSettingsScene(original.path,true)};
        for(int number=2;number<=10;number++)
        {
            var scene=EditorSceneManager.OpenScene("Assets/Editor/CampaignTemplate.unity",OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);
            foreach(var g in scene.GetRootGameObjects())if(g.name=="Level" || g.name=="Campaign Objectives")UnityEngine.Object.DestroyImmediate(g);
            var go=new GameObject("Level");SceneManager.MoveGameObjectToScene(go,scene);
            terrain=Obj("Terrain",Vector2.zero,go.transform).transform;objects=Obj("Interactions",Vector2.zero,go.transform).transform;
            var campaign=go.AddComponent<CampaignLevel>();theme=Themes[number-1];campaign.number=number;campaign.theme=theme;campaign.title=Titles[number-1];campaign.initialSpawn=new Vector2(2,-2.2f);campaign.background=SpriteAt(Kit+"Backgrounds/"+Backgrounds[number-1]+".png");
            campaign.nextScene=number==10?"":$"Assets/Scenes/Level{number+1:00}_{Themes[number]}.unity";
            // The inherited event publishers remain available to the existing HUD and clock.
            var legacy=Obj("WinThreshold",new Vector2(300,0),go.transform).AddComponent<Winning.WinThresholdDetection>();
            var death=UnityEngine.Object.FindObjectsOfType<Character.Lifecycle.Death>().First(d=>d.gameObject.scene==scene);
            var fall=death.GetComponent<Character.Lifecycle.FallDetection>();var so=new SerializedObject(fall);var threshold=Obj("DeathThreshold",new Vector2(0,-18),go.transform);so.FindProperty("deathThreshold").objectReferenceValue=threshold.transform;so.ApplyModifiedPropertiesWithoutUndo();
            var character=scene.GetRootGameObjects().First(g=>g.name=="Character");character.transform.position+=(Vector3)campaign.initialSpawn-death.transform.position;
            MakeLayout(number,campaign);
            ConfigureCamera();
            var scenePath=$"Assets/Scenes/Level{number:00}_{theme}.unity";
            EditorSceneManager.SaveScene(scene,scenePath);scenes.Add(new EditorBuildSettingsScene(scenePath,true));EditorSceneManager.CloseScene(scene,true);
        }
        SceneManager.SetActiveScene(original);EditorBuildSettings.scenes=scenes.ToArray();AssetDatabase.SaveAssets();
        File.WriteAllLines("scripts/level-evidence/build-results.txt",report);
        if(SceneView.lastActiveSceneView!=null)SceneView.lastActiveSceneView.LookAt(new Vector3(48,0,0),Quaternion.identity,9,true);
        Debug.Log("Ten-level campaign built; see scripts/level-evidence/build-results.txt");
    }
}
