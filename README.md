# CS2 `custom_hud_layout`：ModSharp 能力边界探针

> **实测结论（2026-08-26）：** 早期“客户端 addon + ModSharp 可在任意地图提供完全自定义 HUD”的假设已经被否证。普通零改客户端会拒绝 addon 提供的 VXML；当前模块使用 NuGet 公版 `ModSharp.Sharp.Shared 2.1.137` 和纯实体 API 创建 `custom_hud_layout`，以 Valve 内置的 `panorama/layout/btn_alert.vxml_c` 验证实体与客户端基础布局链路。完整证据与复现记录见 [《普通客户端能力边界实测》](docs/custom-hud-retail-client-boundary.zh-CN.md)。下文中的本地 addon 构建流程仅作为失败路径和开发实验保留，不代表可部署到普通社区服玩家。

这个仓库最初用于验证一种与地图解耦的 Custom HUD 架构：客户端 addon 提供静态 VXML/CSS，ModSharp 在任意当前地图上动态创建 `custom_hud_layout` 网络实体。实测已经证明该架构的“自定义客户端 layout”部分会被普通客户端拒绝；下面的结构图保留为原始假设和失败路径记录。

```text
客户端 Panorama addon
VXML / CSS / images
          │
          │ 按实体中的逻辑资源路径加载
          ▼
custom_hud_layout 网络实体
          ▲                       │
          │ dialog variable/class │ buttonId + player
          │                       ▼
                 ModSharp 游戏模式
```

这里没有 Hammer 依赖，也不要求地图预放实体。地图只是游戏模式运行的场景；同一套 UI 可以用于官图或任意社区地图。

## 两种服务端控制器

Valve 官方 `script_zoo` 展示的是：

```text
cs_script 服务端 JavaScript → 地图中的 custom_hud_layout → 客户端 UI
```

本仓库使用的是：

```text
ModSharp C# → 动态创建 custom_hud_layout → 客户端 UI
```

两者操作的是同一种网络实体。被禁止的是 VXML 内部的 Panorama 客户端脚本和事件，不是服务端 JavaScript。官方声明与示例可在本地 Workshop Tools 内容中查看：

```text
content/csgo/maps/editor/zoo/scripts/setup.js
content/csgo/maps/editor/zoo/scripts/welcome.xml
content/csgo/maps/editor/zoo/scripts/welcome.css
content/csgo/maps/editor/zoo/scripts/point_script.d.ts
```

## 客户端资源结构

仓库中的客户端源码遵循 Panorama 的资源分层：

```text
addon/
└─ panorama/
   ├─ layout/custom_game/panorama_layout/welcome.xml
   └─ styles/custom_game/panorama_layout/welcome.css
```

构建时源码会被暂存为 `.vxml`、`.vcss`，再由 Valve 的 `resourcecompiler.exe` 生成：

```text
game/csgo_addons/panorama_layout/
└─ panorama/
   ├─ layout/custom_game/panorama_layout/welcome.vxml_c
   └─ styles/custom_game/panorama_layout/welcome.vcss_c
```

ModSharp 使用的是逻辑资源名，不是仓库源文件名，也不带 `_c`：

```text
panorama/layout/custom_game/panorama_layout/welcome.vxml
```

## 编译客户端 addon

安装 CS2 Workshop Tools 后运行：

```powershell
.\build-addon.ps1 `
    -Cs2Root "D:\game\SteamLibrary\steamapps\common\Counter-Strike Global Offensive"
```

脚本会完成以下操作：

1. 把 XML/CSS 暂存到 `content/csgo_addons/panorama_layout`。
2. 生成 addon 所需的 `addoninfo.txt` 与 Panorama preprocessor 配置。
3. 命令行调用 `resourcecompiler.exe`。
4. 验证 `.vxml_c` 与 `.vcss_c` 已出现在 `game/csgo_addons/panorama_layout`。

它不会修改 `gameinfo.gi`，也不会启动或关闭 CS2。

## 使用 Tools Mode 客户端连接 MMR 实例

从 Workshop Tools 选择 `panorama_layout` addon 启动。其启动模型等价于：

```text
cs2.exe -addon panorama_layout -tools
```

客户端不安装 ModSharp，也不启动本地 listen server。MMR 负责拉起独立 CS2 服务端、部署 ModSharp 与游戏模式模块，并选择实际运行的地图。

在 Tools Mode 控制台直接连接 MMR 分配的实例：

```text
connect <MMR instance address>
```

完整链路是：

```text
Tools 客户端
  -addon panorama_layout
  └─ 挂载 VXML/CSS
          │
          │ connect
          ▼
MMR 独立服务端
  ├─ 官方图或社区图
  ├─ ModSharp
  └─ PanoramaLayout/生化模式模块
          │
          └─ 创建 custom_hud_layout 并同步 UI 状态
```

服务端只需要模块和 ModSharp；Panorama VXML/CSS 是客户端资源，不需要为了 UI 改造或绑定服务器当前地图。客户端解析实体中的逻辑资源名时，会从已经挂载的 `panorama_layout` addon 中找到编译资源。

## ModSharp 端

核心入口位于 [`PanoramaLayoutPlugin.cs`](src/PanoramaLayout/PanoramaLayoutPlugin.cs)：

```csharp
var keyValues = new Dictionary<string, KeyValuesVariantValueItem>
{
    ["origin"] = "0 0 0",
    ["targetname"] = "panorama_layout_probe",
    ["layout"] = "panorama/layout/btn_alert.vxml_c",
};

var layout = entityManager.SpawnEntitySync("custom_hud_layout", keyValues);
```

这与 Swiftly PoC 的 `CreateEntityByDesignerName + DispatchSpawn` 是同一层实体操作。项目只引用 NuGet 公版 API：

```xml
<PackageReference Include="ModSharp.Sharp.Shared"
                  Version="2.1.137"
                  PrivateAssets="all"
                  ExcludeAssets="runtime" />
```

当前探针只使用上述公版实体接口。它设置实体 keyvalues、生成 `custom_hud_layout`，并通过普通客户端实际出现的 `btn_alert` 橙色底板确认渲染链路。

构建模块：

```powershell
dotnet build src\PanoramaLayout\PanoramaLayout.csproj `
    -c Release `
    -o .build\modules\PanoramaLayout
```

产物：

```text
.build/modules/PanoramaLayout/PanoramaLayout.dll
```

## 能力边界

Custom HUD 当前只支持 `Panel`、`Label`、`Image`、`Button` 和 CSS。布局是静态声明式结构，服务端可以：

- 设置 dialog variables；
- 切换 panel CSS classes；
- 为单个玩家覆盖状态；
- 开关输入捕获；
- 接收 Button 点击；
- 创建或销毁整个 layout 实体。

客户端不能在布局中运行 Panorama JS，也不能用 `onactivate` 等属性执行代码。CSS transition/animation 仍由客户端渲染。

Tools Mode 解决的是开发和受控客户端的资源挂载。普通公网玩家不会因为连接 ModSharp 服务器就自动拥有这个 addon；客户端资源的正式分发仍是一个独立部署问题，但与具体运行哪张地图无关。
