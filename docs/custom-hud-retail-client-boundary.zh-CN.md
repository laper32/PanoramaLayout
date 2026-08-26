# CS2 `custom_hud_layout` 普通客户端能力边界实测

> [!IMPORTANT]
> **历史版本记录，并包含一项已确认的实验设计缺陷。** 本文记录的是 build 2000891 发布之前的客户端行为；旧 Tool Mode 的 `Addons cannot add layouts` 转储仍是当时 build 的有效证据。但普通客户端自定义负例向动态实体传入 `.xml`，阳性对照传入 `.vxml_c`，没有控制运行时资源名 suffix。2026-08-26 的实验 G 已证明 build 2000891 中 `loadout.vxml_c` 可以由 Tool Mode addon 跨到 `de_dust2` 完整显示，因此本文不能继续用于证明当前地图资源归属边界。新版勘误和端到端证据见[《build 2000891 重测记录》](custom-hud-build-2000891-retest.zh-CN.md)。

测试日期：2026-08-26  
测试目标：普通、零修改的 CS2 客户端连接运行 ModSharp 的社区服务器，并在任意官图上显示服务器提供的完全自定义 Panorama HUD。

> **本文所测旧 build 的结论：该目标不成立。** 服务端可以动态创建并控制 `custom_hud_layout`，但当时的普通客户端不能加载由 Workshop、MMR 或独立 addon 提供的自定义 VXML/CSS。普通客户端只能使用 Valve 基础游戏中已经存在、且能通过 CustomHud 验证器的少量布局；完全自定义布局在该 build 中属于当前地图内容或受控客户端能力，不是地图无关的社区服务器能力。

这不是“实体能不能创建”的问题，而是“客户端是否信任并允许实例化该布局资源”的问题。

## 一、实测结果

| 场景 | 服务端实体 | 客户端拥有文件 | 客户端允许加载 | 结果 |
|---|---:|---:|---:|---|
| 普通客户端 + Workshop/MMR 分发的自定义 VXML | 成功 | 是 | 否 | 加载失败 |
| Tool Mode + 独立 `-addon panorama_layout` | 成功 | 是 | 否 | 客户端致命错误 |
| 普通客户端 + Valve 内置 `btn_alert.vxml_c` | 成功 | 是（基础游戏） | 是 | **实际显示成功** |
| Valve `script_zoo` + 地图拥有的 `welcome.vxml` | 成功 | 是（地图包） | 是 | 官方样例可用 |
| 修改客户端 `AllowCustomGameUI` | 不属于本测试目标 | 可用 | 取决于修改 | 不是零改客户端方案 |

### 证据索引

| 编号 | 证据 | 证明内容 |
|---|---|---|
| E1 | 服务端生成实体日志 | ModSharp 创建实体、写入 layout 路径成功 |
| E2 | 普通客户端三条 CustomHud/Panorama 警告 | 客户端收到请求，但拒绝创建或验证 addon layout |
| E3 | ResourceCompiler `2 compiled, 0 failed` | 排除源文件没有编译或编译失败 |
| E4 | Tool Mode 客户端致命错误弹窗及两份 `_error.mdmp` | 独立 addon 添加 Panorama layout 被加载器明确禁止 |
| E5 | 公版 `IEntityManager.SpawnEntitySync` 创建实体后，普通客户端出现 `btn_alert` 橙色底板 | 实体、网络同步、公版 ModSharp API 和客户端 CustomHud 渲染链路均正常 |
| E6 | 官方 `script_zoo.vmap`、`map_asset_references` 与地图 VPK | Valve 样例的 VXML 属于当前地图编译依赖 |

因此，以下四件事必须分开讨论：

1. 服务端能否创建网络实体；
2. 客户端是否取得资源文件；
3. 资源是否属于客户端允许的信任域；
4. XML 是否通过 `CustomHud` 的内容验证。

前两项成功，不代表后两项成功。

## 二、服务器端能力已经确认

ModSharp 可以在任意当前地图动态创建 `custom_hud_layout`。自定义资源探针曾产生以下服务端日志（证据 E1，保留原始时间、模块和地图上下文）：

