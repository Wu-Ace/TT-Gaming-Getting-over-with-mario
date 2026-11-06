using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad] public static class Level03DesignedBuilder
{
    [Serializable] public class Donut {public int x,y,width;}
    [Serializable] public class Cell {public int x,y;}
    [Serializable] public class Plan {public int number;public string reference;public Cell[] cells;public Vector2[] stars,checkpoints,route;public Vector2 exit,spawn;public Donut[] donuts;public string title;public float authoredLength;}
    static readonly BindingFlags Flags=BindingFlags.Static|BindingFlags.NonPublic;
    static void Field(string n,object v)=>typeof(CampaignBuilder).GetField(n,Flags).SetValue(null,v);
    static object Call(string n,params object[] a)=>typeof(CampaignBuilder).GetMethod(n,Flags).Invoke(null,a);
    static Level03DesignedBuilder(){EditorApplication.delayCall+=Run;}
    public static void ApplyReferenceBatch()
    {
        File.WriteAllText("scripts/level03-designed.request", "reference silhouette");
        Run();
        if(File.Exists("scripts/level03-designed-evidence/reference-error.txt"))throw new Exception(File.ReadAllText("scripts/level03-designed-evidence/reference-error.txt"));
        KitTerrainRepair.Repair();
    }
    static void Run()
    {
        if(!File.Exists("scripts/level03-designed.request") || EditorApplication.isPlayingOrWillChangePlaymode)return;
        if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty){Debug.LogWarning("Save current scene before Level03 build");return;}
        File.Delete("scripts/level03-designed.request");
        try
        {
            Directory.CreateDirectory("scripts/level03-designed-evidence");string log="Getting Over It inspired climbing courses; independent authored geometry; native .8m cells.\n";
            foreach(var entry in EditorBuildSettings.scenes.Where(s=>s.path=="Assets/Scenes/Level03_Beach.unity"))
            {
                Directory.CreateDirectory("art-backup/pre-level03-designed");var backup="art-backup/pre-level03-designed/"+Path.GetFileName(entry.path);if(!File.Exists(backup))File.Copy(entry.path,backup);
                var scene=EditorSceneManager.OpenScene(entry.path);var level=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<CampaignLevel>(true)).Single();
                var plan=JsonUtility.FromJson<Plan>(File.ReadAllText("scripts/level03-designed.json"));
                // Preserve the existing orbit rigs while replacing the authored route.
                var orbits=level.GetComponentsInChildren<OrbitRidePlatform>(true);
                foreach(var orbit in orbits)orbit.transform.SetParent(level.transform,true);
                foreach(var t in level.transform.Cast<Transform>().Where(t=>t.name=="Terrain" || t.name=="Interactions").ToArray())UnityEngine.Object.DestroyImmediate(t.gameObject);
                var terrain=new GameObject("Terrain");terrain.transform.SetParent(level.transform,false);var objects=new GameObject("Interactions");objects.transform.SetParent(level.transform,false);
                foreach(var orbit in orbits){float angle=orbit.phase*Mathf.PI*2;orbit.transform.position=orbit.center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*orbit.radius;orbit.transform.SetParent(objects.transform,true);}
                level.initialSpawn=plan.spawn;Field("theme",level.theme);Field("terrain",terrain.transform);Field("objects",objects.transform);
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
                const string donutArt="Assets/Art/Campaign/donut-beach.png";
                AssetDatabase.ImportAsset(donutArt,ImportAssetOptions.ForceSynchronousImport);
                var importer=(TextureImporter)AssetImporter.GetAtPath(donutArt);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=150;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.wrapMode=TextureWrapMode.Clamp;importer.mipmapEnabled=false;importer.SaveAndReimport();
                foreach(var spec in plan.donuts){
                    var g=new GameObject("Donut falling bridge");g.layer=6;g.transform.SetParent(objects.transform,false);g.transform.position=new Vector3((spec.x+spec.width*.5f)*.8f,(spec.y+1)*.8f-3.6f-.13f,0);
                    var box=g.AddComponent<BoxCollider2D>();box.size=new Vector2(spec.width*.8f,.26f);
                    var rb=g.AddComponent<Rigidbody2D>();rb.bodyType=RigidbodyType2D.Kinematic;rb.useFullKinematicContacts=true;rb.interpolation=RigidbodyInterpolation2D.Interpolate;
                    g.AddComponent<DonutFallPlatform>();
                    for(int i=0;i<spec.width;i++){var cell=new GameObject("Native ring log");cell.transform.SetParent(g.transform,false);cell.transform.localPosition=new Vector3((i+.5f-spec.width*.5f)*.8f,0,0);var sr=cell.AddComponent<SpriteRenderer>();sr.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(donutArt);sr.sortingOrder=10;}
                }
                for(int i=0;i<3;i++)Call("Star",i,plan.stars[i]);
                foreach(var gem in objects.GetComponentsInChildren<CampaignInteractable>().Where(i=>i.kind==CampaignInteractable.Kind.Star)){gem.name="Objective Diamond "+(gem.starId+1);gem.GetComponent<SpriteRenderer>().sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Campaign/diamond.png");}
                foreach(var r in terrain.GetComponentsInChildren<SpriteRenderer>()){
                    var original=r.sprite;
                    r.sprite=(Sprite)typeof(KitTerrainRepair).GetMethod("SeamSafeSprite",Flags).Invoke(null,new object[]{original});
                    r.transform.localScale=Vector3.one*5;
                }
                for(int i=0;i<plan.checkpoints.Length;i++)Call("Checkpoint",plan.checkpoints[i],i==0?"mid":"late");
                Call("Door",plan.exit);
                var door=objects.GetComponentsInChildren<CampaignInteractable>().Single(i=>i.kind==CampaignInteractable.Kind.Door);door.GetComponent<BoxCollider2D>().size=new Vector2(1.8f,2.4f)/door.transform.localScale.x;
                foreach(var text in objects.GetComponentsInChildren<TextMesh>())text.text=text.text.Replace("STAR","DIAMOND");
                level.route=plan.route;level.title=plan.title;
                // Check objective centers against the actual newly authored solid cells.
                foreach(var p in plan.stars){int x=Mathf.FloorToInt(p.x/.8f),y=Mathf.FloorToInt((p.y+3.6f)/.8f);if(cells.Contains(new Vector2Int(x,y)))throw new Exception("Star embedded in terrain "+level.number);}
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                Render(level);
                log+=$"PASS L{level.number:00} {level.theme}: traced cells={cells.Count}, stars=3, checkpoints={plan.checkpoints.Length}, summit exit={plan.exit}, authored route={plan.authoredLength:F1}.\n";
            }
            EditorSceneManager.OpenScene("Assets/Scenes/Level03_Beach.unity");if(SceneView.lastActiveSceneView!=null)SceneView.lastActiveSceneView.LookAt(new Vector3(41,19,0),Quaternion.identity,25,true);File.WriteAllText("scripts/level03-designed-evidence/build.txt",log);
        }
        catch(Exception e){File.WriteAllText("scripts/level03-designed-evidence/reference-error.txt",e.ToString());Debug.LogException(e);}
    }
    static void Render(CampaignLevel level)
    {
        var go=new GameObject("Reference review camera");var c=go.AddComponent<Camera>();c.orthographic=true;c.orthographicSize=24;c.transform.position=new Vector3(41,19,-20);c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.17f,.23f,.3f);
        var rt=new RenderTexture(2060,1160,24);c.targetTexture=rt;c.Render();RenderTexture.active=rt;var image=new Texture2D(2060,1160,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,2060,1160),0,0);image.Apply();File.WriteAllBytes($"scripts/level03-designed-evidence/L{level.number:00}-map.png",image.EncodeToPNG());RenderTexture.active=null;c.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);
    }
}
// Pipe silhouettes included.
// Final hammer reach adaptations.
// Apply outside Play mode.
// Final branch access and safe checkpoint.
// Aerial revision: disconnected platforms above void.
// Place objectives across both aerial crossings.

// Second aerial crossing uses varied landing widths and alternating rises.
// Reference ledges cleared from cliff cells, 2026-11-06.
