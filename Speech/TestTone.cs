using System.Runtime.InteropServices;
using BepInEx.Logging;

namespace SunHavenAccess.Speech
{
    /// <summary>
    /// Joue un son système Windows standard (l'alias "SystemAsterisk", le petit "ding" de
    /// notification), complètement indépendant de NVDA/SAPI/Tolk. Sert uniquement au
    /// diagnostic : si ce son est entendu mais pas la voix, le souci est spécifique au
    /// pipeline vocal ; si ce son est lui aussi inaudible, le souci est plus général
    /// (périphérique audio, mode plein écran exclusif...).
    /// </summary>
    public static class TestTone
    {
        [DllImport("winmm.dll", CharSet = CharSet.Auto)]
        private static extern bool PlaySound(string pszSound, System.IntPtr hmod, uint fdwSound);

        private const uint SND_ALIAS = 0x00010000;
        private const uint SND_ASYNC = 0x0001;

        public static void Play(ManualLogSource log)
        {
            try
            {
                bool ok = PlaySound("SystemAsterisk", System.IntPtr.Zero, SND_ALIAS | SND_ASYNC);
                log?.LogInfo($"TestTone.Play -> {ok}");
            }
            catch (System.Exception e)
            {
                log?.LogWarning("TestTone a échoué : " + e.Message);
            }
        }
    }
}
