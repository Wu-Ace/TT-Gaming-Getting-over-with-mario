from pathlib import Path
p=Path('design/ui-proposal/render_v3.py');s=p.read_text(encoding='utf-8-sig').replace("/v3'","/v4'")
s=s.replace('points=[(x+65,207),(x+280,240),(x+275,740),(x+65,710)]', '''shapes=[[(45,207),(285,250),(265,720),(85,750)],[(85,250),(270,207),(290,750),(45,715)],[(50,207),(280,242),(255,745),(80,710)],[(80,245),(290,207),(265,715),(45,750)]]
 points=[(x+px,py) for px,py in shapes[i]]''')
s=s.replace("box(im,(250,42,535,98),'#343d47',3);text(im,(268,51),'STARS  2 / 3',28,'white')", """star=Image.open(A/'Interactive/star_0.png').convert('RGBA');star=star.crop(star.getbbox());put(im,star,270,49,34/star.height)
# Match the existing white counter, using its zero glyph as a static sample.
put(im,hud.crop((1550,140,1627,174)),316,45,1.4)""")
s=s.replace("button(im,(466,684,230,60),'返回',False);button(im,(735,684,400,60),'应用设置')", "button(im,(635,684,330,60),'返回',False);text(im,(800,643),'修改即时生效并自动保存',18,'#e8fff2','mm')")
s=s.replace('UI 设计 V3','UI 设计 V4').replace('圆角倾斜四边形底板','四张不同的圆角倾斜四边形底板，交替采用上宽下窄、上窄下宽和不同斜边').replace('应用后保存','修改即时生效并自动保存，不设应用按钮；关闭与返回均保留修改').replace('星星以原有 STARS 文案表示','星星改为 Kit 星星图标加同风格数字').replace('静态稿中金币与时间摘取已有 Game 截图','静态稿中金币、数字与时间摘取已有 Game 截图')
Path('design/ui-proposal/render_v4.py').write_text(s,encoding='utf8')
