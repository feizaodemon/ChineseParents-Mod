using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace ChineseParentsMod
{
    // Game speed, with a drop to 1x while a chosen mini-game window is open (Settings.Minigames).
    public partial class Plugin
    {
        private const float ToastSeconds = 4f;
        private const int MinigamesPerRow = 4;

        // Null for windows that are not part of any listed mini-game.
        private readonly Dictionary<Type, Minigame> m_minigameOf = new Dictionary<Type, Minigame>();
        private Dictionary<EUILayer, List<IWnd>> m_windows;
        private UIRoot m_windowsOwner;
        private Minigame m_minigame;
        private string m_toast;
        private float m_toastUntil;

        private bool InMinigame => m_minigame != null;

        private void UpdateGameSpeed()
        {
            Minigame minigame = Settings.SlowInMinigames.Value ? OpenMinigame() : null;
#if SELFTEST
            if (m_forceMinigame.HasValue) minigame = m_forceMinigame.Value ? Settings.Minigames[0] : null;
#endif
            if (minigame != m_minigame)
            {
                // Moving between two windows of the same game (e.g. picking cards, then the battle) keeps it.
                Minigame left = m_minigame;
                m_minigame = minigame;
                if (Settings.GameSpeed > 1f)
                {
                    ShowToast(minigame != null ? $"进入{minigame.Name}，速度暂时调回 1×" : $"{left.Name}结束，速度恢复 {Settings.GameSpeedLabel}");
                }
            }
            // The game never sets timeScale itself, so the panel owns it.
            float speed = InMinigame ? 1f : Settings.GameSpeed;
            if (Time.timeScale != speed) Time.timeScale = speed;
        }

        private Minigame OpenMinigame()
        {
            UIRoot root = UIRoot.Instance;
            if (root == null)
            {
                return null;
            }
            if (root != m_windowsOwner || m_windows == null)
            {
                // The window table is filled during UIRoot's own setup; keep asking until it exists.
                m_windows = Traverse.Create(root).Field("DicLayer2WndList").GetValue<Dictionary<EUILayer, List<IWnd>>>();
                m_windowsOwner = m_windows == null ? null : root;
                if (m_windows == null) return null;
            }
            foreach (List<IWnd> layer in m_windows.Values)
            {
                foreach (IWnd window in layer)
                {
                    if (window == null || !window.IsShow) continue;
                    Minigame minigame = MinigameOf(window.GetType());
                    if (minigame != null && minigame.Enabled.Value) return minigame;
                }
            }
            return null;
        }

        private Minigame MinigameOf(Type type)
        {
            if (!m_minigameOf.TryGetValue(type, out Minigame result))
            {
                result = Array.Find(Settings.Minigames, game => Array.IndexOf(game.Windows, type.Name) >= 0);
                m_minigameOf[type] = result;
            }
            return result;
        }

        private void DrawMinigameSwitches()
        {
            Settings.SlowInMinigames.Value = SwitchRow("进下面亮着的小游戏时自动调回 1×，结束后恢复", Settings.SlowInMinigames.Value);
            if (!Settings.SlowInMinigames.Value) return;
            for (int i = 0; i < Settings.Minigames.Length; i += MinigamesPerRow)
            {
                GUILayout.Space(m_theme.S(6));
                GUILayout.BeginHorizontal();
                for (int j = i; j < Math.Min(i + MinigamesPerRow, Settings.Minigames.Length); j++)
                {
                    Minigame game = Settings.Minigames[j];
                    bool on = game.Enabled.Value;
                    if (GUILayout.Button(game.Name, on ? m_theme.LockOn : m_theme.LockOff, GUILayout.Width(m_theme.S(110)))) game.Enabled.Value = !on;
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            GUILayout.Label("点名字切换。学习、打工、相亲这类只需要等的环节一直加速。", m_theme.NoteRow);
        }

        private void ShowToast(string text)
        {
            m_toast = text;
            m_toastUntil = Time.unscaledTime + ToastSeconds;
        }

        private void DrawToast(string text)
        {
            var content = new GUIContent(text);
            Vector2 size = m_theme.Toast.CalcSize(content);
            GUI.Label(new Rect((Screen.width - size.x) / 2f, m_theme.S(24), size.x, size.y), content, m_theme.Toast);
        }
    }
}
