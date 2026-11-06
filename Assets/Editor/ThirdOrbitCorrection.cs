using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad] public static class ThirdOrbitCorrection
{
    static ThirdOrbitCorrection(){EditorApplication.delayCall+=Run;}
    public static readonly Vector2Int[] Cells={new Vector2Int(60,7),new Vector2Int(62,9),new Vector2Int(62,5),new Vector2Int(64,7),new Vector2Int(70,7),new Vector2Int(72,9),new Vector2Int(72,5),new Vector2Int(73,7)};
    static void Run(){if(!File.Exists("scripts/third-orbit-correction.request")||EditorApplication.isPlayingOrWillChangePlaymode)return;
        if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)return;
        File.Delete("scripts/third-orbit-correction.request");
        try{
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Level03_Beach.unity");var level=UnityEngine.Object.FindObjectOfType<CampaignLevel>();
            var terrain=level.transform.Find("Terrain");var objects=level.transform.Find("Interactions");
            var plan=JsonUtility.FromJson<Level03DesignedBuilder.Plan>(File.ReadAllText("scripts/level03-designed.json"));
            var renderers=terrain.GetComponentsInChildren<SpriteRenderer>();
            var sample=renderers.First(r=>Cells.Any(c=>Vector2.Distance(r.transform.position,new Vector2((c.x+.5f)*.8f,(c.y+.5f)*.8f-3.6f))<.01f));var sprite=sample.sprite;
            foreach(var r in renderers.Where(r=>Cells.Any(c=>Vector2.Distance(r.transform.position,new Vector2((c.x+.5f)*.8f,(c.y+.5f)*.8f-3.6f))<.01f)).ToArray())UnityEngine.Object.DestroyImmediate(r.gameObject);
            foreach(var box in terrain.GetComponentsInChildren<BoxCollider2D>().Where(b=>new[]{5,7,9}.Any(y=>Mathf.Abs(b.offset.y-((y+.5f)*.8f-3.6f))<.01f)).ToArray())UnityEngine.Object.DestroyImmediate(box.gameObject);
            foreach(int y in new[]{5,7,9}){
                var xs=plan.cells.Where(c=>c.y==y&&!Cells.Contains(new Vector2Int(c.x,c.y))).Select(c=>c.x).OrderBy(x=>x).ToArray();
                for(int i=0;i<xs.Length;){int a=xs[i],z=a;while(++i<xs.Length&&xs[i]==z+1)z=xs[i];var g=new GameObject("Solid row");g.layer=6;g.transform.SetParent(terrain,false);var b=g.AddComponent<BoxCollider2D>();b.offset=new Vector2((a+z+1)*.4f,(y+.5f)*.8f-3.6f);b.size=new Vector2((z-a+1)*.8f,.8f);}
            }
            foreach(var orbit in objects.GetComponentsInChildren<OrbitRidePlatform>().Where(o=>o.center.x>30).ToArray())UnityEngine.Object.DestroyImmediate(orbit.gameObject);
            Add(objects,sprite);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            File.WriteAllText("scripts/third-orbit-correction.txt","PASS saved two groups of four .8m square orbit platforms, radius 1.6m, period 7s, centers (50,2.4)/(58,2.4). Removed eight static cells and rebuilt only affected collider rows. Left ascent orbit group retained. Play verification pending.");
        }catch(Exception e){File.WriteAllText("scripts/third-orbit-correction-error.txt",e.ToString());Debug.LogException(e);}
    }
    public static void Add(Transform parent,Sprite sprite){foreach(float x in new[]{50f,58f})for(int i=0;i<4;i++){
        var g=new GameObject("Orbit square "+x+" "+i);g.layer=6;g.transform.SetParent(parent,false);var o=g.AddComponent<OrbitRidePlatform>();o.center=new Vector2(x,2.4f);o.radius=1.6f;o.period=7;o.phase=i*.25f;float angle=o.phase*Mathf.PI*2;g.transform.position=o.center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*o.radius;
        var rb=g.GetComponent<Rigidbody2D>();rb.bodyType=RigidbodyType2D.Kinematic;rb.useFullKinematicContacts=true;rb.interpolation=RigidbodyInterpolation2D.Interpolate;g.AddComponent<BoxCollider2D>().size=Vector2.one*.8f;
        var visual=new GameObject("Native Beach cell");visual.transform.SetParent(g.transform,false);visual.transform.localScale=Vector3.one*5;var r=visual.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sortingOrder=10;
    }}
}
// Apply confirmed Ferris-wheel orbit groups in Edit mode.
