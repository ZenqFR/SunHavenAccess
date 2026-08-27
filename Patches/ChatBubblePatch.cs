using HarmonyLib;
using UnityEngine;
using Wish;
using SunHavenAccess.Speech;
using SunHavenAccess.Util;

namespace SunHavenAccess.Patches
{
    /// <summary>
    /// Les bulles de discussion qui apparaissent au-dessus des personnages — répliques de PNJ en
    /// passant, et messages des autres joueurs en coopération.
    ///
    /// Le texte seul ne suffit pas : à l'écran, la bulle est ancrée au-dessus de celui qui parle,
    /// et c'est cette position qui dit qui s'exprime. Sans la vue, une réplique arrivait sans
    /// aucune attribution — impossible de distinguer un villageois d'un partenaire de jeu, ni de
    /// savoir lequel de deux PNJ voisins vient de parler.
    ///
    /// `SetupNotification` reçoit justement le transform de l'émetteur en premier paramètre, pour
    /// y accrocher la bulle. On s'en sert pour le nommer.
    /// </summary>
    [HarmonyPatch(typeof(ChatBubble), "SetupNotification")]
    public static class ChatBubblePatch
    {
        private static void Postfix(Transform parent, string text)
        {
            string clean = TextUtil.Clean(text);
            if (string.IsNullOrWhiteSpace(clean)) return;

            string speaker = SpeakerName(parent);
            TolkSpeech.Speak(
                string.IsNullOrWhiteSpace(speaker) ? clean : $"{speaker} : {clean}",
                interrupt: false);
        }

        /// <summary>
        /// Qui parle, d'après l'objet auquel la bulle est accrochée.
        ///
        /// On remonte la hiérarchie plutôt que de regarder le seul parent direct : la bulle est
        /// souvent rattachée à un point d'ancrage enfant du personnage, pas au personnage lui-même.
        ///
        /// Renvoie null si rien de reconnaissable ne s'y trouve — auquel cas la réplique est lue
        /// sans nom, ce qui reste préférable à un nom inventé.
        /// </summary>
        private static string SpeakerName(Transform parent)
        {
            if (parent == null) return null;

            try
            {
                // Un autre joueur d'abord : c'est la distinction qui compte le plus en
                // coopération, et un joueur n'est jamais un PNJ.
                Player player = parent.GetComponentInParent<Player>();
                if (player != null)
                {
                    if (player == Player.Instance) return "Vous";
                    string playerName = TextUtil.Clean(player.name);
                    return string.IsNullOrWhiteSpace(playerName) ? "Un autre joueur" : playerName;
                }

                NPCAI npc = parent.GetComponentInParent<NPCAI>();
                if (npc != null)
                {
                    string npcName = TextUtil.Clean(npc.LocalizedActualNPCName);
                    if (!string.IsNullOrWhiteSpace(npcName)) return npcName;
                }
            }
            catch { }

            return null;
        }
    }

    /// <summary>
    /// Le tchat écrit, distinct des bulles au-dessus des têtes.
    ///
    /// Sun Haven a deux canaux : la bulle éphémère ancrée au personnage, et le journal de tchat.
    /// C'est ce second qui porte les échanges en coopération, et rien ne le lisait — un partenaire
    /// pouvait écrire sans qu'on en sache jamais rien.
    ///
    /// Cette surcharge reçoit le nom de l'émetteur ET son message, ce qui évite d'avoir à deviner
    /// qui parle. Le jeu la colore ensuite pour l'affichage ; on lit les deux morceaux avant
    /// qu'il ne s'en mêle.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.SendChatMessage), new System.Type[] { typeof(string), typeof(string) })]
    public static class ChatLogPatch
    {
        private static void Postfix(string characterName, string message)
        {
            string cleanMessage = TextUtil.Clean(message);
            if (string.IsNullOrWhiteSpace(cleanMessage)) return;

            string cleanName = TextUtil.Clean(characterName);

            // Sans interruption : un message qui arrive pendant qu'on écoute autre chose ne doit
            // pas couper la parole, contrairement à une alerte de combat.
            TolkSpeech.Speak(
                string.IsNullOrWhiteSpace(cleanName) ? cleanMessage : SunHavenAccess.Localization.Language.T($"{cleanName} écrit : {cleanMessage}", $"{cleanName} writes: {cleanMessage}"),
                interrupt: false);
        }
    }
}
