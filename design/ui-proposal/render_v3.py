from PIL import Image,ImageDraw,ImageFont,ImageFilter
from pathlib import Path
import json,hashlib
R=Path.cwd();O=R/'design/ui-proposal/v3';O.mkdir(exist_ok=True)
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
  tree=tile(theme,(720,360,840,720));tree=tree.crop(tree.getbbox())
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
box(im,(82,350,520,690),'#fff6dc',25)
button(im,(115,380,370,76),'开始');button(im,(115,480,370,76),'设置');button(im,(115,580,370,76),'退出')
text(im,(1510,850),'Enter 确认',20,'#fff6dc','rm');im.save(O/'01-home.png')

im=scene();im.alpha_composite(Image.new('RGBA',im.size,(21,53,44,80)));text(im,(85,55),'选择世界',48,'#fff8df');text(im,(88,121),'每一段旅程，都有新的风景',23,'#e7efcf')
for i,(title,sub,bg,theme) in enumerate([('草地起点','01 — 04','hills_day.png','Grassland'),('地下探险','05 — 08','cave_interior.png','Cave'),('雪地旅程','09 — 12','mountains_day.png','Grassland_Winter'),('下一段冒险','敬请期待','hills_night.png','Grassland')]):
 x=85+i*370
 # Round the polygon corners by drawing into a mask and smoothing its boundary.
 maskq=Image.new('L',im.size);qd=ImageDraw.Draw(maskq)
 points=[(x+65,207),(x+280,240),(x+275,740),(x+65,710)]
 qd.polygon(points,fill=255);maskq=maskq.filter(ImageFilter.GaussianBlur(8)).point(lambda v:255 if v>128 else 0).filter(ImageFilter.GaussianBlur(1))
 im.paste(Image.new('RGBA',im.size,'#233f38'),(0,0),maskq)
 text(im,(x+165,247),title,32,'#fff8df','mm')
 thumb=scene(theme,bg).resize((560,315));thumb=thumb.crop((130,0,430,300));mask=Image.new('L',(300,300));ImageDraw.Draw(mask).ellipse((2,2,298,298),fill=255)
 im.paste(thumb,(x+15,310),mask);ImageDraw.Draw(im).ellipse((x+15,310,x+315,610),outline='#fff5db',width=5)
 text(im,(x+165,655),sub,26,'#fff8df','mm');text(im,(x+165,701),'进入世界  →' if i==0 else '未解锁' if i<3 else 'COMING SOON',21,'#cdddba','mm')
button(im,(85,790,190,62),'← 返回',False);im.save(O/'02-world-select.png')

im=scene()
# Reuse existing coin/time HUD pixels in this static proposal; runtime UI remains unchanged.
hud=Image.open(R/'scripts/art-evidence/play-final.png').convert('RGBA')
put(im,hud.crop((1530,140,1627,174)),65,45,1.4)
put(im,hud.crop((1812,140,1932,174)),1040,45,1.4)
box(im,(250,42,535,98),'#343d47',3);text(im,(268,51),'STARS  2 / 3',28,'white')
button(im,(1375,40,160,68),'设置',False);im.save(O/'03-in-game.png')

im=scene().filter(ImageFilter.GaussianBlur(5));im.alpha_composite(Image.new('RGBA',im.size,(13,37,30,145)))
box(im,(415,130,1185,765),'#e3e7e8',10);box(im,(415,130,1185,655),'#14b87b',10)
text(im,(465,165),'设置',40,'#fff8ed');button(im,(1103,146,60,60),'×',False)
for y,label,val in [(290,'音乐音量',65),(405,'音效音量',85)]:
 text(im,(470,y),label,29,'#164b3b');box(im,(715,y-8,1125,y+56),'#f5f5f3',7);box(im,(740,y+19,1098,y+28),'#ccd4d1',4);box(im,(740,y+19,740+int(358*val/100),y+28),'#7bb85c',4);ImageDraw.Draw(im).ellipse((728+int(358*val/100),y+9,752+int(358*val/100),y+37),fill='white');text(im,(1120,y+82),f'{val}%',17,'#e8fff2','rm')
text(im,(470,538),'分辨率',29,'#164b3b');button(im,(715,522,410,65),'自动（跟随屏幕）  v',False);text(im,(715,605),'当前显示器推荐尺寸 · 界面自动适配',18,'#e8fff2')
button(im,(466,684,230,60),'返回',False);button(im,(735,684,400,60),'应用设置');im.save(O/'04-settings.png')
board=Image.new('RGB',(1648,1010),'#f1eddf')
for i,name in enumerate(['01-home','02-world-select','03-in-game','04-settings']):
 pic=Image.open(O/(name+'.png')).resize((800,450));board.paste(pic,(16+i%2*816,35+i//2*495));text(board,(24+i%2*816,7+i//2*495),['主界面','世界选择 · 进度为示意','关内 · 保留现有金币 / 星星 UI','设置'][i],18)
board.save(O/'overview.png')
(O/'proposal.md').write_text("""# UI 设计 V3
主界面：开始、设置、退出使用同一绿色按钮。开始进入选关，不直接开局。
树木底部统一落在地面上，装饰保持等比缩放。
选关：圆角倾斜四边形底板，圆形预览直径 300，大于底板约 215 宽，超出两侧边界；标题和进度仍在底板内。
设置：音乐音量、音效音量、分辨率。默认自动，按当前显示器原生尺寸与支持模式确定分辨率；提供不超过显示器尺寸的可用分辨率选项。显示器改变时重新评估；UI 按参考画布缩放与锚点布局适配不同比例，不拉伸美术。移除全屏开关；实际窗口模式沿用游戏设置。应用后保存，失效的已存选项回退自动。
关内：保留原有金币、星星数量 UI 和计时，新增设置入口。静态稿中金币与时间摘取已有 Game 截图，星星以原有 STARS 文案表示；实现时复用现有组件，不复制截图。示例计数不代表实时状态。
音效候选沿用上一版；尚未试听定稿。
本次仅修改设计稿，没有修改 Unity 场景、代码、碰撞、礼物逻辑。分辨率适配尚未实现和运行验证。
""",encoding='utf8')

