# CS2 `custom_hud_layout`: A ModSharp Capability-Boundary Probe

[简体中文](README.zh-CN.md) · [Build 2000891 report](docs/custom-hud-build-2000891-retest.en.md) · [中文重测记录](docs/custom-hud-build-2000891-retest.zh-CN.md) · [Screenshot evidence](docs/evidence/README.md) / [截图证据](docs/evidence/README.zh-CN.md)

> **Current status (2026-08-26, build 2000891): cross-map Tool Mode, locally preinstalled retail-client resources, server-driven state, and complete interaction have all succeeded.** In addition to the six dialog variables and dynamic class demonstrated by `loadout.vxml_c`, this repository now includes a two-page `server_menu.vxml_c`. When a player enters `.menu` (`!menu` and `/menu` are also accepted by CommandCenter), the server enables the menu and input capture only for that slot. Button clicks return to ModSharp through the `CS_UM_CustomHudClicked` receiver; the server then performs page navigation, chat output, theme switching, back, and close actions. The click receiver, `SetHasClassForPlayer`, and `SetInputCaptureEnabled` have all been verified on the current Windows build. Earlier `invalid resource name` errors came from using a source filename on a dynamic entity; runtime references must use the compiled `.vxml_c` resource name. Packed retail VPK content remains distinct from both loose-resource paths and cannot be inferred from these successes. See the [build 2000891 retest report](docs/custom-hud-build-2000891-retest.en.md) for the full correction, experiment index, and evidence.
>
> Before build 2000891, the retail client rejected addon VXML while Valve's built-in `btn_alert.vxml_c` could still display. Those older experiments remain valid for the client build on which they were performed. Their original logs, dump hashes, and A/B comparison are preserved in the [retail-client capability-boundary report](docs/custom-hud-retail-client-boundary.zh-CN.md) (Chinese).

This repository tests a map-independent Custom HUD architecture: the client supplies static VXML/CSS, while ModSharp dynamically creates a networked `custom_hud_layout` entity on whatever map is currently running. The architecture now works end to end with an explicitly mounted addon in Tool Mode and with locally preinstalled resources in the retail client's base directory. Packed Workshop/MMR VPK delivery and automatic distribution still require separate verification.

![Server-driven two-page Custom HUD menu](docs/evidence/12-interactive-menu-home.png)

```text
Client Panorama addon
VXML / CSS / images
          │
          │ loaded from the logical resource path on the entity
          ▼
networked custom_hud_layout entity
          ▲                       │
          │ dialog variable/class │ buttonId + player
          │                       ▼
                  ModSharp game mode
```

This design has no Hammer dependency and does not require an entity to be baked into the map. The map is merely the scene in which the game mode runs; the same UI can be used on official or community maps.

## Two server-side controllers

Valve's official `script_zoo` demonstrates:

```text
cs_script server-side JavaScript → custom_hud_layout in the map → client UI
```

This repository uses:

```text
ModSharp C# → dynamically created custom_hud_layout → client UI
```

Both control the same type of networked entity. What is prohibited is Panorama client script and events inside VXML, not server-side JavaScript. The official declaration and examples can be inspected in the local Workshop Tools content:

```text
content/csgo/maps/editor/zoo/scripts/setup.js
content/csgo/maps/editor/zoo/scripts/welcome.xml
content/csgo/maps/editor/zoo/scripts/welcome.css
content/csgo/maps/editor/zoo/scripts/point_script.d.ts
```

## Client resource layout

The client sources in this repository follow Panorama's resource hierarchy:

```text
addon/
└─ panorama/
   ├─ layout/custom_game/
   │  ├─ loadout.xml
   │  └─ server_menu.xml
   └─ styles/custom_game/
      ├─ loadout.css
      └─ server_menu.css
```

`loadout` retains the text-variable, dynamic-class, z-index, and cache probes. `server_menu` is the current two-page interactive scenario. The build script compiles the interactive menu by default.

