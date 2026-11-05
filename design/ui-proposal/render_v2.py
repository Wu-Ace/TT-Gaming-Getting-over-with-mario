from PIL import Image,ImageDraw,ImageFont,ImageFilter
from pathlib import Path
import json,hashlib
R=Path.cwd();O=R/'design/ui-proposal/v2';O.mkdir(exist_ok=True)
A=R/'Assets/Art/2D Platformer Kit';U=R/'Assets/Art/Kenney UI Pack/PNG'
font='C:/Windows/Fonts/msyh.ttc'
def text(im,xy,s,size=26,fill='#28483d',anchor=None): ImageDraw.Draw(im).text(xy,s,font=ImageFont.truetype(font,size),fill=fill,anchor=anchor)
def put(im,a,x,y,scale=1):
 a=a.convert('RGBA');a=a.resize((round(a.width*scale),round(a.height*scale)),Image.Resampling.LANCZOS);im.paste(a,(round(x),round(y)),a)
def tile(theme,box): return Image.open(A/f'Tiles/{theme}/_tileset_120_120.png').convert('RGBA').crop(box)
def scene(theme='Grassland',bg='hills_day.png'):
 im=Image.new('RGBA',(1600,900),'#b8e5df');b=Image.open(A/'Backgrounds'/bg).convert('RGBA');sc=max(1600/b.width,900/b.height);put(im,b,0,0,sc)
 # Complete hills and trees, uniformly scaled. Native terrain cells repeat.
 hill=tile(theme,(0,840,480,960)) if theme=='Grassland' else tile(theme,(0,0,600,120))
 if theme=='Grassland':
  for x,y,s in [(-90,540,1.8),(590,475,2.0),(1120,550,1.35)]:put(im,hill,x,y,s)
  tree=tile(theme,(720,360,840,720))
  for x,y,s in [(730,155,1.45),(1090,235,1.2),(1370,135,1.65)]:put(im,tree,x,720-tree.height*s,s)
 for x in range(-40,1600,120):
  put(im,tile(theme,(120,120,240,240)),x,720)
  put(im,tile(theme,(120,240,240,360)),x,840)
 if theme=='Grassland':
  for x in [25,520,1230,1450]:put(im,tile(theme,(720,720,960,840)),x,660,.75)
 return im

def box(im,rect,color,r=20):ImageDraw.Draw(im).rounded_rectangle(rect,r,fill=color)
def button(im,rect,label,green=True):
 x,y,w,h=rect
 src=Image.open(U/('Green' if green else 'Grey')/'Default/button_rectangle_depth_flat.png').convert('RGBA')
 # Three-slice horizontal stretch preserves end caps.
 k=12;scale=h/src.height;src=src.resize((round(src.width*scale),h));cap=round(k*scale)
 put(im,src.crop((0,0,cap,h)),x,y);put(im,src.crop((src.width-cap,0,src.width,h)),x+w-cap,y)
 put(im,src.crop((cap,0,src.width-cap,h)).resize((w-2*cap,h)),x+cap,y)
 text(im,(x+w/2,y+h/2-2),label,28,'#ffffff' if green else '#36504a','mm')

im=scene();text(im,(95,105),'GETTING OVER',55,'#fff8df');text(im,(90,168),'GOOMBAS',90,'#fff8df');text(im,(98,283),'越过障碍，继续向前。',25)
box(im,(82,350,520,667),'#fff6dc',25)
button(im,(115,383,370,76),'开始冒险');button(im,(115,478,370,66),'选择关卡',False);text(im,(298,597),'设置     ·     退出',27,anchor='mm')
text(im,(1510,850),'Enter 确认',20,'#fff6dc','rm');im.save(O/'01-home.png')

