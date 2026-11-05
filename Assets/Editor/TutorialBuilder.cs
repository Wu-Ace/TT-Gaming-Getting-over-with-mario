using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class TutorialBuilder
{
    const string Path="Assets/Scenes/Tutorial.unity";
    static TutorialBuilder(){EditorApplication.delayCall+=Requested;}
    static void Requested(){if(File.Exists("scripts/tutorial.request")&&!EditorApplication.isPlayingOrWillChangePlaymode)Build();}
    [MenuItem("Tools/Tutorial/Build tutorial")]
    public static void Build()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty){var current=UnityEngine.SceneManagement.SceneManager.GetActiveScene();Directory.CreateDirectory("art-backup/pre-tutorial");EditorSceneManager.SaveScene(current,"art-backup/pre-tutorial/loaded-"+DateTime.UtcNow.ToString("HHmmss")+".unity",true);EditorSceneManager.SaveScene(current);}
        try {
            if(File.Exists(Path))throw new Exception("Tutorial already exists; refusing to overwrite.");
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/OneOneScene.unity");
            foreach(var root in scene.GetRootGameObjects())
                if(root.name=="Level" || root.GetComponent<CampaignLevel>()!=null)UnityEngine.Object.DestroyImmediate(root);
            foreach(var item in UnityEngine.Object.FindObjectsOfType<CampaignInteractable>())UnityEngine.Object.DestroyImmediate(item.gameObject);
            var level=new GameObject("Tutorial").AddComponent<CampaignLevel>();
            level.number=0;level.title="Collect three diamonds";level.theme="Grassland";
            level.nextScene="Assets/Scenes/OneOneScene.unity";level.initialSpawn=new Vector2(0,-1.5f);
            level.background=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/2D Platformer Kit/Backgrounds/hills_day.png");
            var terrain=new GameObject("Terrain");terrain.transform.SetParent(level.transform);
            var objects=new GameObject("Interactions");objects.transform.SetParent(level.transform);
            var flags=BindingFlags.Static|BindingFlags.NonPublic;
            typeof(CampaignBuilder).GetField("theme",flags).SetValue(null,"Grassland");
            typeof(CampaignBuilder).GetField("objects",flags).SetValue(null,objects.transform);
            Action<string,object[]> call=(name,args)=>typeof(CampaignBuilder).GetMethod(name,flags).Invoke(null,args);
            for(int x=-12;x<=12;x++)for(int y=0;y<2;y++)
                call("Tile",new object[]{y==1?(x==-12?"tl":x==12?"tr":"top"):(x==-12?"bl":x==12?"br":"bottom"),x,y,terrain.transform});
            terrain.layer=6;var floor=terrain.AddComponent<BoxCollider2D>();floor.offset=new Vector2(.4f,-2.8f);floor.size=new Vector2(20,1.6f);
            for(int i=0;i<3;i++)call("Star",new object[]{i,new Vector2(-2.4f-i*2,-1.1f)});
            foreach(var item in objects.GetComponentsInChildren<CampaignInteractable>().Where(i=>i.kind==CampaignInteractable.Kind.Star)) {
                item.GetComponent<SpriteRenderer>().sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Campaign/diamond.png");
                var hit=item.GetComponent<BoxCollider2D>();var worldSize=Vector2.Scale(hit.size,item.transform.localScale);item.transform.localScale=Vector3.one;hit.size=worldSize;
            }
            call("Door",new object[]{new Vector2(5.6f,-2)});
            foreach(var text in objects.GetComponentsInChildren<TextMesh>())text.text=text.text.Replace("STAR","DIAMOND");
            level.route=new[]{new Vector2(0,-1),new Vector2(-6.4f,-1),new Vector2(5.6f,-1)};
            call("ConfigureCamera",new object[]{});
            foreach(var cam in UnityEngine.Object.FindObjectsOfType<Cinemachine.CinemachineVirtualCamera>()){cam.m_Lens.OrthographicSize=6;var framing=cam.GetCinemachineComponent<Cinemachine.CinemachineFramingTransposer>();if(framing!=null){framing.m_TrackedObjectOffset=Vector3.zero;framing.m_DeadZoneWidth=0;}}
            EditorSceneManager.SaveScene(scene,Path);
            var scenes=EditorBuildSettings.scenes.ToList();scenes.Insert(1,new EditorBuildSettingsScene(Path,true));EditorBuildSettings.scenes=scenes.ToArray();
            Directory.CreateDirectory("scripts/tutorial-evidence");File.WriteAllText("scripts/tutorial-evidence/build.txt","PASS scene generated "+DateTime.UtcNow.ToString("O"));
            File.Delete("scripts/tutorial.request");Debug.Log("Tutorial built: left diamonds, right locked exit, next original level 1.");
        }catch(Exception e){Directory.CreateDirectory("scripts/tutorial-evidence");File.WriteAllText("scripts/tutorial-evidence/build-error.txt",e.ToString());Debug.LogException(e);}
    }
}


