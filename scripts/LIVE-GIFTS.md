# 直播礼物功能

进入 OneOneScene 的 Play 模式会自动安装礼物运行组件，无需修改场景。

## 按键测试
- 1：变大；2：变小；3：向上；4：向左；5：向右；6：向下击飞。
- 7、8、9、0、F1–F10：其余14个示例礼物，默认循环上述六种效果。
- F12：显示/隐藏按键提示。隐藏面板后按键仍生效。
- Backspace：清除持续礼物效果和待执行击飞。
- Game 窗口需要获得键盘焦点。

Tools > Live Gifts > Edit Gift Configuration 创建/选择配置。在 Inspector 修改最多20条映射的 giftId、effect、scale、duration、speed、testKey。Gift ID 必须唯一，测试键建议唯一。配置保存在 Assets/Resources/GiftConfiguration.asset。未创建配置时使用默认20条测试映射。

## 导出开关
Tools > Live Gifts > Include Keyboard Tests In Build：勾选后，当前构建目标组包含 GIFT_TESTING 编译符号；取消后，该组导出不包含 GiftKeyboardTester 类型、按键处理和测试面板。编辑器始终允许测试。切换构建平台后分别检查该菜单。使用正常 Build/Build And Run 即可。

## 直播桥接接口
GiftRuntime.Instance.ReceiveGift(eventId, giftId, quantity, viewer)。必须在 Unity 主线程调用。真实 TikTok 网络接入尚未实现，桥接层需将礼物连击累计数转换为增量，并为每次增量提供稳定唯一ID。游戏保留最近2048个事件ID去重，重开场景后清空。

同倍率体型延长时间，上限30秒；相反倍率覆盖。礼物倍率叠加在原有蘑菇体型之上，不改变蘑菇质量与音效规则。击飞按质量施加同等速度增量，间隔0.25秒，最多排40次，单条消息最多计20份。超过限制的部分丢弃。暂停/死亡时拒绝礼物；重开不补发。

## 验收（2026-11-04）
引擎 Tuanjie 1.10.3 / Unity 2022.3.62t15。此目录无Git仓库。
使用引擎 Roslyn 和当前工程依赖，编译全部玩法源码及新增编辑器代码：release/testing/editor 三个分支均通过，只有现有序列化字段未赋值警告。日志位于 scripts/gift-evidence。release DLL 无 GiftKeyboardTester 类型名；testing/editor 包含。
未启动第二个编辑器。Unity导入、真实Play按键、物理关节/碰撞表现、完整导出包、目标设备与真实直播尚未验证。重点试玩：蘑菇期间大小切换、连续击飞、体型恢复、死亡重开，以及带/不带测试的两种导出。编译通过不能替代这些检查。
