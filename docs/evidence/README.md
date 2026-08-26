# Screenshot evidence manifest

[中文版](README.zh-CN.md)

This directory contains the original client screenshots captured during the 2026-08-26 CS2 `custom_hud_layout` retest.

Codex stored the images as PNG data URIs in the root session JSONL instead of individual files under `.codex/attachments`. The PNG payloads were decoded directly into this directory. They were not reconstructed from rendered chat thumbnails and were not resized, recompressed, cropped, annotated, or stripped. The hashes below identify the exact decoded PNG bytes.

## Intake procedure

For every future image copied into this directory:

1. preserve the image bytes; do not resize, recompress, crop, annotate, or strip metadata;
2. record pixel dimensions, byte length, SHA-256, experiment ID, client mode, map, and observation time below;
3. reference the file from both language versions of the build 2000891 report;
4. keep server logs and compiled-resource hashes as separate evidence—the screenshot does not replace them.

## Received-file ledger

| File | Captured (UTC+8) | Dimensions | Bytes | SHA-256 | Experiment | Client mode/map |
|---|---|---:|---:|---|---|---|
| [`01-asset-browser-welcome-source.png`](01-asset-browser-welcome-source.png) | 2026-08-26 11:15:01 | 340×275 | 33,822 | `0E57E6734E72FF8240A401AAFDEFE269E904566ADE6A10E353C946ED8FD10F10` | A/B context | Workshop Tools / `cs_script_demo_copy` |
| [`02-official-welcome-map.png`](02-official-welcome-map.png) | 2026-08-26 11:17:53 | 670×355 | 91,060 | `63514BF91AB4CC37B51DB8B99B621F351E832BCE7C0520D536A5D40896383F3E` | A | Tool Mode / demo VMAP |
| [`03-edited-welcome.png`](03-edited-welcome.png) | 2026-08-26 11:24:00 | 385×207 | 28,352 | `8678819F5610C215CFA567C3713A68E458CEB3409B08204BF29A2D9488615D20` | B | Tool Mode / demo VMAP |
| [`04-retail-hud-reference.png`](04-retail-hud-reference.png) | 2026-08-26 12:55:40 | 490×73 | 57,121 | `8C3D3A2C570DB30F755B4F18C044A83863B28F0D6786CA91F7752FA5AB3647A2` | Context | Normal CS2 HUD reference |
| [`05-dialog-variable-btn-alert.png`](05-dialog-variable-btn-alert.png) | 2026-08-26 13:33:50 | 148×38 | 9,902 | `6A283651840D7706AE44AEC2D082502D8E96647381DE68D9A21E346FC4626068` | F | Normal client / `de_dust2` |
| [`06-tool-mode-loadout.png`](06-tool-mode-loadout.png) | 2026-08-26 13:47:09 | 203×165 | 25,620 | `6F7B9CCB04AB1CFC338EB362BECD59442221ED9F85C534330749C229046D72D9` | G | Tool Mode addon / `de_dust2` |
| [`07-loadout-radar-overlap.png`](07-loadout-radar-overlap.png) | 2026-08-26 13:56:48 | 281×252 | 65,467 | `9A72B845C024E540DBC7EA93759117014974F5157F46915397966910D963F51F` | H | Normal client / expanded radar |
| [`08-loadout-zindex-fixed.png`](08-loadout-zindex-fixed.png) | 2026-08-26 14:01:41 | 387×401 | 183,828 | `987C862F7A09A29B4E0C19147B5A243AC23198B43A01E63A8BDE17A536E112A5` | H | Normal client / first-person z-index retest |
| [`09-server-class-green-overlap.png`](09-server-class-green-overlap.png) | 2026-08-26 14:09:37 | 303×289 | 98,572 | `9BC8AAF773C3F6D77571CFDD186AB88D5A1CDC8587047E12F5B08E55AF61F5C9` | I/J | Tool Mode / green class, fault state |
| [`10-server-class-green-fixed.png`](10-server-class-green-fixed.png) | 2026-08-26 14:13:36 | 300×285 | 40,759 | `5C637186CFFFC701E577A43AAF5AEDD356CC4B3C3084F4427625429800A74006` | I/J | Tool Mode / green class, fixed state |
| [`11-vcss-cache-after-asset-browser.png`](11-vcss-cache-after-asset-browser.png) | 2026-08-26 14:32:07 | 262×203 | 59,016 | `CF8B51C2FA522E2B6B267972792D48F7DD34DEF89F85510CA1FF7C2E9CE8F71A` | J | Tool Mode / cache follow-up |
| [`12-interactive-menu-home.png`](12-interactive-menu-home.png) | 2026-08-26 14:56:51 | 501×414 | 248,953 | `7E96FEF26045BD4255F18DBB9CEAA49DBC2E50790B34BED1A013EEE5C0B43FB6` | K | Tool Mode / `de_dust2` |
