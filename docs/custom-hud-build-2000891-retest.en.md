# CS2 `custom_hud_layout` build 2000891 retest

Status: **A/B/G/H/I/J/K completed; C/D corrected; packed retail VPK (E) awaits an independent retest after Valve's fix**<br>
Start date: 2026-08-26<br>
Client/server build: `2000891`<br>
PatchVersion: `1.41.7.7`<br>
SourceRevision: `10937988`

This report records the complete retest performed after Valve followed the first `custom_hud_layout` release with build 2000891. It preserves the earlier failures while separating their applicable build, the changes in 2000891, confirmed capabilities, corrected interpretations, and remaining distribution boundary.

- [Chinese version](custom-hud-build-2000891-retest.zh-CN.md)
- [Historical pre-2000891 retail-client boundary report (Chinese)](custom-hud-retail-client-boundary.zh-CN.md)
- [Screenshot evidence manifest](evidence/README.md)

## Current findings

The following statements are confirmed for build 2000891:

1. CS2 addons can compile Panorama resources under `panorama/layout/custom_game` and `panorama/styles/custom_game`.
2. Valve's `cs_script_demo` map loads its map-owned `welcome.vxml` layout.
3. A writable Workshop Tools copy, `cs_script_demo_copy`, compiles and renders a genuinely user-edited VXML.
4. A Tool Mode client with `cs_script_demo_copy` explicitly mounted can join an independent `de_dust2` server and load `loadout.vxml_c`, even though the current VMAP does not reference it.
5. The same layout displays all six dialog variables written by the server through gamedata/native setters.
6. `CCSCustomHudLayout::SetHasClass` can add a class to a target Panel and make the client match a VCSS rule that was initially inactive.
7. The earlier `escape_probe.vxml` and `welcome.vxml` failures did not control source name `.vxml` versus compiled runtime name `.vxml_c`. They cannot establish a VMAP ownership restriction.
8. A normal client launched without `-tools` or `-addon` can load the same compiled layout and style from base `game/csgo/panorama`. The locally preinstalled client model works.
9. Tool Mode, base-directory loose files, and packed retail VPKs are separate loading paths. A participant-supplied Mapcore Discord conversation from 2026-08-26 reports that packed VPK Panorama content still hits a hard stop. The conversation has no public link and is retained only as same-day engineering context; the packed path still requires a reproducible retest.
10. For a remote server-created Custom HUD in Tool Mode, externally recompiling VCSS, selecting the resource in Asset Browser, and disconnecting/reconnecting do not invalidate the loaded style. A full Tool Mode process restart does.
11. A two-page menu completed an end-to-end Windows test: `.menu`, per-player class writes, input capture, Button receiver, server-side page transitions, action output, theme switching, back, and close. The default M binding executes client-side `teammenu` and did not reach the server listener; the normal user flow does not require rebinding.

The following claims must remain separate:

```text
Editable map-owned Custom HUD                              confirmed
Explicitly mounted Tool Mode addon used on another map    confirmed
Server-driven dialog variables in a custom layout         confirmed
Server-driven Panel CSS classes                           confirmed
Base game/csgo loose compiled resources                   confirmed
Retail Workshop/MMR packed VPK                            separate path; hard-stop report, retest pending
```

## Experiment index and verdict vocabulary

Readers interested only in current capabilities should start with G, H, I, K, and the final matrices. C and D explain why `invalid resource name` was initially misattributed to VMAP/resource provenance.

| Experiment | Single question | Client/resource path | Verdict | Present use |
|---|---|---|---|---|
| A | Does Valve's map-owned layout render? | Tool Mode + current VMAP | **Pass** | Official positive baseline |
| B | Does a user-edited VXML really recompile and run? | Tool Mode + current VMAP | **Pass** | Rules out fallback to Valve's precompiled asset |
| C | Can an addon layout escape its VMAP? | Tool Mode + addon, but entity incorrectly names `.vxml` | **Confounded; corrected** | Preserves wrong-suffix `invalid resource name` evidence |
| D | Can a normal client load a base loose file? | Normal client + loose compiled file, but entity names `.vxml` | **Confounded; corrected** | Proves rejection occurred before file-open provenance could be tested |
| E | Can packed Workshop/MMR VPK enter Panorama? | Normal client + packed VPK | **Pending** | Must remain separate from Tool Mode and preinstall |
| F | Can a server dialog variable reach the client? | Valve `btn_alert.vxml_c` | **Pass** | State-sync positive baseline |
| G | Can correct `.vxml_c` escape the current VMAP? | Tool Mode + addon + `de_dust2` | **Pass** | Overturns C's VMAP attribution |
| H | Does local preinstallation of loose compiled resources work? | Normal client + `game/csgo/panorama` | **Pass** | Controlled-client deployment model |
| I | Can the server toggle a Panel class? | Normal client + loose resource | **Pass** | Server-driven styling without client JS |
| J | Does remote dynamic HUD VCSS hot-invalidate? | Tool Mode + bidirectional CSS A/B | **Negative result** | Current process stays cached; full restart required |
| K | Can the primitives form a real two-page menu? | Tool Mode + CommandCenter + Button receiver | **Pass** | `.menu`, navigation, actions, theme, safe close |

Verdicts in this report mean:

