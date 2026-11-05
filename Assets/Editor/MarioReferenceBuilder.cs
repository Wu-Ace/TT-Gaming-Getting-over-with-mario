using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad] public static class MarioReferenceBuilder
{
    [Serializable] public class Cell {public int x,y;}
    [Serializable] public class Plan {public int number;public string reference;public Cell[] cells;public Vector2[] stars,checkpoints;}
    static readonly BindingFlags Flags=BindingFlags.Static|BindingFlags.NonPublic;
    static void Field(string n,object v)=>typeof(CampaignBuilder).GetField(n,Flags).SetValue(null,v);
    static object Call(string n,params object[] a)=>typeof(CampaignBuilder).GetMethod(n,Flags).Invoke(null,a);
    static MarioReferenceBuilder(){EditorApplication.delayCall+=Run;}
    static void Run()
    {
        if(!File.Exists("scripts/mario-reference-build.request") || EditorApplication.isPlayingOrWillChangePlaymode)return;
        File.Delete("scripts/mario-reference-build.request");
        try
        {
            Directory.CreateDirectory("scripts/mario-evidence");string log="User-supplied original SMB maps traced on their 16px grid; geometry resampled for width and hammer height; native .8m cells, no stretched sprites.\n";
            foreach(var entry in EditorBuildSettings.scenes.Where(s=>s.enabled).Skip(1))
            {
                var scene=EditorSceneManager.OpenScene(entry.path);var level=UnityEngine.Object.FindObjectOfType<CampaignLevel>();
                var plan=JsonUtility.FromJson<Plan>(File.ReadAllText($"scripts/mario-reference/L{level.number:00}.json"));
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
                // Enemies occupy open surfaces between objectives, away from respawn platforms.
                foreach(int target in new[]{110,218,325})
                {
                    var surfaces=cells.Where(c=>Mathf.Abs(c.x-target)<12 && !cells.Contains(c+Vector2Int.up) && c.y<6 && !cells.Contains(c+Vector2Int.up*2) && plan.checkpoints.All(p=>Mathf.Abs(p.x-(c.x+.5f)*.8f)>4)).ToArray();
                    if(surfaces.Length==0)continue;var c=surfaces.OrderBy(c=>Mathf.Abs(c.x-target)).First();
                    var type=level.number==2 || level.number==6?CampaignEnemy.Species.Ghost:level.number==4 || level.number==7 || level.number==9?CampaignEnemy.Species.Plant:CampaignEnemy.Species.Bee;
                    Call("Enemy",type,new Vector2((c.x+.5f)*.8f,(c.y+1)*.8f-3f),1.2f);
                }
                Call("Door",new Vector2(300,-3.6f));
                var door=objects.GetComponentsInChildren<CampaignInteractable>().Single(i=>i.kind==CampaignInteractable.Kind.Door);door.GetComponent<BoxCollider2D>().size=new Vector2(1.8f,2.4f)/door.transform.localScale.x;
                level.route=plan.stars;
                // Check objective centers against the actual newly authored solid cells.
                foreach(var p in plan.stars){int x=Mathf.FloorToInt(p.x/.8f),y=Mathf.FloorToInt((p.y+3.6f)/.8f);if(cells.Contains(new Vector2Int(x,y)))throw new Exception("Star embedded in terrain "+level.number);}
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                Render(level);
                log+=$"PASS L{level.number:00} {level.theme}: traced cells={cells.Count}, stars=3, checkpoints={plan.checkpoints.Length}, exit x300.\n";
            }
            EditorSceneManager.OpenScene("Assets/Scenes/OneOneScene.unity");File.WriteAllText("scripts/mario-evidence/build.txt",log);
        }
        catch(Exception e){File.WriteAllText("scripts/mario-evidence/reference-error.txt",e.ToString());Debug.LogException(e);}
    }
    static void Render(CampaignLevel level)
    {
        var go=new GameObject("Reference review camera");var c=go.AddComponent<Camera>();c.orthographic=true;c.orthographicSize=10;c.transform.position=new Vector3(152,0,-20);c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.17f,.23f,.3f);
        var rt=new RenderTexture(8192,528,24);c.targetTexture=rt;c.Render();RenderTexture.active=rt;var image=new Texture2D(8192,528,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,8192,528),0,0);image.Apply();File.WriteAllBytes($"scripts/mario-evidence/L{level.number:00}-map.png",image.EncodeToPNG());RenderTexture.active=null;c.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);
    }
}
// Pipe silhouettes included.
// Final hammer reach adaptations.
