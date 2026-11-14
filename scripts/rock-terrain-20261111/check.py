import re,json
from pathlib import Path
out=Path('scripts/rock-terrain-20261111');report=[]
for old in (out/'backup').glob('*.unity'):
 a=old.read_text(encoding='utf-8-sig');b=(Path('Assets/Scenes')/old.name).read_text(encoding='utf-8-sig')
 def blocks(s):return {m.group(2):m.group(0) for m in re.finditer(r'--- !u!(\d+) &(\d+)\n.*?(?=\n--- !u!|\Z)',s,re.S)}
 aa,bb=blocks(a),blocks(b);changes=[]
 for k,v in aa.items():
  if v!=bb.get(k):changes.append((k,v.splitlines()[0],v.splitlines()[1]))
 col=[k for k,v in aa.items() if re.search(r'^\w*Collider2D:',v,re.M)]
 assert all(aa[k]==bb[k] for k in col),old.name+' collider changed'
 report.append(dict(scene=old.name,colliders=len(col),colliders_unchanged=True,changed_blocks=changes))
(out/'structural-check.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report,indent=2)[:1800])
