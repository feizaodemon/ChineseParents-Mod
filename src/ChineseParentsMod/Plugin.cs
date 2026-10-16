using System;
using System.Collections.Generic;
using System.Globalization;
using BepInEx;
using UnityEngine;

namespace ChineseParentsMod
{
    [BepInPlugin("local.chineseparents.cheatpanel", "Chinese Parents Cheat Panel", "0.1.0")]
    public class Plugin : BaseUnityPlugin
    {
        private const int WindowId = 0x43504D;

        private bool m_show;
        private Rect m_window = new Rect(40, 40, 460, 620);
        private Vector2 m_scroll;
        private string m_message = "";
        private Font m_font;
        private readonly Dictionary<EMainInfoType, string> m_inputs = new Dictionary<EMainInfoType, string>();
        private readonly Dictionary<EMainInfoType, float> m_locked = new Dictionary<EMainInfoType, float>();

        private void Awake()
        {
            Logger.LogInfo("loaded; press F1 to open the panel");
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                m_show = !m_show;
                if (m_show)
                {
                    m_inputs.Clear();
                }
            }
            if (m_locked.Count == 0 || !Cheats.InGame)
            {
                return;
            }
            try
            {
                foreach (KeyValuePair<EMainInfoType, float> pair in m_locked)
                {
                    Cheats.SetAttribute(pair.Key, pair.Value);
                }
            }
            catch (Exception e)
            {
                m_locked.Clear();
                Report("锁定失败，已全部解除：" + e.Message, e);
            }
        }

        private void OnGUI()
        {
            if (!m_show)
            {
                return;
            }
            if (m_font == null)
            {
                // The default IMGUI font has no CJK glyphs on some setups.
                m_font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "Arial" }, 16);
            }
            Font previous = GUI.skin.font;
            GUI.skin.font = m_font;
            m_window = GUILayout.Window(WindowId, m_window, DrawWindow, "中国式家长 修改面板（F1 开关）");
            GUI.skin.font = previous;
        }

        private void DrawWindow(int id)
        {
            bool loggedIn = Cheats.LoggedIn;
            bool inGame = loggedIn && Cheats.InGame;
            GUILayout.Label(inGame ? "状态：游戏中" : loggedIn ? "状态：主菜单（属性需进入存档后修改）" : "状态：未登录，请先进入游戏主菜单");

            GUI.enabled = loggedIn;
            GUILayout.Label("—— 解锁（立即写入账号存档）——");
            if (GUILayout.Button("全成就（含 Steam 成就）")) Run(Cheats.UnlockAchievements);
            if (GUILayout.Button("全图鉴（传承等级 / 特长 / 职业 / 人物）")) Run(Cheats.UnlockCollections);
            if (GUILayout.Button("全传家宝")) Run(Cheats.UnlockTreasures);

            GUI.enabled = inGame;
            GUILayout.Label("—— 属性（回合结束时随游戏存档）——");
            m_scroll = GUILayout.BeginScrollView(m_scroll, GUILayout.Height(330));
            if (inGame)
            {
                foreach ((EMainInfoType type, string name) in Cheats.Attributes)
                {
                    DrawAttribute(type, name);
                }
            }
            GUILayout.EndScrollView();
            GUI.enabled = true;

            GUILayout.Label(m_message);
            GUI.DragWindow();
        }

        private void DrawAttribute(EMainInfoType type, string name)
        {
            float current = Cheats.GetAttribute(type);
            if (!m_inputs.TryGetValue(type, out string text))
            {
                text = current.ToString("0", CultureInfo.InvariantCulture);
            }
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{name} {current:0}", GUILayout.Width(150));
            m_inputs[type] = GUILayout.TextField(text, GUILayout.Width(90));
            bool parsed = float.TryParse(m_inputs[type], NumberStyles.Float, CultureInfo.InvariantCulture, out float value);
            if (GUILayout.Button("设定", GUILayout.Width(60)) && parsed)
            {
                Run(() =>
                {
                    Cheats.SetAttribute(type, value);
                    if (m_locked.ContainsKey(type)) m_locked[type] = value;
                    return $"{name} 设为 {value:0}";
                });
            }
            bool locked = m_locked.ContainsKey(type);
            if (GUILayout.Toggle(locked, "锁定") != locked)
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
                Report("失败：" + e.Message, e);
            }
        }

        private void Report(string message, Exception e = null)
        {
            m_message = message;
            if (e == null) Logger.LogInfo(message);
            else Logger.LogError(message + "\n" + e);
        }
    }
}
