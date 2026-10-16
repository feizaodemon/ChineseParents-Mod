# ChineseParents-Mod

中国式家长（Steam AppID 736190）的修改器插件，基于 BepInEx 5。进游戏后按 F1 打开面板。

![面板截图：解锁页和存档页](docs/preview.jpg)

## 能做什么

| 位置 | 控件 | 效果 | 存到哪里 |
|---|---|---|---|
| 解锁页 | 成就 · 全部解锁 | 游戏里 31 类成就全部完成；“同时解锁 Steam 成就”开着时，Steam 成就一起解锁 | 账号存档、Steam |
| 解锁页 | 图鉴 · 全部解锁 | 传承等级升满，特长、职业、人物全部收录 | 账号存档 |
| 解锁页 | 传家宝 · 全部领取 | 每种传家宝各拿一件 | 账号存档 |
| 属性页 | 设定 / 锁定 | 13 项属性改成任意数值，锁定后不再变动 | 本局存档，回合结束时由游戏保存 |
| 存档页 | 存快照 | 把整个存档文件夹复制一份 | `ModSnapshots` 文件夹 |
| 存档页 | 回滚 / 删除 | 点两次确认。回滚在下次启动游戏时生效 | `ModSnapshots` 文件夹 |
| 存档页 | 解锁前自动存一份快照 | 每次解锁前先存快照，自动快照最多留 10 份 | 插件配置 |
| 设置页 | 文本速度 | 逐字文字按 1×、2×、4×、8× 或瞬间显示 | 插件配置 |
| 设置页 | 跳过开场动画 | 启动时跳过工作室 logo，下次启动生效 | 插件配置 |

- 解锁页在主菜单和游戏中都能用，点完马上写入账号存档。
- 属性页要进入一局游戏后才能用。
- Steam 成就解锁后会出现在个人资料上。想撤销，只能用 Steam Achievement Manager 这类外部工具。不想动 Steam，就把成就卡片里的开关关掉。
- 快照存在 `%USERPROFILE%\AppData\LocalLow\moyuwan\chineseparentsgame\ModSnapshots`，和 `Save` 同级，Steam Cloud 不会同步它。手动快照不会自动删除。
- 鼠标在面板上时，点击不会传到下面的游戏按钮。
- 设置存在 `<游戏目录>\BepInEx\config\local.chineseparents.cheatpanel.cfg`。

## 启动耗时

从进程启动到登录阶段，每种情况测两次取平均（2026-10-16，RTX 5070 机器）：

| 情况 | 耗时 |
|---|---|
| 原版，不装 BepInEx | 3.53 s |
| 装 BepInEx，不跳过开场 | 3.98 s |
| 装 BepInEx，跳过开场（默认） | 2.03 s |

BepInEx 本身多花约 0.45 s。插件自己的加载（读配置、打补丁）约 10–17 ms。开场 logo 固定占 2 s，跳过后比原版快约 1.5 s。

## 实现

- 所有改动都调用游戏自己的管理器：`AchievementMgr`、`CollectionMgr`、`NewTrickMgr`、`NewJobMgr`、`NpcMgr`、`TreasureMgr`、`NewPlayerDataMgr`。
- 账号存档的写法和游戏相同：先调 `NewRecordMgr.RefreshAccountInfo`，再用 `IOHelper.SetData` 写盘，不碰本局存档。
- Steam 成就用 Steamworks.NET 的 `SteamUserStats` 遍历全部成就名。
- 文本速度：游戏的逐字文字都经过 `UIHelper.PlayTxt` 或 `PlayableText.PlayTxt`，Harmony 补丁在这两个入口把时长除以倍速。点击加速和直接显示全文的逻辑不变。
- 跳过开场：补丁替换 `BeginWnd.OnShow`，做法和游戏在调试构建里的处理相同，直接加载 `Login` 场景。
- 快照：复制 `Save` 里除 `steam_autocloud.vdf` 外的全部文件。游戏运行时内存里还有存档数据，所以回滚只先记下目标，下次启动时插件在游戏读存档之前覆盖文件。覆盖前会先把当前存档存成一份“回滚前”快照。
- 防点击穿透：IMGUI 不拦截 uGUI 的点击。补丁在鼠标位于面板上时跳过当帧的 `EventSystem.Update`。直接读 `Input` 的少数游戏代码（如红包小游戏）不受这个补丁影响。
- 面板用 IMGUI 绘制。贴图在运行时生成，字号和圆角按屏幕高度缩放（以 1080p 为基准），插件只有一个 DLL。

