param([string]$Request)
$si=[Diagnostics.ProcessStartInfo]::new();$si.FileName='E:\Tuanjie Cowork\cli\bin\win32-x64\codely.exe';$si.Arguments='serve unity-mcp --stdio --unity-project-path "E:\TT Gaming\TT-Gaming-Getting-over-with-mario"';$si.UseShellExecute=$false;$si.RedirectStandardInput=$true;$si.RedirectStandardOutput=$true;$si.CreateNoWindow=$true
$p=[Diagnostics.Process]::new();$p.StartInfo=$si;$p.Start()|Out-Null
try {
$p.StandardInput.WriteLine('{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"codex","version":"1"}}}')
do {$line=$p.StandardOutput.ReadLine()} until($line -match '"id"\s*:\s*1')
$req=Get-Content $Request -Raw|ConvertFrom-Json
$p.StandardInput.WriteLine((@{jsonrpc='2.0';id=2;method='tools/call';params=$req}|ConvertTo-Json -Depth 20 -Compress))
do {$line=$p.StandardOutput.ReadLine()} until($line -match '"id"\s*:\s*2')
$line | Tee-Object -FilePath ($Request+'.result.json')
}finally{$p.Kill()}
