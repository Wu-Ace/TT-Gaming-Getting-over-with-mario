raise SystemExit("Deprecated: use fix_kit_proportions.py and Tools/Art/Repair Kit Terrain Topology. Never stretch sprites.")
import json,zipfile,io,pathlib,hashlib
from PIL import Image
p=pathlib.Path('scripts/kit-replacement-report.json');r=json.loads(p.read_text());z=zipfile.ZipFile(r'D:/Download/2D Platformer Kit.zip')
for a in r['sprites']:
 n=a['original'];i=int(n.rsplit('_',1)[-1]);src=a['source']
 if 'misc' in n:
  if i in (74,75,76,85,86,87):src=f'Interactive/coin_gold_{[75,76,74,85,86,87].index(i)}.png'
  if i in (80,81,82):src='Interactive/box_question.png'
  if i in (5,386):src='Interactive/heart_0.png'
  if i in (48,49,50):src=f'Interactive/star_{i-48}.png'
  if i==13:src='Interactive/box_plain.png'
 if src!=a['source']:
  size=Image.open(a['asset']).size;Image.open(io.BytesIO(z.read(src))).convert('RGBA').resize(size,Image.Resampling.LANCZOS).save(a['asset']);a['source']=src
# Native background, camera-following at the back of the scene.
bg=pathlib.Path('Assets/Resources/KitBackground.png');bg.parent.mkdir(exist_ok=True);bg.write_bytes(z.read('Backgrounds/hills_day.png'))
meta=pathlib.Path(r['sprites'][0]['asset']+'.meta').read_text();import re
meta=re.sub(r'guid: \w+','guid: '+hashlib.md5(str(bg).encode()).hexdigest(),meta);meta=meta.replace('spritePixelsToUnits: 400','spritePixelsToUnits: 100');pathlib.Path(str(bg)+'.meta').write_text(meta)
p.write_text(json.dumps(r,indent=2))
