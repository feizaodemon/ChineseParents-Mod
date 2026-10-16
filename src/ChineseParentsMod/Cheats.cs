using System.Collections.Generic;
using HarmonyLib;
using Steamworks;

namespace ChineseParentsMod
{
    // Every change goes through the game's own managers, then the account save is written the same
    // way NewRecordMgr does it, so the game's normal save path keeps the result.
    internal static class Cheats
    {
        public static readonly (EMainInfoType Type, string Name)[] Attributes =
        {
            (EMainInfoType.IQ, "智商"),
            (EMainInfoType.EQ, "情商"),
            (EMainInfoType.Imagination, "想象"),
            (EMainInfoType.Memory, "记忆"),
            (EMainInfoType.Stamination, "体质"),
            (EMainInfoType.Charm, "魅力"),
            (EMainInfoType.Wisdom, "智慧"),
            (EMainInfoType.Potentiality, "潜力"),
            (EMainInfoType.Money, "金钱"),
            (EMainInfoType.Pressure, "压力"),
            (EMainInfoType.ParentDream, "父母期望"),
            (EMainInfoType.Shadow, "阴影"),
            (EMainInfoType.Face, "面子"),
        };

        private static NewRecordMgr Records => SingleTon<NewRecordMgr>.Instance;

        // Account managers are initialised from CurSaver after login.
        public static bool LoggedIn => Records.CurSaver != null;

        // Player attributes only exist while a playthrough is loaded.
        public static bool InGame => Records.CurrentRecord != null && Records.CurrentRoleCoreInfo != null;

        public static string UnlockAchievements(bool includeSteam)
        {
            AchievementMgr mgr = SingleTon<AchievementMgr>.Instance;
            Traverse t = Traverse.Create(mgr);
            var maxProcesses = t.Field("m_dicType2MaxProcesses").GetValue<Dictionary<EAchievementType, List<int>>>();
            var process = t.Field("m_dicType2Process").GetValue<Dictionary<EAchievementType, int>>();
            var completed = t.Field("m_dicType2CompletedStage").GetValue<Dictionary<EAchievementType, int>>();
            var fetched = t.Field("m_dicType2FetchedStage").GetValue<Dictionary<EAchievementType, int>>();
            foreach (KeyValuePair<EAchievementType, List<int>> pair in maxProcesses)
            {
                int top = pair.Value.Count - 1;
                process[pair.Key] = pair.Value[top];
                completed[pair.Key] = top;
                fetched[pair.Key] = top;
            }
            mgr.OnChg?.Invoke();
            SaveAccount();
            return "成就已全部完成。" + (includeSteam ? UnlockSteamAchievements() : "Steam 成就没动。");
        }

        private static string UnlockSteamAchievements()
        {
            if (!SteamManager.Initialized)
            {
                return "没连上 Steam，Steam 成就没动。";
            }
            uint total = SteamUserStats.GetNumAchievements();
            int unlocked = 0;
            for (uint i = 0; i < total; i++)
            {
                string name = SteamUserStats.GetAchievementName(i);
                if (SteamUserStats.GetAchievement(name, out bool achieved) && !achieved && SteamUserStats.SetAchievement(name))
                {
                    unlocked++;
                }
            }
            SteamUserStats.StoreStats();
            return unlocked == 0 ? $"Steam 的 {total} 个成就本来就齐了。" : $"Steam 成就 {total} 个，这次新解锁 {unlocked} 个。";
        }

        public static string UnlockCollections()
        {
            // Family collection level: unlocks shop items, jobs, courses and friends by table.
            CollectionMgr collection = SingleTon<CollectionMgr>.Instance;
            int level = collection.LastGenarationLevel;
            while (level < Common.COLLECTION_MAX_LEVEL && SingleTon<TableMgr>.Instance.TableCollection.GetItem(level + 1) != null)
            {
                level++;
            }
            Traverse.Create(collection).Property("LastGenarationLevel").SetValue(level);
            collection.OnLevelChg?.Invoke();

            NewTrickMgr tricks = SingleTon<NewTrickMgr>.Instance;
            int addedTricks = AddMissing(Traverse.Create(tricks).Field("m_familyCollectTricks").GetValue<List<int>>(), tricks.AllTrickIDs);
            tricks.OnTrickChange?.Invoke();

            Traverse jobs = Traverse.Create(SingleTon<NewJobMgr>.Instance);
            int addedJobs = AddMissing(jobs.Field("m_unlockJobs").GetValue<List<int>>(), jobs.Field("m_allEndingIDs").GetValue<List<int>>());

            NpcMgr npcs = SingleTon<NpcMgr>.Instance;
            npcs.UnlockNpcTest(ENpcUnlockConditionType.Collection, level);
            int addedNpcs = AddMissing(npcs.UnlockedNpcIds, SingleTon<TableMgr>.Instance.TableNPC.GetIDList());

            SaveAccount();
            return $"传承等级升到 {level} 级，新收录特长 {addedTricks} 个、职业 {addedJobs} 个、人物 {addedNpcs} 个。";
        }

        public static string UnlockTreasures()
        {
            TreasureMgr mgr = SingleTon<TreasureMgr>.Instance;
            int added = 0;
            foreach (int id in SingleTon<TableMgr>.Instance.TableTreasure.GetIDList())
            {
                if (!mgr.UnlockTreasureIDs.Contains(id))
                {
                    mgr.GainTreasure(id);
                    added++;
                }
            }
            SaveAccount();
            return added == 0 ? "传家宝早就齐了。" : $"新拿到传家宝 {added} 件。";
        }

        public static float GetAttribute(EMainInfoType type) => SingleTon<NewPlayerDataMgr>.Instance[type];

        public static void SetAttribute(EMainInfoType type, float value)
        {
            NewPlayerDataMgr player = SingleTon<NewPlayerDataMgr>.Instance;
            if (player[type] != value)
            {
                player[type] = value;
            }
        }

        // Same two steps as NewRecordMgr.Save, minus the playthrough record, so it is safe mid-round and in the menu.
        private static void SaveAccount()
        {
            AccessTools.Method(typeof(NewRecordMgr), "RefreshAccountInfo").Invoke(Records, null);
            string relative = Traverse.Create(typeof(NewRecordMgr)).Field("PATH_ACCOUNTSUBINFO").GetValue<string>();
            IOHelper.SetData(NewRecordMgr.persistentDataPath + relative, Records.CurSaver.SubInfo);
        }

        private static int AddMissing(List<int> target, List<int> all)
        {
            int added = 0;
            foreach (int id in all)
            {
                if (!target.Contains(id))
                {
                    target.Add(id);
                    added++;
                }
            }
            return added;
        }
    }
}
