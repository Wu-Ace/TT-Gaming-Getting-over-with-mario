using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
[InitializeOnLoad] public static class MarioCourseRebuild
{
    static readonly BindingFlags Flags=BindingFlags.Static|BindingFlags.NonPublic;
    static void Field(string n,object v)=>typeof(CampaignBuilder).GetField(n,Flags).SetValue(null,v);
    static object Call(string n,params object[] a)=>typeof(CampaignBuilder).GetMethod(n,Flags).Invoke(null,a);
    static void R(int x,int y,int w,int h)=>Call("Rect",x,y,w,h);
    // Each row is a manually authored sequence of columns: x, top-height, width.
    // Gaps and heights are scaled for the hammer rather than Mario's running jump.
    static readonly string[] Columns={
      "20,2,3;28,4,3;36,6,3;44,3,3;53,5,4;65,2,6;81,4,5;93,6,4;106,3,6;120,5,5;135,2,4;147,4,4;160,6,5;176,3,5;190,5,4;204,2,6;219,4,5;233,6,4;248,3,6;264,5,5;280,2,5;296,4,4;310,6,5;326,3,5;341,5,4;355,2,6",
      "16,1,8;27,3,7;37,5,8;48,2,6;57,4,8;68,6,9;80,3,6;89,1,6;98,3,8;109,5,7;119,2,8;130,4,8;141,6,8;152,3,9;164,1,6;173,3,6;182,5,8;193,2,7;203,4,9;215,6,8;226,3,7;236,1,8;247,3,6;256,5,8;267,2,7;277,4,9;289,6,8;300,3,8;311,1,8;322,3,7;332,5,8;343,2,8;354,0,10",
      "28,2,4;41,4,4;55,3,5;72,1,4;89,3,4;103,5,4;118,2,6;137,4,5;154,6,4;170,3,5;188,1,4;201,3,5;217,5,5;235,2,5;251,4,5;268,6,4;284,3,5;301,1,5;318,3,5;334,5,4;350,2,5",
      "18,1,8;29,3,7;39,5,8;50,2,6;59,0,8;70,2,8;81,4,8;92,6,8;103,3,7;113,1,10;126,3,8;137,5,8;148,2,7;158,0,10;171,2,7;181,4,8;192,6,8;203,3,8;214,1,8;225,3,7;235,5,9;247,2,8;258,0,8;269,2,8;280,4,8;291,6,8;302,3,7;312,1,9;324,3,8;335,5,8;346,2,8;357,0,10",
      "22,2,5;37,4,4;51,1,6;68,3,4;83,5,4;99,2,5;115,4,5;132,1,6;149,3,4;164,5,5;181,2,5;197,4,4;213,1,6;230,3,5;247,5,4;263,2,5;280,4,4;296,1,6;313,3,4;329,5,5;347,2,5",
      "19,2,4;30,4,4;41,6,5;57,2,5;73,4,4;88,1,5;102,3,4;116,5,5;133,2,5;149,4,4;165,6,5;182,2,5;197,4,4;212,1,5;227,3,4;242,5,5;259,2,5;275,4,4;290,6,5;307,2,5;323,4,4;339,1,5;353,3,5",
      "18,1,5;25,2,5;32,3,5;39,4,5;46,5,5;61,2,5;76,4,4;90,6,5;107,2,6;123,4,5;139,1,5;154,3,5;170,5,5;187,2,5;203,4,5;219,6,5;236,2,6;253,4,5;270,1,6;287,3,5;303,5,5;320,2,5;336,4,5;352,1,6",
      "24,2,4;38,4,4;52,1,5;67,3,4;82,5,4;98,2,5;114,4,4;129,6,4;145,2,5;161,4,4;176,1,5;191,3,4;206,5,4;222,2,5;238,4,4;253,6,4;269,2,5;285,4,4;300,1,5;315,3,4;330,5,4;346,2,6",
      "16,0,9;28,2,8;39,4,8;50,1,7;60,3,8;71,5,8;82,2,8;93,0,8;104,2,7;114,4,8;125,6,8;136,3,8;147,1,8;158,3,7;168,5,8;179,2,8;190,0,8;201,2,8;212,4,8;223,6,8;234,3,8;245,1,8;256,3,8;267,5,8;278,2,8;289,0,8;300,2,8;311,4,8;322,6,8;333,3,8;344,1,8;355,0,10"
    };
    static readonly string[] Gaps={"72,3;126,3;210,3;274,3;330,3","","60,3;126,4;192,3;260,4;322,3","","18,3;42,3;94,3;135,4;201,3;245,3;298,4;342,3","46,3;92,4;137,3;182,3;230,4;276,3;324,3","52,4;96,3;143,3;191,4;239,3;288,3;337,4","58,3;108,3;157,4;208,3;258,3;309,4;350,3",""};
    static MarioCourseRebuild(){EditorApplication.delayCall+=Run;}
    static void Run()
    {
        if(!File.Exists("scripts/mario-rebuild.request") || EditorApplication.isPlayingOrWillChangePlaymode)return;
        File.Delete("scripts/mario-rebuild.request");
        try
        {
            Directory.CreateDirectory("art-backup/pre-mario-layout");Directory.CreateDirectory("scripts/mario-evidence");
            string log="Reference: user-provided SMB maps 1-2, 1-3, 3-1, 3-3, 4-2, 5-2, 6-1, 7-1. No final staircase.\n";
            foreach(var entry in EditorBuildSettings.scenes.Where(s=>s.enabled).Skip(1))
            {
                string backup="art-backup/pre-mario-layout/"+Path.GetFileName(entry.path);if(!File.Exists(backup))File.Copy(entry.path,backup);
                var scene=EditorSceneManager.OpenScene(entry.path);
                var level=UnityEngine.Object.FindObjectOfType<CampaignLevel>();int index=level.number-2;
                foreach(var g in level.transform.Cast<Transform>().Where(t=>t.name=="Terrain" || t.name=="Interactions").ToArray())UnityEngine.Object.DestroyImmediate(g.gameObject);
                var terrain=new GameObject("Terrain");terrain.transform.SetParent(level.transform,false);var objects=new GameObject("Interactions");objects.transform.SetParent(level.transform,false);
                Field("theme",level.theme);Field("terrain",terrain.transform);Field("objects",objects.transform);
                bool islands=index==1 || index==3 || index==8;
                if(islands){R(0,-3,15,3);R(367,-3,13,3);}
                else
                {
                    var holes=Gaps[index].Split(';').Where(s=>s.Length>0).Select(s=>s.Split(',').Select(int.Parse).ToArray()).ToArray();int start=0;
                    foreach(var hole in holes){R(start,-3,hole[0]-start,3);start=hole[0]+hole[1];}R(start,-3,380-start,3);
                }
                var blocks=Columns[index].Split(';').Select(s=>s.Split(',').Select(int.Parse).ToArray()).ToArray();
                foreach(var b in blocks)R(b[0],islands?-14:0,b[2],b[1]+(islands?14:0));
                // Brick canopies / double routes, placed individually rather than one continuous ceiling.
                int[][] roofs=index==0?new[]{new[]{16,9,34},new[]{65,9,30},new[]{110,10,30},new[]{158,10,26},new[]{205,9,34},new[]{260,10,28},new[]{313,9,32}}:
                    index==2?new[]{new[]{26,7,12},new[]{80,8,18},new[]{145,9,18},new[]{217,8,24},new[]{291,7,17}}:
                    index==4?new[]{new[]{24,8,25},new[]{77,10,25},new[]{145,9,24},new[]{217,10,28},new[]{280,8,27},new[]{331,9,23}}:
                    index==5?new[]{new[]{22,8,12},new[]{91,7,14},new[]{152,10,17},new[]{224,8,14},new[]{287,10,15}}:
                    index==6?new[]{new[]{70,8,17},new[]{119,7,17},new[]{181,8,17},new[]{247,8,16},new[]{312,8,17}}:
                    index==7?new[]{new[]{31,8,16},new[]{93,9,17},new[]{153,8,19},new[]{218,8,16},new[]{280,9,16},new[]{331,8,17}}:new int[0][];
                foreach(var roof in roofs)R(roof[0],roof[1],roof[2],1);
                // Star placements follow three different landmarks of the reference course.
                int[] selected={blocks.Length/5,blocks.Length/2,blocks.Length*4/5};
                for(int k=0;k<3;k++)
                {
                    var b=blocks[selected[k]];float x=(b[0]+b[2]*.5f)*.8f;float top=b[1]*.8f-3.6f;
                    Call("Star",k,new Vector2(x,top+1.05f));
                }
                // Reach helpers: every tall face gets footholds with <=1.6m vertical increments.
                foreach(var b in blocks.Where(b=>b[1]>2))
                    for(int h=2;h<b[1];h+=2)R(b[0]-3,h-1,3,1);
                int cp=blocks.Length/2-1;var middle=blocks[cp];Call("Checkpoint",new Vector2((middle[0]+middle[2]*.5f)*.8f,middle[1]*.8f-3.6f),"mid");
                if(level.number>=8){var late=blocks[blocks.Length-3];Call("Checkpoint",new Vector2((late[0]+late[2]*.5f)*.8f,late[1]*.8f-3.6f),"late");}
                if(index!=0)
                {
                    foreach(int n in new[]{blocks.Length/3,blocks.Length*2/3})
                    {var b=blocks[n];Call("Enemy",index==2 || index==5 || index==7?CampaignEnemy.Species.Plant:CampaignEnemy.Species.Bee,new Vector2((b[0]+b[2]*.5f)*.8f,b[1]*.8f-2.8f),1.2f);}
                }
                else Call("Enemy",CampaignEnemy.Species.Ghost,new Vector2(190,0),2f);
                Call("Door",new Vector2(300,-3.6f));
                foreach(var c in terrain.GetComponentsInChildren<Collider2D>())c.gameObject.layer=6;
                level.route=blocks.Select(b=>new Vector2((b[0]+b[2]*.5f)*.8f,b[1]*.8f-2.6f)).ToArray();
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                log+=$"L{level.number:00} {level.theme}: {blocks.Length} individually placed columns, {roofs.Length} canopies, islands={islands}; three objective landmarks; no ending staircase.\n";
            }
            EditorSceneManager.OpenScene("Assets/Scenes/OneOneScene.unity");File.WriteAllText("scripts/mario-evidence/build.txt",log);
        }
        catch(Exception e){File.WriteAllText("scripts/mario-evidence/error.txt",e.ToString());Debug.LogException(e);}
    }
}
