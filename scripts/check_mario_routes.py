import json
from pathlib import Path
from collections import deque

def reach(c):
 nodes={(x,y+1) for x,y in c if all((x,y+i) not in c for i in range(1,4))};start=min(nodes,key=lambda v:abs(v[0]-2)+abs(v[1]));q=deque([start]);seen={start};byx={}
 for x,y in nodes:byx.setdefault(x,[]).append(y)
 while q:
  x,y=q.popleft()
  for xx in range(x-3,x+4):
   for yy in byx.get(xx,[]):
    t=(xx,yy)
    if -7<=yy-y<=2 and t not in seen:seen.add(t);q.append(t)
 return nodes,seen
log=['Conservative surface graph: <=1.6m rise, <=1.6m clear horizontal gap, >=2.4m headroom. This is NOT a physics playthrough.']
for n in range(2,11):
 p=json.loads(Path(f'scripts/mario-reference/L{n:02}.json').read_text());c={(a['x'],a['y']) for a in p['cells']};nodes,seen=reach(c)
 objectives=p['stars']
 ok=[any(abs(x-(s['x']/.8-.5))<1.1 and abs(y-(s['y']+3.6-1.05)/.8)<.1 for x,y in seen) for s in objectives]
 exit_ok=any(x>=374 and y==0 for x,y in seen)
 cp_ok=all(any(abs(x-(s['x']/.8-.5))<1.1 and abs(y-(s['y']+3.6)/.8)<.1 for x,y in seen) for s in p['checkpoints'])
 result=all(ok) and exit_ok and cp_ok
 log.append(f'{"PASS" if result else "FAIL"} L{n:02} stars={ok} exit={exit_ok} checkpoints={cp_ok}')
 if not result:raise RuntimeError(log[-1])
print('\n'.join(log));Path('scripts/mario-evidence/route-graph.txt').write_text('\n'.join(log))
