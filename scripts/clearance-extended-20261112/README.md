# 通道几何补充验证 — 2026-11-12

本轮未修改任何场景、玩家预制体或玩法源码。先前记录的玩家/场景/控制器SHA-256全部仍相同。Unity最终恢复Level01、非Play、场景无未保存改动、临时探针0个。

## 第1、5关：完整几何路径

根据实际碰撞网格生成固定身体+桨姿态的平移路径；第5关限制x=0.8–23.2，路径保持在塔内，覆盖起点至约244高的终点。生成的路径是几何见证，不是原设计详细route，也不是控制器可执行动作。

- 第1关：3651个0.1间距格点，压缩成160条直线，对3个实体多边形执行480次Unity Collider2D.Cast连续扫掠，零碰撞。另完成21900个实际位置采样，最小实际间隙0.02540。
- 第5关：2869个格点，382条直线，1146次连续形状扫掠，零碰撞。线段端点最小间隙0.08513；此数是端点距离，不能称为整条路径的最小距离。连续扫掠检查线段内部是否碰撞。
- path锚点相对spawn/goal目标附近的偏移约0.016–0.034单位；搜索使用Cauldron包围盒中心作为参考。
- 扫掠对象是启用、实体、静态地形。Ground层6包含本轮两关所有永久静态障碍；Closed door seal由CampaignInteractable运行时禁用，因此排除。Character层7自碰撞在当前物理矩阵中关闭。

证据：full-paths.json、compressed-paths.json、sweep-results.json、verify-sweeps.csx；Level01-witness.png和Level05_Cave-witness.png。旧verify-paths逐点长任务的客户端等待超时，full-path-results.json只有第1关结果，不能作为第5关通过证据。第5关使用后续独立完成的连续扫掠结果。

## 第4关：补验桨

上轮三个局部身体路径各自寻找一个可保持的桨朝向。Hinge无角度限制；Slider当前translation=0，在[-0.25,0.975]限制内。本轮保持该伸缩位置，绕真实hinge锚点旋转桨几何；没有扩大可动范围。

| 局部路径 | 桨相对原姿态旋转 | 细分位置检查次数 | 桨对地形最小间隙 |
| --- | ---: | ---: | ---: |
| 1 | 225° | 410 | 0.32548 |
| 2 | 10° | 365 | 0.56913 |
| 3 | 170° | 345 | 0.65189 |

这些桨姿态分别与上轮已验证的身体路径组合，说明三个局部窄口不是身体能过而桨必然无处容纳。未验证不同局部姿态之间的完整转动过程，也未验证主动借力。

## 第2、3关：移动平台的身体容纳空间

按当前VerticalRidePlatform/OrbitRidePlatform的实际运动公式，在每个平台一个周期内采样64个时刻；同时移动本场景其他运动平台到同一绝对时间。身体放在平台顶面+0.025余量，在5个横向偏移位置检查实际多边形对静态地形、其他运动平台和未触发下落平台的距离。

这是Edit模式几何采样，没有实际运行脚本FixedUpdate，没有碰撞响应、触发或正式存档操作。每个时刻只要求至少一个所测横向位置能容纳身体；不证明这些位置之间可连续移动，也不证明跳上/跳离平台可行。

| 场景 | 平台 | 有身体空间的采样时刻 / 64 |
| --- | --- | ---: |
| Level02_Jungle | Vertical moving platform | 64 / 64 |
| Level02_Jungle | Vertical moving platform | 64 / 64 |
| Level02_Jungle | Vertical moving platform | 64 / 64 |
| Level02_Jungle | Vertical moving platform | 64 / 64 |
| Level03_Beach | Orbit platform 0 | 64 / 64 |
| Level03_Beach | Orbit platform 1 | 52 / 64 |
| Level03_Beach | Orbit platform 2 | 44 / 64 |
| Level03_Beach | Orbit platform 3 | 56 / 64 |
| Level03_Beach | Orbit square 50 0 | 34 / 64 |
| Level03_Beach | Orbit square 50 1 | 47 / 64 |
| Level03_Beach | Orbit square 50 2 | 55 / 64 |
| Level03_Beach | Orbit square 50 3 | 51 / 64 |
| Level03_Beach | Orbit square 58 0 | 52 / 64 |
| Level03_Beach | Orbit square 58 1 | 59 / 64 |
| Level03_Beach | Orbit square 58 2 | 55 / 64 |
| Level03_Beach | Orbit square 58 3 | 56 / 64 |

第3关一些环绕平台存在身体站位与其他碰撞体重叠的相位；这是具体的潜在挤压/换位风险，不等于全路线必定无法通过。没有任何平台在全部64个采样时刻都无法容纳身体。本轮未据此调整平台运动或关卡难度。

## 本轮结论与剩余范围

本轮新增验证未确认必须修改的永久几何堵点。全关操作通关、重力/力矩/支撑与借力、完整连续的身体-桨关节动作搜索、环绕平台实际骑乘/换乘、下落平台的触发与恢复、设备/导出均未验收。本报告不能替代这些结论。

检查脚本与报告在scripts/clearance-extended-20261112；上一轮证据在scripts/clearance-20261112。先前已有未提交工作保留。新脚本不写PlayerPrefs、不进入正式Play，不改关卡资产；检查时会打开场景，要求当前无未保存编辑，finally恢复原场景。
