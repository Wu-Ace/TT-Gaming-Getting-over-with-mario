from pathlib import Path
from PIL import Image,ImageDraw,ImageOps
import numpy as np,json
root=Path.cwd();out=root/'Assets/Tiles/RockVariations';out.mkdir(parents=True,exist_ok=True)
old=root/'Assets/Art/11.10';new=root/'Assets/Art/11.11/11.11_3'
def load(p):
 im=Image.open(p).convert('RGBA');canvas=Image.new('RGBA',(128,128));canvas.paste(im,(0,0));return canvas.resize((120,120),Image.Resampling.LANCZOS)
tops=[load(old/'Rock_top1.png'),load(old/'Rock_top2.png')]+[load(new/f'Rock_top{i}.png') for i in range(3,7)]
# Two authored bottoms: disjoint parity groups ensure no identical neighbours.
downs=[load(old/'Rock_down1.png') for _ in range(3)]+[load(old/'Rock_down2.png') for _ in range(3)]
lefts=[load(new/'Rock_side1.png') for _ in range(3)]+[load(new/f'Rock_side{2+i%2}.png') for i in range(3)]
sides=[lefts,[ImageOps.mirror(im) for im in lefts]]
index=[];unique={}
for mask in range(16):
 for t in range(6):
  for d in range(6):
   name=f'm{mask}-t{t if not mask&1 or not mask&4 else 0}-d{d if not mask&2 or not mask&8 else 0}'
   index.append(name)
   if name in unique:continue
   im=Image.new('RGBA',(120,120),(0,0,0,255));a=np.array(im)
   if not mask&1:a[:60]=np.array(tops[t])[:60]
   if not mask&2:a[60:]=np.array(downs[d])[60:]
   if not mask&4:
    side=np.array(sides[0][t]);a[:,:18,:3]=side[:,:18,:3];a[:,:,3]=np.minimum(a[:,:,3],side[:,:,3])
   if not mask&8:
    side=np.array(sides[1][d]);a[:,-18:,:3]=side[:,-18:,:3];a[:,:,3]=np.minimum(a[:,:,3],side[:,:,3])
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

import runpy
runpy.run_path(str(root/'scripts/ground-contact-20261111/align.py'))
