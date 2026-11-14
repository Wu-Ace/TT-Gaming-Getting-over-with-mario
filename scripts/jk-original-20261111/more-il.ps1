. "$PSScriptRoot\disassemble.ps1" | Out-Null
foreach($t in $jkTypes){foreach($m in $t.GetMethods([Reflection.BindingFlags]'Public,NonPublic,Static,Instance,DeclaredOnly')){if($m.Name -eq 'IsBlockEdge'){$t.FullName;Write-IL $m}}}
$t=$jkAssembly.GetType('JumpKing.Level.SlopeBlock');Write-IL ($t.GetMethod('MakeLines',[Reflection.BindingFlags]'NonPublic,Instance'));Write-IL ($t.GetMethod('GetNormal',[Reflection.BindingFlags]'Public,Instance'))
