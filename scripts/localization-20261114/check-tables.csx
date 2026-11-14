using System.Linq;
var root=System.IO.Path.GetFullPath("scripts/localization-20261114");
System.IO.Directory.CreateDirectory(root+"/text-samples");
string original=GameLocalization.Current;
var results=new System.Collections.Generic.List<object>();
var source=UnityEngine.JsonUtility.FromJson<GameLocalization.Locale>(UnityEngine.Resources.Load<UnityEngine.TextAsset>("Localization/en").text);
var sourceKeys=new System.Collections.Generic.HashSet<string>(System.Linq.Enumerable.Select(source.entries,e=>e.key));
var failures=new System.Collections.Generic.List<string>();
foreach(var locale in GameLocalization.Locales)
{
    var keys=new System.Collections.Generic.HashSet<string>();
    foreach(var entry in locale.entries)
    {
        if(!keys.Add(entry.key)||string.IsNullOrWhiteSpace(entry.text))failures.Add(locale.code+": duplicate/empty "+entry.key);
        var src=System.Linq.Enumerable.FirstOrDefault(source.entries,e=>e.key==entry.key);
        if(src==null){failures.Add(locale.code+": extra "+entry.key);continue;}
        var a=System.Linq.Enumerable.OrderBy(System.Linq.Enumerable.Select(System.Text.RegularExpressions.Regex.Matches(src.text,@"\{[^}]+\}").Cast<System.Text.RegularExpressions.Match>(),m=>m.Value),v=>v);
        var b=System.Linq.Enumerable.OrderBy(System.Linq.Enumerable.Select(System.Text.RegularExpressions.Regex.Matches(entry.text,@"\{[^}]+\}").Cast<System.Text.RegularExpressions.Match>(),m=>m.Value),v=>v);
        if(!System.Linq.Enumerable.SequenceEqual(a,b))failures.Add(locale.code+": placeholders "+entry.key);
    }
    if(!keys.SetEquals(sourceKeys))failures.Add(locale.code+": missing keys");
    string coverage=LocalizedWindowsText.InspectCoverage(locale.nativeName+string.Join(" ",System.Linq.Enumerable.Select(locale.entries,e=>e.text)),locale.font);
    if(coverage.Length>0)failures.Add(locale.code+": "+coverage);
    GameLocalization.SetLanguage(locale.code,false);
    if(GameLocalization.Get("menu.start").StartsWith("["))failures.Add(locale.code+": lookup failed");
    string sample=locale.nativeName+"\n"+GameLocalization.Get("menu.level_select")+"\n"+GameLocalization.Format("level.number","number","05")+"\n"+GameLocalization.Format("hud.progress","progress",42)+"\n"+GameLocalization.Format("loading.progress","progress",73)+"\n"+GameLocalization.Get("error.level_unavailable");
    var texture=LocalizedWindowsText.Rasterize(sample,locale.font,1000,420,38,locale.rtl,true);
    var pixels=texture.GetPixels32();for(int i=0;i<pixels.Length;i++){byte v=(byte)(30+pixels[i].a*225/255);pixels[i]=new UnityEngine.Color32(v,v,v,255);}texture.SetPixels32(pixels);texture.Apply();
    System.IO.File.WriteAllBytes(root+"/text-samples/"+locale.code+".png",UnityEngine.ImageConversion.EncodeToPNG(texture));UnityEngine.Object.DestroyImmediate(texture);
    results.Add(new{locale=locale.code,keys=keys.Count,coverage=coverage.Length==0?"PASS":coverage,translationReview=locale.review});
}
GameLocalization.SetLanguage(original,false);
if(GameLocalization.Locales.Count!=29)failures.Add("Expected 29 locales");
if(GameLocalization.Normalize("zh-TW")!="zh-Hant" || GameLocalization.Normalize("zh-CN")!="zh-Hans" || GameLocalization.Normalize("pt-PT")!="pt-BR" || GameLocalization.Normalize("iw")!="he")failures.Add("Locale aliases");
var report=new{status=failures.Count==0?"PASS":"FAIL",generatedUtc=System.DateTime.UtcNow,results,failures};
System.IO.File.WriteAllText(root+"/table-check.json",Codely.Newtonsoft.Json.JsonConvert.SerializeObject(report,Codely.Newtonsoft.Json.Formatting.Indented));
return report;

