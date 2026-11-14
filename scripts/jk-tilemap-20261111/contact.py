from pathlib import Path
from PIL import Image,ImageDraw
import struct
root=Path(r'E:\SteamLibrary\steamapps\common\Jump King\Content')
def read(p):
 b=p.read_bytes();return Image.frombytes('RGBA',struct.unpack_from('<ii',b,69),b[85:])
a=Image.new('RGB',(1000,700),(40,40,40));d=ImageDraw.Draw(a)
for n in range(14):
 p=root/'screens'/'foreground'/f'fg{n+1}.xnb'
 if not p.exists(): continue
 im=read(p);im.thumbnail((240,180));a.paste(im,(n%4*250,n//4*175),im);d.text((n%4*250,n//4*175),str(n+1),fill='yellow')
a.save('scripts/jk-tilemap-20261111/foreground-contact.png')
im=read(root/'level.xnb');o=Image.new('RGB',im.size,(180,180,180));o.paste(im,mask=im.getchannel('A'));o.resize((1560,1170)).save('scripts/jk-tilemap-20261111/data-visible.png')

