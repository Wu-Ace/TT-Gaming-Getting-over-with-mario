raise SystemExit("Deprecated: use fix_kit_proportions.py and Tools/Art/Repair Kit Terrain Topology. Never stretch sprites.")
from pathlib import Path
import re,zipfile,io,json,hashlib,shutil
from PIL import Image,ImageDraw
root=Path('.'); out=Path('Assets/Art/PlatformerKit'); out.mkdir(parents=True,exist_ok=True)
z=zipfile.ZipFile(r'D:/Download/2D Platformer Kit.zip')
backup=Path('art-backup/pre-kit'); backup.mkdir(parents=True,exist_ok=True)
files=[p for p in Path('Assets').rglob('*') if p.suffix in ('.prefab','.unity','.anim','.asset')]
metas={}
for p in Path('Assets/Graphics').glob('*.meta'):
 t=p.read_text(); g=re.search(r'guid: (\w+)',t)[1]
 for b in re.split(r'\n    - serializedVersion:',t)[1:]:
  id=re.search(r'internalID: (-?\d+)',b); n=re.search(r'\n      name: (.*)',b); r=re.search(r'rect:\s+serializedVersion: \d+\s+x: (\S+)\s+y: (\S+)\s+width: (\S+)\s+height: (\S+)',b)
  if id and n and r: metas[(g,id[1])]=(n[1],tuple(map(float,r.groups())))
refs={}
for p in files:
 for id,g in re.findall(r'\{fileID: (-?\d+), guid: (\w+), type: 3\}',p.read_text(errors='ignore')):
  if (g,id) in metas: refs[(g,id)]=None
G=lambda n:f'Tiles/Grassland/grass tiles ({n}).png'
choices={'GroundTile':G(24),'BrickBlock':'Interactive/box_plain.png','StairsBlock':G(50),'QuestionMarkBlock':'Interactive/box_question.png','TubeShort':G(60),'TubeMedium':G(60),'TubeLarge':G(60),'BushSingle':G(10),'BushDouble':G(35),'BushTriple':G(56),'Mountain1':G(4),'Mountain2':G(13),'Castle':'Tiles/House/house tiles (1).png','CloudSingle':G(48),'CloudDouble':G(7),'CloudTriple':G(48),'Flag':'Interactive/flag_green_raised.png','FlagPole':G(18),'FlagPoleTop':G(39)}
for p in Path('Assets/Prefabs').rglob('*.prefab'):
 if p.stem in choices:
  for id,g in re.findall(r'\{fileID: (-?\d+), guid: (\w+), type: 3\}',p.read_text()):
   if (g,id) in refs: refs[(g,id)]=choices[p.stem]
for key in refs:
 name,rect=metas[key]; i=int(name.rsplit('_',1)[-1]) if name.rsplit('_',1)[-1].isdigit() else 0
 if refs[key]: continue
 if 'enemies_sheet' in name:
  refs[key]=f'Enemies/imp_walk_{i%2}.png' if i%15 in (0,1) else ('Enemies/imp_defeated.png' if i%15==2 else (f'Enemies/crab_walk_{i%4}.png' if i%15<11 else 'Enemies/brick_idle.png'))
 elif 'items_sheet' in name:
  refs[key]=('Interactive/box_question.png' if i<3 else f'Interactive/coin_gold_{i%8}.png' if i in (23,24,25,26,27,28,29) else f'Interactive/star_{i%10}.png' if i in (15,16,17,18) else 'Interactive/heart_0.png')
 elif 'misc' in name: refs[key]='Interactive/box_used.png'
 else: refs[key]=G(24)
# Keep player and its bespoke cauldron/hammer rig for a separate art pass.
refs={k:v for k,v in refs.items() if 'mario_sheet' not in metas[k][0]}
records=[]; mapping={}
for (g,id),source in refs.items():
 if source not in z.namelist(): source=G(36)
 n,r=metas[(g,id)]; w,h=map(lambda x:int(x*4),r[2:]); im=Image.open(io.BytesIO(z.read(source))).convert('RGBA').resize((w,h),Image.Resampling.LANCZOS)
 p=out/(g[:6]+'_'+id+'.png'); im.save(p); guid=hashlib.md5(str(p).encode()).hexdigest()
 p.with_suffix('.png.meta').write_text(f'''fileFormatVersion: 2
guid: {guid}
TextureImporter:
  serializedVersion: 12
  mipmaps:
    enableMipMap: 0
  textureSettings:
    filterMode: 1
    wrapU: 1
    wrapV: 1
  textureType: 8
  spriteMode: 1
  spritePixelsToUnits: 400
  spriteMeshType: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  alphaIsTransparency: 1
  textureCompression: 0
''')
 mapping[f'{{fileID: {id}, guid: {g}, type: 3}}']=f'{{fileID: 21300000, guid: {guid}, type: 3}}'
 records.append({'asset':str(p),'source':source,'original':n})
changed=[]
for p in files:
 t=p.read_text(errors='ignore'); new=t
 for a,b in mapping.items(): new=new.replace(a,b)
 if new!=t:
  dest=backup/p;dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,dest);p.write_text(new);changed.append(str(p))
Path('scripts/kit-replacement-report.json').write_text(json.dumps({'sprites':records,'changed':changed},indent=2))
# inspect all generated sprites at useful scale
sheet=Image.new('RGB',(800,((len(records)+9)//10)*90),'#73879a');d=ImageDraw.Draw(sheet)
for i,r in enumerate(records):
 im=Image.open(r['asset']);im.thumbnail((70,65));x=i%10*80;y=i//10*90;sheet.paste(im,(x,y),im);d.text((x,y+65),r['original'].split('_')[-1],fill='white')
sheet.save('scripts/kit-replacement-preview.png')
print(len(records),'sprites;',len(changed),'files updated')

