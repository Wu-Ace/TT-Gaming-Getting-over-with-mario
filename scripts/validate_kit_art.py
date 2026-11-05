from pathlib import Path
import json,re,hashlib
from PIL import Image,ImageOps
r=json.loads(Path('scripts/kit-no-stretch-report.json').read_text());checks=[];root=Path('Assets/Art/2D Platformer Kit')
for a in r['sprites']:
 im=Image.open(a['asset']).convert('RGBA');n=a['original']
 if a['source'].startswith('Hidden:'):continue
 if n.startswith('smb1_scenery_sprites_') and n.rsplit('_',1)[1] in ['22','23','24']:
  for y in range(0,im.height,120):
   for x in range(0,im.width,120):
    left=x==0;right=x+120>=im.width;tile=(27 if left else 58 if right else 24) if y==0 else (44 if left else 17 if right else 31)
    src=Image.open(root/f'Tiles/Grassland/grass tiles ({tile}).png').convert('RGBA');assert src.tobytes()==im.crop((x,y,x+120,y+120)).tobytes(),n
  checks.append(n+': native cells matched exactly; no internal bottom cap')
 else:
  src=Image.open(root/a['source']).convert('RGBA')
  if n in ['smb1_misc_sprites_88','smb1_misc_sprites_79']:src=Image.open(root/'Tiles/Grassland/_tileset_120_120.png').convert('RGBA').crop((240,0,600,120))
  fit=ImageOps.contain(src,im.size,Image.Resampling.LANCZOS);expected=Image.new('RGBA',im.size);expected.paste(fit,((im.width-fit.width)//2,im.height-fit.height));assert expected.tobytes()==im.tobytes(),n
  checks.append(n+': uniform-fit pixels matched')
result=Path('scripts/kit-terrain-repair-result.txt').read_text();assert result.count('SUMMARY total=776 grass=390 soil=386 edgeCells=71')==2;assert result.endswith('Scene saved\n')
checks+=['Level prefab and scene: 776 cells each, 390 surface cells (including partial contact masks) / 386 fully covered soil; saved','Native topology mapping verified against 120px source atlas coordinates']
Path('scripts/art-evidence/checks.txt').write_text('\n'.join(checks));print('PASS',len(checks),'art/topology checks')