## 目录

- `src/ChineseParentsMod/`：插件源码。
  - `Cheats.cs`：解锁和属性逻辑
  - `Patches.cs`：Harmony 补丁（文本速度、防点击穿透、跳过开场）
  - `Snapshots.cs`：快照的复制、清理和启动时回滚
  - `SnapshotTab.cs`：存档页
  - `Settings.cs`：配置项
  - `Plugin.cs`：面板布局
  - `Theme.cs`：配色和样式
  - `Preview.cs`、`SelfTest.cs`、`SelfTestSnapshots.cs`：只在预览构建和自测构建里编译
- `tools/Decompile/`：把游戏 DLL 反编译到 `ref/`，只作本地 API 参考（`ref/` 不提交）。
- `docs/`：README 用的截图。
- `save-backup/`：存档备份（不提交）。

## 构建

```bash
dotnet build src/ChineseParentsMod -c Release
```

### 预览构建

用来调界面，不用手动操作游戏。它在标题画面用假数据打开面板，截 6 张图到 `<游戏目录>\BepInEx\preview\`，然后自动退出游戏。

```bash
dotnet build src/ChineseParentsMod -c Release -p:DefineConstants=PREVIEW -o build/preview
```

### 自测构建

用来测稳定性和启动耗时。它记录启动时间线，等主菜单就绪后跑完下面的检查，把结果写到 `<游戏目录>\BepInEx\selftest.txt`，然后自动退出游戏：

- 三项解锁各跑 3 次，第 2、3 次应该没有新增
- 账号存档写盘后读回，和内存一致
- 5 档文本速度下两个入口的时长都正确，`PlayableText.Duration` 字段会恢复
- 三个页面的 `OnGUI` 耗时，以及开关面板 200 次后的托管内存变化
- 主题按 5 种缩放重建后贴图不增长
- 在临时目录里测快照：跳过 vdf、回滚结果、“回滚前”快照、回滚只执行一次、目标缺失时不动存档、自动快照只留 10 份
- 真实存档快照逐字节一致；解锁前会自动存快照（测试快照用完即删）
- 指针在面板上时输入模块的 `Process` 不再执行，移开后恢复
- 整个过程没有 Unity 错误日志

```bash
dotnet build src/ChineseParentsMod -c Release -p:DefineConstants=SELFTEST -o build/selftest
```

自测会用相同的数据重写账号存档，跑之前先备份存档。属性页需要进入一局游戏，自测覆盖不到。预览和自测用完都要换回正式版 DLL。

## 安装

先关掉游戏。

1. 把 BepInEx 5.4.23.5（x64 Mono）的 `winhttp.dll`、`doorstop_config.ini`、`.doorstop_version` 和 `BepInEx\core\` 放到游戏根目录。
2. 把 `src/ChineseParentsMod/bin/Release/netstandard2.1/ChineseParentsMod.dll` 复制到 `<游戏目录>\BepInEx\plugins\ChineseParentsMod\`。

日志在 `<游戏目录>\BepInEx\LogOutput.log`，本插件的行以 `[... :Chinese Parents Cheat Panel]` 开头。

## 卸载

删掉游戏根目录的 `winhttp.dll`，所有 BepInEx 插件就都不加载了。

## 恢复存档

存档目录：`%USERPROFILE%\AppData\LocalLow\moyuwan\chineseparentsgame\Save`。

优先用存档页回滚：选一份快照，点两次“回滚”，退出游戏再启动。没有快照时手动恢复：

1. 关掉游戏。
2. 用 `save-backup\<日期>\chineseparentsgame-Save\` 里的文件覆盖存档目录。
3. 存档开了 Steam Cloud。恢复后如果 Steam 提示冲突，选本地版本。
