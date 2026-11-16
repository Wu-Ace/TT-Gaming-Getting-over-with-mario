# Odyssey PvP 联机与配置

本说明对应 `pvpnew` 分支。当前实现为 **Unity Gaming Services（UGS）Relay 短房间码联机**，并保留 **局域网 / IPv4 UDP 直连与 `ODP2-…` 邀请**。房主的游戏进程负责比赛模拟，每个房间最多 4 个席位，包含房主、真人客机和最多 1 个测试 Bot。

**当前配置状态（2026-11-16）：工程尚未绑定 Unity 云项目。** `ProjectSettings/ProjectSettings.asset` 中的 `cloudProjectId`、`projectName`、`organizationId` 为空，因此当前直接构建后，互联网建房会提示“此版本暂未配置在线联机服务”。下文的云项目配置和真实 Relay 联机验收仍需完成。

根目录 `pvp_local_plan.md` 保留了早期直连设计和当时的测试记录；当前联机入口、云配置要求及 Bot 行为以本 README 和现有源码为准。Photon、Steam、自建中转目前都未接入。

## 开发环境与依赖

使用 `ProjectSettings/ProjectVersion.txt` 指定的 **团结引擎 1.10.3 / 编辑器 `2022.3.62t15`**。本次验证平台是 Windows 桌面版；微信小游戏、WebGL、移动端不在本次验收范围内。

联机依赖已写入 `Packages/manifest.json` 和 `Packages/packages-lock.json`，无需照着新版本教程重复安装或升级：

| 依赖 | 当前锁定版本 |
| --- | --- |
| Netcode for GameObjects | `1.5.1` |
| Unity Transport | `1.5.0` |
| Relay | `1.0.5` |
| Authentication | `2.0.0` |
| Services Core | `1.14.0` |
| QoS | `1.3.0` |
| Newtonsoft JSON | `3.2.1` |

UGS 相关依赖使用 manifest 中已有的 `https://packages.unity.com` scoped registry，其余包保持工程原有来源。首次打开时等待包解析与编译完成。manifest 另有开发工具本地依赖 `file:../.codely-cli/extensions/TJGenerators/Packages/cn.tuanjie.ai.generators`；若报告该路径不存在，需要先恢复相应工具包，这与云项目绑定是两件事。

