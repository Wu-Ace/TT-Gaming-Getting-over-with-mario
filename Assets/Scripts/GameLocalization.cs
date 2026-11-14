using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

// Tables contain logical Unicode text. Never reverse Arabic/Hebrew strings.
public static class GameLocalization
{
    [Serializable] public sealed class Entry { public string key, text, context; }
    [Serializable] public sealed class Locale
    {
        public string code, nativeName, priority, font, review;
        public bool rtl;
        public Entry[] entries;
    }
    static readonly Dictionary<string, Locale> locales = new Dictionary<string, Locale>();
    static readonly Dictionary<string, Dictionary<string,string>> tables = new Dictionary<string, Dictionary<string,string>>();
    static readonly List<Locale> ordered = new List<Locale>();
    static string current;
    static string PreferenceKey => GameMenuUI.PreferencePrefix + "language.v1";
    public static event Action Changed;
    public static IReadOnlyList<Locale> Locales { get { Initialize(); return ordered; } }
    public static string Current { get { Initialize(); return current; } }
    public static Locale Active { get { Initialize(); return locales[current]; } }
    public static string Normalize(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return "en";
        code=code.Replace('_','-').ToLowerInvariant();
        if(code=="iw")return "he";
        if(code=="in")return "id";
        if(code.StartsWith("zh"))return code.Contains("hant") || code.Contains("tw") || code.Contains("hk") || code.Contains("mo") ? "zh-Hant" : "zh-Hans";
        if(code.StartsWith("pt"))return "pt-BR";
        return code.Split('-')[0];
    }
    public static void Initialize()
    {
        if(current!=null)return;
        foreach(var asset in Resources.LoadAll<TextAsset>("Localization"))
        {
            var locale=JsonUtility.FromJson<Locale>(asset.text);
            if(locale==null || string.IsNullOrEmpty(locale.code) || locale.entries==null)continue;
            locales.Add(locale.code,locale);
            var table=new Dictionary<string,string>();
            foreach(var entry in locale.entries)table.Add(entry.key,entry.text);
            tables.Add(locale.code,table);ordered.Add(locale);
        }
        if(!locales.ContainsKey("en"))throw new InvalidOperationException("English localization table is missing.");
        ordered.Sort((a,b)=> { int p=string.CompareOrdinal(a.priority,b.priority);return p!=0?p:string.CompareOrdinal(a.code,b.code); });
        string saved=PlayerPrefs.GetString(PreferenceKey,"");
        string system;
        try { system=CultureInfo.CurrentUICulture.Name; } catch { system="en"; }
        current=Normalize(string.IsNullOrEmpty(saved)?system:saved);
        if(!locales.ContainsKey(current))current="en";
    }
    // persist=false is used by isolated acceptance checks, never touching player preferences.
    public static void SetLanguage(string code,bool persist=true)
    {
        Initialize();code=Normalize(code);
        if(!locales.ContainsKey(code))code="en";
        bool changed=current!=code;current=code;
        if(persist){PlayerPrefs.SetString(PreferenceKey,code);PlayerPrefs.Save();}
        if(changed){LocalizedWindowsText.Clear();Changed?.Invoke();}
    }
    public static string Get(string key)
    {
        Initialize();string value;
        if(tables[current].TryGetValue(key,out value) && !string.IsNullOrEmpty(value))return value;
        return tables["en"].TryGetValue(key,out value)?value:"["+key+"]";
    }
    public static string Format(string key,string parameter,object value)
    {
        string text=Get(key),token="{"+parameter+"}",formatted=Convert.ToString(value,CultureInfo.InvariantCulture);
        if(Active.rtl)
        {
            if(text.Contains(token+"%"))return text.Replace(token+"%","\u200E"+formatted+"%\u200E");
            formatted="\u200E"+formatted+"\u200E";
        }
        return text.Replace(token,formatted);
    }
}
