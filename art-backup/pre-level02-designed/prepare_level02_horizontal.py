from pathlib import Path
import json,math,hashlib
knots=[(0,0),(26,0),(36,4),(46,8),(56,8),(64,4),(72,0),(82,-4),(92,-4),(102,0),(112,4),(122,4),(130,0),(140,-6),(152,-6),(160,-2),(168,2),(176,6),(184,10),(192,10),(200,6),(208,2),(216,0),(224,0),(232,4),(240,8),(248,8),(256,4),(264,0),(274,-4),(284,-4),(294,0),(304,4),(314,4),(324,0),(334,-2),(344,0),(354,2),(364,0),(380,0)]
c=set()
for (ax,ay),(bx,by) in zip(knots,knots[1:]):
 for x in range(ax,bx):
  h=round(ay+(by-ay)*(x-ax)/(bx-ax))
  for y in range(-12,h):c.add((x,y))
def rect(x,y,w,h):
 for xx in range(x,x+w):
  for yy in range(y,y+h):c.add((xx,yy))
# A roofed passage, a recessed basin, and an optional high branch: different hammer problems.
rect(110,10,19,2);rect(162,12,12,2)
rect(250,7,2,1);rect(253,9,4,1);rect(259,11,4,1);rect(265,13,4,1);rect(269,15,2,1);rect(271,15,9,2)
# Return footholds make the upper branch descend into the route, not end in a blind drop.
rect(282,12,4,1);rect(288,8,4,1);rect(294,5,4,1)
plan={'number':2,'title':'Jungle rises and hollows','cells':[{'x':x,'y':y} for x,y in sorted(c)],'stars':[{'x':42,'y':3.85},{'x':116,'y':-7.35},{'x':220,'y':11.05}],'checkpoints':[{'x':98.0,'y':-.4}], 'exit':{'x':300,'y':-3.6},'route':[{'x':x*.8,'y':y*.8-3.6} for x,y in knots], 'authoredLength':sum(math.dist(a,b) for a,b in zip(knots,knots[1:]))*.8}
# The third star sits over the branch's existing native-cell top (17 cells).
plan['stars'][2]['y']=17*.8-3.6+1.05
Path('scripts/level02-horizontal.json').write_text(json.dumps(plan))
Path('scripts/level02-horizontal-evidence/unrelated-before.json').write_text(json.dumps({p.as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in Path('Assets/Scenes').glob('*.unity') if p.name!='Level02_Jungle.unity'}))
print('route',plan['authoredLength'],'bounds width304 height',22*.8)
