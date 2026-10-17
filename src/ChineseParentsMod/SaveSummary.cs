using System.IO;
using System.Linq;

namespace ChineseParentsMod
{
    // A short line telling which playthrough a snapshot holds, read the way the game's load screen reads it.
    internal static class SaveSummary
    {
        private const int MaxNameLength = 4;

        public static string Describe(string dir)
        {
            // GetFiles("*.sav") would also match the "-bak" copies, so filter by the exact ending.
            FileInfo newest = new DirectoryInfo(dir).GetFiles("GameData-Record-*")
                .Where(f => f.Name.EndsWith(".v2.sav"))
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .FirstOrDefault();
            if (newest == null) return "还没有对局";
            SaverRoleCoreInfo role = (IOHelper.GetData(newest.FullName, typeof(RecordStructure)) as RecordStructure)?.RoleCoreInfo;
            if (role == null) return "对局读不出";

            string name = string.IsNullOrEmpty(role.selfName) ? SingleTon<TranslateMgr>.Instance.GetUIString("InitName") : role.selfName;
            if (name.Length > MaxNameLength) name = name.Substring(0, MaxNameLength) + "…";
            string age;
            try
            {
                age = SingleTon<TableMgr>.Instance.TableWeekDefault.GetItem(role.curRound).Age + " 岁";
            }
            catch
            {
                age = $"第 {role.curRound} 回合";
            }
            string generation = role.curGeneration > 1 ? $"第{role.curGeneration}代 " : "";
            return $"{(role.dlc != 0 ? "DLC " : "")}{name} {generation}{age}";
        }
    }
}
