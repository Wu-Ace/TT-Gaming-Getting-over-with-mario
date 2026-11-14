$ErrorActionPreference="Stop"
. "$PSScriptRoot\inspect-types.ps1" | Out-Null
$bytes=[IO.File]::ReadAllBytes('E:\SteamLibrary\steamapps\common\Jump King\Content\level.xnb');$width=[BitConverter]::ToInt32($bytes,69);$height=[BitConverter]::ToInt32($bytes,73)
$data=New-Object 'Microsoft.Xna.Framework.Color[]' ($width*$height)
for($i=0;$i -lt $data.Length;$i++){$data[$i]=[Microsoft.Xna.Framework.Color]::new([BitConverter]::ToUInt32($bytes,85+$i*4))}
$flags=[Reflection.BindingFlags]'Public,NonPublic,Instance,Static'
$textureType=$jkAssembly.GetType('JumpKing.Level.Sampler.LevelTexture');$ctor=$textureType.GetConstructors($flags)[0];$ctorArgs=[object[]]::new(3);$ctorArgs[0]=$data.PSObject.BaseObject;$ctorArgs[1]=$width;$ctorArgs[2]=$height;$texture=$ctor.Invoke($ctorArgs)
$factoryType=$jkAssembly.GetType('JumpKing.Level.Factory.BaseBlockFactory');$factory=[Activator]::CreateInstance($factoryType);$slopeMethod=$factoryType.GetMethod('GetSlopeType',$flags);$solidMethod=$factoryType.GetMethod('IsSolidBlock',$flags)
$cells=[Collections.Generic.List[object]]::new();$ignored=@{};$slopes=@{}
for($screen=0;$screen -lt 14;$screen++){for($y=0;$y -lt 45;$y++){for($x=0;$x -lt 60;$x++){
$coord=$texture.GetCoord($screen,$x,$y);$color=$texture.GetColor($screen,$x,$y);$kind=-1;$slopeName=$null
if($color.R -eq 255 -and $color.G -eq 0 -and $color.B -eq 0 -and $color.A -eq 255){$stype=$slopeMethod.Invoke($factory,[object[]]@($texture,$screen,$x,$y));$slopeName=$stype.ToString();$kind=switch($slopeName){'TopLeft'{2};'TopRight'{1};'BottomLeft'{3};'BottomRight'{3};'None'{0}};$slopes[$slopeName]++}
elseif($solidMethod.Invoke($factory,[object[]]@($color))){$kind=0}
if($kind -ge 0){$cells.Add(@{x=$x;y=$screen*45+44-$y;kind=$kind;sourceSlope=$slopeName})}elseif($color.A -gt 0){$key=$color.ToString();$ignored[$key]++}
}}}
@{cells=$cells.ToArray();extra=@();source='JumpKing.exe BaseBlockFactory.GetSlopeType + IsSolidBlock';slopes=$slopes;ignored=$ignored;width=$width;height=$height} | ConvertTo-Json -Depth 8 | Set-Content "$PSScriptRoot\plan.json"
"Extracted $($cells.Count) original collision cells";$slopes | ConvertTo-Json;$ignored | ConvertTo-Json




