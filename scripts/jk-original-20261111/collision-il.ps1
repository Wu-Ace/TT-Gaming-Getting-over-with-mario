. "$PSScriptRoot\disassemble.ps1" | Out-Null
$t=$jkAssembly.GetType('JumpKing.Level.SlopeBlock');$m=$t.GetMethods([Reflection.BindingFlags]'Public,NonPublic,Static,Instance')|Where-Object{$_.Name.EndsWith('IBlock.Intersects')};Write-IL $m
