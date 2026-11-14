from pathlib import Path
from PIL import Image,ImageDraw
import numpy as np,json
root=Path.cwd();out=root/'Assets/Tiles/RockVariations';out.mkdir(parents=True,exist_ok=True)
atlas=Image.open(root/'scripts/rock-all-levels-20261111/approved-atlas.png').convert('RGBA');w,h=atlas.size
tops=[];downs=[]
for row,target in [(0,tops),(1,downs)]:
 for i in range(6):
  # Slice equal native cells; remove the atlas guide and matte, not the artwork.
  raw=atlas.crop((i*w//6+2,row*h//2+2,(i+1)*w//6-2,(row+1)*h//2-2)).resize((120,120),Image.Resampling.LANCZOS)
  a=np.array(raw);solid=a[:,:,:3].max(axis=2)<135
  # Keep only the solid side of each contour; no detached matte fragments.
  edge=[]
  for x in range(120):
   ys=np.flatnonzero(solid[:,x]);edge.append(int(ys.min() if row==0 else ys.max()) if len(ys) else (119 if row==0 else 0))
  shoulder=round((edge[3]+edge[-4])/2);shift=(12 if row==0 else 108)-shoulder
  # Uniformly scaled source, then integer translation/padding. Never stretch Y.
  im=Image.new('RGBA',(120,120),(0,0,0,255));p=im.load()
  for x in range(120):
   boundary=max(0,min(119,edge[x]+shift))
   # Shared join height on the outer 3 columns prevents gaps at variant joins.
   distance=min(x,119-x)
   if distance<3:boundary=round(boundary*distance/3+(12 if row==0 else 108)*(1-distance/3))
   for y in range(120):
    inside=y>=boundary if row==0 else y<=boundary
    sy=y-shift
    rgb=tuple(int(v) for v in a[sy,x,:3]) if 0<=sy<120 else (0,0,0)
    if max(rgb)>110:rgb=(0,0,0)
    if max(rgb)<24:rgb=(0,0,0)
    p[x,y]=(*rgb,255) if inside else (0,0,0,0)
  target.append(im)
left=Image.open(root/'Assets/Tiles/RockTerrain/left.png').convert('RGBA').crop((1,1,121,121))
right=Image.open(root/'Assets/Tiles/RockTerrain/right.png').convert('RGBA').crop((1,1,121,121))
index=[];unique={}
for mask in range(16):
 for t in range(6):
  for d in range(6):
   name=f'm{mask}-t{t if not mask&1 else 0}-d{d if not mask&2 else 0}'
   index.append(name)
   if name in unique:continue
   im=Image.new('RGBA',(120,120),(0,0,0,255));a=np.array(im)
   if not mask&1:a[:60]=np.array(tops[t])[:60]
   if not mask&2:a[60:]=np.array(downs[d])[60:]
   if not mask&4:a[:,:,3]=np.minimum(a[:,:,3],np.array(left)[:,:,3])
   if not mask&8:a[:,:,3]=np.minimum(a[:,:,3],np.array(right)[:,:,3])
   a[a[:,:,3]==0,:3]=0
   padded=np.pad(a,((1,1),(1,1),(0,0)),mode='edge');Image.fromarray(padded).save(out/(name+'.png'));unique[name]=True
(root/'scripts/rock-all-levels-20261111/bank.json').write_text(json.dumps(index))
for t in range(6):
 for d in range(6):
  a=np.concatenate([np.array(tops[t])[:20],np.array(downs[d])[100:]],axis=0)
  Image.fromarray(np.pad(a,((1,1),(1,1),(0,0)),mode='edge')).save(out/f'ledge-{t}-{d}.png')
sheet=Image.new('RGB',(720,300),(160,160,160));draw=ImageDraw.Draw(sheet)
for row,group in enumerate([tops,downs]):
 for i,im in enumerate(group):sheet.paste(im,(i*120,row*150+25),im);draw.text((i*120+5,row*150+5),('TOP' if row==0 else 'DOWN')+str(i+1),fill='black')
sheet.save(root/'scripts/rock-all-levels-20261111/import-contact.png')
print('Prepared',len(unique),'unique topology sprites; bank entries',len(index))
