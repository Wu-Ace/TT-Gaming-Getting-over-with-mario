# 第 5 关原版地形复刻

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
