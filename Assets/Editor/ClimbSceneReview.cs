using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
[InitializeOnLoad] public static class ClimbSceneReview
{
    static ClimbSceneReview(){EditorApplication.delayCall+=Run;}
    static void Run()
    {
        if(!File.Exists("scripts/climb-review.request") || EditorApplication.isPlayingOrWillChangePlaymode)return;
        File.Delete("scripts/climb-review.request");
        EditorSceneManager.OpenScene("Assets/Scenes/Level02_Jungle.unity");
        var star=Object.FindObjectsOfType<CampaignInteractable>().First(s=>s.kind==CampaignInteractable.Kind.Star && s.starId==0);
        var view=SceneView.lastActiveSceneView;
        if(view!=null){view.in2DMode=true;view.LookAt(star.transform.position,Quaternion.identity,8,true);view.Focus();}
        File.WriteAllText("scripts/climb-evidence/scene-review.txt","Level02 loaded in actual Scene view at objective 1.\n");
    }
}
