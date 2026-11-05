from pathlib import Path
from PIL import Image,ImageDraw
import shutil
s=Path('design/ui-proposal/render_v4.py').read_text(encoding='utf-8-sig').split('im=scene();text')[0].replace('(720,360,840,720)','(720,360,840,600)')
exec(s)
o=Path('Assets/Resources/MenuUI')
scene().save(o/'landscape.png')
for i,(theme,bg) in enumerate([('Grassland','hills_day.png'),('Cave','cave_interior.png'),('Grassland_Winter','mountains_day.png')]):
 a=scene(theme,bg).resize((640,360)).crop((140,0,500,360));m=Image.new('L',a.size);ImageDraw.Draw(m).ellipse((2,2,358,358),fill=255);a.putalpha(m);ImageDraw.Draw(a).ellipse((2,2,358,358),outline='#fff5db',width=5);a.save(o/f'world{i}.png')
for i,pts in enumerate([[(35,0),(270,40),(250,540),(65,570)],[(75,40),(260,0),(280,570),(35,535)],[(40,0),(270,35),(245,565),(70,530)]]):
 m=Image.new('L',(310,580));ImageDraw.Draw(m).polygon(pts,fill=255);m=m.filter(ImageFilter.GaussianBlur(8)).point(lambda v:255 if v>128 else 0).filter(ImageFilter.GaussianBlur(1));a=Image.new('RGBA',m.size,'#233f38');a.putalpha(m);a.save(o/f'plate{i}.png')
for name,color in [('button','Green'),('buttonGrey','Grey')]:shutil.copyfile(U/color/'Default/button_rectangle_depth_flat.png',o/(name+'.png'))
shutil.copyfile(A/'Interactive/star_0.png',o/'star.png')
for name,source in [('click','click_001.ogg'),('back','back_001.ogg')]:
 p=next(Path('Assets/Sound/Kenney Interface Sounds').rglob(source));shutil.copyfile(p,o/(name+'.ogg'))
