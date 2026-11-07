from pathlib import Path
import json,math
import numpy as np
from PIL import Image,ImageDraw
from scipy.ndimage import label,binary_dilation
src=np.array(Image.open('scripts/level04-layout-reference.png').convert('L'))
labels,n=label(src>150)
areas=np.bincount(labels.ravel()); rooms=np.isin(labels,[i for i in range(2,n+1) if areas[i]>4000])
# Trace the actual enclosed spaces, including all three side bays.
S=20; H=math.ceil(src.shape[0]/S); W=math.ceil(src.shape[1]/S)
inside=set()
for y in range(H):
 for x in range(W):
  patch=rooms[y*S:(y+1)*S,x*S:(x+1)*S]
  if patch.mean()>.35: inside.add((x,H-1-y))
cells=set()
for x,y in inside:
 for dx,dy in [(1,0),(-1,0),(0,1),(0,-1)]:
  if (x+dx,y+dy) not in inside:cells.add((x+dx,y+dy))
# Adapt the reference's disconnected chambers into one open hammer route.
pixels=[(240,1410),(350,1370),(360,1300),(430,1240),(550,1190),(600,1120),(560,1040),(570,970),(450,930),(390,860),(440,780),(550,730),(650,680),(700,620),(610,570),(470,540),(420,480),(370,430),(350,360),(420,310),(450,250),(510,210),(610,170),(720,130),(780,80)]
knots=[(round(x/S),H-1-round(y/S)) for x,y in pixels]
route=[]
for a,b in zip(knots,knots[1:]):
 steps=max(1,math.ceil(max(abs(b[0]-a[0]),abs(b[1]-a[1]))/2))
 for k in range(steps):
  t=k/steps;p=(round(a[0]+(b[0]-a[0])*t),round(a[1]+(b[1]-a[1])*t))
  if not route or p!=route[-1]:route.append(p)
route.append(knots[-1])
# Keep the traced exterior intact; open only the two chamber partitions.
for px,py in [(430,730),(430,510),(722,1155),(332,610),(575,560)]:
 x=round(px/S);y=H-1-round(py/S)
 for xx in range(x-1,x+3):
  for yy in range(y-1,y+4):cells.discard((xx,yy))
# Add wall grips and occasional stepping ledges, retaining swing clearance above each.
platforms=set()
for x,y in route:
 for xx in range(x-1,x+2):
  if (xx,y-1) in inside:platforms.add((xx,y-1))
for x,y in route:
 for xx in range(x,x+2):
  for yy in range(y,y+3):platforms.discard((xx,yy))
cells.update(platforms)
# Anchor each objective and checkpoint to an actual supporting cell.
for idx in [0,len(route)-1]+[min(range(len(route)),key=lambda i:abs(route[i][1]-v)) for v in [22,32,43,53,62]]:
 x,y=route[idx]
 for xx in range(x-1,x+2):cells.add((xx,y-1))
 for xx in range(x-1,x+2):
  for yy in range(y,y+3):cells.discard((xx,yy))
def world(p,h=0):return dict(x=(p[0]+.5)*.8,y=p[1]*.8-3.6+h)
marks=[min(range(len(route)),key=lambda i:abs(route[i][1]-y)) for y in [22,43,62]]
checks=[min(range(len(route)),key=lambda i:abs(route[i][1]-y)) for y in [32,53]]
p=dict(number=4,title='Town winding chambers',reference='Traced user reference silhouette with open chamber transitions and hammer ledges',cells=[dict(x=x,y=y) for x,y in sorted(cells)],stars=[world(route[i],1.05) for i in marks],checkpoints=[world(route[i],.4) for i in checks],spawn=world(route[0],1.3),exit=world(route[-1],.8),route=[world(v) for v in route],donuts=[],authoredLength=sum(math.dist(a,b)*.8 for a,b in zip(route,route[1:])))
Path('scripts/level04-designed.json').write_text(json.dumps(p,indent=2)+'\n')
im=Image.new('RGB',(W*16,H*16),'#eff2f6');d=ImageDraw.Draw(im)
for x,y in cells:d.rectangle((x*16,(H-1-y)*16,x*16+15,(H-y)*16-1),fill='#5c6672')
for i in marks:
 x,y=route[i];d.ellipse((x*16,(H-y-2)*16,x*16+12,(H-y-2)*16+12),fill='#24bdea')
im.save('scripts/level04-retrace-preview.png')
print('Traced',len(cells),'cells;',len(route),'route ledges. Physical traversal NOT verified.')
