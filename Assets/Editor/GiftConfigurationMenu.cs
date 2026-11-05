using System.IO;
using UnityEditor;
using UnityEngine;
namespace LiveGifts.Editor
{
    public static class GiftConfigurationMenu
    {
        [MenuItem("Tools/Live Gifts/Edit Gift Configuration")]
        static void Edit()
        {
            const string path = "Assets/Resources/GiftConfiguration.asset";
            var config = AssetDatabase.LoadAssetAtPath<GiftConfiguration>(path);
            if (config == null)
            {
                Directory.CreateDirectory("Assets/Resources");
                AssetDatabase.Refresh();
                config = ScriptableObject.CreateInstance<GiftConfiguration>();
                AssetDatabase.CreateAsset(config, path);
                AssetDatabase.SaveAssets();
            }
            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);
        }
    }
}
