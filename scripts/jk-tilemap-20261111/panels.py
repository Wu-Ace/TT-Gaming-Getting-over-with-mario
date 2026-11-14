from pathlib import Path
from PIL import Image,ImageDraw
exec(Path('scripts/jk-tilemap-20261111/contact.py').read_text().split("a=Image.new")[0])
a=Image.new('RGB',(1200,720),(50,60,70));d=ImageDraw.Draw(a)
for n in range(14):
 x=n%5*240;y=n//5*240
 for folder,prefix in [('background','bg'),('midground','mg'),('foreground','fg')]:
  p=root/'screens'/folder/f'{prefix}{n+1}.xnb'
  if p.exists():
   im=read(p);im=im.resize((240,180));a.paste(im,(x,y),im)
 mask=read(root/'level.xnb').crop((n//13*60,n%13*45,n//13*60+60,n%13*45+45)).resize((240,180),Image.Resampling.NEAREST)
 # collision footprint below art as small strip
 a.paste(mask.resize((80,60)),(x+160,y+180),mask.resize((80,60)));d.text((x,y),str(n+1),fill='yellow')
a.save('scripts/jk-tilemap-20261111/source-panels.png')
