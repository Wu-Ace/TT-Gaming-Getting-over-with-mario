using System.Linq;
System.Collections.IEnumerator Run()
{
    var ui=GameMenuUI.Instance;string original=GameLocalization.Current,prefix=GameMenuUI.PreferencePrefix;
    GameMenuUI.Testing=true;GameMenuUI.PreferencePrefix="Goombas.LocalizationAcceptance.Game.20261114.";
    var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
    var loading=typeof(GameMenuUI).GetField("loading",flags);
    var progress=typeof(GameMenuUI).GetField("loadingProgress",flags);
    var won=typeof(CampaignLevel).GetField("<Won>k__BackingField",flags);
    string root=System.IO.Path.GetFullPath("scripts/localization-20261114/ui");
    var notes=new System.Collections.Generic.List<string>();
    try
    {
        foreach(var locale in GameLocalization.Locales)
        {
            GameLocalization.SetLanguage(locale.code,false);loading.SetValue(ui,true);progress.SetValue(ui,.37f);
            yield return null;yield return new UnityEngine.WaitForEndOfFrame();
            UnityEngine.ScreenCapture.CaptureScreenshot(root+"/"+locale.code+"-loading.png");yield return null;
        }
        loading.SetValue(ui,false);ui.LoadLevel(0);
        while((bool)loading.GetValue(ui))yield return null;
        UnityEngine.Time.timeScale=0;
        var level=CampaignLevel.Current;if(level==null)throw new System.Exception("Level01 failed to load");
        var tester=UnityEngine.Object.FindObjectsOfType<UnityEngine.MonoBehaviour>().FirstOrDefault(m=>m.GetType().Name=="GiftKeyboardTester");
        if(tester!=null)tester.GetType().GetField("visible",flags)?.SetValue(tester,false);
        foreach(var locale in GameLocalization.Locales)
        {
            GameLocalization.SetLanguage(locale.code,false);won.SetValue(level,false);
            yield return null;yield return new UnityEngine.WaitForEndOfFrame();
            UnityEngine.ScreenCapture.CaptureScreenshot(root+"/"+locale.code+"-hud.png");yield return null;
            won.SetValue(level,true);
            yield return null;yield return new UnityEngine.WaitForEndOfFrame();
            UnityEngine.ScreenCapture.CaptureScreenshot(root+"/"+locale.code+"-completion-preview.png");yield return null;
            notes.Add(locale.code+": loading/HUD/completion preview rendered; completion forced for visual inspection, not a gameplay win.");
        }
        won.SetValue(level,false);
        foreach(var label in UnityEngine.Object.FindObjectsOfType<TMPro.TMP_Text>())if(label.enabled && label.gameObject.activeInHierarchy)notes.Add("Active legacy TMP: "+label.name+" = "+label.text);
        System.IO.File.WriteAllLines(root+"/game-render-results.txt",notes);
        // Unload gameplay while the isolated prefix is still installed.
        yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(GameMenuUI.MenuScene);
        yield return null;
    }
    finally
    {
        loading.SetValue(ui,false);UnityEngine.Time.timeScale=ui.IsMenu?1:0;
        GameMenuUI.PreferencePrefix=prefix;GameMenuUI.Testing=false;GameLocalization.SetLanguage(original,false);
    }
}
return Run();
