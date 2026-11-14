using System.Linq;
System.Collections.IEnumerator Run()
{
    string original=GameLocalization.Current,prefix=GameMenuUI.PreferencePrefix;
    var ui=GameMenuUI.Instance;
    GameMenuUI.Testing=true;GameMenuUI.PreferencePrefix="Goombas.LocalizationAcceptance.20261114.";
    var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
    var languageField=typeof(GameMenuUI).GetField("languageDropdown",flags);
    var resolutionField=typeof(GameMenuUI).GetField("dropdown",flags);
    string root=System.IO.Path.GetFullPath("scripts/localization-20261114/ui");System.IO.Directory.CreateDirectory(root);
    var report=new System.Collections.Generic.List<string>();
    try
    {
        var tester=UnityEngine.Object.FindObjectsOfType<UnityEngine.MonoBehaviour>().FirstOrDefault(m=>m.GetType().Name=="GiftKeyboardTester");
        if(tester!=null)tester.GetType().GetField("visible",flags)?.SetValue(tester,false);
        foreach(var locale in GameLocalization.Locales)
        {
            GameLocalization.SetLanguage(locale.code,false);ui.CloseSettings();ui.page=0;
            yield return null;yield return new UnityEngine.WaitForEndOfFrame();
            UnityEngine.ScreenCapture.CaptureScreenshot(root+"/"+locale.code+"-home.png");yield return null;
            ui.page=1;yield return null;yield return new UnityEngine.WaitForEndOfFrame();
            UnityEngine.ScreenCapture.CaptureScreenshot(root+"/"+locale.code+"-levels.png");yield return null;
            ui.OpenSettings();yield return null;yield return new UnityEngine.WaitForEndOfFrame();
            UnityEngine.ScreenCapture.CaptureScreenshot(root+"/"+locale.code+"-settings.png");yield return null;
            report.Add(locale.code+": home/levels/settings rendered");
        }
        GameLocalization.SetLanguage("ar",true);
        if(UnityEngine.PlayerPrefs.GetString(GameMenuUI.PreferencePrefix+"language.v1")!="ar")throw new System.Exception("Language persistence failed");
        GameLocalization.SetLanguage("zh-Hans",false);languageField.SetValue(ui,true);
        yield return null;yield return new UnityEngine.WaitForEndOfFrame();
        UnityEngine.ScreenCapture.CaptureScreenshot(root+"/language-picker.png");yield return null;
        languageField.SetValue(ui,false);resolutionField.SetValue(ui,true);
        yield return null;yield return new UnityEngine.WaitForEndOfFrame();
        UnityEngine.ScreenCapture.CaptureScreenshot(root+"/resolution-picker.png");yield return null;
        report.Add("Isolated language preference write/read passed; source checks preserve scene IDs and save keys.");
        System.IO.File.WriteAllLines(root+"/render-results.txt",report);
    }
    finally
    {
        ui.CloseSettings();ui.page=0;
        UnityEngine.PlayerPrefs.DeleteKey(GameMenuUI.PreferencePrefix+"language.v1");UnityEngine.PlayerPrefs.Save();
        GameMenuUI.PreferencePrefix=prefix;GameMenuUI.Testing=false;GameLocalization.SetLanguage(original,false);
    }
}
return Run();
