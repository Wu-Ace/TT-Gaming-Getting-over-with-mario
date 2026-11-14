. "$PSScriptRoot\inspect-types.ps1" | Out-Null
$opcodes=@{};[Reflection.Emit.OpCodes].GetFields([Reflection.BindingFlags]'Public,Static') | ForEach-Object { $op=$_.GetValue($null);$opcodes[[int]$op.Value -band 65535]=$op }
function Write-IL($method){
$method.ToString();$bytes=$method.GetMethodBody().GetILAsByteArray();$pos=0
while($pos -lt $bytes.Length){$offset=$pos;$v=[int]$bytes[$pos++];if($v -eq 254){$v=65024+[int]$bytes[$pos++]};$op=$opcodes[$v];$arg='';switch($op.OperandType.ToString()){
'InlineNone'{}
'ShortInlineI'{$arg=[sbyte]::Parse($bytes[$pos++].ToString('X2'),[Globalization.NumberStyles]::HexNumber)}
'ShortInlineVar'{$arg=$bytes[$pos++]}
'InlineVar'{$arg=[BitConverter]::ToUInt16($bytes,$pos);$pos+=2}
'InlineI'{$arg=[BitConverter]::ToInt32($bytes,$pos);$pos+=4}
'InlineR'{$arg=[BitConverter]::ToDouble($bytes,$pos);$pos+=8}
'ShortInlineR'{$arg=[BitConverter]::ToSingle($bytes,$pos);$pos+=4}
'ShortInlineBrTarget'{$arg=$pos+1+[sbyte]::Parse($bytes[$pos].ToString('X2'),[Globalization.NumberStyles]::HexNumber);$pos++}
'InlineBrTarget'{$arg=$pos+4+[BitConverter]::ToInt32($bytes,$pos);$pos+=4}
'InlineSwitch'{$len=[BitConverter]::ToInt32($bytes,$pos);$pos+=4;$arg="switch($len)";$pos+=4*$len}
default{$tok=[BitConverter]::ToInt32($bytes,$pos);$pos+=4;try{if($op.OperandType.ToString() -eq 'InlineString'){$arg=$method.Module.ResolveString($tok)}else{$arg=$method.Module.ResolveMember($tok)}}catch{$arg=$tok}}
};'{0:X4}: {1} {2}' -f $offset,$op.Name,$arg
}}
$t=$jkAssembly.GetType('JumpKing.Level.Factory.BaseBlockFactory');foreach($n in @('GetSlopeType','GetBlock','IsSolidBlock')){Write-IL ($t.GetMethod($n,[Reflection.BindingFlags]'Public,NonPublic,Static,Instance'))}
Write-IL ($jkAssembly.GetType('JumpKing.Level.Sampler.LevelTexture').GetMethod('GetCoord',[Reflection.BindingFlags]'Public,NonPublic,Static,Instance',$null,[type[]]@([int],[int],[int],[int]),$null))

