from pathlib import Path
from PIL import Image
import re,hashlib,shutil,json
root=Path('Assets/Art/Ancient Forest Tileset+parallax')
out=Path('Assets/Art/AncientForestLevel01');out.mkdir(exist_ok=True)
backup=Path('art-backup/pre-ancient-forest');backup.mkdir(exist_ok=True)
scene=Path('Assets/Scenes/OneOneScene.unity')
if not (backup/scene.name).exists():shutil.copy2(scene,backup/scene.name)
meta=Path('Assets/Art/PlatformerKit/Terrain/grass-24.png.meta').read_text()
def save(name,im,ppu=100):
 p=out/(name+'.png');im.save(p)
 m=re.sub(r'guid: .*','guid: '+hashlib.md5(str(p).encode()).hexdigest(),meta)
 m=re.sub(r'spritePixelsToUnits: .*','spritePixelsToUnits: '+str(ppu),m)
 m=re.sub(r'filterMode: .*','filterMode: 0',m)
 Path(str(p)+'.meta').write_text(m)
atlas=Image.open(root/'Tileset Ancient Forest.png').convert('RGBA')
# Inspected native atlas: isolated 112px square island, 16px cells.
for name,x,y in [('tl',80,96),('top',112,96),('tr',176,96),('left',80,144),('fill',112,144),('right',176,144),('bl',80,192),('bottom',112,192),('br',176,192)]:
 save(name,atlas.crop((x,y,x+16,y+16)),100)
props=Image.open(root/'Forest Prop.png').convert('RGBA')
save('rocks',props.crop((62,64,250,128)),100)
save('ruins',props.crop((346,400,530,534)),100)
save('fern',props.crop((562,318,600,354)),100)
save('lamp',props.crop((798,70,843,237)),100)
save('statue',props.crop((744,242,800,360)),100)
bush=Image.open(root/'FrontBushes.png').convert('RGBA')
save('bush',bush.crop((0,222,160,360)),100)
save('undergrowth-layer',bush,100)
save('foreground-layer',Image.open(root/'ForeGround.png').convert('RGBA'),100)
bg=Image.open(root/'BackGround.png').convert('RGBA')
for layer in ['FarTrees.png','MiddleTrees.png','NearTrees.png']:
 layer_image=Image.open(root/layer).convert('RGBA')
 assert layer_image.size==bg.size
 bg=Image.alpha_composite(bg,layer_image)
save('background',bg,100)
print('Prepared scoped Ancient Forest source crops and scene backup')
