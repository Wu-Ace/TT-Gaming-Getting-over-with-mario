from pathlib import Path
import math,json
from collections import deque
# Authored landmark paths, in native tile units. Every path climbs through distinct local forms.
ROUTES=[
[(4,0),(23,4),(29,24),(21,43),(39,62),(34,85),(54,104),(47,126),(68,147),(62,171),(84,193),(100,214),(124,230),(150,246)],
[(4,0),(33,7),(63,25),(77,49),(66,76),(40,95),(22,121),(33,148),(64,165),(89,188),(104,216),(127,237),(150,249)],
[(4,0),(29,4),(36,32),(54,48),(65,75),(58,103),(36,122),(26,151),(48,174),(76,187),(102,212),(117,242),(150,259)],
[(4,0),(24,4),(34,31),(25,58),(43,78),(37,107),(57,127),(49,155),(70,175),(63,204),(87,223),(106,244),(131,257),(150,275)],
[(4,0),(25,7),(45,23),(65,39),(86,56),(98,78),(83,101),(58,111),(29,127),(22,153),(42,178),(67,197),(97,218),(121,239),(150,260)],
[(4,0),(24,5),(36,29),(30,52),(49,73),(43,99),(63,119),(57,145),(77,164),(72,191),(96,213),(117,236),(150,260)],
[(4,0),(32,5),(50,21),(45,48),(66,66),(61,95),(82,115),(76,144),(99,163),(92,190),(116,210),(123,240),(150,267)],
[(4,0),(29,5),(54,22),(76,43),(91,67),(82,93),(58,113),(31,132),(20,158),(37,186),(65,203),(94,226),(121,247),(150,270)],
[(4,0),(25,6),(40,30),(34,55),(53,76),(47,104),(68,126),(61,153),(84,175),(78,202),(104,225),(122,250),(150,280)]
]
TITLES=['Root and rock','Broken sea cliff','Rooftop salvage','Chimney hollow','Snow overhang','Ice wall grips','Castle furniture climb','Volcanic switchback','Cloud needle summit']
# Gait changes along each mountain: broad rock, short neck, stacked debris, narrow hold, overhang.
THICKNESS=[ [9,8,5,11,6,4,9,6,10,4,8,5,9,6], [5,8,4,7,4,9,5,4,7,5,8,4,6,4], [10,5,4,8,5,4,10,5,4,7,4,9,4,5], [7,4,9,5,4,8,5,11,4,6,9,5,4,6], [11,5,7,4,10,5,6,4,9,5,7,4,6,5], [8,5,4,9,5,4,8,5,11,4,7,5,6,4], [11,5,4,8,5,10,4,6,5,9,4,7,5,4], [9,5,11,4,7,5,10,4,8,5,6,4,9,5], [5,4,9,5,4,8,5,4,9,5,4,7,5,4] ]

def reach(c):
 nodes={(x,y+1) for x,y in c if all((x,y+i) not in c for i in range(1,4))}
 start=min(nodes,key=lambda p:abs(p[0]-3)+abs(p[1]));q=deque([start]);seen={start};byx={}
 for x,y in nodes:byx.setdefault(x,[]).append(y)
 while q:
  x,y=q.popleft()
  for xx in range(x-3,x+4):
   for yy in byx.get(xx,[]):
    node=(xx,yy)
    if -10<=yy-y<=2 and node not in seen:seen.add(node);q.append(node)
 return nodes,seen

