# ChineseParents-Mod

中国式家长（Steam AppID 736190）的修改器，基于 BepInEx 5。进游戏后按 F1 打开面板。

![面板截图：解锁页、属性页和存档页](docs/preview.jpg)

## 快速上手

没用过 mod 也能装，三步：

1. 关掉游戏。在 Steam 库里右键“中国式家长”，选“管理 → 浏览本地文件”，打开游戏目录（里面有 `game.exe`）。
2. 把 `ChineseParentsMod-1.0.0.zip` 里的所有文件解压到这个目录。解压后 `winhttp.dll` 和 `game.exe` 在同一层。
3. 启动游戏。标题画面上方出现“按 F1 打开修改器”，就说明装好了。不开面板也能按 F2 切换游戏速度。

压缩包里已经带了 BepInEx，不用另外下载。里面的 `使用说明.txt` 写了同样的安装步骤，另有用法、卸载和排错说明。压缩包由 `tools/package.ps1` 生成，见下文“安装包”。

## 能做什么

| 位置 | 控件 | 效果 | 存到哪里 |
|---|---|---|---|
| 解锁页 | 成就 · 全部解锁 | 游戏里 31 类成就全部完成；“同时解锁 Steam 成就”开着时，Steam 成就一起解锁 | 账号存档、Steam |
| 解锁页 | 图鉴 · 全部解锁 | 传承等级升满，特长、职业、人物全部收录 | 账号存档 |
| 解锁页 | 传家宝 · 全部领取 | 每种传家宝各拿一件 | 账号存档 |
| 解锁页 | 学费 · 设定 | 学费（买传家宝用的钱）改成任意数目 | 账号存档 |
| 属性页 | 设定 / 锁定 | 13 项属性和天赋点改成任意数值，锁定后不再变动 | 本局存档，回合结束时由游戏保存 |
| 存档页 | 存快照 | 把整个存档文件夹复制一份 | `ModSnapshots` 文件夹 |
| 存档页 | 回滚 / 删除 | 每份快照写明最近那局的孩子名字和年龄。点两次确认。回滚在下次启动游戏时生效 | `ModSnapshots` 文件夹 |
| 存档页 | 改动前自动存一份快照 | 解锁、改学费、改属性前先存快照。改属性每回合只存一份。自动快照最多留 10 份 | 插件配置 |
| 设置页 / F2 | 游戏速度 | 动画和等待按 1×、2×、3×、5× 加快，按 F2 依次切换；不是 1× 时标题栏显示“加速”。进选中的小游戏时自动调回 1×，结束后恢复 | 插件配置 |
| 设置页 | 文本速度 | 逐字文字按 1×、2×、4×、8× 或瞬间显示 | 插件配置 |
| 设置页 | 跳过开场动画 | 启动时跳过工作室 logo，下次启动生效 | 插件配置 |

- 解锁页在主菜单和游戏中都能用，点完马上写入账号存档。
- 属性页要进入一局游戏后才能用。属性名直接取自游戏，和游戏界面一致。天赋点在选了天赋线后才用得上。
- 进下面这些小游戏时，速度自动调回 1×，屏幕上方会提示；结束后恢复。设置页可以逐个开关，也可以整个关掉。
  - 默认调回：抢红包、美术考试、作文大赛、打团。前三个按真实时间计时，加速后时间变短、更难拿高分；打团的战斗过程加速后看不清。
  - 默认不调回：面子大战、竞选、才艺表演。它们按回合进行，加速只让动画变快。想看清过程就在设置页打开。
  - 学习、打工、天赋线考试、相亲只需要等，不在名单里，一直加速。
- Steam 成就解锁后会出现在个人资料上。想撤销，只能用 Steam Achievement Manager 这类外部工具。不想动 Steam，就把成就卡片里的开关关掉。
- 改属性前的快照每回合最多一份：游戏只在回合结束时写存档，同一回合内再存一份，内容完全一样。
- 快照存在 `%USERPROFILE%\AppData\LocalLow\moyuwan\chineseparentsgame\ModSnapshots`，和 `Save` 同级，Steam Cloud 不会同步它。手动快照不会自动删除。
- 鼠标在面板上时，点击不会传到下面的游戏按钮。
- 设置存在 `<游戏目录>\BepInEx\config\local.chineseparents.cheatpanel.cfg`。

## 常见问题

- **按 F1 没反应**：确认 `winhttp.dll` 和 `game.exe` 在同一个文件夹，并且游戏目录里生成了 `BepInEx\LogOutput.log`。
- **存档改坏了**：打开存档页，选一份快照，点两次“回滚”，再重启游戏。
- **面板报错**：详情在 `<游戏目录>\BepInEx\LogOutput.log`，本插件的行以 `[... :Chinese Parents Cheat Panel]` 开头。

