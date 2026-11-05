using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class KitTerrainRepair
{
    const string Request = "scripts/kit-terrain-repair.request";
    static KitTerrainRepair() { EditorApplication.delayCall += RunRequested; }
    // Run once after each explicitly requested import.
    static void RunRequested()
    {
        if (!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete(Request);
        try { Repair(); }
        catch (Exception e) { File.WriteAllText("scripts/kit-terrain-repair-error.txt", e.ToString()); Debug.LogException(e); }
    }
    static Sprite Tile(int n)
    {
        var result = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/PlatformerKit/Terrain/grass-" + n + ".png");
        if (result == null) throw new InvalidOperationException("Missing terrain tile " + n);
        return result;
    }
    // Measure the opaque pillar body, excluding transparent padding in the source sprite.
    static Bounds VisibleFootprint(SpriteRenderer renderer)
    {
        var sprite = renderer.sprite;
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        texture.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite.texture)));
        int min = texture.width, max = -1;
        for (int x = 0; x < texture.width; x++)
            if (texture.GetPixel(x, 0).a >= 0.5f) { min = Mathf.Min(min, x); max = x; }
        if (max < min) throw new InvalidOperationException("Pillar has no solid base: " + renderer.name);
        var bounds = renderer.bounds;
        float x0 = bounds.min.x + bounds.size.x * min / texture.width;
        float x1 = bounds.min.x + bounds.size.x * (max + 1) / texture.width;
        if (renderer.flipX) { x0 = bounds.max.x - bounds.size.x * (max + 1) / texture.width; x1 = bounds.max.x - bounds.size.x * min / texture.width; }
        bounds.SetMinMax(new Vector3(x0, bounds.min.y, bounds.min.z), new Vector3(x1, bounds.max.y, bounds.max.z));
        UnityEngine.Object.DestroyImmediate(texture);
        return bounds;
    }
    static Sprite ContactSprite(SpriteRenderer renderer, Bounds cell, int tile, Bounds[] pillars, List<string> evidence)
    {
        var contacts = pillars.Where(p => Mathf.Abs(p.min.y - cell.max.y) < 0.04f &&
            p.max.x > cell.min.x && p.min.x < cell.max.x).ToArray();
        if (contacts.Length == 0) return Tile(tile);
        var top = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        var soil = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        top.LoadImage(File.ReadAllBytes("Assets/Art/PlatformerKit/Terrain/grass-" + tile + ".png"));
        soil.LoadImage(File.ReadAllBytes("Assets/Art/PlatformerKit/Terrain/grass-31.png"));
        var mask = new bool[top.width];
        for (int x = 0; x < top.width; x++)
        {
            float u = (x + 0.5f) / top.width;
            if (renderer.flipX || renderer.transform.lossyScale.x < 0) u = 1 - u;
            float worldX = Mathf.Lerp(cell.min.x, cell.max.x, u);
            mask[x] = contacts.Any(p => worldX >= p.min.x && worldX < p.max.x);
            if (!mask[x]) continue;
            for (int y = 0; y < top.height; y++) top.SetPixel(x, y, soil.GetPixel(x, y));
        }
        var key = tile + "-" + string.Concat(mask.Select(v => v ? "1" : "0"));
        string hash;
        using (var sha = System.Security.Cryptography.SHA256.Create())
            hash = BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(key))).Replace("-", "").Substring(0, 16);
        var folder = "Assets/Art/PlatformerKit/Terrain/Contacts";
        Directory.CreateDirectory(folder);
        var path = folder + "/contact-" + hash + ".png";
        if (!File.Exists(path))
        {
            File.WriteAllBytes(path, top.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 750;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
        evidence.Add(string.Format("CONTACT {0} cellX={1:F4}..{2:F4} maskColumns={3}/{4} pillars={5} asset={6}",
            renderer.name, cell.min.x, cell.max.x, mask.Count(v => v), mask.Length,
            string.Join(";", contacts.Select(p => p.min.x.ToString("F4") + ".." + p.max.x.ToString("F4"))), path));
        UnityEngine.Object.DestroyImmediate(top);
        UnityEngine.Object.DestroyImmediate(soil);
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    // One native pixel of edge extrusion overlaps rasterization cracks without changing colliders.
    static Sprite SeamSafeSprite(Sprite source)
    {
        var sourcePath = AssetDatabase.GetAssetPath(source.texture);
        var folder = "Assets/Art/PlatformerKit/Terrain/SeamSafe";
        Directory.CreateDirectory(folder);
        var path = folder + "/" + Path.GetFileName(sourcePath);
        if (!File.Exists(path) || File.GetLastWriteTimeUtc(sourcePath) > File.GetLastWriteTimeUtc(path))
        {
            var original = new Texture2D(2,2,TextureFormat.RGBA32,false);
            original.LoadImage(File.ReadAllBytes(sourcePath));
            var padded = new Texture2D(original.width+2,original.height+2,TextureFormat.RGBA32,false);
            for(int y=0;y<padded.height;y++)for(int x=0;x<padded.width;x++)
                padded.SetPixel(x,y,original.GetPixel(Mathf.Clamp(x-1,0,original.width-1),Mathf.Clamp(y-1,0,original.height-1)));
            File.WriteAllBytes(path,padded.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(original);UnityEngine.Object.DestroyImmediate(padded);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.spritePixelsPerUnit=750;importer.mipmapEnabled=false;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
            settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static int RepairGround(IEnumerable<SpriteRenderer> renderers, List<string> evidence)
    {
        var all = renderers.ToArray();
        var pillars = all.Where(r => r.name.StartsWith("Tube", StringComparison.Ordinal)).Select(VisibleFootprint).ToArray();
        var ground = all.Where(r => r.name.StartsWith("GroundTile", StringComparison.Ordinal)).ToArray();
        var boxes = ground.Select(r => r.GetComponent<BoxCollider2D>()).ToArray();
        if (boxes.Any(b => b == null)) throw new InvalidOperationException("Ground tile without expected collider");
        var bounds = boxes.Select(b => b.bounds).ToArray();
        int grass = 0, soil = 0, ends = 0;
        for (int i = 0; i < ground.Length; i++)
        {
            var b = bounds[i];
            Func<Vector2, bool> has = delta => bounds.Where((other, j) => j != i).Any(other =>
                Vector2.Distance((Vector2)other.center, (Vector2)b.center + delta) < 0.025f);
            bool above = has(new Vector2(0, b.size.y));
            bool below = has(new Vector2(0, -b.size.y));
            bool left = has(new Vector2(-b.size.x, 0));
            bool right = has(new Vector2(b.size.x, 0));
            int tile = !above ? (!left ? 27 : !right ? 58 : 24)
                : !below ? (!left ? 26 : !right ? 11 : 43)
                : (!left ? 44 : !right ? 17 : 31);
            var sprite = Tile(tile);
            if (!above) sprite = ContactSprite(ground[i], b, tile, pillars, evidence);
            sprite = SeamSafeSprite(sprite);
            var scale = ground[i].transform.lossyScale;
            if (Mathf.Abs(Mathf.Abs(scale.x) - Mathf.Abs(scale.y)) > 0.001f)
                throw new InvalidOperationException("Nonuniform ground transform: " + ground[i].name);
            Undo.RecordObject(ground[i], "Repair terrain topology");
            ground[i].sprite = sprite;
            ground[i].drawMode = SpriteDrawMode.Simple;
            EditorUtility.SetDirty(ground[i]);
            if (PrefabUtility.IsPartOfPrefabInstance(ground[i])) PrefabUtility.RecordPrefabInstancePropertyModifications(ground[i]);
            if (above) soil++; else grass++;
            if (!left || !right) ends++;
            evidence.Add(string.Format("{0}: position={1} tile={2} above={3} below={4} left={5} right={6}",
                ground[i].name, b.center, tile, above, below, left, right));
        }
        evidence.Add(string.Format("SUMMARY total={0} grass={1} soil={2} edgeCells={3}", ground.Length, grass, soil, ends));
        return ground.Length;
    }
    [MenuItem("Tools/Art/Repair Kit Terrain Topology")]
    public static void Repair()
    {
        AssetDatabase.Refresh();
        var evidence = new List<string> { "UTC " + DateTime.UtcNow.ToString("O") };
        var path = "Assets/Prefabs/Level.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            evidence.Add("PREFAB " + path);
            RepairGround(root.GetComponentsInChildren<SpriteRenderer>(true), evidence);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        var scene = SceneManager.GetActiveScene();
        if (scene.path == "Assets/Scenes/OneOneScene.unity")
        {
            bool wasDirty = scene.isDirty;
            evidence.Add("SCENE " + scene.path + " dirtyBefore=" + wasDirty);
            RepairGround(scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<SpriteRenderer>(true)), evidence);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!wasDirty) { EditorSceneManager.SaveScene(scene); evidence.Add("Scene saved"); }
            else
            {
                Directory.CreateDirectory("art-backup/pre-seam-safe");
                var backup="art-backup/pre-seam-safe/OneOneScene.unity";
                if(!File.Exists(backup))File.Copy(scene.path,backup);
                EditorSceneManager.SaveScene(scene);
                evidence.Add("Scene saved including existing loaded edits; previous disk scene backed up");
            }
            if (SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.LookAt(new Vector3(32, 1, 0), Quaternion.identity, 12, true);
        }
        AssetDatabase.SaveAssets();
        File.WriteAllLines("scripts/kit-terrain-repair-result.txt", evidence);
        Debug.Log("Kit terrain repaired. See scripts/kit-terrain-repair-result.txt");
    }
}
