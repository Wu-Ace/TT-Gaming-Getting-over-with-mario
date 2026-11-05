"""Scoped visual dependency fork. Original prefabs, textures and collider records stay intact."""
from pathlib import Path
from PIL import Image,ImageOps
import re,hashlib,json
exec(Path('scripts/prepare_ancient_forest.py').read_text())
guid_file=Path('scripts/ancient-forest-evidence/guids.tsv')
if not guid_file.exists():raise SystemExit('Editor GUID export required; no scene changes made')
editor_guids=dict(line.split('\t',1) for line in guid_file.read_text().splitlines())
def guid(p):
 key=p.as_posix()
 return editor_guids.get(key) or re.search(r'^guid: (.+)$',Path(str(p)+'.meta').read_text(),re.M)[1]
def emit(src,im,name):
 p=out/(name+'.png');im.save(p)
 m=Path(str(src)+'.meta').read_text();g=hashlib.md5(str(p).encode()).hexdigest()
 m=re.sub(r'^guid: .*','guid: '+g,m,flags=re.M)
 m=re.sub(r'textureCompression: \d+','textureCompression: 0',m)
 m=re.sub(r'filterMode: \d+','filterMode: 0',m)
 Path(str(p)+'.meta').write_text(m);return g
roles={27:'tl',24:'top',58:'tr',44:'left',31:'fill',17:'right',26:'bl',43:'bottom',11:'br',15:'fill'}
tiles={k:Image.open(out/(v+'.png')).resize((120,120),Image.Resampling.NEAREST) for k,v in roles.items()}
# Ground.png is the author's detailed terrain strip. Keep its pixels at one
# uniform 2x scale rather than magnifying a 16px atlas cell to 120px.
ground_source=Image.open(root/'Ground.png').convert('RGBA')
surface=ground_source.crop((0,300,60,360)).resize((120,120),Image.Resampling.NEAREST)
interior=Image.new('RGBA',(120,120))
patch=ground_source.crop((0,344,60,352)).resize((120,16),Image.Resampling.NEAREST)
body=Image.new('RGBA',(120,32));body.paste(patch,(0,0));body.paste(ImageOps.flip(patch),(0,16))
for y in range(0,120,32):interior.paste(body,(0,y))
for n in tiles:
 tiles[n]=surface.copy() if n in (27,24,58) else interior.copy()
 if n in (44,26):
  edge=atlas.crop((80,144,88,160)).resize((16,32),Image.Resampling.NEAREST)
  for y in range(0,120,32):tiles[n].paste(edge,(0,y))
 if n in (17,11):
  edge=atlas.crop((184,144,192,160)).resize((16,32),Image.Resampling.NEAREST)
  for y in range(0,120,32):tiles[n].paste(edge,(104,y))
mapping={};records=[]
for role in ['tl','top','tr','left','fill','right','bl','bottom','br']:
 src=Path('Assets/Art/Campaign/Grassland')/(role+'.png')
 if src.exists():mapping[guid(src)]=emit(src,tiles[next(n for n,r in roles.items() if r==role)],'campaign-'+role)
def pad(im):return ImageOps.expand(im,1) if False else im.resize((im.width+2,im.height+2))
def gutter(im):
 a=Image.new('RGBA',(im.width+2,im.height+2));a.paste(im,(1,1))
 a.paste(im.crop((0,0,im.width,1)),(1,0));a.paste(im.crop((0,im.height-1,im.width,im.height)),(1,im.height+1))
 a.paste(a.crop((1,0,2,a.height)),(0,0));a.paste(a.crop((im.width,0,im.width+1,a.height)),(im.width+1,0));return a
