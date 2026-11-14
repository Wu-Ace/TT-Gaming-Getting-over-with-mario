from pathlib import Path
from PIL import Image
import numpy as np
for p in Path('Assets/Tiles/RockVariations').glob('m*.png'):
 if int(p.stem.split('-')[0][1:])&1:continue
 a=np.array(Image.open(p).convert('RGBA'))
 if a.shape[0]!=122:continue
 heights=[int(np.flatnonzero(a[1:61,x,3]>127)[0]) for x in range(19,103) if np.any(a[1:61,x,3]>127)]
 shift=max(heights);b=np.zeros((154,122,4),dtype=np.uint8);b[32:]=a;b[32-shift:82-shift]=a[:50]
 for x in range(1,121):
  if a[50,x,3]>127:b[82-shift:83,x]=(0,0,0,255)
 b[:,0]=b[:,1];b[:,-1]=b[:,-2];Image.fromarray(b).save(p)