def build(n,knots):
 raw=sum(math.dist(a,b) for a,b in zip(knots,knots[1:]))*.8
 scale=312/raw
 knots=[(round(4+(x-4)*scale),round(y*scale)) for x,y in knots]
 c=set(); marks=[]; slabs=[]
 # The visible mountain is made of continuous irregular bodies, with necks, shelves and exposed returns.
 for j,((ax,ay),(bx,by)) in enumerate(zip(knots,knots[1:])):
  thickness=THICKNESS[n-2][j%len(THICKNESS[n-2])]*2;dx=bx-ax
  for x in range(min(ax,bx),max(ax,bx)+1):
   t=(x-ax)/dx if dx else 0;top=round(ay+(by-ay)*t)
   # Broad rock bodies taper at their ends; no identical repeated floating platforms.
   depth=max(2,round(thickness*(.65+.35*math.sin(math.pi*t))))
   for y in range(top-depth,top):c.add((x,y))
  # Resting bays at changes of direction, not at every grip.
  if j in [2,6,9,12]:
   for x in range(bx-3,bx+4):
    for y in range(by-3,by):c.add((x,y))
  marks.append({'x':bx*.8,'y':by*.8-3.6,'kind':['rock lip','neck','turning ledge','overhang','debris edge'][j%5]})
 # Base and summit apron; the summit is at the rightmost side of the whole mountain.
 for x in range(0,14):
  for y in range(-4,0):c.add((x,y))
 ex,ey=knots[-1]
 for x in range(ex-4,ex+7):
  for y in range(ey-4,ey):c.add((x,y))
 # Staggered small holds on exposed rises. Higher holds move toward the rock face,
 # so they do not form low-ceiling stacks above the preceding hold.
 for repeat in range(2):
  for x in sorted({x for x,y in c}):
   ys=[y for xx,y in c if xx==x];top=max(ys)+1
   left=[y for xx,y in c if xx==x-1 and y<top]
   if not left:continue
   low=max(left)+1
   if 2<top-low<24:
    for h in range(low+2,top,2):
     start=x-max(3,(top-h)*2)
     for xx in range(start,start+3):
      if xx>=0 and not any((xx,yy) in c for yy in range(h-1,h+3)):c.add((xx,h-1))
 # Protect a body-clear chain along the authored rock surface; grips remain part of the rock.
 chain=[]
 for a,b in zip(knots,knots[1:]):
  steps=max(1,math.ceil(abs(b[0]-a[0])/2),math.ceil(abs(b[1]-a[1])/2))
  for i in range(steps+1):
   t=i/steps;cx=round(a[0]+(b[0]-a[0])*t);cy=round(a[1]+(b[1]-a[1])*t)
   if abs(b[1]-a[1])>abs(b[0]-a[0])*.8:cx += [-3,-1,1,3,1,-1][i%6]
   chain.append((cx,cy))
 for x,y in chain:
  for yy in range(y,y+3):c.discard((x,yy))
 for x,y in chain:c.add((x,y-1))
 # Surface graph validates conservative landing geometry before editor mutation.
 nodes,seen=reach(c)
 # Landmark objectives are distributed by cumulative route distance, including a sideways pull at each.
 distance=[0]
 for a,b in zip(knots,knots[1:]):distance.append(distance[-1]+math.dist(a,b))
 stars=[];cps=[]
 for f in [.24,.56,.83]:
  i=min(range(1,len(knots)-1),key=lambda i:abs(distance[i]/distance[-1]-f));kx,ky=knots[i]
  options=[p for p in nodes if abs(p[0]-kx)<14 and abs(p[1]-ky)<9]
  if not options:raise RuntimeError(f'L{n} star bay missing')
  x,y=min(options,key=lambda p:abs(p[0]-kx)+abs(p[1]-ky)*2)
  stars.append({'x':(x+.5)*.8,'y':y*.8-3.6+1.05})
 for f in ([.4,.73] if n>=8 else [.4]):
  i=min(range(1,len(knots)-1),key=lambda i:abs(distance[i]/distance[-1]-f));kx,ky=knots[i]
  options=[p for p in nodes if abs(p[0]-kx)<20 and abs(p[1]-ky)<10 and all((p[0]+d,p[1]-1) in c for d in [-2,-1,0,1,2])]
  if not options:
   # A deliberate stable landing bay opens to the outside of the current cliff.
   for x in range(kx-4,kx+5):
    for y in range(ky-3,ky):c.add((x,y))
   nodes,seen=reach(c);options=[p for p in nodes if abs(p[0]-kx)<20 and abs(p[1]-ky)<10 and all((p[0]+d,p[1]-1) in c for d in [-2,-1,0,1,2])]
  x,y=min(options,key=lambda p:abs(p[0]-kx)+abs(p[1]-ky)*2);cps.append({'x':(x+.5)*.8,'y':y*.8-3.6})
 # Repair only disconnected objective approaches with small outside holds, never a flat bypass floor.
 repairs=0
 goals=[(round(v['x']/.8-.5),round((v['y']+3.6-1.05)/.8)) for v in stars]
 goals += [(round(v['x']/.8-.5),round((v['y']+3.6)/.8)) for v in cps]+[(ex+2,ey)]
 for goal_index,(gx,gy) in enumerate(goals):
  for attempt in range(250):
   nodes,seen=reach(c)
   if any(abs(x-gx)<=1 and y==gy for x,y in seen):break
   px,py=min(seen,key=lambda p:math.hypot(p[0]-gx,p[1]-gy))
   if math.hypot(px-gx,py-gy)<=3:
    if goal_index<3:stars[goal_index]={'x':(px+.5)*.8,'y':py*.8-3.6+1.05}
    elif goal_index<3+len(cps):cps[goal_index-3]={'x':(px+.5)*.8,'y':py*.8-3.6}
    else:ex=px-2;ey=py
    break
   nx=px+max(-2,min(2,gx-px));ny=py+max(-2,min(2,gy-py))
   if nx==px and ny!=py:nx=px+2 if px<=gx else px-2
   for yy in range(ny,ny+3):c.discard((nx,yy))
   c.add((nx,ny-1));repairs+=1
  else:raise RuntimeError(f'L{n} unreachable objective {gx,gy} nearest={px,py} attempted={nx,ny} seen={len(seen)} present={(gx,gy) in nodes} cells={[y for x,y in c if x==gx and abs(y-gy)<5]}')
 return {'number':n,'title':TITLES[n-2],'cells':[{'x':x,'y':y} for x,y in sorted(c)],'stars':stars,'checkpoints':cps,'exit':{'x':(ex+2)*.8,'y':ey*.8-3.6},'route':[{'x':x*.8,'y':y*.8-3.6} for x,y in knots],'landmarks':marks,'authoredLength':distance[-1]*.8}

folder=Path('scripts/climb-plans');folder.mkdir(exist_ok=True)
for n,k in enumerate(ROUTES,2):
 p=build(n,k);(folder/f'L{n:02}.json').write_text(json.dumps(p));print(n,round(p['authoredLength'],1),p['exit'],len(p['cells']))
