from PIL import Image,ImageDraw,ImageFont
from pathlib import Path
import json,hashlib
root=Path.cwd(); out=root/'design/ui-proposal'
f='C:/Windows/Fonts/msyh.ttc'
im=Image.new('RGB',(1680,1100),'#eef3e8');d=ImageDraw.Draw(im)
def txt(x,y,s,n=24,c='#254338'): d.text((x,y),s,font=ImageFont.truetype(f,n),fill=c)
def asset(path,box):
 a=Image.open(root/path).convert('RGBA');a.thumbnail((box[2],box[3]),Image.Resampling.LANCZOS);im.paste(a,(box[0]+(box[2]-a.width)//2,box[1]+(box[3]-a.height)//2),a)
def panel(x,y,title):
 d.rounded_rectangle((x,y,x+790,y+440),18,fill='#b8e3e8');asset(Path('Assets/Art/2D Platformer Kit/Backgrounds/hills_day.png'),(x,y+90,790,350));txt(x+24,y+14,title,22)
def btn(x,y,label,w=240):
 # nine-slice style proposal; original corners kept intact
 a=Image.open(root/'Assets/Art/Kenney UI Pack/PNG/Green/Default/button_rectangle_depth_flat.png').convert('RGBA');a=a.resize((w,56));im.paste(a,(x,y),a);txt(x+20,y+9,label,23,'#ffffff')
txt(35,18,'GETTING OVER GOOMBAS / UI 设计提案',32);txt(35,65,'Kenney UI × Interface Sounds × 现有 2D Platformer Kit · 16:9 横屏方向',19)
for x,y,t in [(35,115,'01 / 主界面'),(855,115,'02 / 选关'),(35,595,'03 / 关内 HUD'),(855,595,'04 / 设置 / 暂停')]:panel(x,y,t)
txt(85,200,'GETTING OVER',40);txt(85,250,'GOOMBAS',58);btn(90,340,'继续冒险');btn(90,410,'选择关卡');txt(115,485,'设置     ·     退出',22)
asset(Path('Assets/Art/2D Platformer Kit/Player/player_idle_0.png'),(500,260,180,220))
txt(890,183,'草地世界    /    收集星星，探索终点',23)
for i in range(8):
 x=900+i%4*180;y=245+i//4*135
 d.rounded_rectangle((x,y,x+140,y+108),12,fill='#fff9e5' if i<3 else '#c4d2ca');txt(x+45,y+12,str(i+1).zfill(2),30);txt(x+23,y+65,'★★☆' if i<3 else '未解锁',19)
txt(890,510,'世界切换  ‹   草地 / 洞穴 / 雪地   ›',20)
d.rounded_rectangle((60,650,430,710),12,fill='#fff9e5');txt(80,665,'金币  024     星星  ★★☆',23);txt(590,665,'02:35   Ⅱ',24)
asset(Path('Assets/Art/2D Platformer Kit/Player/player_jump.png'),(345,775,95,110));txt(75,968,'移动 / 跳跃提示仅首次显示；保留中央操作视野',20)
d.rounded_rectangle((1040,655,1490,1010),18,fill='#fff9e5');txt(1190,675,'设置',32)
for y,s,v in [(745,'音乐音量',.65),(815,'音效音量',.85)]:
 txt(1080,y,s,22);d.rounded_rectangle((1220,y+12,1440,y+23),5,fill='#d9dfce');d.rounded_rectangle((1220,y+12,1220+int(220*v),y+23),5,fill='#72b83d')
txt(1080,880,'全屏显示              ✓',22);btn(1100,935,'完成 / 返回',330)
txt(35,1053,'布局示意 · 数值与解锁状态为样例 · 使用真实素材合成，尚未接入 Unity 场景',20)
im.save(out/'ui-design-board.png')
files=list((root/'Assets/Art/Kenney UI Pack').rglob('*'))+list((root/'Assets/Sound/Kenney Interface Sounds').rglob('*'))
(out/'import-manifest.json').write_text(json.dumps({str(p.relative_to(root)):hashlib.sha256(p.read_bytes()).hexdigest() for p in files if p.is_file()},indent=2),encoding='utf8')
