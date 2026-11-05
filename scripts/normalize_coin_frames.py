from pathlib import Path
from PIL import Image
import hashlib
root=Path('Assets/Art/2D Platformer Kit/Interactive');out=Path('Assets/Art/PlatformerKit/CoinFrames');out.mkdir(exist_ok=True)
for i in range(8):
 p=out/f'gold-{i}.png';im=Image.open(root/f'coin_gold_{i}.png').convert('RGBA').crop((28,28,92,92));im.resize((120,120),Image.Resampling.LANCZOS).save(p)
 Path(str(p)+'.meta').write_text(f'''fileFormatVersion: 2
guid: {hashlib.md5(str(p).encode()).hexdigest()}
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
  spritePixelsToUnits: 750
  spriteMeshType: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  alphaIsTransparency: 1
''')
