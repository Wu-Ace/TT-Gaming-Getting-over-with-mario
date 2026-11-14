using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public static class GameFont
{
    static Font font;
    static TMP_FontAsset tmpFont;
    public static Font Main => font != null ? font : (font = Resources.Load<Font>("Fonts/MaokenAssortedSans"));

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install()
    {
        SceneManager.sceneLoaded -= Apply;
        SceneManager.sceneLoaded += Apply;
    }

    static void Apply(Scene scene, LoadSceneMode mode)
    {
        if (Main == null) return;
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var text in root.GetComponentsInChildren<UnityEngine.UI.Text>(true)) text.font = Main;
            foreach (var text in root.GetComponentsInChildren<TextMesh>(true)) text.font = Main;
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (tmpFont == null)
                {
                    tmpFont = TMP_FontAsset.CreateFontAsset(Main);
                    tmpFont.name = "Maoken Assorted Sans Dynamic";
                    tmpFont.isMultiAtlasTexturesEnabled = true;
                }
                text.font = tmpFont;
            }
        }
    }
}
