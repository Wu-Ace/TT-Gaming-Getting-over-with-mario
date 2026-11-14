from pathlib import Path
import hashlib,json,datetime,subprocess
p=Path('scripts/jk-original-20261111')
(p/'README.md').write_text('''# 第 5 关原版地形复刻

已保存 Assets/Scenes/Level05_Cave.unity。范围为此前约定的 Jump King 底部至塔楼前 14 屏，继续使用可编辑 Tilemap。

本版取消“往多保留”：移除上一版的 4,182 个额外格，保留原版 7,565 个地形格。121 个斜坡从本地 JumpKing.exe 的 BaseBlockFactory.GetSlopeType 读取分类，并核对 SlopeBlock 的实际三角顶点；修正上一版 16 格形状。原安装目录只读。安装版本的两种底部斜坡枚举拥有相同三角顶点，转换按实际顶点处理，保留其局部不规则轮廓。

世界方格仍为 0.4 米，地图宽 24 米、高 252 米。项目现有岩石外观、背景和锤子控制继续使用。本次复刻的是地形布局与碰撞轮廓。出生点下调至原版地面上方；现有终点旗移至原版塔楼平台，没有增加承托方块。已删除的钻石、存档旗保持删除。

## 编辑

打开 Level05_Cave，选择 Level/Terrain/Jump King 01-14 - paint here，使用 Jump King Fifth Palette 绘制并保存。Grid 本地 0.8 米、统一缩放 0.5。普通微调直接编辑场景；Tools/Levels/Restore Exact Original Jump King Fifth 会重新恢复原版布局并覆盖地形微调。恢复工具读取本目录 plan.json，不再读取旧版并集计划，并保留当前岩石规则外观。

## 验证

- 编译并执行成功。刷新仍有既有 GridPaintingState ScriptableSingleton 提示，不声称 Console 全干净。
- 原版逐格对照：0 差异；339,595 个碰撞子格取样：0 差异，见 applied.txt。
- 从磁盘重开：7,565 格、0 差异、钻石 0、存档旗 0、终点旗 1，见 reopened.txt。并行美术工作替换了岩石规则资源引用，检查按实格碰撞与斜坡形状核对，不要求特定美术资源文件名。
- 真实 Scene 视图、全部 14 屏编辑器相机渲染及三个真实 Play 画面已检查，见 all-floors.png、play-0/1/2.png。编辑器相机图片不是桌面截图。
- Play 出生未死亡，121 个斜坡内外测试 0 差异。Play 进入请求曾超时，但随后运行检查与三张截图完成，退出 Play 成功；见 play-verification.txt、stop-result.json。
- 完整锤子攀爬、设备、导出未做。截图检查冻结刚体并移动相机，没有重置正式存档。

backup/Level05_Cave.unity 保存本轮修改前的并集地图；backup/InstallJumpKingFifth.cs 保存旧转换器。旧 scripts/jk-tilemap-20261111 报告属于已被本版替代的历史结果。探索性的 IL 和碰撞探针文件不是最终验收依据。原版来源及最终文件哈希见 manifest.json。
''',encoding='utf-8')
old=Path('scripts/jk-tilemap-20261111/README.md');s=old.read_text(encoding='utf-8');notice='> 已被用户后续要求替代：当前为纯原版地形，见 ../jk-original-20261111/README.md。以下是历史并集版本记录。\n\n';old.write_text(notice+s if not s.startswith('> 已被') else s,encoding='utf-8')
h=Path('ai-handoff.md');h.open('a',encoding='utf-8').write('''

## Level05 original-only correction — 2026-11-11
User supersedes more-terrain union with complete original layout within the established bottom-to-tower 14-screen scope. Removed all 4182 extra cells; saved 7565 original cells and121 slopes. Source classifications read from installed JumpKing.exe BaseBlockFactory.GetSlopeType; actual SlopeBlock triangle vertices checked, including shared BottomLeft/BottomRight geometry in this installed build. Corrected16 prior slope shapes. Existing rock art/background/hammer gameplay retained; source is geometry replication. Spawn(11.8,-1.15), finish(11.4,244.45) over original platforms; no support blocks added. Current removed diamonds/checkpoints remain absent. Installer now original-only, repeatable, preserves current ground RuleTile art, reads scripts/jk-original-20261111/plan.json.
PASS 339595 subcell physics samples, original tile comparison0differences; disk reopen7565tiles/0differences/0diamonds/0checkpoints/1finish. Actual Scene and14 rendered floors inspected; live Play spawn alive and121slope inside/outside samples0differences, three actual Game captures inspected. Play-entry RPC timeout occurred but subsequent coroutine evidence completed and Stop succeeded. Refresh retains existing GridPaintingState singleton diagnostic. Full hammer traversal/device/export NOT RUN. Concurrent RockVariations art references retained. Scoped backups, final notes and hashes: scripts/jk-original-20261111/README.md and manifest.json. Supersedes prior union terrain report.
''')
files=[Path('Assets/Scenes/Level05_Cave.unity'),Path('Assets/Editor/InstallJumpKingFifth.cs'),Path('Assets/Scripts/JumpKingRockTile.cs'),h]+list(p.glob('*.txt'))+list(p.glob('*.png'))+[p/'plan.json',p/'README.md',p/'extract-original.ps1']+list(Path('Assets/Tiles/JumpKingFifth').glob('*'))+list(Path('Assets/Tiles/RockVariations').glob('*'))
source=Path(r'E:\SteamLibrary\steamapps\common\Jump King')
r={'utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'head':subprocess.check_output(['git','rev-parse','HEAD'],text=True).strip(),'sources':{str(source/n):hashlib.sha256((source/n).read_bytes()).hexdigest() for n in ['JumpKing.exe','Content/level.xnb']},'files':{str(f):hashlib.sha256(f.read_bytes()).hexdigest() for f in files if f.is_file()}}
(p/'manifest.json').write_text(json.dumps(r,indent=2),encoding='utf-8')
print((p/'reopened.txt').read_text());print((p/'play-verification.txt').read_text())
