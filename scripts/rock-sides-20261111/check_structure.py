import re,json,hashlib
from pathlib import Path
root=Path.cwd();out=root/'scripts/rock-sides-20261111';report=[]
def blocks(s):return {m[2]:m[0] for m in re.finditer(r'--- !u!(\d+) &(-?\d+)\n.*?(?=\n--- !u!|\Z)',s,re.S)}
for p in (out/'backup').glob('*.unity'):
 a=blocks(p.read_text(encoding='utf-8-sig'));b=blocks((root/'Assets/Scenes'/p.name).read_text(encoding='utf-8-sig'))
 cols=[k for k,v in a.items() if re.search(r'^\w*Collider2D:',v,re.M)]
 assert all(a[k]==b[k] for k in cols),(p.name,'collider serialization changed')
 transforms=[k for k,v in a.items() if v.splitlines()[1] in ['Transform:','RectTransform:']]
 for k in transforms:
  for f in ['m_LocalPosition','m_LocalRotation','m_LocalScale','m_Father']:
   assert re.search(r'^  '+f+r':.*$',a[k],re.M)[0]==re.search(r'^  '+f+r':.*$',b[k],re.M)[0],(p.name,k,f)
 report.append(dict(scene=p.name,colliders_unchanged=len(cols),transforms_unchanged=len(transforms)))
(out/'structure.json').write_text(json.dumps(report,indent=2));print(json.dumps(report,indent=2))
