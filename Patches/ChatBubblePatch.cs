using HarmonyLib;
using Wish;
using SunHavenAccess.Speech;
using SunHavenAccess.Util;

namespace SunHavenAccess.Patches
{
    /// <summary>
    /// Vocalise chaque bulle de discussion/notification qui apparaît au-dessus d'un PNJ ou
    /// d'un objet (Wish.ChatBubble.SetupNotification). N'interrompt pas la parole en cours
    /// (interrupt: false) pour ne pas couper une lecture de dialogue déjà en train de parler.
    /// </summary>
    [HarmonyPatch(typeof(ChatBubble), "SetupNotification")]
    public static class ChatBubblePatch
    {
        private static void Postfix(string text)
        {
            string clean = TextUtil.Clean(text);
            if (!string.IsNullOrWhiteSpace(clean))
            {
                TolkSpeech.Speak(clean, interrupt: false);
            }
        }
    }
}
