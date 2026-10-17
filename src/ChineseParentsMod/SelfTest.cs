#if SELFTEST
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using BepInEx;
using DG.Tweening;
using DG.Tweening.Core;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ChineseParentsMod
{
    // Self-test build only: logs the startup timeline, exercises every feature that works outside a
    // playthrough, writes BepInEx\selftest.txt, then quits. It rewrites the account save with the same data.
    public partial class Plugin
    {
        private readonly List<string> m_report = new List<string>();
        private readonly DateTime m_processStart = Process.GetCurrentProcess().StartTime;
        private int m_unityErrors, m_failures;
        private long m_guiTicks;
        private int m_guiFrames;
        private bool m_themeTestPending;

        private void StartSelfTest()
        {
            Mark("插件 Awake");
            Application.logMessageReceived += (condition, stack, type) =>
            {
                if (type == LogType.Error || type == LogType.Exception)
                {
                    m_unityErrors++;
                    m_report.Add($"         [{type}] {condition.Split('\n')[0]}");
                }
            };
            SceneManager.sceneLoaded += (scene, mode) => Mark("场景加载：" + scene.name);
            StartCoroutine(SelfTest());
        }

        private void RecordGuiTime(long ticks)
        {
            m_guiTicks += ticks;
            m_guiFrames++;
        }

        private void Mark(string what) => m_report.Add($"{(DateTime.Now - m_processStart).TotalSeconds,7:0.00}s  {what}");

        private void Check(bool ok, string what)
        {
            if (!ok) m_failures++;
            m_report.Add((ok ? "  通过  " : "  失败  ") + what);
        }

        private IEnumerator SelfTest()
        {
            while (!Cheats.LoggedIn) yield return null;
            Mark("账号存档读完");
            yield return new WaitForSeconds(3f);
            m_report.Add("");

            TestUnlocks();
            TestSaveRoundTrip();
            TestTextSpeed();
            TestTuition();
            yield return TestGameSpeed();
            yield return TestPanel();
            yield return TestThemeRebuild();
            TestSnapshotsInTempDir();
            TestSnapshotsOnRealSave();
            yield return TestClickThrough();

            m_report.Add("");
            m_report.Add($"Unity 错误日志 {m_unityErrors} 条，失败 {m_failures} 项。");
            File.WriteAllLines(Path.Combine(Paths.BepInExRootPath, "selftest.txt"), m_report);
            Application.Quit();
        }

        private void TestUnlocks()
        {
            for (int round = 1; round <= 3; round++)
            {
                string a = Cheats.UnlockAchievements(false);
                string c = Cheats.UnlockCollections();
                string t = Cheats.UnlockTreasures();
                if (round > 1)
                {
                    Check(c.Contains("特长 0 个、职业 0 个、人物 0 个") && t == "传家宝早就齐了。", $"第 {round} 次解锁无新增：{c} {t}");
                }
            }
            string steam = Cheats.UnlockAchievements(true);
            Check(steam.Contains("Steam"), "Steam 成就路径：" + steam);
        }

        // Writes tuition + 1 to the account save, reads it back, then writes the original value again.
        private void TestTuition()
        {
            int original = Cheats.Tuition;
            Cheats.SetTuition(original + 1);
            NewAccountSaverSubInfo disk = null;
            SingleTon<NewRecordMgr>.Instance.ReqAccountSubInfo(x => disk = x);
            Check(Cheats.Tuition == original + 1 && disk != null && disk.Tuition == original + 1, $"学费改写后写盘一致（{original} → {original + 1}）");
            Cheats.SetTuition(original);
            SingleTon<NewRecordMgr>.Instance.ReqAccountSubInfo(x => disk = x);
            Check(disk.Tuition == original, $"学费改回 {original}");
        }

        private IEnumerator TestGameSpeed()
        {
            int saved = Settings.GameSpeedIndex.Value;
            for (int i = 0; i < Settings.GameSpeeds.Length; i++)
            {
                Settings.GameSpeedIndex.Value = i;
                yield return null;
                float start = Time.realtimeSinceStartup;
                yield return new WaitForSeconds(1f);
                float real = Time.realtimeSinceStartup - start;
                float expected = 1f / Settings.GameSpeeds[i];
                Check(Time.timeScale == Settings.GameSpeeds[i] && Mathf.Abs(real - expected) < 0.1f + expected * 0.1f,
                    $"游戏速度 {Settings.GameSpeedLabels[i]}：timeScale {Time.timeScale}，1 秒等待实际 {real:0.00} s");
            }
            Settings.GameSpeedIndex.Value = saved;
            yield return null;
            Check(LoopRunsOnRealTime(-1) && !LoopRunsOnRealTime(1), "无限循环的 DOTweenAnimation 用真实时间，单次的仍跟随游戏速度");
            Check(Time.timeScale == Settings.GameSpeed, $"游戏速度恢复为 {Time.timeScale}");
        }

        // Builds a throwaway scale animation the way a prefab would, then reads the tween's update mode.
        private static bool LoopRunsOnRealTime(int loops)
        {
            var go = new GameObject("cpmod-selftest-loop");
            try
            {
                var anim = go.AddComponent<DOTweenAnimation>();
                anim.target = go.transform;
                anim.animationType = DOTweenAnimationType.Scale;
                anim.endValueV3 = Vector3.one * 2f;
                anim.loops = loops;
                anim.isValid = true;
                anim.CreateTween();
                return Traverse.Create(anim.tween).Field("isIndependentUpdate").GetValue<bool>();
            }
            finally
            {
                UnityEngine.Object.Destroy(go);
            }
        }

        private void TestSaveRoundTrip()
        {
            NewAccountSaverSubInfo disk = null;
            SingleTon<NewRecordMgr>.Instance.ReqAccountSubInfo(x => disk = x);
            Check(disk != null, "账号存档能读回");
            if (disk == null) return;
            Check(disk.UnlockTreasures.Count == SingleTon<TreasureMgr>.Instance.UnlockTreasureIDs.Count, $"传家宝写盘一致（{disk.UnlockTreasures.Count}）");
            Check(disk.FamilyCollectTricks.Count == SingleTon<NewTrickMgr>.Instance.FamilyCollectTricks.Count, $"特长写盘一致（{disk.FamilyCollectTricks.Count}）");
            Check(disk.FamilyUnlockJobs.Count == SingleTon<NewJobMgr>.Instance.UnlockJobs.Count, $"职业写盘一致（{disk.FamilyUnlockJobs.Count}）");
            Check(disk.UnlockedNpcIds.Count == SingleTon<NpcMgr>.Instance.UnlockedNpcIds.Count, $"人物写盘一致（{disk.UnlockedNpcIds.Count}）");
            Check(disk.FamilyLastGenarationLevel == SingleTon<CollectionMgr>.Instance.LastGenarationLevel, $"传承等级写盘一致（{disk.FamilyLastGenarationLevel}）");
            Check(disk.DicType2AchieveProcess_.Count > 0, $"成就进度已写盘（{disk.DicType2AchieveProcess_.Count} 类）");
        }

        private void TestTextSpeed()
        {
            int saved = Settings.TextSpeedIndex.Value;
            var go = new GameObject("cpmod-selftest");
            Text text = go.AddComponent<Text>();
            PlayableText playable = go.AddComponent<PlayableText>();
            playable.PlayMode = ETextPlayMode.ShowAll;
            for (int i = 0; i < Settings.TextSpeeds.Length; i++)
            {
                Settings.TextSpeedIndex.Value = i;
                float expected = Settings.ScaleDuration(2f);
                Tweener tween = text.PlayTxt("测试文本", 2f);
                Check(Mathf.Abs(tween.Duration(false) - expected) < 1e-4f, $"UIHelper.PlayTxt {Settings.TextSpeedLabels[i]}：时长 {tween.Duration(false):0.###} s");
                tween.Kill();

                playable.Duration = 2f;
                playable.SetTxt("测试文本");
                var inner = Traverse.Create(playable).Field("m_curTweener").GetValue<Tweener>();
                Check(inner != null && Mathf.Abs(inner.Duration(false) - expected) < 1e-4f && playable.Duration == 2f,
                    $"PlayableText {Settings.TextSpeedLabels[i]}：时长 {(inner == null ? -1 : inner.Duration(false)):0.###} s，字段恢复为 {playable.Duration}");
                inner?.Kill();
                playable.Clear();
            }
            Settings.TextSpeedIndex.Value = saved;
            Destroy(go);
        }

        private IEnumerator TestPanel()
        {
            long memBefore = GC.GetTotalMemory(true);
            m_show = true;
            for (int tab = 0; tab < Tabs.Length; tab++)
            {
                m_tab = tab;
                m_guiTicks = 0;
                m_guiFrames = 0;
                for (int f = 0; f < 120; f++) yield return null;
                double ms = m_guiFrames == 0 ? -1 : m_guiTicks * 1000.0 / Stopwatch.Frequency / m_guiFrames;
                Check(m_guiFrames > 0 && ms < 2.0, $"{Tabs[tab]}页 OnGUI 平均 {ms:0.000} ms（{m_guiFrames} 次调用）");
            }
            for (int i = 0; i < 200; i++)
            {
                m_show = !m_show;
                m_tab = i % Tabs.Length;
                yield return null;
            }
            m_show = false;
            long memAfter = GC.GetTotalMemory(true);
            Check(memAfter - memBefore < 2 * 1024 * 1024, $"开关面板 200 次后托管内存变化 {(memAfter - memBefore) / 1024} KB");
        }

        // Theme needs GUI.skin, so the rebuild runs inside OnGUI, the same place the real code rebuilds it.
        private IEnumerator TestThemeRebuild()
        {
            m_show = true;
            yield return null;
            int before = Resources.FindObjectsOfTypeAll<Texture2D>().Length;
            m_themeTestPending = true;
            while (m_themeTestPending) yield return null;
            // Object.Destroy takes effect at the end of the frame.
            yield return null;
            m_show = false;
            yield return null;
            int after = Resources.FindObjectsOfTypeAll<Texture2D>().Length;
            Check(after - before <= 0, $"重建后贴图数量变化 {after - before}（应不增长）");
        }

        private void RunPendingThemeTest()
        {
            if (!m_themeTestPending) return;
            m_themeTestPending = false;
            float real = m_theme.Scale;
            var watch = Stopwatch.StartNew();
            foreach (float scale in new[] { 0.7f, 1f, 1.333f, 2f, real })
            {
                m_theme.Dispose();
                m_theme = new Theme(m_font, scale);
            }
            Check(true, $"主题按 5 种缩放重建，平均 {watch.ElapsedMilliseconds / 5.0:0.0} ms");
        }
    }
}
#endif
