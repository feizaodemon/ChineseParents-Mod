#if PREVIEW
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using UnityEngine;

namespace ChineseParentsMod
{
    // Preview build only: opens the panel on the title screen with fake data, saves screenshots, then quits.
    public partial class Plugin
    {
        private int m_previewStage;
        private readonly Dictionary<EMainInfoType, float> m_fake = new Dictionary<EMainInfoType, float>();

        private bool IsLoggedIn() => m_previewStage >= 1;
        private bool IsInGame() => m_previewStage >= 2;
        private float ReadAttribute(EMainInfoType type) => m_fake.TryGetValue(type, out float v) ? v : 120 + Mathf.Abs((int)type) * 37 % 900;
        private void WriteAttribute(EMainInfoType type, float value) => m_fake[type] = value;
        private int m_fakeTuition = 3900;
        private int ReadTuition() => m_fakeTuition;
        private string WriteTuition(int value) => $"学费改成了 {m_fakeTuition = value}。";

        private IEnumerator PreviewShots()
        {
            string dir = Path.Combine(Paths.BepInExRootPath, "preview");
            Directory.CreateDirectory(dir);
            yield return new WaitForSeconds(12f);
            bool opened = Settings.PanelOpened.Value;
            Settings.PanelOpened.Value = false;
            yield return Shot(dir, "0-hint");
            Settings.PanelOpened.Value = opened;
            m_show = true;
            yield return Shot(dir, "0-logged-out");
            m_previewStage = 2;
            Report("成就已全部完成。Steam 成就 23 个，这次新解锁 2 个。");
            yield return Shot(dir, "1-unlock");
            m_tab = 1;
            m_fake[EMainInfoType.IQ] = 9999;
            m_locked[EMainInfoType.IQ] = 9999;
            Report("智商改成了 9999。");
            yield return Shot(dir, "2-attributes");
            m_previewStage = 1;
            m_message = "";
            yield return Shot(dir, "3-attributes-menu");
            m_tab = 2;
            m_snapshots = new List<Snapshot>
            {
                new Snapshot { Name = "a", Time = new DateTime(2026, 10, 16, 21, 40, 12), Kind = SnapshotKind.BeforeUnlock, Note = "成就" },
                new Snapshot { Name = "b", Time = new DateTime(2026, 10, 16, 20, 15, 3), Kind = SnapshotKind.Manual },
                new Snapshot { Name = "c", Time = new DateTime(2026, 10, 15, 23, 2, 47), Kind = SnapshotKind.BeforeRestore },
                new Snapshot { Name = "d", Time = new DateTime(2026, 10, 15, 22, 58, 30), Kind = SnapshotKind.BeforeUnlock, Note = "传家宝" },
            };
            m_pending = "b";
            m_confirmKey = "delete:d";
            m_confirmUntil = float.MaxValue;
            Report("已排好回滚到 10-16 20:15:03，下次启动游戏时生效。");
            yield return Shot(dir, "4-snapshots");
            m_message = "";
            m_tab = 3;
            int speed = Settings.GameSpeedIndex.Value;
            Settings.GameSpeedIndex.Value = 2;
            ShowToast("进入小游戏，速度暂时调回 1×");
            yield return Shot(dir, "5-settings");
            Settings.GameSpeedIndex.Value = speed;
            Application.Quit();
        }

        private static IEnumerator Shot(string dir, string name)
        {
            yield return new WaitForSeconds(1f);
            ScreenCapture.CaptureScreenshot(Path.Combine(dir, name + ".png"));
            yield return new WaitForSeconds(1f);
        }
    }
}
#endif
