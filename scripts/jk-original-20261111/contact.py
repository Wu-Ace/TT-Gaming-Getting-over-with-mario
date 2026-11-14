from pathlib import Path
from PIL import Image,ImageDraw
import json
p=Path('scripts/jk-original-20261111');o=Image.new('RGB',(1440,1800),(35,40,45));dr=ImageDraw.Draw(o)
for i in range(14):
 o.paste(Image.open(p/f'floor-{i+1:02}.png'),(i%3*480,i//3*360));dr.text((i%3*480+8,i//3*360+8),str(i+1),fill='yellow')
o.save(p/'all-floors.png')
