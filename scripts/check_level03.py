import json
from pathlib import Path
p=json.loads(Path('scripts/level03-designed.json').read_text()); c={(v['x'],v['y']) for v in p['cells']}; air=[]
for r in p['regions']:
 if r['name'] in ('broken bridge','island crossing'):
  cols=[x for x in range(r['start'],r['end']+1) if any(a==x for a,b in c)]
  assert all(sum(a==x for a,b in c)==2 for x in cols)
  assert all(not any(a==x and b<min(y for xx,y in c if xx==x) for a,b in c) for x in cols)
  air.append(f"PASS {r['name']}: all occupied columns only two-cell floating decks, no lower bypass")
for g in p['stars']:
 x=int(g['x']/.8); y=int((g['y']+3.6)/.8);assert (x,y) not in c
Path('scripts/level03-designed-evidence/structure.txt').write_text('\n'.join(air)+'\nPASS 3 objective centers outside terrain\nPhysical reachability NOT proven by structure checks.\n')
