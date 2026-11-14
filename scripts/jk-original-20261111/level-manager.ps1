. "$PSScriptRoot\inspect-types.ps1" | Out-Null
$t=$jkAssembly.GetType('JumpKing.Level.LevelManager');$f=[Reflection.BindingFlags]'Public,NonPublic,Static,Instance';$t.GetFields($f)|Where-Object{$_.Name -match 'BLOCKCODE'}|ForEach-Object{$_.Name+'='+$_.GetValue($null)};$t.GetMethods($f)|ForEach-Object{$_.ToString()};$jkTypes|Where-Object{$_.Name -match 'Fake'}|ForEach-Object{$_.FullName}
