# Windows 多语言本地化 — 2026-11-14

## 已接入

29 种语言，P0 12 种、P1 17 种；每种 22 个条目，覆盖主菜单、设置、分辨率、选关、当前五关名称、收集统计、加载、进度、通关标题和加载失败提示。

设置 → 语言：原生语言名称列表，支持滚动、即时切换、保存选择。首次使用系统 UI 语言，无法匹配时使用英语。语言设置保存为原有 UI preference prefix 下独立的 `language.v1`；关卡路径、礼物 ID 和原有存档键不变。

- 简体：`zh-Hans`（兼容 `zh`、`zh-CN`、`zh-SG`）。繁体：`zh-Hant`（兼容 `zh-hant`、`zh-TW`、`zh-HK`、`zh-MO`）。
- 葡萄牙语：`pt-BR`，其他葡萄牙语地区暂统一回退到巴西葡萄牙语。
- 英语原文为维护基准；品牌标题 `GET OVER WITH / ODYSSEY` 保留原文和原字体。
- 仅开发环境出现的 F11 飞行提示、F12 礼物测试面板、日志和不可达的旧界面不属于玩家翻译内容。

## 实现选择与平台边界

本轮保留 IMGUI 原有美术、倾斜变换、裁剪和按钮操作，接入轻量 JSON 语言表；没有增加 Unity Localization 包或迁移整套 UGUI/TMP。

原因：当前项目的主要 UI 都由 IMGUI 即时绘制；仅更换 TMP 字体不能解决全部复杂文字整形。当前交付目标为 Windows，使用 Windows GDI 的复杂文字支持渲染逻辑 Unicode 文本，再作为透明纹理画回原有界面。RTL 保持逻辑顺序，数字参数使用方向标记。文字按实际整形后的尺寸换行、缩小，以约 24 MiB 为缓存清理阈值，切换语言时清理缓存。

目标为 Windows 10/11。其他平台只有旧字体的基本回退绘制，需要实现对应整形后端并重新验收后才能声明支持；不能直接把本轮结果用于小游戏或 macOS。

新增字体文件合计约 63.76 MiB。Noto Sans、Arabic、Hebrew、Thai、Myanmar 和中日韩字体随 StreamingAssets 分发；不复制系统商业字体。字体公开来源和 SIL OFL 授权在 `fetch-fonts.ps1` 与 `Assets/StreamingAssets/LocalizationFonts/OFL*.txt`。少量通用符号使用 Windows 的 Segoe UI Symbol 回退；首轮字形检查包含该回退。

Windows 文字处理依据：
- https://learn.microsoft.com/en-us/windows/win32/gdi/complex-scripts
- https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-drawtextw

## 文案维护

- 运行时语言表：`Assets/Resources/Localization/*.json`。每条有 `key`、`text`、`context`。
- 当前初稿来源：`translations.tsv`。`build-tables.ps1` 会从 TSV **重新生成并覆盖** JSON；人工审校时应同步维护源表，避免重建丢失改动。
- 参数名 `{number}`、`{progress}`、`{coins}`、`{total}`、`{stars}` 不能改名或漏掉，可以根据语序调整位置。
- 新功能使用 `GameLocalization.Get(key)`；数字使用 `Format`。资源路径、事件 ID、日志不要当作译文 ID。
- 一条文字源文更改后，要将各语言对应条目标为待重新审校；本轮全部译文为初稿，没有母语审校签收。

术语：Level=关卡/Stage（表示一关），Progress=当前位置对应路线进度，Sound FX=效果音，Music=背景音乐。关卡名称是描述性名称，可翻译；玩家名、礼物 ID 不翻译。操作文案使用简短、直接的语气。

## 验收与证据

- 编辑器实际编译：`refresh-result.json`；0 错误，原有 `ArmsSimulation.angularDamping` 未使用警告 1 条。
- `table-check.json`：29 语言 × 22 条目的完整性、参数一致性、别名和字体覆盖检查。
- `text-samples/`：逻辑顺序的阿拉伯语、希伯来语、泰语、缅甸语等整形样图。
- `ui/`：29 语言分别在实际 Play 模式下生成 home、levels、settings、loading、hud、completion-preview 截图；列表和分辨率页另有截图。
- `ui/render-results.txt`：菜单渲染、隔离偏好写入/读回。
- `ui/game-render-results.txt`：加载/HUD/通关预览状态说明。通关画面通过临时设置 Won 标志生成，**不代表物理通关测试**。
- 检查运行使用独立 preference prefix，不清空正式存档。结束后恢复语言、prefix、时间倍率，并退出 Play，恢复原编辑场景。

检查范围的限制：截图生成不等于每种语言完成了人工审校。代表性复杂脚本及长文本截图已打开检查；真实鼠标逐行选择、低分辨率全覆盖、Windows 导出包启动和母语审校尚未完成。项目未发布。

## 复现

项目原有 Codely 本地编辑器桥：`node scripts/localization-20261114/bridge.cjs <request.json>`。
`check-tables.csx` 为数据/字体检查；`prepare-play.csx` 要求原场景干净且仅打开一个场景；`check-ui.csx`、`check-game.csx` 为隔离 Play 检查。不要在用户未保存的现场直接执行场景切换。

原文件备份：`GameMenuUI.cs.before`、`CampaignLevel.cs.before`。本轮证据与源码散列见 `manifest.json`。


## 字体后续调整
中英文（en、zh-Hans、zh-Hant）按用户要求恢复猫啃什锦黑；其他语言不变。原 TTF 和原授权一并随 Windows 私有字体分发，新增约 11.73 MiB；以上旧字体包体和截图以 scripts/maoken-locales-20261114 证据为准。

阿拉伯语后续按用户提供的 Lalezar.zip 改用 Lalezar Regular；TTF 与 OFL 授权已随包加入。证据：scripts/lalezar-ar-20261114。

日语后续按用户提供的 Reggae_One.zip 改用 Reggae One；字体与 OFL 授权已加入，实际界面证据见 scripts/reggae-ja-20261114。

韩语按用户提供的 Dokdo.zip 改用 Dokdo Regular，字体与 OFL 授权已加入；实际界面证据见 scripts/dokdo-ko-20261114。

泰语按用户提供的 Kanit.zip 改用 Kanit Regular，字体和 OFL 授权已加入；证据见 scripts/kanit-th-20261114。

缅甸语使用用户字体包中的标准 Regular（183164 字节），文件 NotoSansMyanmar-User-Regular.ttf；旧同名字体停用。已验证实际选中版本，证据 scripts/myanmar-font-20261114。

希伯来语按用户提供的 Suez_One.zip 改用 Suez One Regular，保留 RTL 排版；字体与 OFL 授权已加入，证据 scripts/suez-he-20261114。
