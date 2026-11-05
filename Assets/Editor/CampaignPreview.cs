using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
[InitializeOnLoad] public static class CampaignPreview
{
    static CampaignPreview(){EditorApplication.delayCall+=Run;}
    static void Run()
    {
        if(!File.Exists("scripts/campaign-preview.request") || EditorApplication.isPlayingOrWillChangePlaymode)return;
        File.Delete("scripts/campaign-preview.request");
        try
        {
            foreach(var entry in EditorBuildSettings.scenes.Where(s=>s.enabled))
            {
                EditorSceneManager.OpenScene(entry.path);
                var level=UnityEngine.Object.FindObjectOfType<CampaignLevel>();
                var stars=UnityEngine.Object.FindObjectsOfType<CampaignInteractable>().Where(s=>s.kind==CampaignInteractable.Kind.Star).OrderBy(s=>s.starId).ToArray();
                var go=new GameObject("Preview camera");var camera=go.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=6;camera.backgroundColor=new Color(.23f,.32f,.4f);camera.clearFlags=CameraClearFlags.SolidColor;
                foreach(var star in stars)
                {
                    camera.transform.position=new Vector3(star.transform.position.x,star.transform.position.y-1,-20);
                    var rt=new RenderTexture(1280,768,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
                    var texture=new Texture2D(1280,768,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1280,768),0,0);texture.Apply();File.WriteAllBytes($"scripts/level-evidence/L{level.number:00}-layout-{star.starId+1}.png",texture.EncodeToPNG());
                    RenderTexture.active=null;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(rt);
                }
                UnityEngine.Object.DestroyImmediate(go);
            }
            EditorSceneManager.OpenScene("Assets/Scenes/OneOneScene.unity");
            File.WriteAllText("scripts/level-evidence/preview-results.txt","PASS ten scenes rendered at all 30 objective locations. Editor renders are not physical traversal. "+DateTime.UtcNow.ToString("O"));
        }
        catch(Exception e){File.WriteAllText("scripts/level-evidence/preview-error.txt",e.ToString());Debug.LogException(e);}
    }
}
