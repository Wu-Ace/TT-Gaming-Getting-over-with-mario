using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class InstallValleyParallax
{
    const string Folder = "Assets/Resources/ValleyParallax";
    const string Evidence = "scripts/parallax-20261111";

    [MenuItem("Tools/Art/Install Six Layer Valley Backgrounds")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play before installing backgrounds.");
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new Exception("Preserving unsaved scene edits; save before installation.");
        foreach (string file in new[] { "sky", "mountains", "hill", "architecture", "plain", "foreground" })
        {
            string path = Folder + "/" + file + ".png";
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null) throw new Exception("Missing layer: " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spritePivot = new Vector2(.5f, .5f);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 4096;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
        Directory.CreateDirectory(Evidence + "/backup");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        var report = new System.Collections.Generic.List<string>();
        try
        {
            foreach (var entry in EditorBuildSettings.scenes.Where(s => s.enabled))
            {
                string originalScene = File.ReadAllText(entry.path);
                // Install only authored gameplay scenes; menus keep their own presentation.
                var scene = EditorSceneManager.OpenScene(entry.path);
                var level = UnityEngine.Object.FindObjectOfType<CampaignLevel>();
                if (level == null && scene.name != "OneOneScene") continue;
                string backup = Evidence + "/backup/" + scene.name + ".unity";
                if (!File.Exists(backup)) File.Copy(entry.path, backup);
                var camera = Camera.main;
                if (camera == null) throw new Exception("Missing gameplay camera: " + entry.path);
                var background = camera.GetComponent<KitLandscape>();
                if (background == null) background = camera.gameObject.AddComponent<KitLandscape>();
                else background.Rebuild();
                if (!background.HasLayeredBackground) throw new Exception("Missing imported layers: " + entry.path);
                // Retire only the previous camera-bound forest overlay, if this scene has it.
                var oldDepth = camera.GetComponent<AncientForestDepth>();
                if (oldDepth != null) oldDepth.enabled = false;
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new Exception("Could not save: " + entry.path);
                // RuleTiles can rewrite cached sprites/cell order merely on load. This visual-only
                // installer preserves the author's complete Tilemap serialization verbatim.
                var tilemaps = Regex.Matches(originalScene, @"(?ms)^--- !u!1839735485 &(-?\d+).*?(?=^--- !u!|\z)")
                    .Cast<Match>().ToDictionary(m => m.Groups[1].Value, m => m.Value);
                string savedScene = File.ReadAllText(entry.path);
                savedScene = Regex.Replace(savedScene, @"(?ms)^--- !u!1839735485 &(-?\d+).*?(?=^--- !u!|\z)",
                    m => tilemaps.TryGetValue(m.Groups[1].Value, out string original) ? original : m.Value);
                File.WriteAllText(entry.path, savedScene);
                AssetDatabase.ImportAsset(entry.path);
                report.Add(scene.name + ": six layers installed; camera component only; world geometry untouched");
            }
        }
        finally
        {
            EditorSceneManager.RestoreSceneManagerSetup(setup);
            File.WriteAllLines(Evidence + "/installed.txt", report);
        }
        SceneView.RepaintAll();
        Debug.Log(string.Join("\n", report));
    }
}
