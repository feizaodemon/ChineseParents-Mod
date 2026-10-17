using BepInEx.Configuration;
using UnityEngine;

namespace ChineseParentsMod
{
    // Persistent choices, stored in BepInEx\config\local.chineseparents.cheatpanel.cfg.
    internal static class Settings
    {
        // 0 means instant.
        public static readonly float[] TextSpeeds = { 1f, 2f, 4f, 8f, 0f };
        public static readonly string[] TextSpeedLabels = { "1×", "2×", "4×", "8×", "瞬间" };

        public static readonly float[] GameSpeeds = { 1f, 2f, 3f, 5f };
        public static readonly string[] GameSpeedLabels = { "1×", "2×", "3×", "5×" };

        public static ConfigEntry<int> TextSpeedIndex;
        public static ConfigEntry<int> GameSpeedIndex;
        public static ConfigEntry<bool> SlowInMinigames;

        // Mini-games that can drop the game to 1x, with the windows that make up each one.
        // On by default: the ones where speed changes the result (real-time clocks) or that the user asked for.
        // Study, work, the talent-line exam and blind dates only make you wait, so they are not listed and always keep the speed.
        public static readonly Minigame[] Minigames =
        {
            new Minigame("RedPacket", "抢红包", true, "OldRedPacketWnd"),
            new Minigame("ArtExam", "美术考试", true, "ArtExamWnd"),
            new Minigame("Composition", "作文大赛", true, "CompositeWnd"),
            new Minigame("LoL", "打团", true, "LOLEntryWnd", "LOLSelectWnd", "LOLWnd"),
            new Minigame("FaceFight", "面子大战", false, "FaceFightSelectionWnd", "FaceFightWnd"),
            new Minigame("Candidate", "竞选", false, "OldCandidateWnd"),
            new Minigame("StarShow", "才艺表演", false, "OldStarShowWnd"),
        };
        public static ConfigEntry<bool> SkipIntro;
        public static ConfigEntry<bool> SteamAchievements;
        public static ConfigEntry<bool> AutoSnapshot;
        public static ConfigEntry<bool> PanelOpened;

        public static void Bind(ConfigFile config)
        {
            GameSpeedIndex = config.Bind("Game", "SpeedIndex", 0, "Game speed (Time.timeScale): 0 = 1x, 1 = 2x, 2 = 3x, 3 = 5x.");
            SlowInMinigames = config.Bind("Game", "SlowInMinigames", true, "Drop to 1x while a mini-game window is open, then restore.");
            foreach (Minigame game in Minigames)
            {
                game.Enabled = config.Bind("Minigames", game.Key, game.OnByDefault, $"Drop to 1x during this mini-game ({string.Join(", ", game.Windows)}).");
            }
            TextSpeedIndex = config.Bind("Text", "SpeedIndex", 0, "Typewriter speed: 0 = 1x, 1 = 2x, 2 = 4x, 3 = 8x, 4 = instant.");
            SkipIntro = config.Bind("Startup", "SkipIntro", true, "Skip the studio logo and go straight to the title screen.");
            SteamAchievements = config.Bind("Unlock", "SteamAchievements", true, "Also unlock Steam achievements when unlocking all achievements.");
            AutoSnapshot = config.Bind("Saves", "AutoSnapshot", true, "Snapshot the save folder before every unlock.");
            PanelOpened = config.Bind("Ui", "PanelOpened", false, "Set after the panel is opened once; until then the title screen shows an F1 hint.");
        }

        public static float GameSpeed => GameSpeeds[Mathf.Clamp(GameSpeedIndex.Value, 0, GameSpeeds.Length - 1)];
        public static string GameSpeedLabel => GameSpeedLabels[Mathf.Clamp(GameSpeedIndex.Value, 0, GameSpeeds.Length - 1)];

        public static float ScaleDuration(float duration)
        {
            float speed = TextSpeeds[Mathf.Clamp(TextSpeedIndex.Value, 0, TextSpeeds.Length - 1)];
            // DOTween finishes a near-zero tween on the next update, which reads as instant.
            return speed <= 0f ? 0.01f : duration / speed;
        }
    }

    internal sealed class Minigame
    {
        public readonly string Key;
        public readonly string Name;
        public readonly bool OnByDefault;
        public readonly string[] Windows;
        public ConfigEntry<bool> Enabled;

        public Minigame(string key, string name, bool onByDefault, params string[] windows)
        {
            Key = key;
            Name = name;
            OnByDefault = onByDefault;
            Windows = windows;
        }
    }
}
