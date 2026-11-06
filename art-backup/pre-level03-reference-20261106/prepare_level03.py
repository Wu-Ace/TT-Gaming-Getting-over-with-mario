import json
from pathlib import Path
cells=set();donuts=[]
def rect(a,z,lo,hi):
 for x in range(a,z):
  for y in range(lo,hi):cells.add((x,y))
# Right lower start, leftwards aerial traverse, left ascent, rightwards summit.
rect(106,122,-10,0)
for a,z,h in [(94,99,-1),(82,88,-2),(70,75,0),(58,64,-1),(46,51,0),(34,40,1),(22,28,2),(10,16,3)]:
 if a in [94,70,46,22]:donuts.append(dict(x=a,y=h-1,width=z-a))
 else:rect(a,z,h-2,h)
# A few short stable landings between the long spans.
for a,h in [(100,0),(89,-1),(76,-1),(65,0),(52,-1),(41,1),(29,2),(17,3)]:rect(a,a+3,h-1,h)
rect(2,10,-10,5)
# Zigzag upward along the left cliff: no lower flat bypass.
for a,z,h in [(11,15,7),(5,9,9),(12,16,11),(6,10,13),(13,17,15),(7,11,17),(14,18,19),(8,12,21),(15,20,23),(10,15,25),(17,23,27),(22,30,29)]:
 if h in [11,19,25]:donuts.append(dict(x=a,y=h-1,width=z-a))
 else:rect(a,z,h-2,h)
# Upper rightward traverse ends at a solid headland.
for a,z,h in [(32,36,28),(39,44,27),(47,51,29),(54,60,28),(63,67,27),(70,76,29),(79,83,28),(86,92,27),(95,99,28)]:
 if a in [39,54,70,86]:donuts.append(dict(x=a,y=h-1,width=z-a))
 else:rect(a,z,h-2,h)
rect(102,122,19,28)
v=lambda x,y:dict(x=x,y=y)
p=dict(number=3,reference='User supplied Hop Hop Donut Lifts map; right to left lower crossing, left ascent, upper right exit',title='Beach cliff loop',cells=[v(x,y) for x,y in sorted(cells)],donuts=donuts,stars=[v(48.4,-2.2),v(12,12.6),v(59,20.6)],checkpoints=[v(4,0.4),v(20,19.6)],exit=v(92,18.8),spawn=v(92,-2.9),route=[v(92,-2.9),v(4,.4),v(20,19.6),v(92,19.6)],authoredLength=200)
Path('scripts/level03-designed.json').write_text(json.dumps(p,indent=2))
print(len(cells),len(donuts))
