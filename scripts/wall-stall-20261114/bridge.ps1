param([string]$Request='state.json')
$si=New-Object System.Diagnostics.ProcessStartInfo
$si.FileName='E:\Tuanjie Cowork\cli\bin\win32-x64\codely.exe'
$si.Arguments='serve unity-mcp --stdio --unity-project-path "E:\TT Gaming\TT-Gaming-Getting-over-with-mario"'
$si.UseShellExecute=$false;$si.RedirectStandardInput=$true;$si.RedirectStandardOutput=$true;$si.RedirectStandardError=$true;$si.CreateNoWindow=$true
$p=[System.Diagnostics.Process]::Start($si)
try {
$p.StandardInput.WriteLine('{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"wall-check","version":"1"}}}')
$p.StandardInput.Flush()
do {$line=$p.StandardOutput.ReadLineAsync();if(!$line.Wait(20000)){throw 'Initialize timeout'};$o=$line.Result|ConvertFrom-Json}while($o.id -ne 1)
$p.StandardInput.WriteLine('{"jsonrpc":"2.0","method":"notifications/initialized"}')
$req=Get-Content (Join-Path $PSScriptRoot $Request) -Raw|ConvertFrom-Json
$p.StandardInput.WriteLine((@{jsonrpc='2.0';id=2;method='tools/call';params=$req}|ConvertTo-Json -Depth 30 -Compress));$p.StandardInput.Flush()
do {$line=$p.StandardOutput.ReadLineAsync();if(!$line.Wait(45000)){throw 'Tool timeout'};$o=$line.Result|ConvertFrom-Json}while($o.id -ne 2)
$line.Result|Tee-Object (Join-Path $PSScriptRoot ($Request.Replace('.json','-result.json')))
}finally {$p.Kill()}
