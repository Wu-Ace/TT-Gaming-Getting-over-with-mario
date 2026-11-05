from PIL import Image
from pathlib import Path
import json,shutil
ids=['4a009cde','d482cdaa','4a0edcd0','d46b4d04','f06f4161','7a534eda','786b05d7','78f43f07','d46b4d04']
ranges=[(256,3008),(0,2192),(0,2848),(0,2304),(256,3088),(0,2864),(0,2768),(0,2528),(0,2304)]
folder=Path('scripts/mario-reference');folder.mkdir(exist_ok=True)
for n,(ident,(start,end)) in enumerate(zip(ids,ranges),2):
 p=next(Path('C:/Users/30491/AppData/Local/Temp').glob('codex-clipboard-'+ident+'*.png'));im=Image.open(p).convert('RGB');shutil.copy2(p,folder/(f'L{n:02}-reference.png'))
 cells=set(); source=[]
 for sy in range(2,15):
  for sx in range(start//16,end//16):
   pixels=list(im.crop((sx*16,sy*16,sx*16+16,sy*16+16)).getdata())
   brown=sum(c==(153,78,0) for c in pixels);pink=sum(c==(255,204,197) for c in pixels);blue=sum(c==(0,124,141) for c in pixels);grey=sum(c in [(102,102,102),(173,173,173)] for c in pixels);green=sum(c in [(136,216,0),(13,147,0)] for c in pixels)
   solid=(brown+pink>=180 or brown>=125 or blue>=150 or grey>=55 or (grey>=20 and sum(c==(255,255,255) for c in pixels)>=80) or (green>=100 and brown>=12) or green>=150)
   if sy<13 and sx*16<start+128:continue
   if not solid:continue
   a=8+round((sx*16-start)/(end-start)*350);b=8+round(((sx+1)*16-start)/(end-start)*350)
   # Ninth adaptation uses a different second-half height profile from the same treetop reference.
   y=12-sy
   if n==10 and a>180:y+=1 if a<265 else -1
   for x in range(a,max(a+1,b)):cells.add((x,y))
 cells={(x,round(y*.65) if y>=0 else y) for x,y in cells}
 if n==10:
  first=json.loads((folder/'L03.json').read_text())
  cells={(p['x'],p['y']) for p in first['cells'] if p['x']<180}|{(x,y) for x,y in cells if x>=180}
 # Remove the original end staircase / terminal wall; keep a short level exit apron.
 cells={(x,y) for x,y in cells if x<342 or y<0}
 for x in range(342,380):
  for y in [-3,-2,-1]:cells.add((x,y))
 # Safe spawn and short exit apron replace the original ending staircase.
 for x in list(range(0,10))+list(range(358,380)):
  for y in [-3,-2,-1]:cells.add((x,y))
 # Bridge only impassable horizontal voids. Intermediate islands retain the original silhouette.
 occupied=sorted(set(x for x,y in cells if y<=7))
 for a,b in zip(occupied,occupied[1:]):
  if b-a<=3:continue
  ha=max(y+1 for x,y in cells if x==a and y<=7);hb=max(y+1 for x,y in cells if x==b and y<=7)
  for x in range(a+3,b,5):
   top=round(ha+(hb-ha)*(x-a)/(b-a))
   for xx in [x,x+1,x+2]:cells.add((xx,top-1))
 # Tall solid faces receive small side footholds; gaps are never filled with a continuous floor.
 for x in range(5,358):
  ys=[y for xx,y in cells if xx==x and y<7]
  if not ys:continue
  top=max(ys)+1
  left=[y for xx,y in cells if xx==x-1 and y<7]
  low=max(left)+1 if left else min(0,top-2)
  if top-low>2:
   for h in range(low+2,top,2):
    for xx in range(x-max(3,(top-h)*2),x-max(3,(top-h)*2)+3):
     if not any((xx,yy) in cells for yy in range(h-1,h+3)):cells.add((xx,h-1))
 # Stars use existing upper surfaces in three separated regions, not three new identical platforms.
 stars=[]
 for target in [78,188,300]:
  candidates=[(x,y+1) for x,y in cells if abs(x-target)<24 and (x,y+1) not in cells and 1<=y+1<=7 and (x,y+2) not in cells and (x,y+3) not in cells]
  if not candidates:candidates=[(x,y+1) for x,y in cells if abs(x-target)<24 and (x,y+1) not in cells and y<7]
  x,top=min(candidates,key=lambda q:abs(q[0]-target)-q[1]*.4);stars.append({'x':(x+.5)*.8,'y':top*.8-3.6+1.05})
  # Stable 1.6m footholds along a tall selected approach, outside the destination footprint.
  for h in range(2,top,2):
   for xx in range(x-max(3,(top-h)*2),x-max(3,(top-h)*2)+3):
    if (xx,h-1) not in cells and not any((xx,yy) in cells for yy in range(h,h+3)):cells.add((xx,h-1))
 # Safe checkpoint explicitly on a broad surface with clear headroom.
 cps=[]
 for target in ([133,278] if n>=8 else [133]):
  opts=[(x,y+1) for x,y in cells if abs(x-target)<35 and (x,y+1) not in cells and all((xx,y) in cells for xx in range(x-2,x+3)) and all((x,yy) not in cells for yy in range(y+1,y+5)) and y<7]
  if opts:
   x,t=min(opts,key=lambda q:abs(q[0]-target));cps.append({'x':(x+.5)*.8,'y':t*.8-3.6})
  else:
   x=target
   for xx in range(x-3,x+4):cells.add((xx,-1))
   cps.append({'x':(x+.5)*.8,'y':-3.6})
 if n==2:cells.difference_update((x,3) for x in range(330,333))
 data={'number':n,'reference':str(folder/(f'L{n:02}-reference.png')),'cells':[{'x':x,'y':y} for x,y in sorted(cells)],'stars':stars,'checkpoints':cps}
 (folder/f'L{n:02}.json').write_text(json.dumps(data))
 print(n,len(cells),stars,cps)
