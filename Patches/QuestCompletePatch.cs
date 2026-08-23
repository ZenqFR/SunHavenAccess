using HarmonyLib;
using Wish;
using SunHavenAccess.Speech;
using SunHavenAccess.Util;

namespace SunHavenAccess.Patches
{
    /// <summary>
    /// Annonce quand une quête est rendue/terminée. Postfix sur `Wish.Quest.TurnInQuest(Player)`
    /// (privée : Harmony patch quand même les méthodes privées, comme MailPatch) — appelée par
    /// `Quest.CompleteQuest` juste après la validation des récompenses, donc après que toutes les
    /// conditions de la quête sont réellement remplies. `questAsset` est un champ PUBLIC de
    /// `Quest` (voir Info/QuestAnnouncer.cs) : aucune réflexion nécessaire ici.
    /// </summary>
    [HarmonyPatch(typeof(Quest), "TurnInQuest")]
    public static class QuestCompletePatch
    {
        private static void Postfix(Quest __instance)
        {
            if (__instance?.questAsset == null) return;
            string name = TextUtil.Clean(__instance.questAsset.LocalizedQuestName);
            TolkSpeech.Speak($"Quête terminée : {name}.", interrupt: false);
        }
    }
}
