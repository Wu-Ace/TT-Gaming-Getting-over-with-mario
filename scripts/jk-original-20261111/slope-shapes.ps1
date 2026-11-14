. "$PSScriptRoot\inspect-types.ps1" | Out-Null
$t=$jkAssembly.GetType('JumpKing.Level.SlopeBlock');$et=$jkAssembly.GetType('JumpKing.Level.SlopeType');$flags=[Reflection.BindingFlags]'Public,NonPublic,Static,Instance';$ctor=$t.GetConstructors($flags)[0]
foreach($name in @('TopLeft','TopRight','BottomLeft','BottomRight')){
$argsArray=[object[]]::new(2);$argsArray[0]=[Microsoft.Xna.Framework.Rectangle]::new(0,0,8,8);$argsArray[1]=[Enum]::Parse($et,$name);$block=$ctor.Invoke($argsArray);$lines=$t.GetField('m_lines',$flags).GetValue($block)
$name;foreach($line in $lines){$line.GetType().GetFields($flags)|ForEach-Object { $_.Name+'='+$_.GetValue($line) }}; 'normal='+$block.GetNormal()
}
