exec(open('exports/render_level01_layout.py',encoding='utf-8-sig').read().split("fontpath=")[0])
fontpath='C:/Windows/Fonts/msyh.ttc';f=lambda n:ImageFont.truetype(fontpath,n)
W,H=3100,760
im=Image.new('RGB',(W,H),'#f4f7fa');d=ImageDraw.Draw(im)
d.text((60,25),'第一关：从左往右，拿齐 3 颗钻石，再进入出口',font=f(44),fill='#182b40')
d.text((60,90),'下方地形按当前关卡坐标绘制；上方编号表示主要目标的先后位置',font=f(24),fill='#53677c')
s=9.3;ox=85;zero=560
colors={'ground':'#67855b','block':'#c28b54','pipe':'#208e76','enemy':'#ce4940','coin':'#d6a42c'}
d.rounded_rectangle((45,315,W-45,670),20,fill='#e3edf3')
for x,y,w,h,k in items:
 px=ox+x*s;py=zero-y*s
 if k in ('coin','enemy'):continue
 d.rectangle((px-w*s/2,py-h*s/2,px+w*s/2,py+h*s/2),fill=colors[k])
points=[(0,-2,'起点','出发'),(48,.406,'① 钻石 1','到这里收集'),(123,-2.8,'检查点','中途存档'),(143.709,-.32,'② 钻石 2','继续收集'),(245.08,4.103,'③ 钻石 3','拿齐三颗'),(310,-2.4,'终点','进入出口')]
for i,(x,y,label,sub) in enumerate(points):
 px=ox+x*s;py=zero-y*s
 ty=175 if i%2==0 else 245
 color='#147eab' if '钻石' in label else '#254f61'
 tw=d.textbbox((0,0),label,font=f(29))[2];tx=max(55,min(W-tw-55,px-tw/2))
 d.text((tx,ty),label,font=f(29),fill=color)
 d.line((px,ty+42,px,py-14),fill=color,width=3)
 d.ellipse((px-7,py-7,px+7,py+7),fill=color)
 d.text((tx,ty+44),sub,font=f(18),fill='#667b89')
for a,b in [(85,500),(600,1150),(1500,2200),(2450,2900)]:
 d.line((a,145,b,145),fill='#a5bac6',width=4);d.polygon([(b,145),(b-12,138),(b-12,152)],fill='#a5bac6')
d.text((65,695),'绿色：可站立的地面和台阶     棕色：砖块     深绿色：管道     空白处：地面缺口',font=f(25),fill='#344f60')
im.save('exports/level01-layout-simple.png')
