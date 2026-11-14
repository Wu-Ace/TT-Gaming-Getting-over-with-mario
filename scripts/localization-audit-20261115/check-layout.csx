using System.Linq;
System.Collections.IEnumerator Run()
{
 var ui=GameMenuUI.Instance;string original=GameLocalization.Current;
 var root=System.IO.Path.GetFullPath("scripts/localization-audit-20261115");System.IO.Directory.CreateDirectory(root+"/ui");
 var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
 var tester=UnityEngine.Object.FindObjectsOfType<UnityEngine.MonoBehaviour>().FirstOrDefault(m=>m.GetType().Name=="GiftKeyboardTester");if(tester!=null)tester.GetType().GetField("visible",flags)?.SetValue(tester,false);
 string[] keys={"settings.sfx","settings.music","settings.resolution","settings.language"};
 UnityEngine.Vector2[] bounds={new UnityEngine.Vector2(202*.84f,69),new UnityEngine.Vector2(203*.84f,59),new UnityEngine.Vector2(235*.76f,103*.8f),new UnityEngine.Vector2(290*.76f,103*.8f)};
 var records=new System.Collections.Generic.List<object>();var failures=new System.Collections.Generic.List<string>();
 string Normalize(string text){return System.Text.RegularExpressions.Regex.Replace(text,"[ \\t\\r\\n]+"," ").Trim();}
 void CheckLines(string text,LocalizedWindowsText.TextLayout layout,string context)
 {
  if(!layout.Fits)failures.Add(context+": overflow at minimum size");
  if(Normalize(string.Join(" ",layout.Lines))!=Normalize(text))failures.Add(context+": split/changed a word or combining sequence");
 }
 try
 {
 foreach(var locale in GameLocalization.Locales)
 {
  GameLocalization.SetLanguage(locale.code,false);int size=ui.SettingsCaptionSize;
  if(size>32)failures.Add(locale.code+": common settings font exceeds 32");
  var captions=new System.Collections.Generic.List<object>();
  for(int i=0;i<keys.Length;i++)
  {
   string text=GameLocalization.Get(keys[i]);var layout=LocalizedWindowsText.MeasureLayout(text,locale.font,(int)bounds[i].x-4,(int)bounds[i].y-4,size);
   CheckLines(text,layout,locale.code+":"+keys[i]);
   foreach(float scale in new[]{1f,1.25f,2f})
   {
    var scaled=LocalizedWindowsText.MeasureLayout(text,locale.font,UnityEngine.Mathf.CeilToInt(bounds[i].x*scale),UnityEngine.Mathf.CeilToInt(bounds[i].y*scale),UnityEngine.Mathf.RoundToInt(size*scale));
    CheckLines(text,scaled,locale.code+":"+keys[i]+" scale="+scale);
   }
   captions.Add(new{key=keys[i],size,lines=layout.Lines,width=layout.Width,height=layout.Height});
  }
  if(size<32 && Enumerable.Range(0,4).All(i=>LocalizedWindowsText.MeasureLayout(GameLocalization.Get(keys[i]),locale.font,(int)bounds[i].x-4,(int)bounds[i].y-4,size+1).Fits))failures.Add(locale.code+": chosen size is not maximal");
  foreach(var entry in locale.entries)
  {
   string text=entry.text.Replace("{progress}","100").Replace("{number}","05").Replace("{coins}","40").Replace("{total}","40").Replace("{stars}","3");
   int w=800,h=65,max=24;
   if(entry.key.StartsWith("level.name.")){w=290;h=90;max=30;}
   else if(entry.key=="level.number"){w=230;h=78;max=36;}
   else if(entry.key=="level.play"){w=210;h=60;max=32;}
   else if(entry.key=="level.collectibles"){w=290;h=55;max=24;}
   else if(entry.key=="result.complete"){w=270;h=110;max=48;}
   else if(entry.key=="hud.progress"){w=600;h=58;max=34;}
   else if(entry.key.StartsWith("menu.")||entry.key=="settings.title"){w=400;h=115;max=56;}
   else if(entry.key=="settings.auto"){w=520;h=65;max=32;}
   else if(entry.key=="loading.progress"){w=1280;h=80;max=28;}
   CheckLines(text,LocalizedWindowsText.FitLayout(text,locale.font,w,h,max),locale.code+":"+entry.key);
   if(entry.key=="settings.title" || entry.key=="settings.language" || entry.key=="settings.resolution")
    CheckLines(text,LocalizedWindowsText.FitLayout(text,locale.font,310,85,entry.key=="settings.title"?60:48),locale.code+":dialog heading:"+entry.key);
  }
  string coverage=LocalizedWindowsText.InspectCoverage(locale.nativeName+" ✓ 0123456789×%/"+string.Join(" ",locale.entries.Select(e=>e.text)),locale.font);
  if(coverage.Length>0)failures.Add(locale.code+": glyphs "+coverage);
  ui.CloseSettings();ui.page=0;yield return null;yield return new UnityEngine.WaitForEndOfFrame();UnityEngine.ScreenCapture.CaptureScreenshot(root+"/ui/"+locale.code+"-home.png");yield return null;
  ui.page=1;yield return null;yield return new UnityEngine.WaitForEndOfFrame();UnityEngine.ScreenCapture.CaptureScreenshot(root+"/ui/"+locale.code+"-levels.png");yield return null;
  ui.OpenSettings();yield return null;yield return new UnityEngine.WaitForEndOfFrame();UnityEngine.ScreenCapture.CaptureScreenshot(root+"/ui/"+locale.code+"-settings.png");yield return null;
  records.Add(new{locale=locale.code,font=locale.font,coverage=coverage.Length==0?"PASS":coverage,settingsSize=size,captions,translationReview=locale.review});
 }
 var regression=LocalizedWindowsText.FitLayout("Приключение на пляже","MaokenAssortedSans",290,90,30);
 CheckLines("Приключение на пляже",regression,"Russian word boundary regression");
 if(!regression.Lines.Any(l=>l.Contains("пляже")))failures.Add("Russian final word split");
 if(LocalizedWindowsText.SelectedFontByteLength("Noto Sans Myanmar")!=183164)failures.Add("Wrong Myanmar font revision");
 var report=new{status=failures.Count==0?"PASS":"FAIL",generatedUtc=System.DateTime.UtcNow,viewport=new{UnityEngine.Screen.width,UnityEngine.Screen.height},records,failures};
 System.IO.File.WriteAllText(root+"/layout-results.json",Codely.Newtonsoft.Json.JsonConvert.SerializeObject(report,Codely.Newtonsoft.Json.Formatting.Indented));
 }
 finally {ui.CloseSettings();ui.page=0;GameLocalization.SetLanguage(original,false);}
}
return Run();
