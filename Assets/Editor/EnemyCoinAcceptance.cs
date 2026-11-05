using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Enemy;

[InitializeOnLoad]
public static class EnemyCoinAcceptance
{
    static EnemyDeath subject;
    static GameObject koopa;
    static GameObject popup;
    static float started;
    static Vector3 size;
    static float initialY;
    static int stage, events;
    static string log;
    static EnemyCoinAcceptance()
    {
        EditorApplication.delayCall += StartRequested;
        EditorApplication.playModeStateChanged += Changed;
    }
    static void StartRequested()
    {
        if (!File.Exists("scripts/enemy-coin-test.request") || EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete("scripts/enemy-coin-test.request");
        SessionState.SetBool("EnemyCoinAcceptance.Pending",true);
        EditorApplication.EnterPlaymode();
    }
    static void Changed(PlayModeStateChange state)
    {
        if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("EnemyCoinAcceptance.Pending",false))return;
        SessionState.SetBool("EnemyCoinAcceptance.Pending",false);
        EditorApplication.delayCall+=Begin;
    }
    static void Check(bool pass,string message)
    {
        log+=(pass?"PASS ":"FAIL ")+message+"\n";
        if(!pass)throw new InvalidOperationException(message);
    }
    static void Begin()
    {
        try
        {
            log="UTC "+DateTime.UtcNow.ToString("O")+"\n";
            var clone=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Goomba.prefab"));
            clone.name="Acceptance Goomba";
            clone.transform.position=new Vector3(4,-3.5f,0);
            subject=clone.GetComponentInChildren<EnemyDeath>();
            subject.transform.position=new Vector3(Camera.main.transform.position.x+2,-3.5f,0);
            var renderer=subject.GetComponentsInChildren<SpriteRenderer>().First(r=>r.gameObject.activeInHierarchy);
            size=renderer.bounds.size; initialY=subject.transform.position.y;
            events=0;subject.OnEnemyDeath+=_=>events++;
            subject.SendMessage("DieIfSelf",subject.gameObject);
            subject.Defeat();
            Check(events==1,"Repeated defeat is ignored");
            var corpse=subject.GetComponentsInChildren<SpriteRenderer>().First(r=>r.gameObject.activeInHierarchy);
            Check(corpse!=renderer && !renderer.gameObject.activeInHierarchy,"Authored death entity restored");
            Check(Mathf.Abs(corpse.bounds.size.x-size.x)<0.001f,"Death visual retains full width");
            var group=subject.GetComponent<UnityEngine.Rendering.SortingGroup>();
            Check(group!=null && group.sortingOrder==32760 && SortingLayer.GetLayerValueFromID(group.sortingLayerID)==SortingLayer.layers.Max(l=>l.value),"Corpse renders above all world layers");
            Check(subject.GetComponentsInChildren<Collider2D>(true).All(c=>!c.enabled),"Corpse cannot collide or hurt player");
            Check(subject.GetComponent<Rigidbody2D>().velocity.y>0,"Upward launch velocity");
            koopa=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/KoopaTroopa.prefab"));
            koopa.transform.position=new Vector3(12,-3.5f,0);
            var lifecycle=koopa.GetComponentInChildren<EnemyDeath>();
            lifecycle.SendMessage("DieIfSelf",lifecycle.gameObject);
            Check(koopa.GetComponentInChildren<KoopaTroopaShellContactDetection>()!=null,"Koopa stomp retains active shell");
            var paths=new[]{"Assets/Animations/CoinSpin.anim","Assets/Animations/Ui/CoinUiIcon.anim","Assets/Animations/Blocks/CoinQuestionMarkBlockInside.anim"};
            foreach(var path in paths)
            {
                var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                var binding=AnimationUtility.GetObjectReferenceCurveBindings(clip).Single(b=>b.propertyName=="m_Sprite");
                var sprites=AnimationUtility.GetObjectReferenceCurve(clip,binding).Select(k=>(Sprite)k.value).Distinct().ToArray();
                Check(sprites.Length==8 && sprites.All(s=>Mathf.Abs(s.bounds.size.y-0.16f)<0.0001f && s.pivot==new Vector2(60,60)),path+": 8 equal-height centered frames");
            }
            popup=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Obstacles/CoinQuestionMarkBlock.prefab"));
            popup.SetActive(true);
            popup.transform.position=new Vector3(Camera.main.transform.position.x+2,-1.5f,0);
            popup.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Inside").gameObject.SetActive(true);
            Directory.CreateDirectory("scripts/enemy-coin-evidence");
            stage=0;started=Time.time;EditorApplication.update+=Tick;
        }
        catch(Exception e){Finish(e);}
    }
    static void Tick()
    {
        try
        {
            float t=Time.time-started;
            if(stage==0 && t>=0.18f)
            {
                Check(subject!=null && subject.transform.position.y>initialY+0.3f,"Actual physics hop rises");
                ScreenCapture.CaptureScreenshot(Path.GetFullPath("scripts/enemy-coin-evidence/hop-and-coin.png"));
                stage++;
            }
            if(stage==1 && t>=1.3f)
            {
                Check(subject!=null && subject.transform.position.y<initialY-3,"Corpse falls through floor below spawn");
                ScreenCapture.CaptureScreenshot(Path.GetFullPath("scripts/enemy-coin-evidence/fall-and-coin.png"));
                UnityEngine.Object.Destroy(koopa);UnityEngine.Object.Destroy(popup);stage++;
            }
            if(stage==2 && t>=4.3f){Check(subject==null,"Corpse automatically destroyed");Finish(null);}
        }
        catch(Exception e){Finish(e);}
    }
    static void Finish(Exception error)
    {
        EditorApplication.update-=Tick;
        Directory.CreateDirectory("scripts/enemy-coin-evidence");
        File.WriteAllText("scripts/enemy-coin-evidence/play-results.txt",log+(error==null?"COMPLETE\n":error.ToString()));
        EditorApplication.ExitPlaymode();
    }
}