im=scene();im.alpha_composite(Image.new('RGBA',im.size,(21,53,44,80)));text(im,(85,55),'选择世界',48,'#fff8df');text(im,(88,121),'每一段旅程，都有新的风景',23,'#e7efcf')
for i,(title,sub,bg,theme) in enumerate([('草地起点','01 — 04','hills_day.png','Grassland'),('地下探险','05 — 08','cave_interior.png','Cave'),('雪地旅程','09 — 12','mountains_day.png','Grassland_Winter'),('下一段冒险','敬请期待','hills_night.png','Grassland')]):
 x=85+i*370;box(im,(x,207,x+330,740),'#233f38dd',12)
 text(im,(x+165,247),title,32,'#fff8df','mm')
 thumb=scene(theme,bg).resize((560,315));thumb=thumb.crop((130,0,430,300));mask=Image.new('L',(300,300));ImageDraw.Draw(mask).ellipse((2,2,298,298),fill=255)
 im.paste(thumb,(x+15,310),mask);ImageDraw.Draw(im).ellipse((x+15,310,x+315,610),outline='#fff5db',width=5)
 text(im,(x+165,655),sub,26,'#fff8df','mm');text(im,(x+165,701),'进入世界  →' if i==0 else '未解锁' if i<3 else 'COMING SOON',21,'#cdddba','mm')
button(im,(85,790,190,62),'← 返回',False);im.save(O/'02-world-select.png')

im=scene();box(im,(64,40,265,108),'#fff8e8');text(im,(164,73),'02:35',36,anchor='mm');button(im,(1375,40,160,68),'设置',False);im.save(O/'03-in-game.png')

im=scene().filter(ImageFilter.GaussianBlur(5));im.alpha_composite(Image.new('RGBA',im.size,(13,37,30,145)))
box(im,(415,130,1185,765),'#e3e7e8',10);box(im,(415,130,1185,655),'#14b87b',10)
text(im,(465,165),'设置',40,'#fff8ed');button(im,(1103,146,60,60),'×',False)
for y,label,val in [(290,'音乐音量',65),(405,'音效音量',85)]:
 text(im,(470,y),label,29,'#164b3b');box(im,(715,y-8,1125,y+56),'#f5f5f3',7);box(im,(740,y+19,1098,y+28),'#ccd4d1',4);box(im,(740,y+19,740+int(358*val/100),y+28),'#7bb85c',4);ImageDraw.Draw(im).ellipse((728+int(358*val/100),y+9,752+int(358*val/100),y+37),fill='white');text(im,(1120,y+82),f'{val}%',17,'#e8fff2','rm')
text(im,(470,550),'全屏显示',29,'#164b3b');button(im,(715,532,410,65),'开启',False)
button(im,(466,684,230,60),'返回',False);button(im,(735,684,400,60),'应用设置');im.save(O/'04-settings.png')
board=Image.new('RGB',(1648,1010),'#f1eddf')
for i,name in enumerate(['01-home','02-world-select','03-in-game','04-settings']):
 pic=Image.open(O/(name+'.png')).resize((800,450));board.paste(pic,(16+i%2*816,35+i//2*495));text(board,(24+i%2*816,7+i//2*495),['主界面','世界选择 · 进度为示意','关内 · 仅时间和设置','设置'][i],18)
board.save(O/'overview.png')
(O/'proposal.md').write_text('''# UI 设计 V2
使用 Kit 原生图集搭建远景、完整丘陵、中景树木、重复地面和前景灌木；保持装饰等比缩放。场景与碰撞均未修改。
主界面：左侧奶油菜单，右侧场景。开始冒险读取进度，无存档进入第一关。
选关：世界卡片，圆形主题预览；进入世界后展示关卡列表。图中世界划分、锁定状态为提案，需按实际关卡映射，不代表已实现功能。
关内：只有左上时间、右上设置。暂停时停止计时，重开重置本关计时；不显示金币或星星 HUD。
设置：绿色主面板、浅色控制槽、底部返回与应用；音乐/音效/全屏。未应用的修改在返回时撤销，应用后本地保存。关内打开时背景压暗并暂停；直播礼物处理规则在实现时保留现有逻辑。
音效候选：click_001.ogg 点击；confirmation_001.ogg 应用；back_001.ogg 返回。待试听确定响度；拖动音量条不连续触发点击声。
交付为素材合成静态设计稿，尚未接入 Unity Scene/Game，也未进行运行验收。未使用素材包 Player 替换游戏现有锅锤角色。
''',encoding='utf8')

