# 截图证据清单

[English version](README.md)

本目录保存 2026-08-26 CS2 `custom_hud_layout` 重测期间采集的原始客户端截图。

Codex 将图片以 PNG data URI 的形式保存在 root session JSONL 中，而不是作为独立文件放在 `.codex/attachments` 下。本目录中的文件由这些 PNG payload 直接解码而来，并非从聊天界面显示的缩略图重建；图片未经缩放、重压缩、裁剪、标注或剥离处理。下列哈希标识解码后 PNG 的确切字节内容。

## 归档流程

今后每次向本目录加入图片时：

1. 保留图片原始字节；不得缩放、重压缩、裁剪、标注或剥离元数据；
2. 在下表记录像素尺寸、字节长度、SHA-256、实验编号、客户端模式、地图及观察时间；
3. 在 build 2000891 实验记录的中英文版本中同时引用该文件；
4. 服务端日志和已编译资源哈希应作为独立证据保存——截图不能取代它们。

## 接收文件台账

| 文件 | 截图时间（UTC+8） | 尺寸 | 字节数 | SHA-256 | 实验 | 客户端模式／地图 |
|---|---|---:|---:|---|---|---|
| [`01-asset-browser-welcome-source.png`](01-asset-browser-welcome-source.png) | 2026-08-26 11:15:01 | 340×275 | 33,822 | `0E57E6734E72FF8240A401AAFDEFE269E904566ADE6A10E353C946ED8FD10F10` | A/B 背景资料 | Workshop Tools / `cs_script_demo_copy` |
| [`02-official-welcome-map.png`](02-official-welcome-map.png) | 2026-08-26 11:17:53 | 670×355 | 91,060 | `63514BF91AB4CC37B51DB8B99B621F351E832BCE7C0520D536A5D40896383F3E` | A | Tool Mode / demo VMAP |
| [`03-edited-welcome.png`](03-edited-welcome.png) | 2026-08-26 11:24:00 | 385×207 | 28,352 | `8678819F5610C215CFA567C3713A68E458CEB3409B08204BF29A2D9488615D20` | B | Tool Mode / demo VMAP |
| [`04-retail-hud-reference.png`](04-retail-hud-reference.png) | 2026-08-26 12:55:40 | 490×73 | 57,121 | `8C3D3A2C570DB30F755B4F18C044A83863B28F0D6786CA91F7752FA5AB3647A2` | 背景资料 | 普通 CS2 HUD 参考 |
| [`05-dialog-variable-btn-alert.png`](05-dialog-variable-btn-alert.png) | 2026-08-26 13:33:50 | 148×38 | 9,902 | `6A283651840D7706AE44AEC2D082502D8E96647381DE68D9A21E346FC4626068` | F | 普通客户端 / `de_dust2` |
| [`06-tool-mode-loadout.png`](06-tool-mode-loadout.png) | 2026-08-26 13:47:09 | 203×165 | 25,620 | `6F7B9CCB04AB1CFC338EB362BECD59442221ED9F85C534330749C229046D72D9` | G | Tool Mode addon / `de_dust2` |
| [`07-loadout-radar-overlap.png`](07-loadout-radar-overlap.png) | 2026-08-26 13:56:48 | 281×252 | 65,467 | `9A72B845C024E540DBC7EA93759117014974F5157F46915397966910D963F51F` | H | 普通客户端 / 展开雷达 |
| [`08-loadout-zindex-fixed.png`](08-loadout-zindex-fixed.png) | 2026-08-26 14:01:41 | 387×401 | 183,828 | `987C862F7A09A29B4E0C19147B5A243AC23198B43A01E63A8BDE17A536E112A5` | H | 普通客户端 / 第一人称 z-index 重测 |
| [`09-server-class-green-overlap.png`](09-server-class-green-overlap.png) | 2026-08-26 14:09:37 | 303×289 | 98,572 | `9BC8AAF773C3F6D77571CFDD186AB88D5A1CDC8587047E12F5B08E55AF61F5C9` | I/J | Tool Mode / 绿色 class，故障状态 |
| [`10-server-class-green-fixed.png`](10-server-class-green-fixed.png) | 2026-08-26 14:13:36 | 300×285 | 40,759 | `5C637186CFFFC701E577A43AAF5AEDD356CC4B3C3084F4427625429800A74006` | I/J | Tool Mode / 绿色 class，修复状态 |
| [`11-vcss-cache-after-asset-browser.png`](11-vcss-cache-after-asset-browser.png) | 2026-08-26 14:32:07 | 262×203 | 59,016 | `CF8B51C2FA522E2B6B267972792D48F7DD34DEF89F85510CA1FF7C2E9CE8F71A` | J | Tool Mode / 缓存后续验证 |
| [`12-interactive-menu-home.png`](12-interactive-menu-home.png) | 2026-08-26 14:56:51 | 501×414 | 248,953 | `7E96FEF26045BD4255F18DBB9CEAA49DBC2E50790B34BED1A013EEE5C0B43FB6` | K | Tool Mode / `de_dust2` |