```text
L<CoreCLR> [08/26 00:45:00] | Information |
PanoramaLayout.PanoramaLayoutPlugin | de_dust2
Spawned custom_hud_layout entity 303:
target=swift_menu_custom_hud,
layout=panorama/layout/custom_game/swift_menu_custom_hud.xml
```

这证明：

- 实体类存在；
- 实体可以由插件动态生成，不要求 Hammer 预放；
- layout 路径能够写入实体并同步；
- 问题不在 ModSharp 的实体创建阶段。

但服务端日志只能证明服务端状态成立，不能证明客户端成功创建 Panorama 面板。

## 三、Workshop/MMR 解决分发，不解决授权

测试使用 Workshop 项 `3789924061` 向客户端提供：

```text
panorama/layout/custom_game/swift_menu_custom_hud.vxml_c
```

普通客户端收到实体后报告（证据 E2，按客户端控制台原文逐行转录）：

```text
Failed to create 'panorama/layout/custom_game/swift_menu_custom_hud.xml':
client disallowing panorama layout file creation

[custom_hud] Failed to load layout
'panorama/layout/custom_game/swift_menu_custom_hud.xml'.

[custom_hud] Layout xml did not pass CustomHud validation
"panorama/layout/custom_game/swift_menu_custom_hud.xml"
```

这组错误说明文件分发和资源实例化是两层机制。客户端即使已经下载并挂载 VPK，也不会因此获得创建自定义 Panorama layout 的权限。

### E2 逐行解释

| 客户端原文 | 所在阶段 | 能够证明什么 |
|---|---|---|
| `Failed to create ...: client disallowing panorama layout file creation` | Panorama layout 创建 | 客户端主动拒绝创建该 layout；不是服务端没有创建实体 |
| `[custom_hud] Failed to load layout ...` | `custom_hud_layout` 客户端处理 | 客户端已经处理到该网络实体及其 layout 路径，但资源加载没有成功 |
| `Layout xml did not pass CustomHud validation` | CustomHud 内容/来源验证 | layout 没有通过客户端专用验证器；服务端不能绕过该结果 |

三条警告来自客户端，而不是 ModSharp 服务端日志。它们与 E1 同时出现：服务端实体成功存在，客户端布局仍然失败。因此，“服务端成功生成实体”等价于“客户端 UI 成功显示”的说法被同一次测试直接否定。

后续实验已经推翻“`.xml` 与 `.vxml_c` 不构成路径差异”这一解释：动态 `custom_hud_layout` 的运行时 keyvalue 必须引用编译资源名 `.vxml_c`。E4 弹窗确实证明旧 build 的 Tool Mode addon 策略会在已经定位 `_c` 后终止；但 E2 使用 `.xml` 的普通客户端日志不能单独证明资源来源/信任域被拒绝。

服务器挂载 addon 同样不会改变客户端权限。网络连接不会把服务端的资源搜索路径、Tool Mode 状态或 `gameinfo.gi` 设置传给客户端。

## 四、Tool Mode 也不等于允许独立 addon 注入布局

本地 addon 已成功编译以下资源：

```text
game/csgo_addons/panorama_layout/
└─ panorama/
   ├─ layout/custom_game/swift_menu_custom_hud.vxml_c
   └─ styles/custom_game/swift_menu_custom_hud.vcss_c
```

ResourceCompiler 结果为（证据 E3）：

```text
OK: 2 compiled, 0 failed, 0 skipped
```

客户端以 `-tools -addon panorama_layout` 启动时仍然直接终止。测试截图中的致命错误弹窗原文如下（证据 E4，人工逐字转录）：

```text
FATAL ERROR: Error loading
panorama/layout/custom_game/swift_menu_custom_hud.vxml_c:
Addons cannot add layouts.
```

CS2 为两次相同失败生成了客户端崩溃转储。对转储执行二进制文本检索，可以恢复与弹窗一致的 `AbortMessage` 和 `FATAL ERROR`：

| 转储文件 | SHA-256 |
|---|---|
| `cs2_2026_0826_003748_0_error.mdmp` | `66E94D5B7CE544153F10770C9737CE830F4BCD1478400020ECDB3D59DA8FC9AE` |
| `cs2_2026_0826_004014_0_error.mdmp` | `3E5790DA885B6FF87BB236987BF2B3F3D832184BF704C121289F4DFB3928ADA2` |

