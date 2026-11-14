$ErrorActionPreference='Stop'
$root=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$path=Join-Path $root 'Assets/Scripts/GameMenuUI.cs'
$source=Get-Content -LiteralPath $path -Raw
function Replace-Exact([string]$before,[string]$after){
 if(-not $script:source.Contains($before)){throw "Source mismatch: $before"}
 $script:source=$script:source.Replace($before,$after)
}
Replace-Exact '    bool dropdown;' "    bool dropdown;`n    bool languageDropdown;`n    Vector2 languageScroll;"
Replace-Exact 'font=GameFont.Main;' 'font=GameFont.Main;GameLocalization.Initialize();'
Replace-Exact 'settings=true;dropdown=false;' 'settings=true;dropdown=false;languageDropdown=false;'
Replace-Exact 'settings=false;dropdown=false;' 'settings=false;dropdown=false;languageDropdown=false;'
Replace-Exact 'error="Level unavailable. Please return and try again.";' 'error=GameLocalization.Get("error.level_unavailable");'
Replace-Exact 'catch(Exception exception) { error=exception.Message; }' 'catch(Exception exception) { Debug.LogException(exception);error=GameLocalization.Get("error.level_unavailable"); }'
Replace-Exact 'var s=new GUIStyle(label){fontSize=size,alignment=center?TextAnchor.MiddleCenter:TextAnchor.MiddleLeft};s.normal.textColor=color??label.normal.textColor;GUI.Label(r,value,s);' 'LocalizedWindowsText.Draw(r,value,size,color??label.normal.textColor,center);'
Replace-Exact 'GUI.Label(new Rect(0,Screen.height*.45f,Screen.width,80),"正在准备关卡… "+Mathf.RoundToInt(Mathf.Clamp01(loadingProgress/.9f)*100)+"%",loadingStyle);' 'LocalizedWindowsText.Draw(new Rect(0,Screen.height*.45f,Screen.width,80),GameLocalization.Format("loading.progress","progress",Mathf.RoundToInt(Mathf.Clamp01(loadingProgress/.9f)*100)),28,Color.white,true);'
Replace-Exact '"关卡进度 "+Mathf.FloorToInt(progress*100)+"%"' 'GameLocalization.Format("hud.progress","progress",Mathf.FloorToInt(progress*100))'
# Brand wording and existing font are deliberately stable.
Replace-Exact 'Text(new Rect(95,55,1500,175),"GET OVER WITH",137,Color.white);' 'GUI.Label(new Rect(95,55,1500,175),"GET OVER WITH",BrandStyle(137));'
Replace-Exact 'Text(new Rect(95,205,1500,250),"ODYSSEY",205,Color.white);' 'GUI.Label(new Rect(95,205,1500,250),"ODYSSEY",BrandStyle(205));'
Replace-Exact '0,"START",' '0,GameLocalization.Get("menu.start"),'
Replace-Exact '1,"OPTIONS",' '1,GameLocalization.Get("settings.title"),'
Replace-Exact '2,"EXIT",' '2,GameLocalization.Get("menu.exit"),'
Replace-Exact 'GUI.Label(rect,caption,style);' 'LocalizedWindowsText.Draw(rect,caption,56,Color.white,true);'
Replace-Exact '"选择关卡",72' 'GameLocalization.Get("menu.level_select"),72'
Replace-Exact '"关卡 "+(n+1).ToString("00")' 'GameLocalization.Format("level.number","number",(n+1).ToString("00"))'
Replace-Exact 'Names[n],30' 'GameLocalization.Get("level.name."+(n+1)),30'
Replace-Exact '"金币 "+CampaignLevel.SavedCoins(Scenes[n])+"/"+CampaignLevel.CoinTotal+"   "+CampaignLevel.CoinStars(CampaignLevel.SavedCoins(Scenes[n]))+"/3 星"' 'GameLocalization.Get("level.collectibles").Replace("{coins}",CampaignLevel.SavedCoins(Scenes[n]).ToString()).Replace("{total}",CampaignLevel.CoinTotal.ToString()).Replace("{stars}",CampaignLevel.CoinStars(CampaignLevel.SavedCoins(Scenes[n])).ToString())'
Replace-Exact 'PanelText(enter,"进入关卡",32,-3);' 'PanelText(enter,GameLocalization.Get("level.play"),32,-3);'
Replace-Exact '"返回",40,-3' 'GameLocalization.Get("menu.back"),40,-3'
Replace-Exact 'art=="button_quality"?26:text=="LEVEL SELECT"?36:48' 'art=="button_quality"?26:36'
Replace-Exact 'dropdown?"RESOLUTION":"OPTIONS",dropdown?48:60' 'GameLocalization.Get(languageDropdown?"settings.language":dropdown?"settings.resolution":"settings.title"),dropdown||languageDropdown?48:60'
Replace-Exact '()=>{if(dropdown)dropdown=false;else CloseSettings();}' '()=>{if(languageDropdown)languageDropdown=false;else if(dropdown)dropdown=false;else CloseSettings();}'
Replace-Exact '        if(dropdown)' "        if(languageDropdown){LanguageOptions();return;}`n        if(dropdown)"
Replace-Exact 'i==0?"AUTO":' 'i==0?GameLocalization.Get("settings.auto"):'
Replace-Exact '"button_quality","RESOLUTION",()=>dropdown=true' '"button_quality",GameLocalization.Get("settings.resolution"),()=>dropdown=true'
Replace-Exact '        if(!IsMenu)' "        PanelButton(new Rect(815,464,290,103),`"button_quality`",GameLocalization.Get(`"settings.language`"),()=>{languageDropdown=true;languageScroll=Vector2.zero;});`n        if(!IsMenu)"
Replace-Exact '"button_10","LEVEL SELECT",ReturnToLevelMenu' '"button_10",GameLocalization.Get("menu.level_select"),ReturnToLevelMenu'
Replace-Exact 'PanelText(plate,name,32,musicRow?-2:4);' 'PanelText(plate,GameLocalization.Get(musicRow?"settings.music":"settings.sfx"),32,musicRow?-2:4);'
$insert=@'
    GUIStyle BrandStyle(int size)
    {
        var style=new GUIStyle(label){fontSize=size,alignment=TextAnchor.MiddleLeft};
        style.normal.textColor=Color.white;return style;
    }
    void LanguageOptions()
    {
        var locales=GameLocalization.Locales;
        var viewport=new Rect(490,292,650,320);
        languageScroll=GUI.BeginScrollView(viewport,languageScroll,new Rect(0,0,620,locales.Count*68));
        for(int i=0;i<locales.Count;i++)
        {
            var locale=locales[i];var row=new Rect(8,i*68,590,60);
            bool selected=GameLocalization.Current==locale.code;
            Fill(row,selected?new Color32(61,109,88,255):new Color32(48,62,53,230));
            LocalizedWindowsText.Draw(new Rect(25,i*68+3,550,54),locale.nativeName+(selected?" ✓":""),30,Color.white,true,locale);
            if(FeedbackButton(row,GUIContent.none,GUIStyle.none))
            {GameLocalization.SetLanguage(locale.code);Sound();}
        }
        GUI.EndScrollView();
        PanelButton(new Rect(676,635,295,80),"button_quality",GameLocalization.Get("menu.back"),()=>languageDropdown=false);
    }
'@
Replace-Exact '    float VolumeRow(float y,string name,float value)' ($insert+"`n    float VolumeRow(float y,string name,float value)")
Set-Content -LiteralPath $path -Value $source -Encoding utf8
$campaign=Join-Path $root 'Assets/Scripts/CampaignLevel.cs'
$content=Get-Content $campaign -Raw
if(-not $content.Contains('"LEVEL COMPLETE",60')){throw 'Completion source mismatch'}
$content.Replace('"LEVEL COMPLETE",60','GameLocalization.Get("result.complete"),60')|Set-Content $campaign -Encoding utf8
