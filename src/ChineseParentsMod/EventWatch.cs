using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ChineseParentsMod
{
    // Logs which windows each kind of special event opens, once per kind per session. A new mini-game (from a DLC,
    // say) then shows up in LogOutput.log with its window names, ready to add to Settings.Minigames.
    public partial class Plugin
    {
        private const float EventWatchSeconds = 8f;

        private readonly HashSet<string> m_loggedEvents = new HashSet<string>();
        private readonly HashSet<Type> m_windowsBeforeEvent = new HashSet<Type>();
        private readonly List<Type> m_eventWindows = new List<Type>();
        private readonly List<Type> m_shownScratch = new List<Type>();
        private string m_watchedEvent;
        private float m_watchUntil;
        private string m_lastEventLog;

        internal void WatchSpecialEvent(string type)
        {
            if (m_watchedEvent != null || m_loggedEvents.Contains(type)) return;
            try
            {
                m_windowsBeforeEvent.Clear();
                m_eventWindows.Clear();
                AddShownWindows(m_shownScratch);
                m_windowsBeforeEvent.UnionWith(m_shownScratch);
                m_watchedEvent = type;
                m_watchUntil = Time.unscaledTime + EventWatchSeconds;
            }
            catch (Exception e)
            {
                Logger.LogWarning($"cannot watch special event {type}: {e.Message}");
            }
        }

        private void UpdateEventWatch()
        {
            if (m_watchedEvent == null) return;
            try
            {
                AddShownWindows(m_shownScratch);
                foreach (Type type in m_shownScratch)
                {
                    if (!m_windowsBeforeEvent.Contains(type) && !m_eventWindows.Contains(type)) m_eventWindows.Add(type);
                }
                if (Time.unscaledTime < m_watchUntil) return;
                string windows = m_eventWindows.Count == 0 ? "no new window" : string.Join(", ", m_eventWindows.Select(DescribeWindow));
                m_lastEventLog = $"special event {m_watchedEvent} opened: {windows}";
                Logger.LogInfo(m_lastEventLog);
            }
            catch (Exception e)
            {
                Logger.LogWarning($"special event {m_watchedEvent} watch failed: {e.Message}");
            }
            m_loggedEvents.Add(m_watchedEvent);
            m_watchedEvent = null;
        }

        private string DescribeWindow(Type type)
        {
            Minigame game = MinigameOf(type);
            return game == null ? type.Name : $"{type.Name} (mini-game {game.Key})";
        }

        private void AddShownWindows(List<Type> into)
        {
            into.Clear();
            if (WindowTable() == null) return;
            foreach (List<IWnd> layer in m_windows.Values)
            {
                foreach (IWnd window in layer)
                {
                    if (window != null && window.IsShow) into.Add(window.GetType());
                }
            }
        }
    }
}
