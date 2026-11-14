from pathlib import Path
from PIL import Image,ImageDraw
import json,hashlib
out=Path('scripts/jk-tilemap-20261111');im=Image.open(out/'level-data.png');rows=(out/'current-raster.txt').read_text().splitlines();cells=[];extra=[];counts={}
for n in range(14):
 for y in range(45):
  for x in range(60):
   sx=n//13*60+x;sy=n%13*45+y;p=im.getpixel((sx,sy));Y=n*45+44-y
   if p in [(0,0,0,255),(255,0,0,255)]:
    kind=0
    if p[0]==255:
     def black(dx,dy):return 0<=sx+dx<780 and 0<=sy+dy<585 and im.getpixel((sx+dx,sy+dy))==(0,0,0,255)
     u,d,l,r=black(0,-1),black(0,1),black(-1,0),black(1,0)
     if (u or d) and (l or r):kind=1 if d and l else 2 if d and r else 3 if u and l else 4
     # ambiguous slope endpoint kept as full cell (more terrain)
    cells.append(dict(x=x,y=Y,kind=kind))
# Original red slope bands may be two pixels thick. Resolve the whole straight
# band first, then use full cells inside and triangles only on its outer line.
bypos={(c['x'],c['y']):c for c in cells}
red={(c['x'],c['y']) for c in cells if im.getpixel((c['y']//45//13*60+c['x'],(c['y']//45)%13*45+44-c['y']%45))==(255,0,0,255)}
black=set(bypos)-red
pending=set(red)
while pending:
 start=pending.pop();group={start};stack=[start]
 while stack:
  x,y=stack.pop()
  for dx in [-1,0,1]:
   for dy in [-1,0,1]:
    n=(x+dx,y+dy)
    if n in pending:pending.remove(n);group.add(n);stack.append(n)
 if len(group)<3:continue
 def variance(sign):
  vals=[y-sign*x for x,y in group];mean=sum(vals)/len(vals);return sum((v-mean)**2 for v in vals)/len(vals)
 sign=min([1,-1],key=variance)
 values=[y-sign*x for x,y in group]
 neighbors={(x+dx,y+dy) for x,y in group for dx,dy in [(1,0),(-1,0),(0,1),(0,-1)] if (x+dx,y+dy) in black}
 if not neighbors:continue
 lower=sum(y-sign*x for x,y in neighbors)/len(neighbors)<sum(values)/len(values)
 edge=max(values) if lower else min(values)
 for x,y in group:
  value=y-sign*x
  # For parallel interior rows the original band is completely solid.
  bypos[x,y]['kind']=(2 if lower else 3) if sign==1 and value==edge else (1 if lower else 4) if sign==-1 and value==edge else 0
source={(c['x'],c['y']) for c in cells}
for y,row in enumerate(rows):
 for x,v in enumerate(row):
  if v=='1' and (x,y) not in source:extra.append(dict(x=x,y=y,kind=0))
(out/'plan.json').write_text(json.dumps(dict(cells=cells,extra=extra),indent=1))
a=Image.new('RGB',(60,630),(29,36,45))
for c in cells:a.putpixel((c['x'],629-c['y']),(190,190,190) if c['kind']==0 else (240,180,75))
for c in extra:a.putpixel((c['x'],629-c['y']),(70,150,225))
a.resize((360,3780),Image.Resampling.NEAREST).save(out/'union-preview.png')
print('source',len(cells),'slopes',sum(c['kind']>0 for c in cells),'extras',len(extra))
