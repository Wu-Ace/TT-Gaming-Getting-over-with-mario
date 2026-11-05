using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
[InitializeOnLoad] public static class MenuUISetup
{
    static MenuUISetup(){EditorApplication.delayCall+=Run;}
    static void Run()
    {
        if(!File.Exists("scripts/menu-ui-setup.request") || EditorApplication.isPlayingOrWillChangePlaymode)return;
        File.Delete("scripts/menu-ui-setup.request");
        try
        {
            foreach(string path in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/Resources/MenuUI"}).Select(AssetDatabase.GUIDToAssetPath))
            {
                var t=(TextureImporter)AssetImporter.GetAtPath(path);t.textureCompression=TextureImporterCompression.Uncompressed;t.wrapMode=TextureWrapMode.Clamp;t.mipmapEnabled=false;t.alphaIsTransparency=true;t.SaveAndReimport();
            }
            string scenePath="Assets/Scenes/MainMenu.unity";
            // Never discard a dirty user scene to create the menu.
            if(SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Active scene is dirty; save it before menu setup.");
            string previous=SceneManager.GetActiveScene().path;
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var camera=new GameObject("Menu Camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color32(35,63,56,255);camera.gameObject.AddComponent<AudioListener>();
            EditorSceneManager.SaveScene(scene,scenePath);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(scenePath,true)}.Concat(EditorBuildSettings.scenes.Where(s=>s.path!=scenePath)).ToArray();
            EditorSceneManager.OpenScene(scenePath);
            File.WriteAllText("scripts/ui-evidence/setup.txt","PASS menu scene, build entry, texture imports. "+DateTime.UtcNow.ToString("O"));
        }catch(Exception e){File.WriteAllText("scripts/ui-evidence/setup-error.txt",e.ToString());Debug.LogException(e);}
    }
}
