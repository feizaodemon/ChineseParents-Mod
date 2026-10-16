# ChineseParents-Mod

中国式家长（Steam AppID 736190）的 BepInEx 5 插件。按 F1 打开修改面板。

## 功能

| 按钮 | 做什么 | 写到哪里 |
|---|---|---|
| 全成就（含 Steam 成就） | 游戏内 31 类成就全部完成；Steam 成就逐个解锁 | 账号存档 + Steam |
| 全图鉴 | 传承等级升到最高；特长、职业、人物全部收录 | 账号存档 |
| 全传家宝 | 传家宝表里每件各得一件 | 账号存档 |
| 属性 | 13 项属性可设定，可锁定（每帧写回） | 本局存档，回合结束时由游戏保存 |

- 解锁按钮在登录后（主菜单或游戏中）可用，点完立即写账号存档。
- 属性只在进入存档后可用。
- Steam 成就解锁后显示在个人资料上。要撤销只能用 Steam Achievement Manager 之类的外部工具。

## 实现要点

- 全部改动走游戏自己的管理器：`AchievementMgr`、`CollectionMgr`、`NewTrickMgr`、`NewJobMgr`、`NpcMgr`、`TreasureMgr`、`NewPlayerDataMgr`。
- 账号存档用游戏的 `NewRecordMgr.RefreshAccountInfo` + `IOHelper.SetData` 写，不碰本局存档。
- Steam 成就用 Steamworks.NET 的 `SteamUserStats` 遍历全部成就名。

## 目录

- `src/ChineseParentsMod/`：插件源码
- `tools/Decompile/`：把游戏 DLL 反编译到 `ref/`，只作本地 API 参考（`ref/` 不提交）
- `save-backup/`：存档备份（不提交）

## 构建

```bash
dotnet build src/ChineseParentsMod -c Release
```

## 安装

游戏必须关闭。

1. BepInEx 5.4.23.5（x64 Mono）：`winhttp.dll`、`doorstop_config.ini`、`.doorstop_version`、`BepInEx\core\` 放到游戏根目录。
2. 复制 `src/ChineseParentsMod/bin/Release/netstandard2.1/ChineseParentsMod.dll` 到 `<游戏目录>\BepInEx\plugins\ChineseParentsMod\`。

日志：`<游戏目录>\BepInEx\LogOutput.log`，本插件的行以 `[... :Chinese Parents Cheat Panel]` 开头。

## 卸载

删除游戏根目录的 `winhttp.dll` 即停用全部 BepInEx 插件。

## 恢复存档

存档目录：`%USERPROFILE%\AppData\LocalLow\moyuwan\chineseparentsgame\Save`。

1. 关闭游戏。
2. 用 `save-backup\<日期>\chineseparentsgame-Save\` 里的文件覆盖存档目录。
3. 存档开了 Steam Cloud。恢复后若 Steam 提示冲突，选本地版本。
