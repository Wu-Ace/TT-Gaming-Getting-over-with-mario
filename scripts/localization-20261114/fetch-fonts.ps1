$ErrorActionPreference='Stop'
$root=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$out=Join-Path $root 'Assets/StreamingAssets/LocalizationFonts'
New-Item -ItemType Directory -Force $out | Out-Null
$base='https://raw.githubusercontent.com/notofonts/noto-fonts/main/'
foreach($family in @('NotoSans','NotoSansArabic','NotoSansHebrew','NotoSansThai','NotoSansMyanmar')){
 $file="$family-Regular.ttf"
 Invoke-WebRequest -Uri ($base+"hinted/ttf/$family/$file") -OutFile (Join-Path $out $file)
}
Invoke-WebRequest -Uri ($base+'LICENSE') -OutFile (Join-Path $out 'OFL.txt')
foreach($row in @(@('Japanese','jp'),@('Korean','kr'),@('SimplifiedChinese','sc'),@('TraditionalChinese','tc'))){
 $file="NotoSansCJK$($row[1])-Regular.otf"
 Invoke-WebRequest -Uri ("https://raw.githubusercontent.com/notofonts/noto-cjk/main/Sans/OTF/$($row[0])/$file") -OutFile (Join-Path $out $file)
}
Invoke-WebRequest -Uri 'https://raw.githubusercontent.com/notofonts/noto-cjk/main/Sans/LICENSE' -OutFile (Join-Path $out 'OFL-CJK.txt')
Get-ChildItem $out -File|Select-Object Name,Length
