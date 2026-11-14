. "$PSScriptRoot\inspect-types.ps1" | Out-Null
foreach($n in @('JumpKing.Level.Sampler.LevelTexture','JumpKing.Level.Sampler.Sampler','JumpKing.TextureSampler','JumpKing.Level.Factory.BaseBlockFactory','JumpKing.Level.SlopeBlock','JumpKing.Level.SlopeType')){
$t=$jkAssembly.GetType($n);$n;$t.GetFields([Reflection.BindingFlags]'Public,NonPublic,Static,Instance') | ForEach-Object { $_.ToString() };$t.GetConstructors([Reflection.BindingFlags]'Public,NonPublic,Instance')|ForEach-Object {$_.ToString()};if($t.IsEnum){[Enum]::GetNames($t)|ForEach-Object{ $_+'='+[int][Enum]::Parse($t,$_)} }
}
