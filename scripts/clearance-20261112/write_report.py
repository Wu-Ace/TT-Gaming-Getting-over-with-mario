from pathlib import Path
import json,hashlib,subprocess,datetime
root=Path.cwd(); out=root/'scripts/clearance-20261112'
bridge=(root/'scripts/water-20261112/verification/bridge.py').read_text();bridge=bridge.replace("out=r/'scripts/water-20261112/verification'","out=r/'scripts/clearance-20261112'");(out/'bridge.py').write_text(bridge)
results=json.loads((out/'unity-witness-results.json').read_text()); total=sum(x['samples'] for x in results)
report=f'''# 静态通道几何检查 — 2026-11-12

项目：{root}。Tuanjie 1.10.3 / Unity 2022.3.62t15。

## 本轮结论

教学关和第1–5关完成静态网格初筛；未确认任何必须修改的几何堵死位置。未修改玩家、地形、美术或玩法参数。保留既有难度。

第4关三处疑似卡口经过实际 Unity PolygonCollider2D.Distance 加密复核，存在连续身体通道。玩家 Cauldron 和 Mario 碰撞体同属冻结旋转的 Cauldron 刚体，身体总包围尺寸约0.765×1.5。桨属可转动的独立刚体。

| 区域（世界坐标） | Unity路径样本数 | 路径最小间隙 |
| --- | ---: | ---: |
| x26.25–27.85, y28.2–29.2 | {results[0]['samples']} | {results[0]['minDistance']:.5f} |
| x26.55–27.85, y30.8–31.6 | {results[1]['samples']} | {results[1]['minDistance']:.5f} |
| x15.45–16.85, y38.05–38.85 | {results[2]['samples']} | {results[2]['minDistance']:.5f} |

共{total}次实际Unity身体位置检查；相邻采样点约0.005单位。这里是隔离几何探针，并非用操控器实际通过关卡。

## 方法与证据

- geometry.json：Unity导出的六关实际碰撞网格。只纳入启用的实体碰撞体，保留类型/层/刚体类别。导出不执行Start，运行时改变单独说明。
- screen_body.py：0.05单位网格、身体实际三角形并集、卷积构造身体配置空间。screening.json为初筛。
- screen.py：另纳入桨的当前固定朝向，增加一格保护余量；rig-screening.json不代表可转动桨的完整配置空间。
- route_corridors.py：限定在原路线2.4/4/6单位附近，避免把任意远处绕路当作原路线可通。第1关无详细route，第5关只有首尾点，无法据此检查完整预期路线。
- 第4关初筛断开，经Unity原始多边形查询证实是保守栅格造成的误报。没有据初筛自动删格子。
- unity-local-grids.json / unity-local-connectivity.json：0.02间隔的局部精查及连通性。local-witness-paths.json / unity-witness-results.json：三个局部通过路径及Unity复核。
- Closed door seal在CampaignInteractable.Refresh中运行时禁用，不能作为永久堵点；筛查排除此对象。没有进入Play，也没有改写PlayerPrefs或正式存档。

## 验收范围

| 检查项 | 状态 |
| --- | --- |
| 六关静态碰撞网格读取、配置空间初筛 | 已做 |
| 第4关三处疑似卡口实际Unity身体几何复核 | 通过 |
| 修改源码编译 | 不适用：无游戏源码修改；检查脚本已执行 |
| 完整控制器/桨关节配置空间搜索 | 未做 |
| 移动/旋转/下落平台的时序通道 | 未做 |
| 重力、借力、跳跃高度、全程操作通关 | 未做 |
| 第1、5关完整预期路线验证 | 未做：缺少细化路线 |
| 设备/导出 | 未做 |

本报告是一次静态几何筛查及疑点复核，不是六关绝对可通证明。不会将采样未找到通路直接等同于数学上不可能，也不会为满足“有修改”而扩大高难度通道。

## 复跑

在工程目录运行 `python scripts/clearance-20261112/bridge.py scripts/clearance-20261112/export.json` 导出后，运行 `screen_body.py`、`screen.py`、`route_corridors.py`。Unity检查会退出Play；运行前保留/备份现场。当前export用附加场景读取，会临时出现重复全局灯光日志；局部复核使用单场景打开并在finally恢复原场景，要求原场景无未保存编辑。

本轮最终恢复Level01编辑模式。已有未提交更改保留。SHA清单记录相关源码、六关场景和证据的采集版本，不能单独证明完整玩法验收。
'''
(out/'README.md').write_text(report,encoding='utf-8')
with (root/'ai-handoff.md').open('a',encoding='utf-8') as f:f.write('\n\n2026-11-12 — Static clearance audit: six enabled gameplay scenes exported and 0.05-unit body/rig configuration-space screening performed. Three Level04 raster bottlenecks resolved using actual Unity polygon distance grids and 1120 continuous-witness samples (min separations .02107/.02672/.02037); none confirmed impossible, no gameplay/terrain edits. Restored Level01 Edit mode. Scripts/evidence: scripts/clearance-20261112/README.md. NOT a full traversal guarantee: no dynamic platform timing, articulated paddle search, controller/gravity testing, detailed Level01/05 route proof, device/export. Existing uncommitted work preserved.\n')
files=list(out.glob('*')); files+=[root/'Assets/Prefabs/Character.prefab',root/'Assets/Scripts/CampaignInteractable.cs',root/'Assets/Scripts/Character/Control/ArmsSimulation.cs',root/'ProjectSettings/ProjectVersion.txt']
files += [root/x['scene'] for x in json.loads((out/'geometry.json').read_text())]
manifest={'utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'head':subprocess.check_output(['git','rev-parse','HEAD'],text=True).strip(),'files':{str(p.relative_to(root)):hashlib.sha256(p.read_bytes()).hexdigest() for p in files if p.is_file() and p.name!='manifest.json'}}
(out/'manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8');(out/'git-status.txt').write_text(subprocess.check_output(['git','status','--short'],text=True),encoding='utf-8')
print('Saved report, handoff and hashes; no game assets edited.')
