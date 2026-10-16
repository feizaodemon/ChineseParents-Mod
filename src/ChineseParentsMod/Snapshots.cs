using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

namespace ChineseParentsMod
{
    internal enum SnapshotKind { Manual, BeforeUnlock, BeforeRestore }

    internal sealed class Snapshot
    {
        public string Dir;
        public string Name;
        public DateTime Time;
        public SnapshotKind Kind;
        public string Note;

        public string When => Time.ToString("MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

        public string What =>
            Kind == SnapshotKind.Manual ? "手动" :
            Kind == SnapshotKind.BeforeUnlock ? (Note == "学费" ? "改学费前" : $"解锁{Note}前") : "回滚前";
    }

    // Whole-folder copies of the game's Save directory. They live in ModSnapshots next to Save, so Steam Cloud
    // (which syncs Save) never sees them. A restore is only scheduled while the game runs and is applied by the
    // next launch before the game reads its saves, so nothing held in memory can overwrite the restored files.
    internal static class Snapshots
    {
        public const int KeepAutomatic = 10;
        internal const string PendingFile = "pending-restore.txt";
        private const string CloudFile = "steam_autocloud.vdf";

        // Steam build: NewRecordMgr.persistentDataPath is Application.persistentDataPath. GlobalObj does not exist
        // yet when the plugin wakes up, so use Unity's path directly.
        public static string SaveDir => Path.Combine(Application.persistentDataPath, "Save");
        public static string Root => Path.Combine(Application.persistentDataPath, "ModSnapshots");

        public static Snapshot Create(SnapshotKind kind, string note = "") => Create(SaveDir, Root, kind, note);

        public static List<Snapshot> List() => List(Root);

        public static void Delete(Snapshot snapshot) => Directory.Delete(snapshot.Dir, true);

        public static string PendingName() => PendingName(Root);

        public static void ScheduleRestore(Snapshot snapshot) => File.WriteAllText(Path.Combine(Root, PendingFile), snapshot.Name);

        public static void CancelRestore() => File.Delete(Path.Combine(Root, PendingFile));

        public static string ApplyPendingRestore() => ApplyPendingRestore(SaveDir, Root);

        internal static Snapshot Create(string saveDir, string root, SnapshotKind kind, string note)
        {
            string name = $"{DateTime.Now:yyyyMMdd-HHmmss-fff}_{kind}_{note}";
            string dir = Path.Combine(root, name);
            Directory.CreateDirectory(dir);
            foreach (string file in Directory.GetFiles(saveDir))
            {
                string fileName = Path.GetFileName(file);
                if (fileName != CloudFile)
                {
                    File.Copy(file, Path.Combine(dir, fileName));
                }
            }
            if (kind != SnapshotKind.Manual)
            {
                foreach (Snapshot old in List(root).Where(s => s.Kind != SnapshotKind.Manual).Skip(KeepAutomatic))
                {
                    Delete(old);
                }
            }
            return Parse(dir);
        }

        internal static List<Snapshot> List(string root)
        {
            if (!Directory.Exists(root))
            {
                return new List<Snapshot>();
            }
            return Directory.GetDirectories(root).Select(Parse).Where(s => s != null).OrderByDescending(s => s.Time).ToList();
        }

        internal static string PendingName(string root)
        {
            string path = Path.Combine(root, PendingFile);
            return File.Exists(path) ? File.ReadAllText(path).Trim() : null;
        }

        // Returns a line for the log, or null when nothing was pending.
        internal static string ApplyPendingRestore(string saveDir, string root)
        {
            string name = PendingName(root);
            if (name == null)
            {
                return null;
            }
            File.Delete(Path.Combine(root, PendingFile));
            string source = Path.Combine(root, name);
            if (!Directory.Exists(source))
            {
                return $"pending restore skipped: snapshot {name} no longer exists";
            }
            Snapshot before = Create(saveDir, root, SnapshotKind.BeforeRestore, "");
            var keep = new HashSet<string>(Directory.GetFiles(source).Select(Path.GetFileName)) { CloudFile };
            // Files written after the snapshot (e.g. a new playthrough slot) go away; the BeforeRestore copy keeps them.
            foreach (string file in Directory.GetFiles(saveDir))
            {
                if (!keep.Contains(Path.GetFileName(file)))
                {
                    File.Delete(file);
                }
            }
            foreach (string file in Directory.GetFiles(source))
            {
                File.Copy(file, Path.Combine(saveDir, Path.GetFileName(file)), true);
            }
            return $"restored snapshot {name}; previous saves kept in {before.Name}";
        }

        private static Snapshot Parse(string dir)
        {
            string name = Path.GetFileName(dir);
            string[] parts = name.Split(new[] { '_' }, 3);
            if (parts.Length < 2
                || !DateTime.TryParseExact(parts[0], "yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime time)
                || !Enum.TryParse(parts[1], out SnapshotKind kind))
            {
                return null;
            }
            return new Snapshot { Dir = dir, Name = name, Time = time, Kind = kind, Note = parts.Length > 2 ? parts[2] : "" };
        }
    }
}
