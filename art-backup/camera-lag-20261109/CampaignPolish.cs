using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad] public static class CampaignPolish
{
    static CampaignPolish(){EditorApplication.delayCall+=Run;}
    static void Run()
    {
        if(!File.Exists("scripts/campaign-polish.request") || EditorApplication.isPlayingOrWillChangePlaymode)return;
        File.Delete("scripts/campaign-polish.request");
        try
        {
            foreach(var path in Directory.GetFiles("Assets/Art/Campaign","*.png",SearchOption.AllDirectories))
            {
                var ti=AssetImporter.GetAtPath(path.Replace('\\','/')) as TextureImporter;if(ti==null)continue;
                if(ti.textureType==TextureImporterType.Sprite && Mathf.Approximately(ti.spritePixelsPerUnit,150))continue;
                ti.textureType=TextureImporterType.Sprite;ti.spriteImportMode=SpriteImportMode.Single;ti.spritePixelsPerUnit=150;ti.mipmapEnabled=false;ti.wrapMode=TextureWrapMode.Clamp;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.SaveAndReimport();
            }
            string log="UTC "+DateTime.UtcNow.ToString("O")+"\n";
            foreach(var entry in EditorBuildSettings.scenes.Where(s=>s.enabled))
            {
                var scene=EditorSceneManager.OpenScene(entry.path,OpenSceneMode.Single);
                if(entry.path!="Assets/Scenes/OneOneScene.unity")
                    foreach(var duplicate in scene.GetRootGameObjects().Where(g=>g.name=="Campaign Objectives").ToArray())UnityEngine.Object.DestroyImmediate(duplicate);
                var level=UnityEngine.Object.FindObjectOfType<CampaignLevel>();
                var all=UnityEngine.Object.FindObjectsOfType<CampaignInteractable>();
                var door=all.Single(s=>s.kind==CampaignInteractable.Kind.Door);
                string folder="Assets/Art/Campaign/"+level.theme+"/";
                door.inactiveSprite=AssetDatabase.LoadAssetAtPath<Sprite>(folder+"door.png");door.activeSprite=AssetDatabase.LoadAssetAtPath<Sprite>(folder+"door-open.png");
                door.GetComponent<SpriteRenderer>().sprite=door.inactiveSprite;door.transform.localScale=Vector3.one*(2.4f/door.inactiveSprite.bounds.size.y);
                var hit=door.GetComponent<BoxCollider2D>();hit.size=new Vector2(1.8f,2.4f)/door.transform.localScale.x;
                if(level.number==9)
                {
                    var safe=all.First(s=>s.kind==CampaignInteractable.Kind.Checkpoint && s.link=="mid");
                    safe.transform.position=new Vector3(100,safe.transform.position.y,0);safe.respawn=new Vector2(100,safe.respawn.y);
                }
                var stars=all.Where(s=>s.kind==CampaignInteractable.Kind.Star).ToArray();
                if(stars.Length!=3 || stars.Select(s=>s.starId).Distinct().Count()!=3)throw new Exception("Bad stars "+entry.path);
                var renderers=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<SpriteRenderer>(true)).Where(r=>r.sprite!=null).ToArray();
                if(level.number>1)
                {
                    var wrong=renderers.Where(r=>AssetDatabase.GetAssetPath(r.sprite).StartsWith("Assets/Art/Campaign/") && !AssetDatabase.GetAssetPath(r.sprite).StartsWith(folder)).ToArray();
                    if(wrong.Length!=0)throw new Exception("Mixed tile themes "+entry.path);
                }
                // All level-authored collision surfaces use the project's collision layer.
                foreach(var c in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Collider2D>(true)))
                    if(c.transform.IsChildOf(level.transform) && !c.isTrigger && c.GetComponent<CampaignInteractable>()==null)c.gameObject.layer=6;
                foreach(var vcam in UnityEngine.Object.FindObjectsOfType<Cinemachine.CinemachineVirtualCamera>())
                {
                    var framing=vcam.GetCinemachineComponent<Cinemachine.CinemachineFramingTransposer>();
                    if(framing!=null){framing.m_ScreenY=.55f;framing.m_DeadZoneHeight=.08f;framing.m_SoftZoneHeight=.8f;framing.m_YDamping=.35f;framing.m_XDamping=.35f;}
                }
                foreach(var star in stars)
                {
                    var label=GameObject.Find("Star marker");
                    // Labels belonging to collected objectives disappear with their star.
                    var nearest=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<TextMesh>(true)).Where(t=>t.name=="Star marker").OrderBy(t=>Vector2.Distance(t.transform.position,star.transform.position)).FirstOrDefault();
                    if(nearest!=null)nearest.transform.SetParent(star.transform,true);
                }
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                log+=$"PASS L{level.number:00} theme={level.theme} stars=3 exit={door.transform.position.x:F1} renderers={renderers.Length}\n";
            }
            EditorSceneManager.OpenScene("Assets/Scenes/OneOneScene.unity");
            File.WriteAllText("scripts/level-evidence/structure-results.txt",log);
        }
        catch(Exception e){File.WriteAllText("scripts/level-evidence/polish-error.txt",e.ToString());Debug.LogException(e);}
    }
}
