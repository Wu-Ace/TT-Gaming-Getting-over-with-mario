import json
from collections import deque
from pathlib import Path
p=Path('scripts/jk-tilemap-20261111');plan=json.loads((p/'plan.json').read_text());report={}
for name,cells in [('original',plan['cells']),('union',plan['cells']+plan['extra'])]:
 solid={(c['x'],c['y']) for c in cells};free={(x,y) for x in range(1,59) for y in range(5,630) if (x,y) not in solid};start=min(free,key=lambda c:(c[0]-30)**2+(c[1]-6)**2);seen={start};q=deque([start])
 while q:
  x,y=q.popleft()
  for n in [(x-1,y),(x+1,y),(x,y-1),(x,y+1)]:
   if n in free and n not in seen:seen.add(n);q.append(n)
 report[name]=dict(start=start,highestConnectedRow=max(y for x,y in seen),topRowReached=any(y==629 for x,y in seen))
(p/'connectivity.json').write_text(json.dumps(report,indent=2));print(report)
