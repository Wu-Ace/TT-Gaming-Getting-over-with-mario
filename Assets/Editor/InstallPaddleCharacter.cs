using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Character.Control;
public static class InstallPaddleCharacter
{
    [MenuItem("Tools/Art/Install 11.10 Paddle Character")]
    public static void Apply()
    {
        string[] names={"body","weapon","leftarm1","leftarm2","rightarm1","rightarm2"};
        var sprites=names.Select(n=>{
            string p="Assets/Art/11.10/Player_"+n+".png";
            var t=(TextureImporter)AssetImporter.GetAtPath(p);
            t.textureType=TextureImporterType.Sprite; t.spriteImportMode=SpriteImportMode.Single;
            t.spritePixelsPerUnit=100; t.mipmapEnabled=false;t.alphaIsTransparency=true;
            t.textureCompression=TextureImporterCompression.Uncompressed;t.maxTextureSize=2048;
            t.filterMode=FilterMode.Bilinear;t.wrapMode=TextureWrapMode.Clamp;t.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(p);
        }).ToArray();
        string path="Assets/Prefabs/Character.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try {
            var rig=root.GetComponent<PaddleCharacterVisual>(); if(!rig) rig=root.AddComponent<PaddleCharacterVisual>();
            rig.body=root.transform.Find("Cauldron");rig.tool=root.transform.Find("Hammer");rig.tip=rig.tool.Find("Shape/Head");
            rig.legacy=root.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>!r.transform.name.StartsWith("NewPlayer_") && !r.GetComponent<Character.Control.TargetMovement>()).ToArray();
            var parts=names.Select((n,i)=>{
                var tr=root.transform.Find("NewPlayer_"+n);
                if(!tr){tr=new GameObject("NewPlayer_"+n).transform;tr.SetParent(root.transform,false);}
                var r=tr.GetComponent<SpriteRenderer>(); if(!r) r=tr.gameObject.AddComponent<SpriteRenderer>();
                r.sprite=sprites[i];r.sortingLayerID=rig.body.GetComponent<SpriteRenderer>().sortingLayerID;
                r.sortingOrder=new[]{2,5,1,4,3,6}[i];return r;
            }).ToArray();
            rig.torso=parts[0];rig.paddle=parts[1];rig.leftUpper=parts[2];rig.leftLower=parts[3];rig.rightUpper=parts[4];rig.rightLower=parts[5];
            rig.Pose();PrefabUtility.SaveAsPrefabAsset(root,path);
        } finally {PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();
        File.WriteAllText("scripts/player-art-20261111/install.txt","Installed six original-aspect sprites and two-bone arms on shared Character prefab. "+DateTime.UtcNow.ToString("O"));
    }
}

