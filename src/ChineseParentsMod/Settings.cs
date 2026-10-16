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

        public static ConfigEntry<int> TextSpeedIndex;
        public static ConfigEntry<bool> SkipIntro;
        public static ConfigEntry<bool> SteamAchievements;

        public static void Bind(ConfigFile config)
        {
            TextSpeedIndex = config.Bind("Text", "SpeedIndex", 0, "Typewriter speed: 0 = 1x, 1 = 2x, 2 = 4x, 3 = 8x, 4 = instant.");
            SkipIntro = config.Bind("Startup", "SkipIntro", true, "Skip the studio logo and go straight to the title screen.");
            SteamAchievements = config.Bind("Unlock", "SteamAchievements", true, "Also unlock Steam achievements when unlocking all achievements.");
        }

        public static float ScaleDuration(float duration)
        {
            float speed = TextSpeeds[Mathf.Clamp(TextSpeedIndex.Value, 0, TextSpeeds.Length - 1)];
            // DOTween finishes a near-zero tween on the next update, which reads as instant.
            return speed <= 0f ? 0.01f : duration / speed;
        }
    }
}
