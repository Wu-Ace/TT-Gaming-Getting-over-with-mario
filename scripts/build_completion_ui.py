from PIL import Image,ImageDraw
from pathlib import Path
root=Path('Assets/Art/Kenney UI Pack/PNG'); im=Image.new('RGBA',(656,808));d=ImageDraw.Draw(im)
d.rounded_rectangle((12,132,644,760),9,fill='#147f9c');d.rounded_rectangle((12,124,644,748),9,fill='#199fcb',outline='#53c4e1',width=5)
d.rounded_rectangle((48,340,608,564),5,fill='#ffca05',outline='#ffe994',width=5);d.rectangle((61,354,595,550),fill='#d9dbe5',outline='white',width=6)
def paste(n,b,c='Yellow'):
 ps=list((root/c).rglob(n+'.png'));p=next((p for p in ps if 'Default' in p.parts),ps[0]);a=Image.open(p).convert('RGBA');s=min(b[2]/a.width,b[3]/a.height);a=a.resize((round(a.width*s),round(a.height*s)),Image.Resampling.LANCZOS);im.alpha_composite(a,(b[0]+(b[2]-a.width)//2,b[1]+(b[3]-a.height)//2))
paste('star',(64,45,180,180));paste('star',(204,0,236,236));paste('star_outline_depth',(420,50,180,180));paste('button_round_depth_border',(152,628,164,168));paste('button_round_depth_flat',(332,628,164,168));paste('icon_repeat_dark',(196,671,76,76),'Extra');paste('icon_play_dark',(380,675,68,68),'Extra');im.save('Assets/Resources/CompletionUI/panel.png')