## 卸载

删掉游戏目录里的 `winhttp.dll`，所有 BepInEx 插件就都不加载了。想删干净，再删掉 `BepInEx` 文件夹、`doorstop_config.ini` 和 `.doorstop_version`。

## 恢复存档

存档目录：`%USERPROFILE%\AppData\LocalLow\moyuwan\chineseparentsgame\Save`。

优先用存档页回滚。没有快照时手动恢复：

1. 关掉游戏。
2. 用 `save-backup\<日期>\chineseparentsgame-Save\` 里的文件覆盖存档目录。
3. 存档开了 Steam Cloud。恢复后如果 Steam 提示冲突，选本地版本。

---

以下内容给开发者。

## 启动耗时

从进程启动到登录阶段，每种情况测两次取平均（2026-10-16，RTX 5070 机器）：

| 情况 | 耗时 |
|---|---|
| 原版，不装 BepInEx | 3.53 s |
| 装 BepInEx，不跳过开场 | 3.98 s |
| 装 BepInEx，跳过开场（默认） | 2.03 s |

BepInEx 本身多花约 0.45 s。插件自己的加载（读配置、打补丁）约 10–17 ms。开场 logo 固定占 2 s，跳过后比原版快约 1.5 s。

## 实现

- 所有改动都调用游戏自己的管理器：`AchievementMgr`、`CollectionMgr`、`NewTrickMgr`、`NewJobMgr`、`NpcMgr`、`TreasureMgr`、`TuitionMgr`、`TalentLineMgr`、`NewPlayerDataMgr`。
- 账号存档的写法和游戏相同：先调 `NewRecordMgr.RefreshAccountInfo`，再用 `IOHelper.SetData` 写盘，不碰本局存档。学费也在账号存档里，所以改完马上写盘。
- 天赋点在 `TalentLineMgr`，不在属性表里。属性页用一个枚举范围外的键把它当作第 14 行，和其他属性一样能设定和锁定，回合结束时随本局存档保存。
- Steam 成就用 Steamworks.NET 的 `SteamUserStats` 遍历全部成就名。
- 游戏速度：设置 `Time.timeScale`。游戏代码从不改这个值，所以插件每帧把它保持在所选倍速。无限循环的 `DOTweenAnimation`（按钮光芒等装饰）改用真实时间，不跟着加速。插件每帧读 `UIRoot` 的窗口表；名单（`Settings.Minigames`）里有开着的小游戏窗口在显示，就临时用 1×。名单的依据：抢红包、美术考试直接用 `Time.deltaTime` 计时，作文大赛用 `UpdateMono` 的任务计时（同样取 `Time.deltaTime`），而且每帧判定一次是否生成灵感气泡，加速后气泡变少；打团、面子大战、竞选、才艺表演是回合制，只有动画受影响。打工、天赋线考试、相亲虽然也有计时，但玩家只需要等，结果不受倍速影响。
- 文本速度：游戏的逐字文字都经过 `UIHelper.PlayTxt` 或 `PlayableText.PlayTxt`，Harmony 补丁在这两个入口把时长除以倍速。点击加速和直接显示全文的逻辑不变。
- 跳过开场：补丁替换 `BeginWnd.OnShow`，做法和游戏在调试构建里的处理相同，直接加载 `Login` 场景。
- 快照：复制 `Save` 里除 `steam_autocloud.vdf` 外的全部文件。游戏运行时内存里还有存档数据，所以回滚只先记下目标，下次启动时插件在游戏读存档之前覆盖文件。覆盖前会先把当前存档存成一份“回滚前”快照。
- 防点击穿透：IMGUI 不拦截 uGUI 的点击。补丁在鼠标位于面板上时跳过当帧的 `EventSystem.Update`。直接读 `Input` 的少数游戏代码（如红包小游戏）不受这个补丁影响。
- 快照摘要：用游戏自己的 `IOHelper.GetData` 读快照里最近修改的 `GameData-Record-*.v2.sav`，名字和年龄的取法和读档界面（`RecordWndUnit`）相同。只读列出的 8 份，每次打开存档页读一次。
- 特殊事件记录：补丁挂在 `NewActionMgr.DoActionEffect` 前面。事件开始后 8 秒内新出现的窗口写进日志，每类事件每次启动只记一次，格式是 `special event <类型> opened: <窗口>`。DLC 加了新小游戏时，按这行日志把窗口名加进 `Settings.Minigames`。
- 首次提示：面板打开过一次之前，屏幕上方显示“按 F1 打开修改器”。打开后在配置里记下，不再显示。
- 容错：补丁逐个挂上。游戏更新后某个目标方法找不到时，只关掉对应的功能，面板会写明哪项没能启用，其他功能照常可用。小游戏检测每帧运行，出错时保持所选倍速，日志只记一次。
- 面板用 IMGUI 绘制。贴图在运行时生成，字号和圆角按屏幕高度缩放（以 1080p 为基准），插件只有一个 DLL。

## 目录

- `src/ChineseParentsMod/`：插件源码。
  - `Cheats.cs`：解锁、学费和属性逻辑
  - `Patches.cs`：Harmony 补丁（文本速度、装饰光效、防点击穿透、跳过开场）和逐个挂补丁的 `PatchLoader`
  - `GameSpeed.cs`：游戏速度、F2 切换和小游戏自动 1×
  - `EventWatch.cs`：特殊事件打开了哪些窗口
  - `SaveSummary.cs`：读快照里的孩子名字和年龄
  - `Snapshots.cs`：快照的复制、清理和启动时回滚
  - `SnapshotTab.cs`：存档页
  - `Settings.cs`：配置项
  - `Plugin.cs`：面板布局
  - `Theme.cs`：配色和样式
  - `Preview.cs`、`SelfTest.cs`、`SelfTestSnapshots.cs`：只在预览构建和自测构建里编译
- `tools/package.ps1`：打新手安装包。
- `tools/Decompile/`：把游戏 DLL 反编译到 `ref/`，只作本地 API 参考（`ref/` 不提交）。
- `docs/`：README 截图和安装包里的 `使用说明.txt`。
- `save-backup/`：存档备份（不提交）。

## 构建

```bash
dotnet build src/ChineseParentsMod -c Release
```

### 安装包

从一份能用的 BepInEx 安装（默认是本机游戏目录）里取 `winhttp.dll`、`doorstop_config.ini`、`.doorstop_version` 和 `BepInEx\core\`，再加上正式版 DLL 和 `使用说明.txt`，打成 `build/ChineseParentsMod-<版本>.zip`：

```bash
pwsh tools/package.ps1
```

### 手动安装

先关掉游戏。

1. 把 BepInEx 5.4.23.5（x64 Mono）的 `winhttp.dll`、`doorstop_config.ini`、`.doorstop_version` 和 `BepInEx\core\` 放到游戏根目录。
2. 把 `src/ChineseParentsMod/bin/Release/netstandard2.1/ChineseParentsMod.dll` 复制到 `<游戏目录>\BepInEx\plugins\ChineseParentsMod\`。

### 预览构建

用来调界面，不用手动操作游戏。它在标题画面用假数据打开面板，截 7 张图到 `<游戏目录>\BepInEx\preview\`，然后自动退出游戏。

```bash
dotnet build src/ChineseParentsMod -c Release -p:DefineConstants=PREVIEW -o build/preview
```

### 自测构建

用来测稳定性和启动耗时。它记录启动时间线，等主菜单就绪后跑完下面的检查，把结果写到 `<游戏目录>\BepInEx\selftest.txt`，然后自动退出游戏：

- 三项解锁各跑 3 次，第 2、3 次应该没有新增
- 账号存档写盘后读回，和内存一致
- 学费改成原值 + 1 后写盘读回一致，再改回原值
- 4 档游戏速度下 `timeScale` 正确，1 秒的游戏内等待按倍速缩短，测完恢复
- 无限循环的 `DOTweenAnimation` 用真实时间；小游戏名单里的窗口类名都存在并对应到各自的小游戏；学习窗口不在名单里；主菜单不误判为小游戏；3× 下进出小游戏会切到 1× 再恢复 3×，提示里有小游戏名
- 一个补丁的目标不存在时，其余 6 个补丁照常挂上，面板提示没能启用的功能；小游戏检测出错时异常被接住，速度保持不变
- F2 在 4 档速度间循环并弹出提示；特殊事件记录写进日志，同类事件只记一次
- 属性名取自游戏的本地化表
- 5 档文本速度下两个入口的时长都正确，`PlayableText.Duration` 字段会恢复
- 四个页面的 `OnGUI` 耗时，以及开关面板 200 次后的托管内存变化
- 主题按 5 种缩放重建后贴图不增长
- 在临时目录里测快照：跳过 vdf、回滚结果、“回滚前”快照、回滚只执行一次、目标缺失时不动存档、自动快照只留 10 份
- 真实存档快照逐字节一致；解锁前会自动存快照；改属性前每回合只存一份；快照摘要能读出名字和年龄，空文件夹显示“还没有对局”（测试快照用完即删）
- 指针在面板上时输入模块的 `Process` 不再执行，移开后恢复
- 整个过程没有 Unity 错误日志

```bash
dotnet build src/ChineseParentsMod -c Release -p:DefineConstants=SELFTEST -o build/selftest
```

自测会用相同的数据重写账号存档，跑之前先备份存档。属性页和天赋点需要进入一局游戏，自测覆盖不到。预览和自测用完都要换回正式版 DLL。
