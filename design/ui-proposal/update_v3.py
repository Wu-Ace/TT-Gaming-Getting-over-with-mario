from pathlib import Path
p=Path('design/ui-proposal/render_v2.py');s=p.read_text(encoding='utf-8-sig').replace("/v2'","/v3'")
s=s.replace("box(im,(82,350,520,667),'#fff6dc',25)","box(im,(82,350,520,690),'#fff6dc',25)")
s=s.replace("button(im,(115,383,370,76),'开始冒险');button(im,(115,478,370,66),'选择关卡',False);text(im,(298,597),'设置     ·     退出',27,anchor='mm')","button(im,(115,380,370,76),'开始');button(im,(115,480,370,76),'设置');button(im,(115,580,370,76),'退出')")
s=s.replace("x=85+i*370;box(im,(x,207,x+330,740),'#233f38dd',12)","""x=85+i*370
 # Round the polygon corners by drawing into a mask and smoothing its boundary.
 maskq=Image.new('L',im.size);qd=ImageDraw.Draw(maskq)
 points=[(x+65,207),(x+280,240),(x+275,740),(x+65,710)]
 qd.polygon(points,fill=255);maskq=maskq.filter(ImageFilter.GaussianBlur(8)).point(lambda v:255 if v>128 else 0).filter(ImageFilter.GaussianBlur(1))
 im.paste(Image.new('RGBA',im.size,'#233f38'),(0,0),maskq)""")
s=s.replace("text(im,(470,550),'全屏显示',29,'#164b3b');button(im,(715,532,410,65),'开启',False)","text(im,(470,538),'分辨率',29,'#164b3b');button(im,(715,522,410,65),'自动（跟随屏幕） ▾',False);text(im,(715,605),'当前显示器推荐尺寸 · 界面自动适配',18,'#e8fff2')")
s=s.replace("im=scene();box(im,(64,40,265,108),'#fff8e8');text(im,(164,73),'02:35',36,anchor='mm');button(im,(1375,40,160,68),'设置',False);im.save(O/'03-in-game.png')", """im=scene()
# Reuse existing coin/time HUD pixels in this static proposal; runtime UI remains unchanged.
hud=Image.open(R/'scripts/art-evidence/play-final.png').convert('RGBA')
put(im,hud.crop((1530,140,1627,174)),65,45,1.4)
put(im,hud.crop((1812,140,1932,174)),1040,45,1.4)
box(im,(250,42,535,98),'#343d47',3);text(im,(268,51),'STARS  2 / 3',28,'white')
button(im,(1375,40,160,68),'设置',False);im.save(O/'03-in-game.png')""")
s=s.replace('关内 · 仅时间和设置','关内 · 保留现有金币 / 星星 UI')
s=s[:s.index("(O/'proposal.md').write_text")]+'''(O/'proposal.md').write_text("""# UI 设计 V3
主界面：开始、设置、退出使用同一绿色按钮。开始进入选关，不直接开局。
树木底部统一落在地面上，装饰保持等比缩放。
选关：圆角倾斜四边形底板，圆形预览直径 300，大于底板约 215 宽，超出两侧边界；标题和进度仍在底板内。
设置：音乐音量、音效音量、分辨率。默认自动，按当前显示器原生尺寸与支持模式确定分辨率；提供不超过显示器尺寸的可用分辨率选项。显示器改变时重新评估；UI 按参考画布缩放与锚点布局适配不同比例，不拉伸美术。移除全屏开关；实际窗口模式沿用游戏设置。应用后保存，失效的已存选项回退自动。
关内：保留原有金币、星星数量 UI 和计时，新增设置入口。静态稿中金币与时间摘取已有 Game 截图，星星以原有 STARS 文案表示；实现时复用现有组件，不复制截图。示例计数不代表实时状态。
音效候选沿用上一版；尚未试听定稿。
本次仅修改设计稿，没有修改 Unity 场景、代码、碰撞、礼物逻辑。分辨率适配尚未实现和运行验证。
""",encoding='utf8')
'''
Path('design/ui-proposal/render_v3.py').write_text(s,encoding='utf8')
