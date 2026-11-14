from pathlib import Path
import json,hashlib,datetime,subprocess
r=Path.cwd();p=r/'scripts/crush-landing-fix-20261112';groups=[json.loads((p/f).read_text()) for f in ['physics-results.json','actual-player-results.json','play-results.json']];assert all(x['pass'] for g in groups for x in g);assert not (r/'Assets/Scenes/CrushDeath_Acceptance_Temp.unity').exists()
report='''# 修复普通掉落被误判为挤压 — 2026-11-12

用户报告偶尔从空中掉下会“摔死”。当前没有按落差/落地速度扣血的系统；确认之前新增挤压检测存在单面深度嵌入>0.06即死亡的分支，因此一次落地的短暂地形重叠也可能被当作致命挤压。没有采集到用户那一次的具体死亡现场，不能断言其一定由此分支触发。

## 修改

只修改PlayerCrushDetection.cs。删除单面深嵌入立即死亡规则。现在必须同时满足：身体靠近两面相对的有效实体碰撞面，且两个障碍物沿夹压轴的相对运动确实使空隙收拢，才触发Death.Kill。玩家自身的下落速度不参与障碍收拢速度。静止地形/同速平移的两平台不会因玩家落地而被认作夹压；真实收拢的平台夹压仍死亡，仍不自动脱困。

增加障碍物位移/旋转采样：MovePosition在物理解算后可能报告速度0，读取实际位移作为回退，避免删除深嵌入规则后漏掉真实夹压。连续碰撞查询同一个物理步重复使用采样，不让玩家复合碰撞体将采样清零。

死亡水位/越界线、敌人、尖刺死亡逻辑没有更改。原水位判断依据身体位置是否低于waterHeight-0.2；没有水时依据配置的deathThreshold。它与落地速度无关。

## 验证

- Unity刷新编译：0错误、0警告。
- 13项隔离几何/真实Physics2D夹压回归：通过。包括单面深嵌入不死亡、高速下落位于静止接触面之间不算夹压、同速两平台不算夹压，左右/上下真正收拢与实际运动墙夹压仍能识别。
- 10项真实玩家复合碰撞体检查：通过。以0、-15、-30、-50单位/秒初速度落地不误判，并实际落在地面上；运动天花板夹压仍被识别。
- 10项隔离实际Play集成：通过。普通接触存活，实际运动墙夹压触发一次死亡，原有impact反馈及死亡释放碰撞流程正常，重复Kill不重复事件。
- 调试过程中，去掉深嵌入后曾出现MovePosition速度0造成夹压漏判，已补位移采样并重新通过上述全部33项。solver-trace仅是中间诊断，不是最终通过证据。
- 用户具体出问题地点现场重现、完整关卡试玩、实际死亡画面渲染、设备/导出：未做。

本轮测试场景无CampaignLevel、偏好前缀随机隔离、不改写或重置正式存档；测试场景及meta已删除，最终恢复原Level01 Edit模式、场景干净。之前未提交的工作保留。测试脚本/最终结果/源码SHA-256均在本目录，修改前源码在backup。

本报告取代上一轮“深度嵌入即死”的规则说明。最终证据：physics-results.json、actual-player-results.json、play-results.json、refresh-result.json、cleanup-result.json。
'''
(p/'README.md').write_text(report,encoding='utf-8')
with (r/'ai-handoff.md').open('a',encoding='utf-8') as f:f.write('\n\n2026-11-12 — Landing false-positive correction supersedes crush-death deep-embedding rule: user reports occasional falling death. Removed single-surface >.06 embedding kill. PlayerCrushDetection now requires opposing body contacts AND relative obstacle motion closing their gap, excludes player falling velocity. Uses observed obstacle position/rotation when MovePosition reports zero post-solver velocity. Static landings and same-velocity platforms safe; real squeezing remains fatal without recovery. Unity compile0errors0warnings; physics13 + actual compound polygon10 (landing speed0/-15/-30/-50) + actual Play death integration10 PASS. User exact incident not reproduced; water/death-line/enemy/spike logic unchanged. Restored Level01 clean/stopped, isolated scene deleted, no formal saves reset/written. Evidence/backup/hash scripts/crush-landing-fix-20261112/README.md.\n')
(p/'git-status.txt').write_text(subprocess.check_output(['git','status','--short'],text=True),encoding='utf-8')
files=[x for x in p.rglob('*') if x.is_file() and x.name!='manifest.json']+[r/'Assets/Scripts/Character/Lifecycle/PlayerCrushDetection.cs',r/'Assets/Scripts/Character/Lifecycle/Death.cs',r/'Assets/Scripts/Character/Lifecycle/FallDetection.cs',r/'Assets/Prefabs/Character.prefab',r/'ProjectSettings/ProjectVersion.txt']
(p/'manifest.json').write_text(json.dumps({'utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'head':subprocess.check_output(['git','rev-parse','HEAD'],text=True).strip(),'files':{x.relative_to(r).as_posix():hashlib.sha256(x.read_bytes()).hexdigest() for x in files}},indent=2),encoding='utf-8');print('33 checks passed; final report saved.')
