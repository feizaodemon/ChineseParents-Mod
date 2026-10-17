using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace ChineseParentsMod
{
    // Game speed, with a drop to 1x while a mini-game window is open: speeding those up only makes them harder.
    public partial class Plugin
    {
        private const float ToastSeconds = 4f;

        // Only the mini-games that run on a real-time clock (Time.deltaTime in RedPacket, AimBar and ColorPlate).
        // The turn-based ones (study, face fight, election, quiz...) only have animations, so they keep the speed.
        internal static readonly HashSet<string> MinigameWindows = new HashSet<string> { "OldRedPacketWnd", "ArtExamWnd" };

        private readonly Dictionary<Type, bool> m_isMinigame = new Dictionary<Type, bool>();
        private Dictionary<EUILayer, List<IWnd>> m_windows;
        private UIRoot m_windowsOwner;
        private bool m_inMinigame;
        private string m_toast;
        private float m_toastUntil;

        private void UpdateGameSpeed()
        {
            bool inMinigame = Settings.SlowInMinigames.Value && MinigameOpen();
#if SELFTEST
            if (m_forceMinigame.HasValue) inMinigame = m_forceMinigame.Value;
#endif
            if (inMinigame != m_inMinigame)
            {
                m_inMinigame = inMinigame;
                if (Settings.GameSpeed > 1f)
                {
                    ShowToast(inMinigame ? "进入小游戏，速度暂时调回 1×" : "小游戏结束，速度恢复 " + Settings.GameSpeedLabel);
                }
            }
            // The game never sets timeScale itself, so the panel owns it.
            float speed = m_inMinigame ? 1f : Settings.GameSpeed;
            if (Time.timeScale != speed) Time.timeScale = speed;
        }

        private bool MinigameOpen()
        {
            UIRoot root = UIRoot.Instance;
            if (root == null)
            {
                return false;
            }
            if (root != m_windowsOwner || m_windows == null)
            {
                // The window table is filled during UIRoot's own setup; keep asking until it exists.
                m_windows = Traverse.Create(root).Field("DicLayer2WndList").GetValue<Dictionary<EUILayer, List<IWnd>>>();
                m_windowsOwner = m_windows == null ? null : root;
                if (m_windows == null) return false;
            }
            foreach (List<IWnd> layer in m_windows.Values)
            {
                foreach (IWnd window in layer)
                {
                    if (window != null && window.IsShow && IsMinigame(window.GetType())) return true;
                }
            }
            return false;
        }

        private bool IsMinigame(Type type)
        {
            if (!m_isMinigame.TryGetValue(type, out bool result))
            {
                result = MinigameWindows.Contains(type.Name);
                m_isMinigame[type] = result;
            }
            return result;
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
