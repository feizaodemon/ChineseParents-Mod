# ChineseParents-Mod

中国式家长（Steam AppID 736190）的修改器插件，基于 BepInEx 5。进游戏后按 F1 打开面板。

![面板截图：解锁页、属性页、主菜单下的属性页](docs/preview.jpg)

## 能做什么

| 位置 | 按钮 | 效果 | 存到哪里 |
|---|---|---|---|
| 解锁页 | 成就 · 全部解锁 | 游戏里 31 类成就全部完成，Steam 成就一起解锁 | 账号存档、Steam |
| 解锁页 | 图鉴 · 全部解锁 | 传承等级升满，特长、职业、人物全部收录 | 账号存档 |
| 解锁页 | 传家宝 · 全部领取 | 每种传家宝各拿一件 | 账号存档 |
| 属性页 | 设定 / 锁定 | 13 项属性改成任意数值，锁定后不再变动 | 本局存档，回合结束时由游戏保存 |

- 解锁页在主菜单和游戏中都能用，点完马上写入账号存档。
- 属性页要进入一局游戏后才能用。
- Steam 成就解锁后会出现在个人资料上。想撤销，只能用 Steam Achievement Manager 这类外部工具。

## 实现

- 所有改动都调用游戏自己的管理器：`AchievementMgr`、`CollectionMgr`、`NewTrickMgr`、`NewJobMgr`、`NpcMgr`、`TreasureMgr`、`NewPlayerDataMgr`。
- 账号存档的写法和游戏相同：先调 `NewRecordMgr.RefreshAccountInfo`，再用 `IOHelper.SetData` 写盘，不碰本局存档。
- Steam 成就用 Steamworks.NET 的 `SteamUserStats` 遍历全部成就名。
- 面板用 IMGUI 绘制。贴图在运行时生成，字号和圆角按屏幕高度缩放（以 1080p 为基准），插件只有一个 DLL。

## 目录

- `src/ChineseParentsMod/`：插件源码。`Cheats.cs` 放游戏逻辑，`Plugin.cs` 放面板布局，`Theme.cs` 放配色和样式。
- `tools/Decompile/`：把游戏 DLL 反编译到 `ref/`，只作本地 API 参考（`ref/` 不提交）。
- `docs/`：README 用的截图。
- `save-backup/`：存档备份（不提交）。

## 构建

```bash
dotnet build src/ChineseParentsMod -c Release
```

### 预览版

预览版用来调界面，不用手动操作游戏。它会在标题画面用假数据打开面板，截 4 张图到 `<游戏目录>\BepInEx\preview\`，然后自动退出游戏。

```bash
dotnet build src/ChineseParentsMod -c Release -p:DefineConstants=PREVIEW -o build/preview
```

用完记得换回正式版 DLL。

## 安装

先关掉游戏。

1. 把 BepInEx 5.4.23.5（x64 Mono）的 `winhttp.dll`、`doorstop_config.ini`、`.doorstop_version` 和 `BepInEx\core\` 放到游戏根目录。
2. 把 `src/ChineseParentsMod/bin/Release/netstandard2.1/ChineseParentsMod.dll` 复制到 `<游戏目录>\BepInEx\plugins\ChineseParentsMod\`。

日志在 `<游戏目录>\BepInEx\LogOutput.log`，本插件的行以 `[... :Chinese Parents Cheat Panel]` 开头。

## 卸载

删掉游戏根目录的 `winhttp.dll`，所有 BepInEx 插件就都不加载了。

## 恢复存档

存档目录：`%USERPROFILE%\AppData\LocalLow\moyuwan\chineseparentsgame\Save`。

1. 关掉游戏。
2. 用 `save-backup\<日期>\chineseparentsgame-Save\` 里的文件覆盖存档目录。
3. 存档开了 Steam Cloud。恢复后如果 Steam 提示冲突，选本地版本。