两份转储中的核心文本均为：

```text
FATAL ERROR: Error loading
panorama/layout/custom_game/swift_menu_custom_hud.vxml_c:
Addons cannot add layouts.
```

这让 E4 不只依赖截图转录：客户端生成的原始错误转储也保存了同一失败原因。SHA-256 用于标识本次实验的原始转储，避免后续混淆不同启动或不同客户端版本产生的文件。

因此，“文件已正确编译”和“以 Tool Mode 启动”仍不足以证明独立 addon layout 可用。Tool Mode 下由当前地图拥有的资源，与一个试图扩展全局 `panorama/layout` 的平面 addon，不是同一条加载路径。

这条证据尤其关键，因为致命错误发生在 addon layout 加载阶段，错误文本直接使用 `Addons cannot add layouts`，没有把失败归因于：

- VXML 语法错误；
- CSS 语法错误；
- 资源文件缺失；
- 服务端没有挂载 addon；
- ModSharp 没有创建实体；
- 网络状态没有同步。

### 对照实验排除了什么

两轮服务端测试使用同一个 ModSharp 模块、同一种动态实体和同一张 `de_dust2`，决定性客户端变量只有 layout 资源：

| 项目 | 自定义 addon layout | Valve 基础 layout |
|---|---|---|
| 地图 | `de_dust2` | `de_dust2` |
| 创建方式 | ModSharp 动态实体 | ModSharp 动态实体 |
| 服务端实体 | 成功 | 成功 |
| layout | `panorama/layout/custom_game/swift_menu_custom_hud.xml` | `panorama/layout/btn_alert.vxml_c` |
| dialog variable | 不影响加载结论 | `MainMenuWatchAlertText/alert_value` |
| 普通客户端结果 | 三条加载/验证警告，不显示 | **实际显示成功** |

这个 A/B 对照排除了以下解释：

- **不是 ModSharp API 坏了。** 同一 API 使用基础 layout 可以显示。
- **不是实体没有同步。** 客户端错误中出现了服务端写入的完整 layout 路径。
- **不是官图天然不能使用 `custom_hud_layout`。** `de_dust2` 上的基础 layout 已经显示。
- **不是客户端完全禁用了 CustomHud。** 客户端能够渲染通过验证的基础 layout。
- **不是 ResourceCompiler 失败。** 自定义 XML/CSS 明确得到 `2 compiled, 0 failed`。
- **不是 Workshop/MMR 没有完成文件传输就能解释全部现象。** 独立本地 addon 中资源确定存在时，加载器仍明确报告 `Addons cannot add layouts`。

当时还遗漏了另一个决定性差异：自定义负例使用 `.xml`，Valve 基础阳性对照使用 `.vxml_c`。所以这组 A/B 不能单独把失败归因于资源来源/信任域。对于旧 build，E4 仍独立证明 Tool Mode addon 当时被 `Addons cannot add layouts` 拦截；对于 build 2000891，实验 G 已证明 Tool Mode addon 自定义 layout 跨地图成功。

## 五、客户端总闸仍然关闭

当前客户端的：

```text
game/csgo_core/gameinfo.gi
```

包含：

```text
Panorama
{
    "AllowCustomGameUI" 0
}
```

`panorama.dll` 中还可以找到与当前行为一致的字符串：

```text
AddonLayoutPath
AllowCustomGameUI
Error loading %s: Addons cannot add layouts.
Error loading %s: Addons can only add layouts in the %s subdirectory.
Failed to create '%s': client disallowing panorama layout file creation
```

`-insecure` 不会改变这个设置。`-insecure` 处理的是 VAC/安全服务器环境，不是 Panorama 的资源信任策略。

手工修改客户端 `gameinfo.gi` 可能改变实验结果，但那已经是客户端改造，不能用于证明普通玩家连接服务器即可使用。

## 六、为什么 Valve 的 `script_zoo` 可以工作

官方样例不是把 layout 作为独立 addon 注入全局 Panorama，而是把它作为地图资产。

本地官方源文件：

