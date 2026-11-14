from PIL import Image
from pathlib import Path
src=Path('Assets/Art/11.10'); dst=Path('Assets/Tiles/RockTerrain');dst.mkdir(parents=True,exist_ok=True)
def load(name):
 im=Image.open(src/(name+'.png')).convert('RGBA'); c=Image.new('RGBA',(128,128));c.paste(im,(0,0));return c
base=Image.new('RGBA',(128,128),(0,0,0,255)); top=load('Rock_top1');bottom=load('Rock_down1')
images={'tl':load('Rock_1'),'tr':load('Rock_2'),'bl':load('Rock_3'),'br':load('Rock_4'),'top':top,'bottom':bottom,'fill':base}
for role,source in [('left',images['bl']),('right',images['br'])]:
 im=base.copy()
 for y in range(128):
  row=source.crop((0,20+y%60,128,21+y%60));im.paste(row,(0,y))
 images[role]=im
# Compose exposed edges independently; thin platforms need both top and bottom.
for role,t,l,r,b in [('single',1,1,1,1),('thin',1,0,0,1),('thinleft',1,1,0,1),('thinright',1,0,1,1),('column',0,1,1,0),('coltop',1,1,1,0),('colbottom',0,1,1,1)]:
 im=base.copy()
 if t:im.paste(top.crop((0,0,128,35)),(0,0))
 if b:im.paste(bottom.crop((0,100,128,128)),(0,100))
 if l:im.paste(images['left'].crop((0,0,18,128)),(0,0))
 if r:im.paste(images['right'].crop((110,0,128,128)),(110,0))
 images[role]=im
# Second authored top keeps the same canvas, scale and lower edge composition.
top2=load('Rock_top2');images['top2']=top2
for role in ['single','thin','thinleft','thinright','coltop']:
 im=images[role].copy();im.paste(top2.crop((0,0,128,35)),(0,0))
 if role in ['single','thinleft','coltop']:im.paste(images['left'].crop((0,0,18,35)),(0,0))
 if role in ['single','thinright','coltop']:im.paste(images['right'].crop((110,0,128,35)),(110,0))
 images[role+'2']=im
ledge=Image.new('RGBA',(128,43),(0,0,0,255));ledge.paste(top.crop((0,0,128,24)),(0,0));ledge.paste(bottom.crop((0,109,128,128)),(0,24));ledge.save(dst/'ledge.png')
for role,im in images.items():
 im=im.resize((120,120),Image.Resampling.LANCZOS);out=Image.new('RGBA',(122,122));out.paste(im,(1,1));out.paste(im.crop((0,0,1,120)),(0,1));out.paste(im.crop((119,0,120,120)),(121,1));out.paste(out.crop((0,1,122,2)),(0,0));out.paste(out.crop((0,120,122,121)),(0,121));out.save(dst/(role+'.png'))
sheet=Image.new('RGB',(4*160,((len(images)+3)//4)*155),(170,170,170))
from PIL import ImageDraw
d=ImageDraw.Draw(sheet)
for i,(role,im) in enumerate(images.items()):
 x=i%4*160;y=i//4*155;sheet.paste(im,(x+15,y+22),im);d.text((x+5,y+5),role,fill='black')
sheet.save('scripts/rock-terrain-20261111/contact.png')
