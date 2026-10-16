#if PREVIEW
using System.Collections;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using UnityEngine;

namespace ChineseParentsMod
{
    // Preview build only: opens the panel on the title screen with fake data, saves screenshots, then quits.
    public partial class Plugin
    {
        private int m_previewStage;
        private readonly Dictionary<EMainInfoType, float> m_fake = new Dictionary<EMainInfoType, float>();

        private bool IsLoggedIn() => m_previewStage >= 1;
        private bool IsInGame() => m_previewStage >= 2;
        private float ReadAttribute(EMainInfoType type) => m_fake.TryGetValue(type, out float v) ? v : 120 + (int)type * 37;
        private void WriteAttribute(EMainInfoType type, float value) => m_fake[type] = value;

        private IEnumerator PreviewShots()
        {
            string dir = Path.Combine(Paths.BepInExRootPath, "preview");
            Directory.CreateDirectory(dir);
            yield return new WaitForSeconds(12f);
            m_show = true;
            yield return Shot(dir, "0-logged-out");
            m_previewStage = 2;
            Report("成就已全部完成。Steam 成就 23 个，这次新解锁 2 个。");
            yield return Shot(dir, "1-unlock");
            m_tab = 1;
            m_fake[EMainInfoType.IQ] = 9999;
            m_locked[EMainInfoType.IQ] = 9999;
            Report("智商改成了 9999。");
            yield return Shot(dir, "2-attributes");
            m_previewStage = 1;
            m_message = "";
            yield return Shot(dir, "3-attributes-menu");
            m_tab = 2;
            yield return Shot(dir, "4-settings");
            Application.Quit();
        }

        private static IEnumerator Shot(string dir, string name)
        {
            yield return new WaitForSeconds(1f);
            ScreenCapture.CaptureScreenshot(Path.Combine(dir, name + ".png"));
            yield return new WaitForSeconds(1f);
        }
    }
}
#endif
