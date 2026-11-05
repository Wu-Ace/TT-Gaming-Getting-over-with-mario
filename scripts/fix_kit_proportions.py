from pathlib import Path
from PIL import Image,ImageOps
import json,re,shutil
root=Path('Assets/Art/2D Platformer Kit'); report=json.loads(Path('scripts/kit-replacement-report.json').read_text()); backup=Path('art-backup/pre-no-stretch');backup.mkdir(exist_ok=True)
for a in report['sprites']:
 p=Path(a['asset'])
 if not (backup/p.name).exists(): shutil.copy2(p,backup/p.name)
 if not (backup/(p.name+'.meta')).exists(): shutil.copy2(str(p)+'.meta',backup/(p.name+'.meta'))
 old=Image.open(backup/p.name);w,h=round(old.width*750/400),round(old.height*750/400);canvas=Image.new('RGBA',(w,h))
 if a['source'].startswith('Hidden:'):canvas.save(p);continue
 src=Image.open(root/a['source']).convert('RGBA')
 if a['source']=='Enemies/imp_defeated.png':
  w=h=120;canvas=Image.new('RGBA',(w,h))
 if a['original'] in ['smb1_scenery_sprites_22','smb1_scenery_sprites_23','smb1_scenery_sprites_24']:
  # Terrain pillar: native square cells, caps/sides/base; no whole-image stretch.
  for y in range(0,h,120):
   for x in range(0,w,120):
    left=x==0;right=x+120>=w;top=y==0;bottom=False # Pillars join the ground: no rounded bottom cap.
    n=(27 if left else 58 if right else 24) if top else (26 if left else 11 if right else 43) if bottom else (44 if left else 17 if right else 31)
    tile=Image.open(root/f'Tiles/Grassland/grass tiles ({n}).png').convert('RGBA');canvas.paste(tile,(x,y))
  a['composition']='native 120px terrain cells: cap + repeated sides/body, open base joins ground'
 else:
  if a['original'] in ['smb1_misc_sprites_88','smb1_misc_sprites_79']:
   src=Image.open(root/'Tiles/Grassland/_tileset_120_120.png').convert('RGBA').crop((240,0,600,120))
   a['composition']='mirrored native hill halves, uniform fit'
  fitted=ImageOps.contain(src,(w,h),Image.Resampling.LANCZOS);canvas.paste(fitted,((w-fitted.width)//2,h-fitted.height))
  a['composition']='uniform fit, bottom aligned, transparent padding'
 canvas.save(p);meta=Path(str(p)+'.meta');meta.write_text(re.sub(r'spritePixelsToUnits: \S+','spritePixelsToUnits: 750',meta.read_text()))
# Native topology tiles copied without changing their pixels; unique stable local import settings.
import hashlib
out=Path('Assets/Art/PlatformerKit/Terrain');out.mkdir(exist_ok=True)
for n in [24,27,58,44,31,17,26,43,11]:
 p=out/f'grass-{n}.png';shutil.copyfile(root/f'Tiles/Grassland/grass tiles ({n}).png',p)
 meta=Path(report['sprites'][0]['asset']+'.meta').read_text();meta=re.sub(r'guid: [^\n]+','guid: '+hashlib.md5(str(p).encode()).hexdigest(),meta);meta=re.sub(r'spritePixelsToUnits: \S+','spritePixelsToUnits: 750',meta);Path(str(p)+'.meta').write_text(meta)
Path('scripts/kit-no-stretch-report.json').write_text(json.dumps(report,indent=2))
