using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace ChineseParentsMod
{
    [BepInPlugin(Guid, "Chinese Parents Cheat Panel", "0.4.0")]
    public partial class Plugin : BaseUnityPlugin
    {
        private const string Guid = "local.chineseparents.cheatpanel";
        private const int WindowId = 0x43504D;
        private const float WindowWidth = 560f;
        // Layout sizes are authored for a 1080p screen; Theme scales them to the real one.
        private const float DesignHeight = 1080f;
        private static readonly string[] Tabs = { "解锁", "属性", "存档", "设置" };

        // Read by BlockClickThroughPatch; updated every frame from the real mouse position.
        public static bool PointerOverPanel { get; private set; }

        private bool m_show;
        private int m_tab;
        private Rect m_window;
        private string m_message = "";
        private bool m_messageIsError;
        private Theme m_theme;
        private Font m_font;
        private readonly Dictionary<EMainInfoType, string> m_inputs = new Dictionary<EMainInfoType, string>();
        private readonly Dictionary<EMainInfoType, float> m_locked = new Dictionary<EMainInfoType, float>();

        private void Awake()
        {
            var watch = Stopwatch.StartNew();
            // Before the game reads any save file.
            try
            {
                string restored = Snapshots.ApplyPendingRestore();
                if (restored != null) Logger.LogInfo(restored);
            }
            catch (Exception e)
            {
                Logger.LogError("pending restore failed: " + e);
            }
            Settings.Bind(Config);
            new Harmony(Guid).PatchAll(typeof(Plugin).Assembly);
            Logger.LogInfo($"loaded in {watch.ElapsedMilliseconds} ms; press F1 to open the panel");
#if PREVIEW
            StartCoroutine(PreviewShots());
#elif SELFTEST
            StartSelfTest();
#endif
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                m_show = !m_show;
                m_inputs.Clear();
                m_snapshots = null;
            }
            Vector3 mouse = Input.mousePosition;
            PointerOverPanel = m_show && m_window.Contains(new Vector2(mouse.x, Screen.height - mouse.y));
#if SELFTEST
            if (m_forcePointerOverPanel.HasValue) PointerOverPanel = m_forcePointerOverPanel.Value;
#endif
            if (m_locked.Count == 0 || !IsInGame())
            {
                return;
            }
            try
            {
                foreach (KeyValuePair<EMainInfoType, float> pair in m_locked)
                {
                    WriteAttribute(pair.Key, pair.Value);
                }
            }
            catch (Exception e)
            {
                m_locked.Clear();
                Report("锁定出错，已全部解除：" + e.Message, e);
            }
        }

        private void OnGUI()
        {
            if (!m_show)
            {
                return;
            }
#if SELFTEST
            long start = Stopwatch.GetTimestamp();
            RunPendingThemeTest();
#endif
            float scale = Mathf.Max(0.7f, Screen.height / DesignHeight);
            if (m_theme == null || m_theme.Scale != scale)
            {
                // The default IMGUI font has no CJK glyphs on some setups.
                m_font = m_font ?? Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei UI", "Microsoft YaHei", "SimHei" }, 16);
                m_theme?.Dispose();
                m_theme = new Theme(m_font, scale);
                m_window = new Rect(m_theme.S(60), m_theme.S(60), m_theme.S(WindowWidth), 0);
            }
            m_window.x = Mathf.Clamp(m_window.x, 0f, Mathf.Max(0f, Screen.width - m_window.width));
            m_window.y = Mathf.Clamp(m_window.y, 0f, Mathf.Max(0f, Screen.height - m_window.height));
            if (Event.current.type == EventType.Repaint)
            {
                float pad = m_theme.S(20);
                m_theme.Shadow.Draw(new Rect(m_window.x - pad, m_window.y - pad * 0.6f, m_window.width + pad * 2, m_window.height + pad * 2), false, false, false, false);
            }
            // Height 0 lets GUILayout shrink the window when the content gets shorter.
            m_window.height = 0;
            m_window = GUILayout.Window(WindowId, m_window, DrawWindow, GUIContent.none, m_theme.Window, GUILayout.Width(m_theme.S(WindowWidth)));
#if SELFTEST
            RecordGuiTime(Stopwatch.GetTimestamp() - start);
#endif
        }

        private void DrawWindow(int id)
        {
            bool loggedIn = IsLoggedIn();
            bool inGame = loggedIn && IsInGame();

            GUILayout.BeginHorizontal();
            GUILayout.Label("中国式家长 修改器", m_theme.Title);
            GUILayout.Label(inGame ? "游戏中" : loggedIn ? "主菜单" : "读取中", inGame ? m_theme.ChipGreen : loggedIn ? m_theme.ChipAmber : m_theme.ChipGray);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("×", m_theme.Close)) m_show = false;
            GUILayout.EndHorizontal();
            GUILayout.Label("F1 打开或收起，拖动标题栏可以移动", m_theme.Hint);
            GUILayout.Space(m_theme.S(14));

            GUILayout.BeginHorizontal(m_theme.Segment);
            int tab = GUILayout.Toolbar(m_tab, Tabs, m_theme.Tab);
            if (tab != m_tab) m_snapshots = null;
            m_tab = tab;
            GUILayout.EndHorizontal();
            GUILayout.Space(m_theme.S(14));

            if (m_tab == 0) DrawUnlocks(loggedIn);
            else if (m_tab == 1) DrawAttributes(inGame);
            else if (m_tab == 2) DrawSnapshots();
            else DrawSettings();

            if (m_message.Length > 0)
            {
                GUILayout.Space(m_theme.S(10));
                m_theme.Message.normal.textColor = m_messageIsError ? Theme.Seal : Theme.Good;
                GUILayout.Label(m_message, m_theme.Message);
            }
            GUI.DragWindow(new Rect(0, 0, 10000, m_theme.S(72)));
        }

        private void DrawUnlocks(bool loggedIn)
        {
            bool steam = Settings.SteamAchievements.Value;
            GUI.enabled = loggedIn;
            Card("成就", steam ? "游戏里的成就全部完成，Steam 成就也一起解锁。" : "只完成游戏里的成就，Steam 成就不动。",
                () => ActionButton("全部解锁", () => SnapshotThen("成就", () => Cheats.UnlockAchievements(steam))),
                () => Settings.SteamAchievements.Value = SwitchRow("同时解锁 Steam 成就（解锁后会显示在 Steam 个人资料上）", steam));
            Card("图鉴", "传承等级升满，特长、职业、人物全部收录。", () => ActionButton("全部解锁", () => SnapshotThen("图鉴", Cheats.UnlockCollections)));
            Card("传家宝", "每种传家宝各拿一件。", () => ActionButton("全部领取", () => SnapshotThen("传家宝", Cheats.UnlockTreasures)));
            GUI.enabled = true;
            GUILayout.Label(!loggedIn ? "存档还没读完，稍等一下。" : Settings.AutoSnapshot.Value
                ? "点完马上存进账号存档，正在玩的这局不受影响。解锁前会自动存快照。"
                : "点完马上存进账号存档，正在玩的这局不受影响。", m_theme.Note);
        }

        private void DrawSettings()
        {
            Card("文本速度", "对话和事件文字逐字出现的速度。点一下对话框仍然可以直接显示全文。", null, () =>
            {
                GUILayout.Space(m_theme.S(8));
                GUILayout.BeginHorizontal(m_theme.Segment);
                Settings.TextSpeedIndex.Value = GUILayout.Toolbar(Settings.TextSpeedIndex.Value, Settings.TextSpeedLabels, m_theme.Tab);
                GUILayout.EndHorizontal();
            });
            Card("跳过开场动画", "启动时跳过工作室 logo，直接进标题画面。下次启动生效。",
                () => Settings.SkipIntro.Value = Switch(Settings.SkipIntro.Value), descriptionWidth: 400);
            GUILayout.Label("设置自动保存，重启游戏后仍然有效。", m_theme.Note);
        }

        // A white card: title and description on the left, an optional control on the right, optional extra row below.
        private void Card(string title, string description, Action right, Action below = null, float descriptionWidth = 330)
        {
            GUILayout.BeginVertical(m_theme.Card);
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            GUILayout.Label(title, m_theme.Section);
            GUILayout.Label(description, m_theme.Body, GUILayout.Width(m_theme.S(right == null ? 480 : descriptionWidth)));
            GUILayout.EndVertical();
            if (right != null)
            {
                GUILayout.FlexibleSpace();
                GUILayout.BeginVertical();
                GUILayout.FlexibleSpace();
                right();
                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
            below?.Invoke();
            GUILayout.EndVertical();
        }

        private void ActionButton(string label, Func<string> action)
        {
            if (GUILayout.Button(label, m_theme.Primary, GUILayout.Width(m_theme.S(112)))) Run(action);
        }

        private bool SwitchRow(string label, bool on)
        {
            GUILayout.Space(m_theme.S(8));
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, m_theme.NoteRow, GUILayout.Width(m_theme.S(390)));
            GUILayout.FlexibleSpace();
            on = Switch(on);
            GUILayout.EndHorizontal();
            return on;
        }

        private bool Switch(bool on)
        {
            bool clicked = GUILayout.Button(on ? "开" : "关", on ? m_theme.LockOn : m_theme.LockOff, GUILayout.Width(m_theme.S(64)));
            return clicked ? !on : on;
        }

        private void DrawAttributes(bool inGame)
        {
            if (!inGame)
            {
                GUILayout.Label("进入一局游戏后才能改属性。", m_theme.Empty, GUILayout.Height(m_theme.S(140)));
                return;
            }
            for (int i = 0; i < Cheats.Attributes.Length; i++)
            {
                (EMainInfoType type, string name) = Cheats.Attributes[i];
                DrawAttribute(type, name, i % 2 == 0 ? m_theme.Row : m_theme.RowAlt);
            }
            GUILayout.Space(m_theme.S(6));
            GUILayout.Label("锁定后数值不再变动。改动在本回合结束时跟着游戏一起存档。", m_theme.Note);
        }

        private void DrawAttribute(EMainInfoType type, string name, GUIStyle row)
        {
            float current = ReadAttribute(type);
            if (!m_inputs.TryGetValue(type, out string text))
            {
                text = current.ToString("0", CultureInfo.InvariantCulture);
            }
            GUILayout.BeginHorizontal(row);
            GUILayout.Label(name, m_theme.RowName, GUILayout.Width(m_theme.S(96)));
            GUILayout.Label(current.ToString("0", CultureInfo.InvariantCulture), m_theme.Value, GUILayout.Width(m_theme.S(90)));
            GUILayout.FlexibleSpace();
            m_inputs[type] = GUILayout.TextField(text, m_theme.Field, GUILayout.Width(m_theme.S(110)));
            bool parsed = float.TryParse(m_inputs[type], NumberStyles.Float, CultureInfo.InvariantCulture, out float value);
            if (GUILayout.Button("设定", m_theme.Ghost, GUILayout.Width(m_theme.S(64))) && parsed)
            {
                Run(() =>
                {
                    WriteAttribute(type, value);
                    if (m_locked.ContainsKey(type)) m_locked[type] = value;
                    return $"{name}改成了 {value:0}。";
                });
            }
            bool locked = m_locked.ContainsKey(type);
            if (GUILayout.Button(locked ? "已锁定" : "锁定", locked ? m_theme.LockOn : m_theme.LockOff, GUILayout.Width(m_theme.S(76))))
            {
                if (locked) m_locked.Remove(type);
                else m_locked[type] = parsed ? value : current;
            }
            GUILayout.EndHorizontal();
        }

        private void Run(Func<string> action)
        {
            try
            {
                Report(action());
            }
            catch (Exception e)
            {
                Report($"出错了：{e.Message}。详情见 BepInEx 日志。", e);
            }
        }

        private void Report(string message, Exception e = null)
        {
            m_message = message;
            m_messageIsError = e != null;
            if (e == null) Logger.LogInfo(message);
            else Logger.LogError(message + "\n" + e);
        }

#if !PREVIEW
        private static bool IsLoggedIn() => Cheats.LoggedIn;
        private static bool IsInGame() => Cheats.InGame;
        private static float ReadAttribute(EMainInfoType type) => Cheats.GetAttribute(type);
        private static void WriteAttribute(EMainInfoType type, float value) => Cheats.SetAttribute(type, value);
#endif
    }
}
