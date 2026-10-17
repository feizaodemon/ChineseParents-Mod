using System;
using System.Collections.Generic;
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

    // Endless DOTweenAnimation loops (button glows, pulsing hints) are decoration set up in prefabs. Run them on
    // real time so the game speed setting does not make them flash faster; one-shot tweens still follow it.
    [HarmonyPatch(typeof(DG.Tweening.DOTweenAnimation), nameof(DG.Tweening.DOTweenAnimation.CreateTween))]
    internal static class DecorativeLoopPatch
    {
        private static void Prefix(DG.Tweening.DOTweenAnimation __instance)
        {
            if (__instance.loops == -1) __instance.isIndependentUpdate = true;
        }
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

    // Every special event (red packet, composition contest, DLC games...) starts here. Prefix, so the windows
    // already open before the event can be told apart from the ones it opens.
    [HarmonyPatch(typeof(NewActionMgr), nameof(NewActionMgr.DoActionEffect))]
    internal static class SpecialEventPatch
    {
        private static void Prefix(ExtraActionInfoPair _obj)
        {
            if (_obj != null) Plugin.Instance?.WatchSpecialEvent(_obj.type.ToString());
        }
    }

#if SELFTEST
    // Targets a method that does not exist, so the self-test can check that one bad patch leaves the rest working.
    [HarmonyPatch(typeof(Plugin), "NoSuchMethod")]
    internal static class BrokenPatchForSelfTest
    {
        private static void Prefix() { }
    }
#endif

    internal static class PatchLoader
    {
        private static readonly Dictionary<string, string> Features = new Dictionary<string, string>
        {
            { nameof(TypewriterPatch), "文本速度" },
            { nameof(PlayableTextPatch), "文本速度" },
            { nameof(DecorativeLoopPatch), "装饰光效不跟着加速" },
            { nameof(BlockClickThroughPatch), "防点击穿透" },
            { nameof(SkipIntroPatch), "跳过开场动画" },
            { nameof(SpecialEventPatch), "特殊事件记录" },
        };

        // One class at a time: if a game update renames one target, only that feature stops working.
        // Returns the player-facing names of the features that could not be patched.
        public static List<string> Apply(Harmony harmony, Action<string> logError)
        {
            var failed = new List<string>();
            foreach (Type type in AccessTools.GetTypesFromAssembly(typeof(PatchLoader).Assembly))
            {
                if (!type.IsDefined(typeof(HarmonyPatch), false)) continue;
                try
                {
                    harmony.CreateClassProcessor(type).Patch();
                }
                catch (Exception e)
                {
                    logError($"patch {type.Name} failed, that feature is off: {e.Message}");
                    string feature = Features.TryGetValue(type.Name, out string name) ? name : type.Name;
                    if (!failed.Contains(feature)) failed.Add(feature);
                }
            }
            return failed;
        }
    }
}