During a build, the `.xml` and `.css` sources are staged into the addon content directory, then compiled by Valve's `resourcecompiler.exe` into:

```text
game/csgo_addons/panorama_layout/
└─ panorama/
   ├─ layout/custom_game/server_menu.vxml_c
   └─ styles/custom_game/server_menu.vcss_c
```

The runtime `layout` keyvalue on `custom_hud_layout` must use the compiled resource name, including the `_c` suffix:

```text
panorama/layout/custom_game/server_menu.vxml_c
```

The `.vxml` form commonly seen in Hammer/VMAP authoring is a source dependency name and cannot be copied directly onto a dynamic entity. The most important correction from the build 2000891 retest is that `.vxml` produces `invalid resource name`, while the correct `.vxml_c` resource in the same Tool Mode addon loads successfully after connecting to `de_dust2`.

## Building the client addon

After installing CS2 Workshop Tools, run:

```powershell
.\build-addon.ps1 `
    -Cs2Root "D:\game\SteamLibrary\steamapps\common\Counter-Strike Global Offensive"
```

The script:

1. stages XML/CSS under `content/csgo_addons/panorama_layout`;
2. creates the addon's `addoninfo.txt` and Panorama preprocessor configuration;
3. invokes `resourcecompiler.exe` from the command line;
4. verifies that `.vxml_c` and `.vcss_c` exist under `game/csgo_addons/panorama_layout`.

It does not modify `gameinfo.gi`, start CS2, or stop CS2.

## Connecting a Tools Mode client to an MMR instance

Launch the `panorama_layout` addon from Workshop Tools. Its launch model is equivalent to:

```text
cs2.exe -addon panorama_layout -tools
```

The client does not install ModSharp or start a local listen server. MMR launches the dedicated CS2 server, deploys ModSharp and the game-mode module, and selects the map that actually runs.

Connect directly to the MMR-assigned instance from the Tools Mode console:

```text
connect <MMR instance address>
```

The full path is:

```text
Tools client
  -addon panorama_layout
  └─ mounts VXML/CSS
          │
          │ connect
          ▼
MMR dedicated server
  ├─ official or community map
  ├─ ModSharp
  └─ PanoramaLayout/game-mode module
          │
          └─ creates custom_hud_layout and synchronizes UI state
```

The server needs only the module and ModSharp. Panorama VXML/CSS are client resources; the server's current map does not need to be modified or bound to the UI. When the client resolves the logical resource name on the entity, it finds the compiled resource in the already mounted `panorama_layout` addon.

## ModSharp side

The main entry point is [`PanoramaLayoutPlugin.cs`](src/PanoramaLayout/PanoramaLayoutPlugin.cs):

```csharp
var keyValues = new Dictionary<string, KeyValuesVariantValueItem>
{
    ["origin"] = "0 0 0",
    ["targetname"] = "panorama_server_menu",
    ["layout"] = "panorama/layout/custom_game/server_menu.vxml_c",
};

var layout = entityManager.SpawnEntitySync("custom_hud_layout", keyValues);
```

This operates at the same entity layer as the Swiftly proof of concept's `CreateEntityByDesignerName + DispatchSpawn`. The project references only public NuGet APIs:

```xml
<PackageReference Include="ModSharp.Sharp.Shared"
                  Version="2.1.137"
                  PrivateAssets="all"
                  ExcludeAssets="runtime" />
<PackageReference Include="ModSharp.Sharp.Modules.CommandCenter.Shared"
                  Version="2.1.137"
                  PrivateAssets="all"
                  ExcludeAssets="runtime" />
