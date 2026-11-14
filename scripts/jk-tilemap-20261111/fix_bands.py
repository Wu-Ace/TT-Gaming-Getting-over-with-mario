from pathlib import Path
p=Path('scripts/jk-tilemap-20261111/build_plan.py');s=p.read_text();s=s.replace("source={(c['x'],c['y']) for c in cells}",'''# Original red slope bands may be two pixels thick. Resolve the whole straight
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
source={(c['x'],c['y']) for c in cells}''');p.write_text(s)