```text
content/csgo/maps/editor/zoo/script_zoo.vmap
content/csgo/maps/editor/zoo/scripts/welcome.xml
content/csgo/maps/editor/zoo/scripts/welcome.css
content/csgo/maps/editor/zoo/scripts/setup.js
```

`script_zoo.vmap` 中包含：

```text
classname = custom_hud_layout
targetname = welcome_layout
layout = maps/editor/zoo/scripts/welcome.vxml
```

同一 VMAP 的 `map_asset_references` 包含：

```text
maps/editor/zoo/scripts/welcome.vxml
```

编译后的：

```text
game/csgo/maps/editor/zoo/script_zoo.vpk
```

也包含该 VXML。`setup.js` 只负责通过 `welcome_layout` 找到实体，然后调用 class、dialog variable 和 input capture API。

可确认的事实是：`welcome.vxml` 被地图编译器识别为依赖并随地图包交付。客户端具体是在 CustomHud 验证阶段直接查询 `map_asset_references`，还是通过等价的“当前地图资源来源”信息判断信任，不影响工程结论：这个 VXML 属于当前地图，而不是一个地图无关的服务器 UI 包。

Hammer 中预放实体不是服务端运行时创建实体的必要条件；它同时承担了声明布局路径、让地图编译器收集资源的作用。在自己的地图中，插件仍可控制预放实体，或动态创建引用同一地图资源的实体。

但 Valve 官图的资源依赖已经编译完成，服务器插件和外部 addon 不能在玩家连接时向 `de_dust2` 的地图依赖清单追加 VXML。

## 七、普通客户端唯一确认可行的路线

普通客户端可以加载 Valve 基础游戏已有、且通过严格验证的布局。

实测成功的布局：

```text
panorama/layout/btn_alert.vxml_c
```

本仓库只引用 NuGet 公版 `ModSharp.Sharp.Shared 2.1.137`，并直接生成实体（证据 E5）：

```csharp
var keyValues = new Dictionary<string, KeyValuesVariantValueItem>
{
    ["origin"] = "0 0 0",
    ["targetname"] = "panorama_layout_probe",
    ["layout"] = "panorama/layout/btn_alert.vxml_c",
};

var layout = entityManager.SpawnEntitySync(
    "custom_hud_layout",
    keyValues);
```

这与 Swiftly PoC 的 `CreateEntityByDesignerName + DispatchSpawn` 是同一层操作：给 `custom_hud_layout` 设置 keyvalues，然后生成实体。

公版构建部署并重启 `cs2-dev` 后，真实服务端日志为：

```text
L<CoreCLR> [08/26 01:34:53] | Information |
PanoramaLayout.PanoramaLayoutPlugin
Loading PanoramaLayout build 2026-08-25T17:33:56Z; hotReload=False

L<CoreCLR> [08/26 01:34:53] | Information |
PanoramaLayout.PanoramaLayoutPlugin | de_dust2
Spawned public-API custom_hud_layout entity 303:
build=2026-08-25T17:33:56Z,
target=panorama_layout_probe,
layout=panorama/layout/btn_alert.vxml_c,
path=IEntityManager.SpawnEntitySync
```

普通客户端随后实际出现了 `btn_alert` 的橙色竖条底板。条内没有文字是当前探针的预期结果：代码只设置实体 keyvalues，没有为布局中的 `{s:alert_value}` 提供内容。这说明 E5 不只停留在服务端实体日志，客户端确实实例化并渲染了 Valve 基础 layout。

这条链路不需要：

- Tool Mode；
- Workshop 项；
- 客户端 addon；
- 自定义地图；
- 客户端文件修改。

但它不是完全自定义 UI。布局结构、位置、CSS、Panel 数量、现有图片和变量槽都由 Valve 决定。服务端只能利用布局已经暴露的 dialog variable 和 class。

