from pathlib import Path
import json,hashlib,datetime,subprocess
root=Path.cwd();out=root/'scripts/crush-death-20261112'; paths=['Assets/Scripts/Character/Lifecycle/Death.cs','Assets/Scripts/Character/Lifecycle/PlayerCrushDetection.cs','Assets/Scripts/Character/Lifecycle/PlayerCrushDetection.cs.meta','Assets/Scripts/PlayerUnstuck.cs','Assets/Prefabs/Character.prefab','ProjectSettings/Physics2DSettings.asset','ProjectSettings/ProjectVersion.txt']
checks=[json.loads((out/f).read_text()) for f in ['physics-results.json','play-results.json','actual-player-results.json']];assert all(x['pass'] for group in checks for x in group);assert not (root/'Assets/Scenes/CrushDeath_Acceptance_Temp.unity').exists();assert (out/'backup/PlayerUnstuck.cs').read_bytes()==(root/'Assets/Scripts/PlayerUnstuck.cs').read_bytes()
report='''# 挤压死亡 — 2026-11-12

## 行为

Death.Awake自动添加PlayerCrushDetection，覆盖使用该玩家的全部关卡；停止添加旧PlayerUnstuck，并禁用已有恢复组件。旧恢复类保留给现有工具引用，本轮没有改写它。

- 只检测Cauldron刚体拥有的启用实体碰撞体，含上身Mario复合形状；桨和触发器不计入。
- 身体与有效外部碰撞体深度嵌入超过0.06单位，立即判死亡。
- 身体接近两侧相对碰撞面（0.02单位接触容差，法线点积<-0.65），两侧相对闭合速度之和超过0.05单位/秒，也判死亡；不要求物理解算器允许严重穿模。
- 每个物理步以及碰撞进入/持续时检查；使用原有Death.Kill，原有死亡反馈和关卡重启流程不变。不再等0.65秒或退回安全落脚点。
- 正常站立、贴墙、开离的夹面、只有一侧推动且有逃逸空间、触发器、自身/被忽略的碰撞对都不会因为接触本身死亡。
- 深度嵌入判定是通用身体陷入规则，不仅限于移动平台：若身体实际嵌入地形超过容差也会死亡。

## 验收

| 范围 | 结果 | 证据 |
| --- | --- | --- |
| Unity刷新、C#编译 | 通过，0错误0警告 | refresh-result.json |
| 11项隔离实际几何/Physics2D解算回归 | 全通过 | physics-results.json |
| 隔离场景实际Play死亡集成10项 | 全通过 | play-results.json |
| 真实玩家Cauldron+Mario复合多边形6项 | 全通过 | actual-player-results.json |
| 真实第3关环绕平台骑乘/被挤全流程 | 未做 | 本轮仅隔离挤压物理及死亡集成 |
| 死亡画面渲染、完整关卡通关、设备/导出 | 未做 | 不作通过承诺 |

Play回归验证自动安装检测器、旧恢复未启用、静止接触存活、实际运动墙夹压触发Death一次、重复Kill不重复事件、使用impact死亡表现组件、身体碰撞与模拟在死亡后释放。测试没有CampaignLevel，不存在关卡进度写入；测试偏好前缀为独立随机命名，没有重置正式PlayerPrefs。Play在反馈重启前退出。

真实玩家复合多边形回归包含紧通道静止安全、运动天花板夹压、初速度0和-15的实际物理下落落地，均通过。

最终恢复Level01、非Play、场景无未保存编辑、无编译中的任务。临时CrushDeath_Acceptance_Temp场景及meta已通过AssetDatabase删除。未更改关卡地形、玩家预制体或正式存档。既有无关未提交工作保留。备份在backup，源文件及证据SHA-256在manifest.json。
'''
(out/'README.md').write_text(report,encoding='utf-8');(out/'git-status.txt').write_text(subprocess.check_output(['git','status','--short'],text=True),encoding='utf-8')
with (root/'ai-handoff.md').open('a',encoding='utf-8') as f:f.write('\n\n2026-11-12 — User requests fatal crushing instead of recovery. Death.Awake now installs PlayerCrushDetection and disables pre-existing PlayerUnstuck. New body-only detector kills through existing Death.Kill on >.06 deep embedding or opposing contacts with >.05 closing speed; .02 contact skin, dot<-.65. Blade/triggers/own or ignored collisions excluded. No .65s wait/rewind. Unity compile0errors0warnings PASS; isolated physics11, actual Play integration10, actual player compound polygons6 checks PASS (27). No formal progress writes/reset, isolated Play scene deleted, editor restored Level01 stopped/clean. No terrain/prefab edits. Real Level03 riding/crush scenarios, rendered death presentation, full traversal/device/export NOT VERIFIED. Backup/evidence/hash: scripts/crush-death-20261112/README.md.\n')
files=[root/p for p in paths]+[p for p in out.rglob('*') if p.is_file() and p.name!='manifest.json']
(out/'manifest.json').write_text(json.dumps({'utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'head':subprocess.check_output(['git','rev-parse','HEAD'],text=True).strip(),'files':{p.relative_to(root).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in files if p.exists()}},indent=2),encoding='utf-8');print('27 checks PASS; report and hashes saved.')
