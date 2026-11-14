. "$PSScriptRoot\inspect-types.ps1" | Out-Null
[Microsoft.Xna.Framework.Color].GetConstructors() | ForEach-Object { $_.ToString() }
$t=$jkAssembly.GetType('JumpKing.Level.Factory.BaseBlockFactory');$t.GetFields([Reflection.BindingFlags]'Public,NonPublic,Static') | ForEach-Object { $_.Name+'='+$_.GetValue($null) }
