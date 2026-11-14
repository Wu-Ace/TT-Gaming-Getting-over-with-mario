. "$PSScriptRoot\inspect-types.ps1" | Out-Null
$t=$jkAssembly.GetType('JumpKing.Level.SlopeBlock');$et=$jkAssembly.GetType('JumpKing.Level.SlopeType');$flags=[Reflection.BindingFlags]'Public,NonPublic,Static,Instance';$ctor=$t.GetConstructors($flags)[0];$intersect=$t.GetMethods($flags)|Where-Object { $_.Name.EndsWith('IBlock.Intersects') }
foreach($name in @('TopLeft','TopRight','BottomLeft','BottomRight')){
$argsArray=[object[]]::new(2);$argsArray[0]=[Microsoft.Xna.Framework.Rectangle]::new(0,0,120,120);$argsArray[1]=[Enum]::Parse($et,$name);$block=$ctor.Invoke($argsArray);$name
foreach($xy in @(@(20,20),@(98,20),@(20,98),@(98,98))){$a=[object[]]::new(2);$a[0]=[Microsoft.Xna.Framework.Rectangle]::new($xy[0],$xy[1],1,1);$a[1]=[Microsoft.Xna.Framework.Rectangle]::new($xy[0],$xy[1],1,1);$result=$intersect.Invoke($block,$a);"$xy -> $result adjusted=$($a[1])"}
}