Unity 现在的部分在线教程针对 Unity 6 的统一 Multiplayer Services SDK。本工程仍使用独立 Relay SDK；阅读最新文档时不要直接替换为 `com.unity.services.multiplayer`。当前包的官方参考见 [Relay 1.0.5 文档](https://docs.unity3d.com/Packages/com.unity.services.relay@1.0/manual/index.html)。

## 配置互联网联机：由一名项目维护者完成

玩家拿到已配置的构建后，不需要注册 Unity 开发者账号，也不需要配置公网 IP 或路由器端口映射。维护者需要能够登录 Unity Cloud，并拥有目标组织和云项目的配置权限。

1. 登录 [Unity Dashboard](https://cloud.unity.com/)，在正确组织下创建或选择本游戏的云项目。确认项目名称和 Project ID，避免把房主、客机分别绑定到不同项目。
2. 使用指定编辑器打开本工程，在 **Edit → Project Settings → Services** 中选择已有云项目或创建并关联新项目，完成 Link。编辑器中的项目名称、组织和 Project ID 应与 Dashboard 一致。官方步骤见 [关联本地工程与 Unity 云项目](https://docs.unity.com/en-us/cloud/projects/configure-project-for-unity-cloud)。团结编辑器界面文字可能略有不同，以实际 Services 配置为准。
3. 确认该云项目的 Authentication 服务可用。本工程已经调用 `UnityServices.InitializeAsync()` 与 `AuthenticationService.Instance.SignInAnonymouslyAsync()`；采用匿名认证，无须添加 Steam、Google 等登录提供方，也无须让玩家填写账号密码。参见 [Authentication 初始化](https://docs.unity.com/en-us/authentication/get-started) 和 [匿名登录](https://docs.unity.com/en-us/authentication/use-anon-sign-in)。
4. 在同一云项目的 Multiplayer / Relay 产品页，按当前 Dashboard 的引导启用 Relay；若已经启用，无须重复操作。核对当前环境和服务状态。本工程没有显式调用 `SetEnvironmentName`，初始化沿用项目配置，未指定时使用 SDK 默认 `production` 环境。Relay 的作用是实际转发游戏流量，参见 [通过 Relay 连接玩家](https://docs.unity.com/en-us/mps-sdk/connect-players)。
5. 回到编辑器保存配置，确认 `ProjectSettings/ProjectSettings.asset` 的 `cloudProjectId` 不为空，并与 Dashboard 的 Project ID 一致；运行时 `Application.cloudProjectId` 也应有值。这里只检查“已填写”还不能证明服务可用，必须继续进行下文的实际建房与加入验收。
6. 用这份已绑定工程重新构建并把**同一份新构建**发给所有测试玩家。旧的未绑定可执行文件不会自动获得新配置。

工程已经在运行时创建 `NetworkManager` 和 `UnityTransport`，无须再向 MainMenu 场景手工添加第二套网络组件，也无须填写 API Key、服务账号密钥或个人登录令牌。云项目标识是配置，私密凭据不要放进源码、README 或提交记录。

如果维护者无法登录 Unity Cloud，当前 Relay 配置就尚未完成；本地可继续测试局域网 / 直连与 Bot，但不能据此宣称互联网联机已经可用。

## 玩家使用流程

云项目配置和构建完成后：

1. 房主进入 **联机 → 创建房间**，输入房名并创建。默认走 Relay，不用填写 IP 或端口。
2. 房主点击 **复制房间码**，把当前房间的短码发给朋友，并保持游戏和房间运行。
3. 客机进入 **联机 → 加入房间**，粘贴短码并加入。程序接受首尾空格和小写输入，会先规范化再处理。
4. 房主选关，客机点击准备，房主开始比赛。房主重新选关后，真人客机需要重新准备。

房间码对应当前房主仍在运行的房间，不是永久邀请码。房主离开、关闭游戏或重新建房后，应重新获取当前房间码。本版没有主机迁移；房主退出会中断现有联机。

## 局域网 / 直连与旧邀请

从 **联机 → 创建房间 → 局域网 / 直连** 进入此模式。局域网可把分享地址留空，程序尝试选取本机 IPv4；电脑有多块网卡或虚拟网卡时，应核对并填写客机实际可达的那张网卡地址。默认本地 UDP 端口为 `7777`，分享端口留空时使用同一个端口。

创建后复制**完整的 `ODP2-…` 邀请**，客机仍从“加入房间”粘贴。此入口兼容当前格式的旧直连邀请；单独输入 `192.168.x.x:7777` 或公网 `IP:端口` 并不是有效邀请。更新邀请或重新建房后，旧邀请不应继续使用。

`192.168.x.x`、`10.x.x.x` 等私网地址只在可达的私有网络内有意义，不能直接供异地互联网玩家连接。直连模式跨公网还需要房主确有可入站的公网 IPv4、正确的 UDP 映射和防火墙放行；运营商 CGNAT 下通常无法仅靠家用路由器端口映射解决。本模式没有自动打洞、中转回退或自动端口映射功能。普通异地玩家应使用已配置的 Relay 短码流程。

## 构建与版本一致性

- 双方使用相同源码、资源、依赖及版本生成的构建，最稳妥的做法是直接分发同一个构建目录。
- 游戏会校验 `Application.version` 和内部 `ContentRevision` 的兼容标记；仅把版本号手工改成一样，不能保证不同资源或代码一定兼容。
- 以现有 `ProjectSettings/EditorBuildSettings.asset` 为准保留构建场景。当前包含 MainMenu、Tutorial、Level01–Level10 及 PvpArena；缺少所选关卡会导致加载失败。
- 当前默认 Relay 传输是 **DTLS / UDP**。不要把这个桌面版本的验证结果直接推广到浏览器或微信小游戏。

## 测试 Bot

房主可在等待房间添加最多 1 个测试 Bot。Bot 占用四个席位中的一个，自动准备，便于单个真人开赛和测试碰撞；它不是自动通关 AI，也不参与排名或阻止真人完成后结算。

当前 Bot 关闭玩家输入和桨控制，保留比赛阶段的刚体物理模拟，会受重力自然落到地形上，并可被玩家轻推。出生点原本就在地面附近时不会出现明显下落。加载、倒计时或结算中的冻结属于对局流程，不应当作比赛阶段悬空故障。

## 常见问题

| 现象 | 检查方法 |
| --- | --- |
| “此版本暂未配置在线联机服务” | 核对工程云绑定及同一云项目的 Relay 状态；若刚配置过，重新构建并替换双方旧程序。 |
| “请输入有效的房间码” | 输入服务返回的短码或完整 `ODP2-…` 邀请，不要输入裸 IP、端口或说明文字。 |
| “邀请无效或已失效” | 这是直连邀请路径的错误；确认复制了房主当前完整邀请，房主未更新邀请或重建房间，双方版本一致。 |
| “房间码无效或已失效” | 确认房主仍在当前房间，重新复制短码；检查双方是否来自同一个 UGS 云项目和配置。 |
| “房主和客机需要使用相同的游戏版本” | 给双方重新分发同一份构建，不要混用旧包。 |
| “无法连接在线联机服务”或 Relay 连接超时 | 检查 Authentication、Relay 状态、网络和控制台的数值错误码；能打开网页不代表 UDP 中转通畅。 |
| 直连连接超时 | 核对邀请里的可达地址、UDP 端口、防火墙及所在网络；私网地址和 CGNAT 问题不会因为邀请码格式正确而消失。 |
| “尝试次数过多” | 停止重复点击，稍候再试。 |
| 房间满员或无法开赛 | 房间最多 4 席，Bot 也占席；至少 2 名成员，真人客机需准备，房主才可开始。 |
| 所选关卡加载失败 | 核对双方构建包含相同场景，并保留现有 Build Settings 场景列表。 |

管理网络防火墙时，按 [Unity 官方 Relay 端口与协议](https://docs.unity.com/en-us/mps-sdk/networking/ports) 核对客户端到服务端的出站访问。当前官方列出服务 API 使用 HTTP/HTTPS，Relay 常用 UDP `37000–37100`，QoS 使用 UDP `7778`；这不是要求玩家给家用路由器配置这些入站端口映射。

## 验证记录与待完成项

截至 2026-11-16，当前源码与依赖已和验证构建副本核对一致，已完成：

- 指定版本团结编辑器编译及 Windows 开发构建。
- 同机两个独立游戏进程通过 UDP 直连建房、使用完整邀请加入并同步成员，房主和客机均通过。
- 未绑定云项目时的明确错误提示、短房间码输入路径、局域网入口及相关 UI 检查。
- Bot 在 Tutorial 和 Level01–Level10 共 11 个关卡的落地与稳定性验证，结果 `failures=0`；另验证了落地后的轻推接触，并检查实际下落与落地画面。

**尚未完成：** 当前真实云项目的成功绑定与 Relay 启用、真实 Relay 建房/短码加入/数据往返、两地不同网络之间的实际对战、实际跨网延迟及断线表现。已有的本机 UDP 验证不能替代这些测试。

完成云配置后，先用两个独立客户端验证建房、短码加入、准备状态往返和退出；同机多开自动测试应使用不同 Authentication profile。随后由两台位于不同网络的电脑使用同一构建实际开赛，检查关卡加载、角色与碰撞同步、完赛、返回房间和房主退出。在真实 Relay 测试通过前，不把“代码接入完成”表述为“异地联机已经验证通过”。

## 代码位置

| 文件 | 职责 |
| --- | --- |
| `Assets/Scripts/Pvp/Networking/PvpRelay.cs` | UGS 初始化、匿名认证、Relay 分配、短码加入及错误映射。 |
| `Assets/Scripts/Pvp/Networking/PvpTransport.cs` | NGO/UTP 传输、Relay 与直连配置、邀请兼容校验、成员批准及消息收发。 |
| `Assets/Scripts/Pvp/Match/PvpSession.cs` | 房间、准备、选关、比赛与结果状态。 |
| `Assets/Scripts/Pvp/Match/PvpActor.cs` | 角色物理、输入和测试 Bot 行为。 |
| `Assets/Scripts/GameMenuUI.Pvp.cs` | 建房、加入、局域网入口及房间菜单。 |
| `Assets/Resources/Localization/` | 简体中文、繁体中文与英文联机文案。 |