terrain=Path('Assets/Art/PlatformerKit/Terrain')
soil=Image.open(terrain/'grass-31.png').convert('RGBA')
for src in terrain.rglob('*.png'):
 if 'grass-' in src.name:
  n=int(re.search(r'grass-(\d+)',src.name)[1])
  if n not in roles:continue
  im=tiles[n].copy()
 elif 'contact-' in src.name:
  old=Image.open(src).convert('RGBA');old=old.crop((1,1,121,121)) if old.width==122 else old
  # Recover exact pre-authored per-column contact mask, including partial-cell footprints.
  options=[]
  for n in [27,24,58]:
   top=Image.open(terrain/('grass-'+str(n)+'.png')).convert('RGBA')
   score=0;mask=[]
   for x in range(120):
    is_soil=all(old.getpixel((x,y))==soil.getpixel((x,y)) for y in range(120))
    mask.append(is_soil)
    if not is_soil:score+=sum(old.getpixel((x,y))!=top.getpixel((x,y)) for y in range(120))
   options.append((score,n,mask))
  _,n,mask=min(options,key=lambda z:z[0]);im=tiles[n].copy()
  for x,covered in enumerate(mask):
   if covered:im.paste(tiles[31].crop((x,0,x+1,120)),(x,0))
 else:continue
 if 'SeamSafe' in src.parts:im=gutter(im)
 mapping[guid(src)]=emit(src,im,'terrain-'+src.parent.name+'-'+src.stem)
for item in json.loads(Path('scripts/kit-replacement-report.json').read_text())['sprites']:
 src=Path(item['asset'].replace('\\','/'));orig=item['original'];old=Image.open(src).convert('RGBA');im=None
 if orig in ['smb1_scenery_sprites_22','smb1_scenery_sprites_23','smb1_scenery_sprites_24']:
  im=Image.new('RGBA',old.size)
  for y in range(0,old.height,120):
   for x in range(0,old.width,120):
    n=(27 if x==0 else 58 if x+120>=old.width else 24) if y==0 else (44 if x==0 else 17 if x+120>=old.width else 31)
    im.paste(tiles[n],(x,y))
 elif orig in ['smb1_misc_sprites_88','smb1_misc_sprites_79','smb1_scenery_sprites squares_393','smb1_scenery_sprites squares_387']:
  prop=Image.open(out/('rocks.png' if 'misc' in orig else 'fern.png'));f=ImageOps.contain(prop,old.size,Image.Resampling.NEAREST);im=Image.new('RGBA',old.size);im.paste(f,((old.width-f.width)//2,old.height-f.height))
 if im is not None:mapping[guid(src)]=emit(src,im,'adapted-'+src.stem)
# Recursively fork only prefab dependencies with changed visuals. FileIDs and all non-reference bytes retained.
prefabs={guid(p):p for p in Path('Assets/Prefabs').rglob('*.prefab')}
forks=out/'Prefabs';forks.mkdir(exist_ok=True)
visited={}
def fork(g):
 if g in mapping:return mapping[g]
 if g in visited:return visited[g]
 if g not in prefabs:return g
 visited[g]=g;p=prefabs[g];s=p.read_text();new=re.sub(r'guid: ([a-f0-9]{32})',lambda m:'guid: '+fork(m[1]),s)
 if new!=s:
  target=forks/(p.stem+'-'+g[:8]+'.prefab');target.write_text(new)
  ng=hashlib.md5(str(target).encode()).hexdigest();Path(str(target)+'.meta').write_text(re.sub(r'^guid: .*','guid: '+ng,Path(str(p)+'.meta').read_text(),flags=re.M));visited[g]=ng;records.append(str(target))
 return visited[g]
s=scene.read_text();new=re.sub(r'guid: ([a-f0-9]{32})',lambda m:'guid: '+fork(m[1]),s)
new=re.sub(r'(  background: \{fileID: 21300000, guid: )[^,]+',lambda m:m[1]+guid(out/'background.png'),new)
scene.write_text(new)
e=Path('scripts/ancient-forest-evidence');e.mkdir(exist_ok=True)
files=[scene,*out.rglob('*')];manifest={str(p):hashlib.sha256(p.read_bytes()).hexdigest() for p in files if p.is_file()}
(e/'manifest.json').write_text(json.dumps(manifest,indent=2))
(e/'applied.json').write_text(json.dumps({'forkedPrefabs':records,'textureCount':len(mapping),'validation':'Offline references only; Unity Scene/Game pending'},indent=2))
print('Applied scoped visual fork:',len(records),'prefabs,',len(mapping),'texture variants')
