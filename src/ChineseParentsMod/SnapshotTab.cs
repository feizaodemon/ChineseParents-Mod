using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ChineseParentsMod
{
    // The 存档 tab: take a snapshot, schedule a rollback, delete old snapshots.
    public partial class Plugin
    {
        private const int VisibleSnapshots = 8;
        private const float ConfirmSeconds = 4f;

        // Null means "read the folder again on the next draw".
        private List<Snapshot> m_snapshots;
        private string m_pending;
        private string m_confirmKey;
        private float m_confirmUntil;

        // If the snapshot fails the unlock does not run, so there is never an unlock without a way back.
        private string SnapshotThen(string what, Func<string> unlock)
        {
            if (Settings.AutoSnapshot.Value)
            {
                Snapshots.Create(SnapshotKind.BeforeUnlock, what);
                m_snapshots = null;
            }
            return unlock();
        }

        private void DrawSnapshots()
        {
            if (m_snapshots == null)
            {
                try
                {
                    m_snapshots = Snapshots.List();
                    m_pending = Snapshots.PendingName();
                }
                catch (Exception e)
                {
                    m_snapshots = new List<Snapshot>();
                    m_pending = null;
                    Report("读不了快照目录：" + e.Message, e);
                }
            }
            List<Snapshot> list = m_snapshots;

            if (m_pending != null)
            {
                Snapshot target = list.Find(s => s.Name == m_pending);
                Card("回滚已排好：" + (target == null ? m_pending : target.When), "下次启动游戏时生效。当前存档会先另存为一份“回滚前”快照。", null, () =>
                {
                    GUILayout.Space(m_theme.S(8));
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("现在退出游戏", m_theme.Confirm, GUILayout.Width(m_theme.S(130)))) Application.Quit();
                    if (GUILayout.Button("取消回滚", m_theme.Ghost, GUILayout.Width(m_theme.S(100))))
                    {
                        Run(() =>
                        {
                            Snapshots.CancelRestore();
                            m_snapshots = null;
                            return "回滚已取消。";
                        });
                    }
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                });
            }

            Card("存一份快照", "把现在的存档整个备份一份。正在玩的这局到回合结束才存档，之前的进度不在快照里。",
                () => ActionButton("存快照", () =>
                {
                    Snapshot s = Snapshots.Create(SnapshotKind.Manual);
                    m_snapshots = null;
                    return $"快照已存：{s.When}。";
                }),
                () => Settings.AutoSnapshot.Value = SwitchRow("改动前自动存一份快照（最多留 10 份）", Settings.AutoSnapshot.Value));

            if (list.Count == 0)
            {
                GUILayout.Label("还没有快照。", m_theme.Empty, GUILayout.Height(m_theme.S(80)));
            }
            for (int i = 0; i < list.Count && i < VisibleSnapshots; i++)
            {
                DrawSnapshotRow(list[i], i % 2 == 0 ? m_theme.Row : m_theme.RowAlt);
            }

            GUILayout.Space(m_theme.S(6));
            GUILayout.BeginHorizontal();
            string more = list.Count > VisibleSnapshots ? $"另有 {list.Count - VisibleSnapshots} 份更早的没列出。" : "";
            GUILayout.Label($"回滚在下次启动时生效。手动快照不会自动删除。{more}", m_theme.Note, GUILayout.Width(m_theme.S(380)));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("打开文件夹", m_theme.Ghost, GUILayout.Width(m_theme.S(100))))
            {
                Directory.CreateDirectory(Snapshots.Root);
                Application.OpenURL(new Uri(Snapshots.Root).AbsoluteUri);
            }
            GUILayout.EndHorizontal();
        }

        private void DrawSnapshotRow(Snapshot snapshot, GUIStyle row)
        {
            bool pending = snapshot.Name == m_pending;
            GUILayout.BeginHorizontal(row);
            GUILayout.Label(snapshot.When, m_theme.RowName, GUILayout.Width(m_theme.S(140)));
            GUILayout.Label(snapshot.What, m_theme.NoteRow, GUILayout.Width(m_theme.S(110)));
            GUILayout.FlexibleSpace();
            if (pending)
            {
                GUILayout.Label("将回滚到这里", m_theme.ChipAmber);
            }
            else if (ConfirmButton("restore:" + snapshot.Name, "回滚"))
            {
                Run(() =>
                {
                    Snapshots.ScheduleRestore(snapshot);
                    m_snapshots = null;
                    return $"已排好回滚到 {snapshot.When}，下次启动游戏时生效。";
                });
            }
            if (ConfirmButton("delete:" + snapshot.Name, "删除"))
            {
                Run(() =>
                {
                    if (pending) Snapshots.CancelRestore();
                    Snapshots.Delete(snapshot);
                    m_snapshots = null;
                    return pending ? $"已删除 {snapshot.When} 的快照，回滚也取消了。" : $"已删除 {snapshot.When} 的快照。";
                });
            }
            GUILayout.EndHorizontal();
        }

        // First click arms the button for a few seconds; only a second click inside that window acts.
        private bool ConfirmButton(string key, string label)
        {
            bool armed = m_confirmKey == key && Time.unscaledTime < m_confirmUntil;
            if (!GUILayout.Button(armed ? "确认？" : label, armed ? m_theme.Confirm : m_theme.Ghost, GUILayout.Width(m_theme.S(72))))
            {
                return false;
            }
            if (armed)
            {
                m_confirmKey = null;
                return true;
            }
            m_confirmKey = key;
            m_confirmUntil = Time.unscaledTime + ConfirmSeconds;
            return false;
        }
    }
}
