> 已被用户后续要求替代：当前为纯原版地形，见 ../jk-original-20261111/README.md。以下是历史并集版本记录。

# 第 5 关：Jump King 原版地形 Tilemap（2026-11-11）

已保存 Assets/Scenes/Level05_Cave.unity。读取用户安装目录 Content/level.xnb 的未压缩 Texture2D，按原生每屏 60×45 格、每列 13 屏的排列提取原版前 14 屏（森林至塔楼）。原游戏安装目录只读；项目继续使用现有岩石素材及背景，没有导入原版前景美术。

## 地形规则

- 原版黑色实地和红色斜坡占地共 7,565 格；按连续红色色带统一求斜线，114 个边界三角格，其余内部/短端点使用完整格。其他色码不作为静态地形。
- 当前场景的额外地形按楼层归一化，对每格九点取样，取与原版的并集，保留 4,182 个额外格，共 11,747 格。这里的保留是对齐格子的取样近似，不是保持旧 Sprite 的原世界坐标或逐顶点形状。
- 世界格子 0.4×0.4 米；宽 24 米、高 252 米，y=-3.6 至 248.4。旧版把参考图压扁，本次恢复方格比例，因此高度由约 115 米增加到 252 米。出生点、目标和旗子已经随地形重新安置。
- 所有静态地形统一为 Level/Terrain/Jump King 01-14 - paint here；仅一个 Tilemap 和一个 TilemapCollider2D。引擎 CompositeCollider2D 漏掉两个孤立实格，因此使用标准 TilemapCollider2D；已重新检查覆盖。
- JumpKingRockTile 让岩石规则把斜坡识别为相邻地形，避免内侧被当成外边缘裁出锯齿。斜坡是精确三角碰撞，不用拉伸 Sprite 拼装。

## 继续编辑

1. 打开 Level05_Cave，选择 Level/Terrain/Jump King 01-14 - paint here。
2. 打开 Window > 2D > Tile Palette，选择 Jump King Fifth Palette。Palette 预制件：Assets/Tiles/JumpKingFifth/Jump King Fifth Palette.prefab。
3. 第一格 Terrain 是自动拼接岩石；其余四格 Slope1–4 是左下、右下、左上、右上实心三角。直接绘制/擦除，然后保存场景。
4. Grid 本地格尺寸是 0.8，Transform X/Y 统一缩放为 0.5，所以世界格子为 0.4。不要独立改变 X/Y 比例。
5. InstallJumpKingFifth.Apply 仅供初次重建，会替换地形；正常微调只改 Tilemap，不要再次运行重建菜单。原有 Play Terrain Save 工具的 Tilemap 绘制保存没有在本轮验证，请在编辑模式绘制。

## 本轮证据与边界

- 真实编辑器成功编译并执行新脚本；刷新阶段仍报告既有 Tilemap GridPaintingState 的 ScriptableSingleton 提示，不能声称 Console 全干净。早期失败报告是修复过程，不是最终结果。
- 最终保存后的 11,747 实格、26,053 空格中心分类检查：0 差异。最终 114 个三角格在真实 Play 中分别测试内部和外部：0 差异。
- 通道几何检查：底部到顶部连通；按 3×4 格（约 1.2×1.6 米）的保守空间检查也能连通。这没有验证锤子动力、实际跳跃距离和所有目标可达性。
- 已检查全部 14 屏编辑器相机渲染（floor-01 至 floor-14、all-floors.png），以及真实 Play 的出生点、下水道斜坡、塔楼画面（play-0 至 play-2）。编辑器相机渲染不是桌面截图。
- Play 出生时角色未死亡。最终相机截图测试冻结刚体并移动相机，不是攀爬通关；未修改 PlayerPrefs/正式存档。
- 较早的 Play/刷新中出现 TemporaryStrength.cs:115、TemporaryGrowth.cs:122 的既有空引用；最终 capture-result.json 未记录新报错。角色相关并行工作仍在进行，不将本轮地形检查当成整个游戏验收通过。
- 完整锤子攀爬、碰撞接缝手感、目标可达性、设备与导出：未做。

## 回退与版本

backup/Level05_Cave.unity 是本轮首次修改前的场景副本。plan.json 保留原版和额外格的分别记录；current-raster.txt 是迁移前地形取样，union-preview.png 中蓝色为额外格、灰色为原版实格、金色为边界斜坡。manifest.json 记录实际源码、资源、场景与证据 SHA-256。

用户编辑器被其他并行工作继续使用；最终场景已落盘，不保证结束时用户当前窗口仍停在第 5 关。