独立社区项目 [`cs2-customhud`](https://gitlab.com/cs2-server-plugins/cs2-customhud) 的 README 也明确记录了同一限制：网络只发送状态，不发送 layout；普通客户端只能使用基础游戏中已有并能通过验证的少量布局。该项目估计约有 6 个基础布局符合验证要求，并以 `btn_alert` 的 `{s:alert_value}` 为例。

## 八、这对社区服务器意味着什么

对于通知、短文本、计时器或调试状态，复用 Valve 内置布局可能足够。

对于生化模式等完整游戏模式 UI，通常需要：

- 多区域长期状态展示；
- 自定义布局与层级；
- 自定义 CSS；
- 自定义图标和图片；
- 技能栏、选择菜单和按钮；
- 每玩家独立内容与交互状态。

少量 Valve 预制布局无法构成这类 UI。创建多个 `btn_alert` 实体也不会获得新的布局能力，并且可能在相同位置重叠。

所以截至本次测试：

> `custom_hud_layout` 已经提供服务器驱动状态的技术通道，但没有向地图无关的社区服务器开放自定义视图资源。

## 九、常见误判

### “服务器成功创建实体，所以自定义 HUD 已经可用”

错误。实体存在只证明服务器阶段成功；客户端仍会独立验证资源来源与 XML 内容。

### “Workshop 能把文件下载给玩家，所以能显示”

错误。分发不等于 Panorama 加载授权。

### “服务端挂载 addon，客户端自然会继承”

错误。服务端与客户端拥有独立的文件系统搜索路径和安全策略。

### “开 `-insecure` 就能解除限制”

错误。当前错误来自 Panorama 的 `AllowCustomGameUI`/layout 加载策略，不是 VAC。

### “Tool Mode 肯定允许所有自定义 Panorama”

在本文所测旧 build 中错误：独立 addon 向 `panorama/layout/custom_game` 添加 VXML 时仍触发致命错误。build 2000891 已改变这一前提；新版实验 B/G 分别证明可写 addon 布局和显式挂载 addon 的跨地图 `.vxml_c` 均可运行。

### “`script_zoo` 能显示，所以官图上的社区服插件也能显示”

错误。`script_zoo` 的 VXML 是地图编译依赖并随地图 VPK 交付；官图没有我们的 VXML 依赖。

## 十、什么变化可以推翻本文结论

> build 2000891 已经满足下面第 1 项，并同时改变了 addon VPK 白名单和 Panorama 资源类型配置。当前 Tool Mode 跨地图结论已被实验 G 推翻；基础 `game/csgo/panorama` loose-file 已由实验 H 重测成功。只有 packed retail VPK 仍是独立待回归路径。

出现以下任一变化后，应重新测试：

1. Valve 将零改客户端的 `Panorama/AllowCustomGameUI` 改为允许；
2. Valve 提供服务器声明的、签名或沙箱化的 Workshop UI 依赖机制；
3. Valve 允许 addon 为当前连接注册受限的 `custom_game` layout，而不要求地图拥有资源；
4. Valve 增加足够通用的基础游戏 CustomHud 布局，使服务端能够在固定安全组件内自由组合 UI；
5. 官方文档或客户端更新明确改变资源信任规则。

在这些变化发生之前，任何“普通玩家连接任意社区服即可获得完全自定义 Panorama HUD”的宣称，都应同时给出以下端到端证据：

- 未修改的普通客户端；
- 不使用 Tool Mode；
- 不加载自定义地图；
- 不修改 `gameinfo.gi`；
- 自定义 VXML/CSS 不是 Valve 基础资源；
- 客户端没有 `Addons cannot add layouts` 或 CustomHud validation 错误；
- UI 在实际客户端画面中显示，而不仅是服务端实体日志成功。

缺少其中任一项，都不能证明地图无关、零客户端改造的社区服务器自定义 UI 已经成立。

## 参考

- [Valve：Counter-Strike 2 Update（MAP SCRIPTING）](https://steamcommunity.com/games/730/announcements/detail/1841579228676851)
- Valve 本地样例：`content/csgo/maps/editor/zoo/script_zoo.vmap`
- Valve 本地样例：`content/csgo/maps/editor/zoo/scripts/welcome.xml`
- Valve 本地样例：`content/csgo/maps/editor/zoo/scripts/setup.js`
- 客户端配置：`game/csgo_core/gameinfo.gi`
- 独立 PoC：[`cs2-customhud`](https://gitlab.com/cs2-server-plugins/cs2-customhud)
- 本仓库探针：`src/PanoramaLayout/PanoramaLayoutPlugin.cs`
