$jkRoot='E:\SteamLibrary\steamapps\common\Jump King'
[void][Reflection.Assembly]::LoadFrom((Join-Path $jkRoot 'MonoGame.Framework.dll'))
$jkAssembly=[Reflection.Assembly]::LoadFrom((Join-Path $jkRoot 'JumpKing.exe'))
try { $jkTypes=$jkAssembly.GetTypes() } catch [Reflection.ReflectionTypeLoadException] { $jkTypes=$_.Exception.Types | Where-Object { $_ } }
$jkTypes | Where-Object { $_.FullName -match 'Slope|Sampler|BaseBlockFactory' } | ForEach-Object { $_.FullName; $_.GetMethods([Reflection.BindingFlags]'Public,NonPublic,Static,Instance,DeclaredOnly') | ForEach-Object { '  '+$_.ToString() } }
