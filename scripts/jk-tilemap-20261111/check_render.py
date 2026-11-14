import json
from PIL import Image
from pathlib import Path
p=Path('scripts/jk-tilemap-20261111');d=json.loads((p/'plan.json').read_text());bad=[]
for c in d['cells']:
 if not c['kind']:continue
 k=c['kind'];im=Image.open(p/f"floor-{c['y']//45+1:02}.png")
 px=round((c['x']+(1/3 if k in [1,3] else 2/3))*8);py=round(360-(c['y']%45+(1/3 if k in [1,2] else 2/3))*8)
 rgb=im.getpixel((px,py))
 if max(rgb)>30:bad.append((c,rgb))
print('render interior mismatches',len(bad));print(bad[:10])