- **Pass**: server evidence and actual client rendering/interaction both agree.
- **Negative result**: a controlled A/B reliably establishes that a capability is absent or does not refresh.
- **Confounded; corrected**: the observation was real, but a second uncontrolled variable invalidated the original attribution.
- **Pending**: no end-to-end result currently meets the evidence bar; nothing is inferred from adjacent paths.

### Reproduction entry points

| Content | Repository path |
|---|---|
| Tool Mode loadout probe | `addon/panorama/layout/custom_game/loadout.xml`, `addon/panorama/styles/custom_game/loadout.css` |
| Two-page interactive menu | `addon/panorama/layout/custom_game/server_menu.xml`, `addon/panorama/styles/custom_game/server_menu.css` |
| Dynamic entity, commands, page state machine | `src/PanoramaLayout/PanoramaLayoutPlugin.cs` |
| Dialog/class/input native wrapper | `src/PanoramaLayout/CustomHudNativeProbe.cs` |
| Button receiver detour | `src/PanoramaLayout/CustomHudClickProbe.cs` |
| Current five-function gamedata | `gamedata/panorama_layout_customhud.jsonc` |
| Addon build entry point | `build-addon.ps1` |

The 12 original PNG payloads were recovered losslessly from the Codex root session data URIs and checked into [`docs/evidence`](evidence/README.md). They were not reconstructed from chat thumbnails or recompressed. The evidence ledger records dimensions, byte lengths, and SHA-256; screenshots supplement rather than replace source, compiler output, resource hashes, and server logs.

## 1. Why the retest was required

The earlier experiment was not fabricated or meaningless. On its client build it produced:

```text
Addons cannot add layouts.
client disallowing panorama layout file creation
Layout xml did not pass CustomHud validation
```

Build 2000891 changed the client assumptions on which those failures depended.

### Timeline

