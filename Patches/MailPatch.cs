using System.Reflection;
using HarmonyLib;
using Wish;
using SunHavenAccess.Speech;
using SunHavenAccess.Util;

namespace SunHavenAccess.Patches
{
    /// <summary>
    /// Lit à voix haute le contenu d'une lettre dès l'ouverture de la boîte aux lettres (ou en
    /// passant à la lettre suivante) — jusqu'ici, seul le texte visible mais jamais annoncé
    /// automatiquement (aucun mécanisme équivalent à la lecture des dialogues). Postfix sur
    /// `Wish.Mailbox.OpenMail()` (privée : Harmony patch quand même les méthodes privées), qui
    /// vient de composer le texte complet (message + signature + nom de l'expéditeur + post-
    /// scriptum) dans `MailUI.messageTMP.text` juste avant. `mailUI` est un champ D'INSTANCE
    /// privé de Mailbox : lu par réflexion, comme pour la touche du tchat et la quantité des
    /// objets ailleurs dans le mod. `OpenNextMail()` appelle aussi `OpenMail()` en interne, donc
    /// ce seul point d'accroche couvre l'ouverture ET le passage à la lettre suivante.
    /// </summary>
    [HarmonyPatch(typeof(Mailbox), "OpenMail")]
    public static class MailReaderPatch
    {
        private static FieldInfo _mailUIField;

        private static void Postfix(Mailbox __instance)
        {
            _mailUIField ??= typeof(Mailbox).GetField("mailUI", BindingFlags.NonPublic | BindingFlags.Instance);
            var mailUI = _mailUIField?.GetValue(__instance) as MailUI;
            if (mailUI?.messageTMP == null) return;

            string clean = TextUtil.Clean(mailUI.messageTMP.text);
            if (string.IsNullOrWhiteSpace(clean)) return;

            TolkSpeech.Speak(clean, interrupt: true);
        }
    }
}
