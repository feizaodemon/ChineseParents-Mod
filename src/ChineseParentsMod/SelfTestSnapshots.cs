#if SELFTEST
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using UnityEngine.EventSystems;

namespace ChineseParentsMod
{
    // Self-test build only: snapshot, rollback and click-through checks.
    public partial class Plugin
    {
        private bool? m_forcePointerOverPanel;
        private static int s_processCalls;

        // Everything runs on throwaway folders, so the real saves are never touched here.
        private void TestSnapshotsInTempDir()
        {
            string temp = Path.Combine(Path.GetTempPath(), "cpmod-selftest-" + System.Guid.NewGuid().ToString("N"));
            string save = Path.Combine(temp, "Save");
            string root = Path.Combine(temp, "ModSnapshots");
            try
            {
                Directory.CreateDirectory(save);
                File.WriteAllText(Path.Combine(save, "a.dat"), "a1");
                File.WriteAllText(Path.Combine(save, "b.dat"), "b1");
                File.WriteAllText(Path.Combine(save, "steam_autocloud.vdf"), "cloud");

                Snapshot manual = Snapshots.Create(save, root, SnapshotKind.Manual, "");
                string[] copied = Directory.GetFiles(manual.Dir).Select(Path.GetFileName).OrderBy(n => n).ToArray();
                Check(copied.SequenceEqual(new[] { "a.dat", "b.dat" }), "快照复制存档文件，跳过 steam_autocloud.vdf：" + string.Join(",", copied));

                File.WriteAllText(Path.Combine(save, "a.dat"), "a2");
                File.WriteAllText(Path.Combine(save, "c.dat"), "c2");
                File.WriteAllText(Path.Combine(root, Snapshots.PendingFile), manual.Name);
                Thread.Sleep(5);
                string log = Snapshots.ApplyPendingRestore(save, root);
                Snapshot before = Snapshots.List(root).FirstOrDefault(s => s.Kind == SnapshotKind.BeforeRestore);
                Check(File.ReadAllText(Path.Combine(save, "a.dat")) == "a1"
                    && !File.Exists(Path.Combine(save, "c.dat"))
                    && File.ReadAllText(Path.Combine(save, "steam_autocloud.vdf")) == "cloud",
                    "回滚后存档和快照一致，新增文件删除，vdf 保留");
                Check(before != null
                    && File.ReadAllText(Path.Combine(before.Dir, "a.dat")) == "a2"
                    && File.Exists(Path.Combine(before.Dir, "c.dat")),
                    "回滚前的存档另存为“回滚前”快照");
                Check(Snapshots.PendingName(root) == null && Snapshots.ApplyPendingRestore(save, root) == null, "回滚只执行一次：" + log);

                File.WriteAllText(Path.Combine(root, Snapshots.PendingFile), "20000101-000000-000_Manual_");
                string missing = Snapshots.ApplyPendingRestore(save, root);
                Check(missing != null && missing.Contains("no longer exists") && File.ReadAllText(Path.Combine(save, "a.dat")) == "a1",
                    "快照不存在时跳过回滚，存档不动");

                for (int i = 0; i < Snapshots.KeepAutomatic + 3; i++)
                {
                    Thread.Sleep(5);
                    Snapshots.Create(save, root, SnapshotKind.BeforeUnlock, "成就");
                }
                int automatic = Snapshots.List(root).Count(s => s.Kind != SnapshotKind.Manual);
                bool manualKept = Snapshots.List(root).Any(s => s.Name == manual.Name);
                Check(automatic == Snapshots.KeepAutomatic && manualKept, $"自动快照只留 {automatic} 份，手动快照保留：{manualKept}");
            }
            catch (Exception e)
            {
                Check(false, "临时目录快照测试异常：" + e);
            }
            finally
            {
                if (Directory.Exists(temp)) Directory.Delete(temp, true);
            }
        }

        // One snapshot of the real save folder, compared byte for byte, then deleted again.
        private void TestSnapshotsOnRealSave()
        {
            Snapshot snapshot = null;
            try
            {
                Thread.Sleep(5);
                snapshot = Snapshots.Create(SnapshotKind.Manual, "selftest");
                string[] files = Directory.GetFiles(Snapshots.SaveDir).Where(f => Path.GetFileName(f) != "steam_autocloud.vdf").ToArray();
                bool same = files.Length == Directory.GetFiles(snapshot.Dir).Length
                    && files.All(f => File.ReadAllBytes(f).SequenceEqual(File.ReadAllBytes(Path.Combine(snapshot.Dir, Path.GetFileName(f)))));
                Check(same, $"真实存档快照逐字节一致（{files.Length} 个文件）");

                Thread.Sleep(5);
                int count = Snapshots.List().Count;
                bool autoSetting = Settings.AutoSnapshot.Value;
                Settings.AutoSnapshot.Value = true;
                SnapshotThen("传家宝", Cheats.UnlockTreasures);
                Settings.AutoSnapshot.Value = autoSetting;
                Snapshot auto = Snapshots.List().First();
                Check(Snapshots.List().Count == count + 1 && auto.Kind == SnapshotKind.BeforeUnlock && auto.What == "解锁传家宝前",
                    "解锁前自动存快照：" + auto.What);
                Snapshots.Delete(auto);
                Check(new Snapshot { Kind = SnapshotKind.BeforeUnlock, Note = "学费" }.What == "改学费前", "学费快照显示为“改学费前”");
            }
            catch (Exception e)
            {
                Check(false, "真实存档快照测试异常：" + e);
            }
            finally
            {
                if (snapshot != null && Directory.Exists(snapshot.Dir)) Snapshots.Delete(snapshot);
            }
        }

        // Counts the input module's Process calls (the step that turns mouse state into clicks)
        // with the pointer forced over the panel and away from it.
        private IEnumerator TestClickThrough()
        {
            MethodInfo update = AccessTools.Method(typeof(EventSystem), "Update");
            HarmonyLib.Patches info = Harmony.GetPatchInfo(update);
            Check(info != null && info.Prefixes.Any(p => p.owner == Guid), "EventSystem.Update 防穿透补丁已挂上");

            BaseInputModule module = EventSystem.current != null ? EventSystem.current.currentInputModule : null;
            if (module == null)
            {
                Check(false, "找不到当前输入模块");
                yield break;
            }
            MethodInfo process = AccessTools.Method(module.GetType(), "Process");
            var harmony = new Harmony(Guid + ".selftest");
            harmony.Patch(process, postfix: new HarmonyMethod(typeof(Plugin), nameof(CountProcess)));

            m_forcePointerOverPanel = true;
            yield return null;
            s_processCalls = 0;
            for (int f = 0; f < 30; f++) yield return null;
            int blocked = s_processCalls;

            m_forcePointerOverPanel = false;
            yield return null;
            s_processCalls = 0;
            for (int f = 0; f < 30; f++) yield return null;
            int open = s_processCalls;

            m_forcePointerOverPanel = null;
            harmony.UnpatchSelf();
            Check(blocked == 0 && open >= 25, $"指针在面板上时 {module.GetType().Name}.Process 调用 {blocked} 次，离开后 {open} 次（30 帧）");
        }

        private static void CountProcess() => s_processCalls++;
    }
}
#endif
