using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad] public static class ClimbCourseBuilder
{
    [Serializable] public class Cell {public int x,y;}
    [Serializable] public class Plan {public int number;public string reference;public Cell[] cells;public Vector2[] stars,checkpoints,route;public Vector2 exit;public string title;public float authoredLength;}
    static readonly BindingFlags Flags=BindingFlags.Static|BindingFlags.NonPublic;
    static void Field(string n,object v)=>typeof(CampaignBuilder).GetField(n,Flags).SetValue(null,v);
    static object Call(string n,params object[] a)=>typeof(CampaignBuilder).GetMethod(n,Flags).Invoke(null,a);
    static ClimbCourseBuilder(){EditorApplication.delayCall+=Run;}
    static void Run()
    {
        if(!File.Exists("scripts/climb-build.request") || EditorApplication.isPlayingOrWillChangePlaymode)return;
        File.Delete("scripts/climb-build.request");
        try
        {
            Directory.CreateDirectory("scripts/climb-evidence");string log="Getting Over It inspired climbing courses; independent authored geometry; native .8m cells.\n";
            foreach(var entry in EditorBuildSettings.scenes.Where(s=>s.enabled && Path.GetFileName(s.path).StartsWith("Level")))
            {
                Directory.CreateDirectory("art-backup/pre-climb-layout");var backup="art-backup/pre-climb-layout/"+Path.GetFileName(entry.path);if(!File.Exists(backup))File.Copy(entry.path,backup);
                var scene=EditorSceneManager.OpenScene(entry.path);var level=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<CampaignLevel>(true)).Single();
                var plan=JsonUtility.FromJson<Plan>(File.ReadAllText($"scripts/climb-plans/L{level.number:00}.json"));
                foreach(var t in level.transform.Cast<Transform>().Where(t=>t.name=="Terrain" || t.name=="Interactions").ToArray())UnityEngine.Object.DestroyImmediate(t.gameObject);
                var terrain=new GameObject("Terrain");terrain.transform.SetParent(level.transform,false);var objects=new GameObject("Interactions");objects.transform.SetParent(level.transform,false);
                Field("theme",level.theme);Field("terrain",terrain.transform);Field("objects",objects.transform);
                var cells=new HashSet<Vector2Int>(plan.cells.Select(c=>new Vector2Int(c.x,c.y)));
                foreach(var c in cells)
                {
                    bool up=cells.Contains(c+Vector2Int.up),down=cells.Contains(c+Vector2Int.down),left=cells.Contains(c+Vector2Int.left),right=cells.Contains(c+Vector2Int.right);
                    string role=!up?(!left?"tl":!right?"tr":"top"):!down?(!left?"bl":!right?"br":"bottom"):!left?"left":!right?"right":"fill";
                    Call("Tile",role,c.x,c.y,terrain.transform);
                }
                foreach(int y in cells.Select(c=>c.y).Distinct())
                {
                    var xs=cells.Where(c=>c.y==y).Select(c=>c.x).OrderBy(x=>x).ToArray();int start=xs[0],end=start;
                    Action flush=()=>{var go=new GameObject("Solid row");go.layer=6;go.transform.SetParent(terrain.transform,false);var col=go.AddComponent<BoxCollider2D>();col.offset=new Vector2((start+end+1)*.4f,(y+.5f)*.8f-3.6f);col.size=new Vector2((end-start+1)*.8f,.8f);if(level.number==7)col.sharedMaterial=AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Art/Campaign/IceGlide.physicsMaterial2D");};
                    foreach(int x in xs.Skip(1)){if(x==end+1){end=x;continue;}flush();start=end=x;}flush();
                }
                for(int i=0;i<3;i++)Call("Star",i,plan.stars[i]);
                for(int i=0;i<plan.checkpoints.Length;i++)Call("Checkpoint",plan.checkpoints[i],i==0?"mid":"late");
                Call("Door",plan.exit);
                var door=objects.GetComponentsInChildren<CampaignInteractable>().Single(i=>i.kind==CampaignInteractable.Kind.Door);door.GetComponent<BoxCollider2D>().size=new Vector2(1.8f,2.4f)/door.transform.localScale.x;
                level.route=plan.route;level.title=plan.title;
                // Check objective centers against the actual newly authored solid cells.
                foreach(var p in plan.stars){int x=Mathf.FloorToInt(p.x/.8f),y=Mathf.FloorToInt((p.y+3.6f)/.8f);if(cells.Contains(new Vector2Int(x,y)))throw new Exception("Star embedded in terrain "+level.number);}
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                Render(level);
                log+=$"PASS L{level.number:00} {level.theme}: traced cells={cells.Count}, stars=3, checkpoints={plan.checkpoints.Length}, summit exit={plan.exit}, authored route={plan.authoredLength:F1}.\n";
            }
            EditorSceneManager.OpenScene("Assets/Scenes/OneOneScene.unity");File.WriteAllText("scripts/climb-evidence/build.txt",log);
        }
        catch(Exception e){File.WriteAllText("scripts/climb-evidence/reference-error.txt",e.ToString());Debug.LogException(e);}
    }
    static void Render(CampaignLevel level)
    {
        var go=new GameObject("Reference review camera");var c=go.AddComponent<Camera>();c.orthographic=true;float summit=level.route.Max(p=>p.y);float edge=level.route.Max(p=>p.x);c.orthographicSize=(summit+22)/2;c.transform.position=new Vector3(edge/2,summit/2,-20);c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.17f,.23f,.3f);
        var rt=new RenderTexture(1536,2048,24);c.targetTexture=rt;c.Render();RenderTexture.active=rt;var image=new Texture2D(1536,2048,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1536,2048),0,0);image.Apply();File.WriteAllBytes($"scripts/climb-evidence/L{level.number:00}-map.png",image.EncodeToPNG());RenderTexture.active=null;c.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);
    }
}
// Pipe silhouettes included.
// Final hammer reach adaptations.
// Apply outside Play mode.
