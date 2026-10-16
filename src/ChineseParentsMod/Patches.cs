using HarmonyLib;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace ChineseParentsMod
{
    // Every typewriter text in the game goes through one of these two entry points.
    [HarmonyPatch(typeof(UIHelper), nameof(UIHelper.PlayTxt))]
    internal static class TypewriterPatch
    {
        private static void Prefix(ref float _duration) => _duration = Settings.ScaleDuration(_duration);
    }

    [HarmonyPatch(typeof(PlayableText), nameof(PlayableText.PlayTxt))]
    internal static class PlayableTextPatch
    {
        // Duration is a serialized field, so shorten it only for this call.
        private static void Prefix(PlayableText __instance, out float __state)
        {
            __state = __instance.Duration;
            __instance.Duration = Settings.ScaleDuration(__state);
        }

        private static void Postfix(PlayableText __instance, float __state) => __instance.Duration = __state;
    }

    // IMGUI does not consume clicks for uGUI, so a click on the panel would also hit the game button under it.
    // While the pointer is over the panel, skip the frame's uGUI input processing.
    [HarmonyPatch(typeof(EventSystem), "Update")]
    internal static class BlockClickThroughPatch
    {
        private static bool Prefix() => !Plugin.PointerOverPanel;
    }

    // The studio logo is a 2 s fade before the title screen; the game itself skips it in debug builds.
    [HarmonyPatch(typeof(BeginWnd), "OnShow")]
    internal static class SkipIntroPatch
    {
        private static bool Prefix()
        {
            if (!Settings.SkipIntro.Value)
            {
                return true;
            }
            UIRoot.Instance.DestoryAllWnd();
            SceneManager.LoadScene("Login");
            return false;
        }
    }
}
