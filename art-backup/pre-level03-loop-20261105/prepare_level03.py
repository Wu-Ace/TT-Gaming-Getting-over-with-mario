import json
from pathlib import Path
# One required surface per column; voids have no lower bypass.
sections=[
('broken shoreline',[0]*5+[2]*3+[0]*3+[3]*3+[1]*4+[4]*3+[2]*3+[0]*3+[2]*3+[4]*3+[3]*3+[5]*3+[3]*3),
('reef teeth',[4]*3+[6]*2+[4]*3+[7]*3+[5]*2+[8]*3+[6]*3+[9]*3+[7]*3+[10]*3+[8]*3+[11]*3),
('broken bridge',sum(([h]*3+[None] for h in [11,13,15,17,15,12,9,6]),[])),
('tidal pockets',[4]*3+[2]*3+[0]*4+[2]*3+[4]*3+[6]*3+[4]*3+[2]*3+[0]*3+[2]*3+[4]*3+[6]*3),
('high ledges',[7]*3+[9]*3+[11]*3+[13]*3+[15]*4+[13]*3+[11]*3+[9]*3+[11]*3+[13]*3+[15]*3+[17]*4),
('island crossing',sum(([h]*w+[None]*gap for h,w,gap in zip([17,19,17,20,18,16,13,10,7,6],[3,2,4,2,3,4,2,3,2,4],[1,1,2,1,1,2,1,1,1,1])),[])),
('block barriers',[5]*3+[7]*2+[5]*3+[8]*2+[6]*3+[9]*3+[7]*3+[10]*2+[8]*3+[11]*3+[9]*3+[12]*3+[10]*3),
('final headland',[11]*3+[13]*3+[15]*3+[14]*3+[12]*3+[10]*3+[8]*3+[6]*3+[8]*3+[10]*3+[12]*3+[14]*5)]
heights=[];regions=[]
for name,values in sections:
 regions.append(dict(name=name,start=len(heights),end=len(heights)+len(values)-1));heights+=values
cells=[]
for x,h in enumerate(heights):
 if h is not None:
  # Bridge/island sections have thin decks above empty space.
  thin=any(r['start']<=x<=r['end'] and r['name'] in ('broken bridge','island crossing') for r in regions)
  for y in range(h-2 if thin else -6,h):cells.append(dict(x=x,y=y))
def surface(x):return heights[x]*.8-3.6
gemx=[90,150,200];gemx=[min(range(max(0,x-4),min(len(heights),x+5)),key=lambda i:abs(i-x) if heights[i]!=None else 999) for x in gemx]
plan=dict(cells=cells,stars=[dict(x=(x+.5)*.8,y=surface(x)+.9) for x in gemx],checkpoints=[dict(x=(x+.5)*.8,y=surface(x)) for x in [72,180] if heights[x] is not None],exit=dict(x=(len(heights)-2)*.8,y=surface(len(heights)-2)),route=[dict(x=(x+.5)*.8,y=surface(x)+1) for x in range(len(heights)) if heights[x]!=None],title='Broken coast: one way forward',regions=regions)
Path('scripts/level03-designed.json').write_text(json.dumps(plan,indent=2))
Path('scripts/level03-design.txt').write_text('\n'.join(f"{r['name']}: {r['start']*.8:.1f}..{(r['end']+1)*.8:.1f}m" for r in regions)+'\nSingle surface, no lower ground under decks; gaps .8m, adjacent rises <=2.4m; physical traversal still requires validation.')



