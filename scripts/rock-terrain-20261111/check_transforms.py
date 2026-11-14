from pathlib import Path
import re,json
out=Path('scripts/rock-terrain-20261111'); results=[]
for p in (out/'backup').glob('*.unity'):
 def parse(s):return {m[2]:m[0] for m in re.finditer(r'--- !u!(\d+) &(\d+)\n.*?(?=\n--- !u!|\Z)',s,re.S)}
 a=parse(p.read_text(encoding='utf-8-sig'));b=parse((Path('Assets/Scenes')/p.name).read_text(encoding='utf-8-sig'))
 transforms={re.search(r'm_GameObject: \{fileID: (\d+)\}',v)[1]:k for k,v in a.items() if v.splitlines()[1]=='Transform:'}
 count=0
 for k,v in a.items():
  if not re.search(r'^\w*Collider2D:',v,re.M):continue
  go=re.search(r'm_GameObject: \{fileID: (\d+)\}',v)[1];t=transforms[go]
  while t!='0':
   assert all(re.search(r"^  "+field+r":.*$",a[t],re.M)[0]==re.search(r"^  "+field+r":.*$",b[t],re.M)[0] for field in ["m_LocalPosition","m_LocalRotation","m_LocalScale","m_Father"]),(p.name,k,t,'collider ancestor transform changed')
   t=re.search(r'm_Father: \{fileID: (\d+)\}',a[t])[1]
  count+=1
 results.append(dict(scene=p.name,verified_collider_transform_chains=count))
(out/'collider-transform-check.json').write_text(json.dumps(results,indent=2));print(results)