```

Entity creation still uses only those public APIs. The state and interaction layer resolves five CS functions required by the current scenario from [`gamedata/panorama_layout_customhud.jsonc`](gamedata/panorama_layout_customhud.jsonc): `SetDialogVariableString`, `SetHasClass`, `SetHasClassForPlayer`, `SetInputCaptureEnabled`, and `CustomHudClickedReceiver`. The first two retain the existing loadout-probe capabilities. The final three drive per-player visibility, page state, mouse input, and Button callbacks for [`server_menu.xml`](addon/panorama/layout/custom_game/server_menu.xml). These functions synchronize state; they do not distribute or mount the client layout.

Build the module with:

```powershell
dotnet build src\PanoramaLayout\PanoramaLayout.csproj `
    -c Release `
    -o .build\modules\PanoramaLayout
```

Output:

```text
.build/modules/PanoramaLayout/PanoramaLayout.dll
```

The state probe also requires the gamedata file to be deployed as:

```text
game/sharp/gamedata/panorama_layout_customhud.jsonc
```

The module registers it with `IGameData.Register("panorama_layout_customhud")`. The gamedata must be present before the module loads. DLL hot updates are still delivered to the module's `reload/` subdirectory and consumed by the next map-start event.

## Capability boundaries

Custom HUD currently supports only `Panel`, `Label`, `Image`, `Button`, and CSS. The layout is a static declarative structure. The server can:

- set dialog variables;
- toggle panel CSS classes;
- override state for an individual player;
- enable or disable input capture;
- receive Button clicks;
- create or destroy the entire layout entity.

The client cannot run Panorama JS inside the layout or execute code through attributes such as `onactivate`. CSS transitions and animations are still rendered client-side.

This retest verified two server-state channels. All six dialog variables displayed correctly, and `SetHasClass("Loadout", "server-class-ok", true)` caused the client to enter a green activation state that existed only in VCSS. Because the class was not predeclared in the VXML, the visual change is independent end-to-end evidence.

The complex interaction scenario also passed on an actual Windows server. Entering `.menu` in chat opens a two-page menu and enables input capture for the invoking slot; clicking close releases input. Entering `.menu` again also acts as a close toggle. Clicking the first item makes the server switch the `is-active` class between the two page panels. Buttons on the second page can write to chat, show confirmation state, switch to a cyan theme, go back, and close. The click path hooks `CustomHudClickedReceiver` directly and depends on neither the current map's point_script/Pulse graph nor client-side Panorama JS. The default M-key `teammenu` is a client command and does not reach a server-side CommandCenter listener; ordinary users do not need to rebind it and can use `.menu` instead.

The stacking order between Custom HUD and the expanded radar depends on Panorama stacking contexts, selectors, and the exact HUD state. A base `.loadout` rule with `z-index: 1000` was sufficient to alter the order in one experiment. In the class experiment, moving `z-index: 10000` into `.loadout.server-class-ok` made the green panel render consistently above the radar. CSS can address the issue, but no single value should be presented as a permanent guarantee for every HUD combination.

A Custom HUD dynamically created by a remote server in Tool Mode also exhibited process-level resource caching. Recompiling back and forth between a known `4128`-byte faulty VCSS and a known `4174`-byte fixed VCSS did not change the current view. Selecting the resource in Asset Browser and reconnecting after `disconnect` also failed to invalidate the cache. Only a full Tool Mode restart reliably loaded the current VCSS from disk. This result is limited to the tested path and should not be generalized to all map-authoring resources.

Tools Mode confirmed that custom addon VXML/VCSS can escape the VMAP that owns them. A `cs_script_demo_copy` client connected to `de_dust2` on a dedicated server and displayed the complete `loadout.vxml_c` with all six server variables. This result depends on the correct compiled resource name and an explicitly mounted client addon.

The retail-client base-directory loose-file path has also been successfully retested with `.vxml_c`: a client launched without `-tools` or `-addon` can load locally preinstalled custom layout and style resources. This still does not prove that packed retail VPK content works. In a same-day Mapcore Discord exchange supplied by an experiment participant on 2026-08-26, a Valve developer stated that Panorama still had a hard stop for packed addon content outside tools and that a fix was planned. The exchange has no public URL and is retained only as contemporaneous context.

## License

This project is released under the [MIT License](LICENSE).