| Time | Event | Meaning |
|---|---|---|
| 2026-08-24 | Valve [first announced `custom_hud_layout`](https://steamcommunity.com/app/730/announcements) | Official support for Panel, Label, Image, Button, and CSS; no client scripts |
| 2026-08-24 23:42 UTC | GameTracking build 2000888 commit `fd04856` | First appearance of the entity, APIs, and official zoo example |
| 2026-08-26 00:45–01:34 UTC+8 | This project completed the old addon/`btn_alert` A/B | Addon VXML rejected; Valve base layout rendered |
| 2026-08-25 19:52:46 UTC | Steam [build 24934554](https://steamdb.info/patchnotes/24934554/) published | Published after the old experiment, without patch notes |
| 2026-08-25 19:58 UTC | GameTracking commit [`acfe24d`](https://github.com/SteamTracking/GameTracking-CS2/commit/acfe24d588d2df0a26da0f964e44d780bd3070eb) | Build 2000891 with Custom UI and addon packaging changes |
| 2026-08-26 | Steam integrity verification and local reinspection | Local official files confirmed at build 2000891; no manual `gameinfo.gi` edit |

The old experiment did not save its contemporary `steam.inf`. Its assignment to build 2000888 is an inference from the experiment time and the Steam/GameTracking publication sequence. The certain fact is that it predated build 2000891.

### Decisive changes in build 2000891

1. `game/csgo_core/gameinfo.gi` changed the global gate:

```diff
-"AllowCustomGameUI" 0
+"AllowCustomGameUI" 1
```

2. `game/csgo/gameinfo.gi` added both custom-game Panorama directories to `AddonConfig/VpkDirectories`:

```text
"include" "panorama/layout/custom_game"
"include" "panorama/styles/custom_game"
```

3. `game/bin/assettypes_common.txt` retained `CompilePanorama` for `panorama_style` and `panorama_layout` and removed their CS2 retail-mod hiding rule.

4. Valve shipped `cs_script_demo`, whose read-only assets include:

```text
panorama/layout/custom_game/welcome.vxml
panorama/styles/custom_game/welcome.vcss
```

These changes directly affect the stage at which the old experiment failed. A pre-2000891 conclusion cannot be projected onto 2000891 without a retest.

## 2. Experimental design

### Variables that must remain separate

| Variable | Values | Question |
|---|---|---|
| Layout ownership | referenced / not referenced by current VMAP | Does CustomHud require current-map ownership? |
| Client resource mount | explicit Tool Mode addon / base preinstall / packed delivery | Is the file both available and trusted on this path? |
| Entity origin | preplaced in VMAP / dynamically spawned by ModSharp | Does success depend on Hammer placement? |

A Welcome panel in `cs_script_demo` cannot by itself prove that a ModSharp-created entity on `de_dust2` can load the same resource.

### Evidence bar

Each experiment should preserve:

1. `steam.inf` client/server version, PatchVersion, and SourceRevision;
2. client launch mode and explicitly mounted addon;
3. server map, module build time, and logical layout path;
4. server entity-creation logs;
5. client CustomHud/Panorama console output;
6. an original client screenshot;
7. compiled resource timestamps and SHA-256;
8. whether the current VMAP contains the layout in `map_asset_references`.

The following implications are invalid:

```text
server entity exists != client UI rendered
file downloaded != file mounted
file mounted != CustomHud validation passed
one map rendered != every map can render
Tool Mode rendered != a normal client automatically obtained and rendered
```

## 3. Environment integrity: pass

After Steam integrity verification, the local installation reported:

```text
ClientVersion=2000891
ServerVersion=2000891
PatchVersion=1.41.7.7
SourceRevision=10937988
VersionDate=Aug 25 2026
VersionTime=11:28:13
```

The official installation also contained `AllowCustomGameUI=1`, the two addon VPK allowlist directories, `resourcecompiler.exe`, visible/compilable Panorama layout and style asset types, and complete compiled assets for Valve's `cs_script_demo`. No official CS2 file was modified during this stage.

## 4. Experiment A: Valve map-owned layout

### Procedure and ownership

1. Copy Valve's read-only `cs_script_demo` in Workshop Tools.
2. Let Tools create writable addon `cs_script_demo_copy`.
3. Load the copied demo map.
4. Observe the Welcome panel and Dismiss button.

The copy contains source and compiled forms:

```text
content/csgo_addons/cs_script_demo_copy/panorama/layout/custom_game/welcome.xml
game/csgo_addons/cs_script_demo_copy/panorama/layout/custom_game/welcome.vxml_c
```

Binary DMX inspection of `cs_script_demo.vmap` found:

```text
map_asset_references
└─ panorama/layout/custom_game/welcome.vxml

custom_hud_layout
├─ targetname = welcome_layout
└─ layout = panorama/layout/custom_game/welcome.vxml
```

The current map therefore declares the resource, preplaces the entity, and controls it through its map script.

### Result: pass

The client displayed:

```text
Welcome to the cs_script demo map!
[ Dismiss ]
```

![Valve cs_script_demo Welcome panel](evidence/02-official-welcome-map.png)

This is the official positive baseline. It does not prove that a layout can leave the VMAP that owns it.

## 5. Experiment B: user-edited map-owned layout

### Controlled edit

Only `welcome.xml` changed. The style, VMAP, map script, entity properties, and `gameinfo.gi` remained unchanged.

```xml
<Panel id="dialog" class="Dismissed">
    <Label class="Body" text="PANORAMA EDITED" />
    <Label class="Body" text="CAN IT ESCAPE?" />
    <Button id="dismiss_button">
        <Label text="TEST DUST II" />
    </Button>
</Panel>
```

Workshop Tools regenerated `welcome.vxml_c`. Evidence recorded:

```text
source timestamp:  2026-08-26 11:22:29 UTC+8
output timestamp:  2026-08-26 11:22:50 UTC+8
output SHA-256:    CF08A093BC8F7B94B87F2A559A8CE2A30AD51894DB6710F6A43D1BB3A30A32BD
```

The compiled binary contained all three unique strings, and the client displayed all of them.

### Result: pass

User-authored Panorama VXML is genuinely compiled and executed in build 2000891; the client was not falling back to Valve's original precompiled layout. The resource was still owned by the current VMAP, so this experiment alone did not answer whether it could escape.

![User-edited and recompiled Welcome layout](evidence/03-edited-welcome.png)

## 6. Experiment C: cross-map misattribution caused by the wrong suffix

Status: **completed as an observation; the original “cross-map failure” interpretation was overturned by G**

### Intended question

The intended path was:

```text
Tool Mode client explicitly mounts cs_script_demo_copy
        │
        │ connect
        ▼
independent server runs de_dust2
        │
        └─ ModSharp dynamically creates custom_hud_layout
              layout = panorama/layout/custom_game/escape_probe.vxml
```

`escape_probe` was created so `welcome` would not confound the test through its VMAP reference, default `Dismissed` class, or `setup.vjs` behavior. The probe was unique, visible by default, script-free, absent from all VMAP references, and compiled with a recorded hash.

### Server and mount control

Only the Tool Mode client explicitly mounted the Panorama addon. The independent server mounted ModSharp and this probe module through `C:/workshop/projects/playground/instance.config.ts`, while its map stayed `de_dust2`.

The module build directory was symlink-mounted as:

```text
C:/workshop/projects/PanoramaLayout/.build/modules/PanoramaLayout
  -> game/sharp/modules/PanoramaLayout
```

Runtime DLL updates were staged under the module's `reload/` subdirectory and consumed on the next map-start event. ModSharp performed `Unload -> Update -> Load(hotReload: true)` without restarting the CS2 server process.

Preparation evidence:

- `escape_probe.xml` used unique strings `PANORAMA ESCAPED`, `HELLO, DUST II`, and `NOT IN VMAP`.
- ResourceCompiler reported `1 compiled, 0 failed, 0 skipped`.
- `escape_probe.vxml_c` SHA-256: `A805C1583276F23CC58AF2337C0037B05DF63289B814C69B05CB68D25D72134D`.
- Binary search found `welcome.vxml`, but not `escape_probe`, in `cs_script_demo.vmap`.
- Module build: zero errors and zero warnings.
- MMR `cs2-dev` ran `de_dust2`.

The server logged a valid dynamic entity, but with the source-style name:

```text
Spawned public-API custom_hud_layout entity 303:
target=panorama_layout_probe,
layout=panorama/layout/custom_game/escape_probe.vxml,
path=IEntityManager.SpawnEntitySync
```

### Client observations

Three controlled observations were preserved:

1. The Tool Mode client joined `de_dust2` and rejected newly indexed `escape_probe`:

```text
Layout xml is an invalid resource name "panorama/layout/custom_game/escape_probe.vxml"
```

Restarting Tools made `escape_probe` appear in `tools_asset_info.bin`, but the error remained. This ruled out a stale addon index, not the suffix problem.

2. The server switched to map-owned, already proven `welcome.vxml`; the client still reported:

```text
Layout xml is an invalid resource name "panorama/layout/custom_game/welcome.vxml"
```

At the time this looked like a current-map ownership boundary. G later exposed the shared uncontrolled variable: both custom negatives named `.vxml`.

3. With client, server, map, API, and dynamic entity origin unchanged, the Valve base resource used its compiled runtime name:

```text
panorama/layout/btn_alert.vxml_c
```

It rendered on the same `de_dust2` client.

### Corrected verdict

| Current map | Requested layout | Observation |
|---|---|---|
| demo VMAP | addon `welcome.vxml` | rendered in its authoring/map-owned path |
| `de_dust2` | addon `escape_probe.vxml` | `invalid resource name` |
| `de_dust2` | addon `welcome.vxml` | `invalid resource name` |
| `de_dust2` | Valve `btn_alert.vxml_c` | **rendered** |

The two negative custom cases and the positive Valve case differed in suffix. C preserves the original observations but cannot support a provenance or VMAP-ownership conclusion. G later passed with custom `loadout.vxml_c` on the same unrelated map.

## 7. Experiment D: normal-client loose-file first attempt, suffix uncontrolled

Status: **original negative conclusion invalid; H retested successfully with `.vxml_c`**

### Intended question

This experiment tested a local preinstallation model without Tool Mode, addon mount, or `gameinfo.gi` override. The compiled bytes were copied to:

```text
game/csgo/panorama/layout/custom_game/escape_probe.vxml_c
game/csgo/panorama/styles/custom_game/welcome.vcss_c
```

Neither target existed before the test. Recorded files:

| File | Bytes | SHA-256 |
|---|---:|---|
| `escape_probe.vxml_c` | 1,562 | `A805C1583276F23CC58AF2337C0037B05DF63289B814C69B05CB68D25D72134D` |
| `welcome.vcss_c` | 2,391 | `FB632BD0FFD92ED0A2B5EBE3D4191E20C81CAD27946609EFA0802B400B1328EC` |

The client was a normal Steam launch with no `-tools`, no `-addon`, and no `gameinfo.gi` modification. It connected to the same independent `de_dust2` server.

The server entity, however, still requested:

```text
panorama/layout/custom_game/escape_probe.vxml
```

The client reported:

```text
[custom_hud] Layout xml is an invalid resource name "panorama/layout/custom_game/escape_probe.vxml"
```

### Corrected verdict

The entity request failed runtime resource-name validation before the experiment could establish whether `filesystem_stdio` enumerated or opened the loose compiled file. Physical presence of `.vxml_c` did not fix an entity that asked for `.vxml`.

H repeated the same deployment model with `loadout.vxml_c` and passed. D now documents only the wrong-suffix behavior. The hash-matched temporary files, which did not exist before the test, were deleted after cleanup without removing their parent directories or unrelated files.

## 8. Experiment E: packed retail Workshop/MMR VPK

Status: **independent pending item; Tool Mode success cannot substitute for it**

Tool Mode uses development loose output, `tools_asset_info.bin`, and explicit `-addon ... -tools` mounting. Packed retail VPK follows another client path. G proves only the former.

A participant supplied a same-day Mapcore Discord conversation in which a Valve developer answered that packed addon content entering Panorama still had a hard stop and that Valve would fix it. Because the conversation has no public URL, it is engineering context rather than reproducible proof.

After the fix, the packed-path retest must require:

- no Tool Mode;
- no client `-addon` argument;
- no `gameinfo.gi` modification;
- an official or unrelated community map;
- completed Workshop/MMR delivery;
- automatic client mount;
- a ModSharp dynamic entity naming custom `.vxml_c`;
- actual UI rendering without CustomHud validation errors.

The retest must separately record download, VPK mount, client termination behavior, layout validation, and final rendering. “Workshop download completed” and “server entity exists” are not sufficient.

## 9. Experiment F: server-driven dialog variable

Status: **pass; server write and client substitution confirmed**

### Objective and minimal native scope

The resource remained Valve's known-positive `btn_alert.vxml_c`; only its dialog variable changed:

```text
MainMenuWatchAlertText/alert_value = GAMEDATA 2000891 OK
```

The initial implementation copied only the current `CCSCustomHudLayout::SetDialogVariableString` Windows/Linux signature, gamedata registration lifecycle, three-argument `CUtlString` marshalling, and one global variable write. It did not yet include class, per-player state, input capture, or click handling.

Repository files:

```text
gamedata/panorama_layout_customhud.jsonc
src/PanoramaLayout/CustomHudNativeProbe.cs
```

Gamedata keys use actual CS function names. Project identity belongs in the gamedata filename, not a fabricated `PanoramaLayout::` symbol prefix.

The Windows server logged:

```text
Registered gamedata panorama_layout_customhud.jsonc
Resolved CCSCustomHudLayout::SetDialogVariableString at 0x7FF889583C20
Spawned custom_hud_layout entity 472:
layout=panorama/layout/btn_alert.vxml_c,
dialogVariable=MainMenuWatchAlertText/alert_value,
value=GAMEDATA 2000891 OK,
nativeApplied=true
```

### Client result: pass

The normal client rendered the orange `btn_alert` strip with:

```text
GAMEDA...
```

![Server dialog variable rendered by Valve btn_alert](evidence/05-dialog-variable-btn-alert.png)

The truncation is produced by Valve's fixed-width style. The unique prefix, compared with the previous empty strip, is sufficient A/B evidence that the server value reached the client and underwent dialog-variable substitution.

## 10. Experiment G: Tool Mode plus `cs2-customhud` loadout across maps

Status: **pass; overturns C's resource-ownership interpretation**

### Why this experiment was necessary

C's two custom negative cases requested source dependency names:

```text
panorama/layout/custom_game/escape_probe.vxml
panorama/layout/custom_game/welcome.vxml
```

Its positive control and the upstream `cs2-customhud` example use compiled runtime names ending in `.vxml_c`. G kept Tool Mode, addon, independent server, `de_dust2`, and dynamic entity creation unchanged while fixing this variable.

### Minimal compile correction to the upstream layout

The upstream `loadout.vxml` initially failed the current ResourceCompiler:

```text
RESOURCE COMPILE ERROR:
Found root panel with 'id' attribute, which is not permitted.
```

Following Valve's compilable `welcome.xml`, the business Panel was wrapped in an id-less root Panel while all labels and variable bindings remained unchanged:

```xml
<Panel class="Root">
    <Panel id="Loadout" class="loadout">
        ...unchanged labels and bindings...
    </Panel>
</Panel>
```

Compilation then reported:

```text
OK: 2 compiled, 0 failed, 0 skipped
```

| Output | Bytes | SHA-256 |
|---|---:|---|
| `loadout.vxml_c` | 1,799 | `BEBDA146C7F44F23206B0A90EB91F58A84F624A932C53B0D800DAA87A9F359EB` |
| `loadout.vcss_c` | 3,721 | `DABCBFAA21C834AA214BDC7AB16455DCA9A6E7E2110158AC304D2573A16E4DA2` |

The corrected sources are retained under `addon/panorama/.../loadout.xml` and `loadout.css`.

### Cross-map and mount controls

The real client command line contained:

```text
cs2.exe -addon cs_script_demo_copy -tools ... -insecure
```

After Tools startup, `tools_asset_info.bin` contained `loadout`:

```text
bytes:       5,218
timestamp:   2026-08-26 13:46:31 UTC+8
SHA-256:     A1C2FFCD1426D361974EF5A81AAB824C8026E5EBD6ECB8C0AD3C6554112C267F
```

`cs_script_demo.vmap` still referenced only `welcome.vxml` and contained no `loadout` string. Its SHA-256 was:

```text
CA1FC0E13C1BF590A966A02F546793FAC8552B0A15032FC7BAA4169DE5035F29
```

The client did not run the demo VMAP. It joined independent `de_dust2`, where the server used:

```text
layout = panorama/layout/custom_game/loadout.vxml_c
```

### Server state writes

Using F's verified native setter, the server wrote:

```text
LoadoutName/pname           = TOOL MODE STATE OK
LoadoutPrimary/primary      = AK-47
LoadoutSecondary/secondary  = DEAGLE
LoadoutKnife/knife          = KARAMBIT
LoadoutNades/nades          = SMOKE + FLASH
LoadoutArmor/armor          = 100 + HELMET
```

The server confirmed `dialogVariablesApplied=6/6`.

### Client result: pass

The actual client displayed the full custom orange/black panel and all six values on `de_dust2`:

![Custom Tool Mode loadout rendered on de_dust2](evidence/06-tool-mode-loadout.png)

This proves that the custom compiled layout and style can leave the VMAP that owns neither of them, and that all custom Panel IDs and dialog variables work across the independent server boundary. C's `invalid resource name` came from `.vxml`, not map ownership.

This result is limited to an explicitly mounted Tool Mode addon. It says nothing about base loose preinstallation or packed retail VPK.

## 11. Experiment H: normal client with base-directory loose files

Status: **pass; locally preinstalled client model established**

### Single changed variable

G's server, map, dynamic entity path, six dialog variables, VXML/VCSS bytes, and hashes were retained. Only client resource provenance changed:

```text
G: game/csgo_addons/cs_script_demo_copy + -addon ... -tools
H: game/csgo/panorama                  + normal client
```

Neither destination existed before deployment:

```text
game/csgo/panorama/layout/custom_game/loadout.vxml_c
game/csgo/panorama/styles/custom_game/loadout.vcss_c
```

The files matched G exactly:

| File | Bytes | SHA-256 |
|---|---:|---|
| `loadout.vxml_c` | 1,799 | `BEBDA146C7F44F23206B0A90EB91F58A84F624A932C53B0D800DAA87A9F359EB` |
| `loadout.vcss_c` | 3,721 | `DABCBFAA21C834AA214BDC7AB16455DCA9A6E7E2110158AC304D2573A16E4DA2` |

The successful client process was:

```text
cs2.exe -steam -worldwide -insecure
```

It had no `-tools`, no `-addon`, and no `gameinfo.gi` override.

### Client result: pass

The normal client rendered the same complete custom loadout. An expanded radar initially overlapped part of it; the surrounding custom background, title, and values remained visible, establishing a z-order issue rather than a resource failure.

![Initial radar overlap](evidence/07-loadout-radar-overlap.png)

The only VCSS change was:

```css
.loadout
{
    z-index: 1000;
}
```

ResourceCompiler accepted it with `1 compiled, 0 failed`. The new VCSS was 3,763 bytes with SHA-256:

```text
232A5765A7312C12056E17088720C0475E4E5E7FDD3D6E9F185BCB18FA63AF9F
```

After a full normal-client restart and first-person retest, the loadout rendered above the expanded radar:

![First-person z-index retest](evidence/08-loadout-zindex-fixed.png)

### Verdict

> In build 2000891, a deployment program can preinstall compiled custom VXML/VCSS under base `game/csgo/panorama`, and a normal client without `-tools` or `-addon` can render the dynamic Custom HUD on an arbitrary server map—provided the entity uses `.vxml_c`.

This is a controlled-client preinstallation model, not zero-install distribution to public players and not evidence that packed Workshop/MMR VPK works.

After the experiment, the two files that had not existed before the test were hash-checked and removed. Parent directories and unrelated files were preserved.

## 12. Experiment I: server-driven Panel class

Status: **pass; `SetHasClass` through client VCSS state is established**

### Controlled state selector

The VXML target remained:

```xml
<Panel id="Loadout" class="loadout">
```

It did not contain `server-class-ok`. VCSS added an initially unmatched rule:

```css
.loadout.server-class-ok
{
    border: 3px solid #48ff8a;
    box-shadow: fill #1aff70a0 0px 0px 14px 0px;
}

.loadout.server-class-ok .loadout__name
{
    color: #48ff8a;
}
```

Only a successful server class write could produce the green border, title, and glow.

### Native and gamedata evidence

`CustomHudNativeProbe.cs` wrapped:

```text
CCSCustomHudLayout::SetDialogVariableString
CCSCustomHudLayout::SetHasClass
```

The module called:

```csharp
TrySetHasClass(layout, "Loadout", "server-class-ok", enabled: true)
```

At this experiment stage, the normalized gamedata was 1,132 bytes with SHA-256:

```text
7FD141A45BAE5110DF8BF92046438DCB276B904F963FD56DF4FAF8BD2F5486BD
```

The server logged successful Windows signature resolution and application:

```text
Resolved CCSCustomHudLayout::SetDialogVariableString at 0x7FF889583C20
Resolved CCSCustomHudLayout::SetHasClass at 0x7FF889583FF0
Set panel class Loadout/server-class-ok=true; nativeApplied=true
Spawned custom_hud_layout entity 497: dialogVariablesApplied=6/6, classApplied=true
```

### Client result: pass

The normal client rendered the title, border, and glow in green while keeping all six variables. Since the source Panel had no such class, the screenshot establishes the complete native-state-to-VCSS chain.

![Green server class with the initial stacking fault](evidence/09-server-class-green-overlap.png)

The first green screenshot again showed radar overlap. Adding `z-index: 10000` to the activated selector produced a final 4,174-byte VCSS with SHA-256:

```text
2F9A8A54E376488600ECB89A5B13990F8C9D6968EA10FE89A06E7ABFC47E0503
```

After a full client restart, the green loadout rendered above the radar:

![Green class state after cold-start fix](evidence/10-server-class-green-fixed.png)

The test changed both selector placement and z-index value across observations, so it does not isolate specificity from numeric value or HUD rebuild timing. That uncertainty affects stacking diagnosis, not the established `SetHasClass` chain.

## 13. Experiment J: VCSS cache boundary for remote dynamic Custom HUD in Tool Mode

Status: **negative result established; external compilation does not invalidate the current process, full restart does**

### Bidirectional A/B

Two byte- and hash-identified VCSS versions were alternated in the same Tool Mode addon, server, and dynamic entity:

| Version | `.loadout.server-class-ok` | Bytes | SHA-256 |
|---|---|---:|---|
| Fault | no additional z-index | 4,128 | `3E8F682ECE6A7B3CC8A0779921318D6EB865F4B0E27E304B0E9F3C8918E56281` |
| Fix | `z-index: 10000` | 4,174 | `2F9A8A54E376488600ECB89A5B13990F8C9D6968EA10FE89A06E7ABFC47E0503` |

Both versions retained the base `.loadout { z-index: 1000; }`. ResourceCompiler reported `1 compiled, 0 failed, 0 skipped` on every switch.

### A: overwrite a fixed process with the fault version

The client cold-loaded the fixed version, with the green panel above the radar. While it stayed connected, the fault VCSS was compiled to the addon output. The visible state did not change. Selecting/triggering the resource in Asset Browser still did not change it.

![Asset Browser did not invalidate the dynamic HUD style cache](evidence/11-vcss-cache-after-asset-browser.png)

After a complete Tool Mode restart, the same server entity reliably displayed the fault state. The disk file had therefore changed; the pre-restart visual stability was cache behavior, not a failed compile.

### B: overwrite a fault process with the fixed version

Starting from a cold-loaded fault process, the fix was compiled back to the known 4,174-byte hash. Results:

```text
compile while connected              still fault state
disconnect and reconnect             still fault state
fully restart Tool Mode              fixed state loaded
```

The server process, logical resource name, six variables, and `server-class-ok` state remained unchanged throughout.

### Verdict and scope

> For this explicitly mounted addon, remote server-created `custom_hud_layout` path, overwriting the compiled VCSS does not invalidate the style already loaded by the current Tool Mode process. Disconnect/reconnect is insufficient; full process restart reloads it.

The bidirectional test rules out a one-off visual coincidence. Cache lifetime spans server connections and layout entity reconstruction and is close to the client process or addon-mount lifetime.

A plausible but unconfirmed explanation is that Workshop Tools hot-update tracking covers VMAP/tool-document authoring dependencies, while a remote network `custom_hud_layout` is outside that dependency graph. The experiment establishes the cache boundary, not Valve's internal implementation or test coverage.

The addon and repository ended on the fixed `2F9A8A…E0503` version.

## 14. Experiment K: two-page server-driven interactive menu

Status: **pass; command, per-player state, input capture, Button return path, and server page state machine all operate**

### Architecture

The previous primitives were composed into an actual menu:

```text
player types .menu (CommandCenter also accepts !menu and /menu)
  -> CommandCenter supplies the caller IGameClient
  -> SetHasClassForPlayer(slot, ServerMenu, is-open, true)
  -> SetInputCaptureEnabled(slot, true)
  -> player clicks MenuOpenActions
  -> CS_UM_CustomHudClicked (390)
  -> CustomHudClickedReceiver detour
  -> server changes is-active on the two page Panels
  -> second-page Buttons return to the server
  -> chat output / theme / back / close
```

“Next page” is not a second VXML load. `MenuHomePage` and `MenuActionsPage` live in one static layout; the server changes per-player classes. The client layout contains only Panel, Label, and Button. It has no `<scripts>`, `onactivate`, or other Panorama JS.

The command uses the current `Sharp.Modules.CommandCenter` API. It does not use the obsolete CommandManager.

### Client resources

Sources:

```text
addon/panorama/layout/custom_game/server_menu.xml
addon/panorama/styles/custom_game/server_menu.css
```

ResourceCompiler reported:

```text
OK: 2 compiled, 0 failed, 0 skipped
```

| Output | Bytes | SHA-256 |
|---|---:|---|
| `server_menu.vxml_c` | 2,848 | `32B4740055F2D41FB8BFE063F1117390B1DDF8951C50E3FEF765C9E483ED1730` |
| `server_menu.vcss_c` | 8,014 | `6FB14C1F39B23D2C504DD1C48B611228A6651A3D75F4B55F9515A03BE2005582` |

Runtime paths:

```text
panorama/layout/custom_game/server_menu.vxml_c
panorama/styles/custom_game/server_menu.vcss_c
```

### Required gamedata

The current scene uses the necessary subset from the same-day `cs2-customhud/.assets/gamedata/customhud.jsonc`:

| CS function | Purpose | Runtime result |
|---|---|---|
| `CCSCustomHudLayout::SetDialogVariableString` | retained text probe | previously verified |
| `CCSCustomHudLayout::SetHasClass` | retained global class probe | previously verified |
| `CCSCustomHudLayout::SetHasClassForPlayer` | per-player menu/page state | resolved and exercised |
| `CCSCustomHudLayout::SetInputCaptureEnabled` | per-player mouse input | resolved and exercised |
| `CCSCustomHudLayout::CustomHudClickedReceiver` | inbound msg 390 Button ID | detoured and exercised |

Unused per-player dialog/intern helpers were not copied. The expanded gamedata is 2,746 bytes with SHA-256:

```text
CDF070B77A7B834A31265528C020146D5A8DD8590AD96378D1D419A6CE5424DC
```

### Startup and first-open evidence

The module built with zero errors and warnings. Initial server logs included:

```text
Installed CCSCustomHudLayout::CustomHudClickedReceiver hook at 0x7FF88A279F80
Registered !menu / ms_menu through CommandCenter
Spawned interactive custom_hud_layout entity 303
layout=panorama/layout/custom_game/server_menu.vxml_c
```

Player `laper32`, slot 2, typed `.menu`. Windows signatures resolved lazily and the server applied the initial state:

```text
Resolved CCSCustomHudLayout::SetHasClassForPlayer at 0x7FF88A8C4290
Resolved CCSCustomHudLayout::SetInputCaptureEnabled at 0x7FF88A8C4320
Set player panel class slot=2 ServerMenu/is-open=True; nativeApplied=True
Set player panel class slot=2 MenuHomePage/is-active=True; nativeApplied=True
Opened interactive menu for slot 2 (laper32)
```

The client showed the complete black/orange menu and a Button hover state over independent `de_dust2`. Hover itself is client evidence that input capture was active.

![Two-page server menu home page](evidence/12-interactive-menu-home.png)

### Default M key: negative boundary

The client reported its default bind:

```text
bind [player 0]: "m" = "teammenu"
```

Registering `ICommandRegistry.AddCommandListener("teammenu", ...)` succeeded at startup, but pressing M did not enter the callback and did not open the custom menu. A menu log initially attributed to M was corrected by the user: it came from `.menu`.

Ptr.Enterprise's comparable pattern listens to `player_ping`, a client string command that reaches the server. `teammenu` opens client UI and did not cross the same boundary. CommandCenter cannot intercept a physical key whose command never reaches the server.

A developer can voluntarily run `bind m "ms_menu"`, which replaces `teammenu`, but this is client configuration and is not a production prerequisite. Normal users use `.menu`; no team-menu cvar is required.

The ineffective listener was removed. The generic `menu` handler was also implemented as a per-slot open/close toggle. A DLL placed under `reload/` was consumed by `changelevel de_dust2` without restarting the process:

```text
Reloading module [PanoramaLayout]...
Loading PanoramaLayout interactive menu build 2026-08-26T07:16:32Z; hotReload=True
Module [PanoramaLayout] reloaded successfully
Registered !menu / ms_menu through CommandCenter
Spawned interactive custom_hud_layout entity 344:
layout=panorama/layout/custom_game/server_menu.vxml_c
```

This proves that the runtime no longer contains the ineffective listener and that the command/layout reloaded. The first `.menu` open and Button close were already exercised. The newly implemented “second `.menu` closes” branch compiled and loaded but does not yet have a separately preserved second-chat-command runtime log.

### Button return and page state machine

Close first proved input release:

```text
Received Custom HUD click MenuClose from slot 2
Set player panel class slot=2 ServerMenu/is-open=False; nativeApplied=True
Closed interactive menu for slot 2: inputReleased=True, hidden=True
```

After reopening with `.menu`, the complete interaction produced:

```text
Received Custom HUD click MenuOpenActions from slot 2
MenuHomePage/is-active=False
MenuActionsPage/is-active=True

Received Custom HUD click MenuPrintHello from slot 2
MenuActionResult/is-visible=True

Received Custom HUD click MenuToggleAccent from slot 2
ServerMenu/is-accent=True

Received Custom HUD click MenuBack from slot 2
MenuActionsPage/is-active=False
MenuHomePage/is-active=True

Received Custom HUD click MenuClose from slot 2
ServerMenu/is-open=False
inputReleased=True, hidden=True
```

`MenuPrintHello` also printed only to the clicking client:

```text
[Server Menu] Hello laper32; ModSharp received MenuPrintHello.
```

Unknown Button IDs are ignored by a server-side switch allowlist. The receiver also verifies the returned layout entity pointer and processes only the entity created by this module.

### Verdict and remaining K boundaries

> Build 2000891 Custom HUD is sufficient for a real server-driven menu without client JS: chat open, per-player state, mouse input, Button return, multipage navigation, server actions, and safe close work independently of VMAP and point_script.

The upstream Windows signatures/click receiver that previously had static-analysis-only status were exercised on the current Windows build. Addresses remain build- and ASLR-dependent and must be re-resolved after updates.

Only one human player was used. The function calls are per-player, but a two-human visual A/B is still required before claiming that a second player cannot see the first player's menu. Button spamming, disconnect cleanup, round restart, and packed retail VPK remain separate reliability/distribution tests.

## 15. Final verdict matrices

### Client resource loading and provenance

| Client resource source | Client mode | Current map | Result | What it proves |
|---|---|---|---|---|
| Addon resource referenced by current VMAP | Tool Mode | `cs_script_demo_copy` | Pass | Addon VXML is editable, compilable, renderable |
| Explicitly mounted addon loose compiled resource | Tool Mode | independent `de_dust2` | Pass | Addon resource can leave the VMAP that owns neither it nor the entity |
| Base `game/csgo/panorama` loose compiled resource | Normal client | independent `de_dust2` | Pass | Controlled-client local preinstallation works |
| Workshop/MMR packed VPK | Normal client | arbitrary map | Pending | Cannot be inferred from the other three paths |

### Server control and interaction

| Capability | Entry point | Evidence | Verdict and scope |
|---|---|---|---|
| Create/destroy layout | public `IEntityManager.SpawnEntitySync` | server entity logs plus matching client HUD | **Pass** |
| Dialog variable | `CCSCustomHudLayout::SetDialogVariableString` | Valve alert text and loadout 6/6 | **Pass** |
| Global Panel class | `CCSCustomHudLayout::SetHasClass` | source lacks class; client turns green | **Pass** |
| Per-player Panel class | `CCSCustomHudLayout::SetHasClassForPlayer` | slot 2 menu/page writes and client UI | **Call path passes**; two-human isolation pending |
| Per-player input capture | `CCSCustomHudLayout::SetInputCaptureEnabled` | hover/click and `inputReleased=True` | **Pass** |
| Button return | `CCSCustomHudLayout::CustomHudClickedReceiver` | msg 390 receives every allowlisted ID | **Pass on Windows** |
| Chat entry | CommandCenter generic `menu` | actual `.menu` opens | **Pass**; `!` and `/` are equivalent API prefixes |
| Default M key | client `teammenu` | pressing M did not reach listener | **Negative result**; not a user entry |
| Module hot reload | `reload/` plus next map-start event | unload/load and `hotReload=True` logs | **Pass**; separate from client VCSS cache |

### One-line status

```text
editable inside map = yes
Tool Mode addon across maps = yes (`loadout.vxml_c`, de_dust2)
server dialog variables = yes (6/6)
server Panel class = yes (`Loadout/server-class-ok=true`)
base game/csgo loose file = yes (normal client, no -tools/-addon)
Tool Mode remote dynamic HUD VCSS hot refresh = no (full process restart required)
Tool Mode dynamic HUD Button interaction = yes (Windows, msg 390, full two-page flow)
default M/teammenu server takeover = no (client-side; use `.menu`)
retail Workshop/MMR packed VPK = independent path; fix/retest pending
```

Remaining production work is limited to packed retail VPK, a two-human per-player visual-isolation A/B, and reliability stress tests for Button spamming, disconnect, and round restart.

## 16. Maintenance rules

Until the packed retail VPK retest is complete:

1. preserve the build 2000888 failure evidence;
2. never describe the old conclusion as a permanent boundary across all CS2 builds;
3. never present the `cs_script_demo` map-owned pass as cross-map evidence;
4. record the CS2 build before choosing regressions after every Valve update;
5. state client mode, resource source, current map, and entity origin in every final claim;
6. preserve original screenshots and hashes under [`docs/evidence`](evidence/README.md).

This keeps historical facts intact while allowing engineering conclusions to change when Valve changes the implementation, without mixing builds or resource trust domains.
