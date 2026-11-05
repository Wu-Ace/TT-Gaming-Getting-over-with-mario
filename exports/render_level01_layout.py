from pathlib import Path
import re, math, hashlib
from PIL import Image,ImageDraw,ImageFont
out=Path('exports'); items=[]
s=Path('Assets/Prefabs/Level.prefab').read_text()
for b in re.split(r'--- !u!',s):
 if 'PrefabInstance:' not in b:continue
 props=dict(re.findall(r'propertyPath: ([^\n]+)\n      value: ([^\n]*)',b))
 name=props.get('m_Name','')
 if props.get('m_IsActive')=='0':continue
 if 'm_LocalPosition.x' not in props:continue
 x=float(props['m_LocalPosition.x']);y=float(props.get('m_LocalPosition.y',0))
 if any(k in name for k in ['GroundTile','StairsBlock']):kind='ground';w=h=.8
 elif 'Block' in name:kind='block';w=h=.8
 elif 'Tube' in name:kind='pipe';w=1.6;h= {'TubeShort':1.6,'TubeMedium':2.4,'TubeLarge':3.2}.get(name.split(' (')[0],2.4)
 elif 'Goomba' in name or 'Koopa' in name:kind='enemy';w=h=.7
 elif name.startswith('Coin'):kind='coin';w=h=.35
 else:continue
 items.append((x,y,w,h,kind))
scene=Path('Assets/Scenes/OneOneScene.unity').read_text(); names={};trans={}
for b in re.split(r'--- !u!',scene):
 if b.startswith('1 &'):
  id=re.match(r'1 &(\d+)',b)[1];m=re.search(r'  m_Name: (.*)',b);names[id]=m[1] if m else ''
 if b.startswith('4 &'):
  g=re.search(r'm_GameObject: \{fileID: (\d+)',b);p=re.search(r'm_LocalPosition: \{x: ([^,]+), y: ([^,]+)',b)
  if g and p:trans[g[1]]=(float(p[1]),float(p[2]))
marks=[]
for g,n in names.items():
 if g in trans and ('Objective Diamond' in n or 'Checkpoint' in n or 'Three Diamond Exit' in n):marks.append((*trans[g],n))
print('markers',marks,'items',len(items))
fontpath='C:/Windows/Fonts/msyh.ttc';f=lambda n:ImageFont.truetype(fontpath,n)
im=Image.new('RGB',(2200,1400),'#101d2a');d=ImageDraw.Draw(im)
d.text((55,24),'第一关 · 关卡布局图',font=f(38),fill='white')
d.text((55,80),'当前场景坐标示意 · OneOneScene / Level.prefab · 从左向右阅读，分四段连续展示',font=f(21),fill='#b4c6d6')
colors={'ground':'#76af69','block':'#cc9862','pipe':'#45b5a2','enemy':'#f27e72','coin':'#fbd461'}
scale=25;span=80
for row in range(4):
 left=row*span;top=155+row*285;zero=top+205
 d.rounded_rectangle((40,top-10,2160,top+255),12,fill='#1b3042')
 d.text((60,top),f'{row+1:02}   {left}–{left+span} m',font=f(20),fill='#c7d8e7')
 for x in range(left,left+span+1,10):
  px=100+(x-left)*scale;d.line((px,top+40,px,top+230),fill='#294458');d.text((px,top+232),str(x),font=f(15),fill='#8ea9bc')
 for x,y,w,h,k in items:
  if not left<=x<left+span:continue
  px=100+(x-left)*scale;py=zero-y*scale
  box=(px-w*scale/2,py-h*scale/2,px+w*scale/2,py+h*scale/2)
  if k in ('enemy','coin'):d.ellipse(box,fill=colors[k])
  else:d.rectangle(box,fill=colors[k],outline='#132d30')
 for x,y,n in marks:
  if left<=x<left+span:
   px=100+(x-left)*scale;py=zero-y*scale
   label='钻石 '+n[-1] if 'Diamond ' in n and 'Objective' in n else ('检查点' if 'Checkpoint' in n else '出口')
   d.line((px,top+48,px,py),fill='#81dafa',width=2);d.text((px-30,top+25),label,font=f(18),fill='#81dafa');d.ellipse((px-6,py-6,px+6,py+6),fill='#81dafa')
for i,(k,label) in enumerate(zip(colors,['地面 / 台阶','砖块 / 问号块','管道','敌人','金币'])):
 x=65+i*335;d.rectangle((x,1320,x+22,1342),fill=colors[k]);d.text((x+34,1316),label,font=f(21),fill='white')
d.text((65,1360),'布局示意，非 Unity 实机截图；图形简化表示对象，管道尺寸为示意。',font=f(17),fill='#93aabd')
im.save(out/'level01-layout.png')
(out/'level01-layout-source.txt').write_text('\n'.join(str(p)+' '+hashlib.sha256(p.read_bytes()).hexdigest() for p in [Path('Assets/Scenes/OneOneScene.unity'),Path('Assets/Prefabs/Level.prefab')]),encoding='utf-8')
