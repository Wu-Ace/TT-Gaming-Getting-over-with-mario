from pathlib import Path
from PIL import Image
import struct,collections
p=Path(r'E:\SteamLibrary\steamapps\common\Jump King\Content\level.xnb');b=p.read_bytes();o= b.index(b'Texture2DReader')+len(b'Texture2DReader')+7
print(o,b[o:o+20])
# fixed reader header ends at byte 69
for i in range(60,85):
 if struct.unpack_from('<i',b,i)[0]==780: print('width offset',i)
w,h=struct.unpack_from('<ii',b,69);print(w,h)
im=Image.frombytes('RGBA',(w,h),b[85:85+w*h*4]);im.save('scripts/jk-tilemap-20261111/level-data.png'); print(collections.Counter(im.getdata()).most_common(15))

