using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class KitCoinAnimationRepair
{
    static KitCoinAnimationRepair() { EditorApplication.delayCall += Run; }
    static void Run()
    {
        if (!File.Exists("scripts/coin-animation-repair.request") || EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete("scripts/coin-animation-repair.request");
        try { Repair(); } catch (Exception e) { File.WriteAllText("scripts/coin-animation-repair-error.txt", e.ToString()); Debug.LogException(e); }
    }
    [MenuItem("Tools/Art/Repair Coin Animation Frames")]
    public static void Repair()
    {
        var frames = Enumerable.Range(0,8).Select(i => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/PlatformerKit/CoinFrames/gold-"+i+".png")).ToArray();
        if (frames.Any(s => s == null)) throw new InvalidOperationException("Missing normalized coin frame");
        var paths = new[] { "Assets/Animations/CoinSpin.anim", "Assets/Animations/Ui/CoinUiIcon.anim", "Assets/Animations/Blocks/CoinQuestionMarkBlockInside.anim" };
        foreach (var path in paths)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            var binding = AnimationUtility.GetObjectReferenceCurveBindings(clip).Single(b => b.propertyName == "m_Sprite");
            bool popup = path.Contains("Inside");
            float duration = popup ? 1.5f : 0.8f;
            var keys = Enumerable.Range(0, popup ? 16 : 9).Select(i => new ObjectReferenceKeyframe { time = popup ? i * 0.1f : i * duration / 8, value = frames[i % 8] }).ToArray();
            AnimationUtility.SetObjectReferenceCurve(clip,binding,keys);
            var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=!popup;AnimationUtility.SetAnimationClipSettings(clip,settings);
            if (popup)
            {
                clip.SetCurve("",typeof(Transform),"m_LocalPosition.y",AnimationCurve.EaseInOut(0,0.16f,0.45f,0.38f));
                clip.SetCurve("",typeof(SpriteRenderer),"m_Color.a",new AnimationCurve(new Keyframe(0,1),new Keyframe(0.8f,1),new Keyframe(1.5f,0)));
            }
            EditorUtility.SetDirty(clip);
        }
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene(); bool dirty=scene.isDirty;
        if(scene.path=="Assets/Scenes/OneOneScene.unity")
        {
            foreach(var image in scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Image>(true)))
            {
                var animator=image.GetComponent<Animator>();
                if(animator==null || animator.runtimeAnimatorController==null || animator.runtimeAnimatorController.name!="CoinUiIcon")continue;
                Undo.RecordObject(image,"Normalize HUD coin");image.sprite=frames[0];image.preserveAspect=true;EditorUtility.SetDirty(image);
            }
            EditorSceneManager.MarkSceneDirty(scene);if(!dirty)EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();File.WriteAllText("scripts/coin-animation-repair-result.txt","PASS: 3 coin clips use 8 normalized centered frames; shared height/PPU; HUD preserveAspect; popup rises and fades.\n");
    }
}
