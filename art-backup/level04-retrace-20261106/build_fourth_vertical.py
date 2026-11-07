"""Author a native-cell vertical switchback from the user's chamber reference."""
import json, math
from pathlib import Path
cells=set()
def rect(x,y,w,h=1):
    cells.update((xx,yy) for xx in range(x,x+w) for yy in range(y,y+h))
# Five staggered chambers; open transitions and no low roof above the climbing line.
centers=[10,8,11,8,12]
for room,c in enumerate(centers):
    base=room*16
    rect(c-8,base-2,1,18)
    rect(c+8,base-2,1,18)
    if room:
        # Open the full transition between chambers rather than sealing it with a ceiling.
        pass
rect(2,-3,17,3)
route=[]
for step in range(41):
    room=min(step//8,4); t=(step%8)/8; c=round(centers[room]*(1-t)+centers[min(room+1,4)]*t)
    # Four-cell spacing vertically at the same X leaves room to swing the hammer.
    wave=[-4,-2,0,2,4,2,0,-2][step%8]
    x=c+wave; y=step*2
    rect(x-1,y-1,3)
    route.append({'x':(x+.5)*.8,'y':y*.8-3.6})
# Rest bays at each chamber turn. Objectives sit on the main route, above solid footing.
for step in [8,16,24,32,40]:
    p=route[step]; x=round(p['x']/.8-.5); y=step*2
    rect(x-2,y-1,5)
def above(step,height=1.05):
    p=route[step]; return dict(x=p['x'],y=p['y']+height)
p=dict(number=4,title='Town switchback ascent',reference='User vertical chamber reference, adapted to hammer play; not a 1000m scale claim',
       cells=[dict(x=x,y=y) for x,y in sorted(cells)],stars=[above(s) for s in [10,23,36]],
       checkpoints=[above(s,.4) for s in [16,32]],exit=above(40,.8),spawn=above(0,1.3),
       route=route,donuts=[],authoredLength=sum(math.dist(tuple(a.values()),tuple(b.values())) for a,b in zip(route,route[1:])))
Path('scripts/level04-designed.json').write_text(json.dumps(p,indent=2)+'\n')
print('Authored',len(cells),'cells; 40 transitions; rise 1.6m; landing width 2.4m; physical traversal pending')
