using UnityEngine;
using Wish;
using SunHavenAccess.Speech;

namespace SunHavenAccess.Info
{
    /// <summary>
    /// Touche dédiée : santé, mana et bourse, à la demande.
    ///
    /// Le « mana » de Sun Haven n'est pas une réserve de sorts mais la jauge que consomment les
    /// outils — c'est le terme employé par le jeu et par le wiki, donc celui qu'on garde ici :
    /// inventer « énergie » obligerait à traduire mentalement chaque fois qu'on lit une aide
    /// extérieure.
    ///
    /// Les trois monnaies sont annoncées ensemble parce qu'aucune autre touche ne les donne, et
    /// qu'on les consulte pour la même raison : savoir si l'on peut se permettre un achat. Celles
    /// à zéro sont passées sous silence — en début de partie, orbes et tickets valent zéro
    /// pendant des heures et les réciter à chaque fois n'apprendrait rien.
    /// </summary>
    public static class StatusAnnouncer
    {
        public static void Announce()
        {
            Player player = Player.Instance;
            if (player == null)
            {
                TolkSpeech.Speak("Le jeu n'est pas encore chargé.", true);
                return;
            }

            int health = Mathf.CeilToInt(Mathf.Max(0f, player.Health));
            int maxHealth = Mathf.CeilToInt(player.MaxHealth);
            int mana = Mathf.CeilToInt(Mathf.Max(0f, player.Mana));
            int maxMana = Mathf.CeilToInt(player.MaxMana);

            TolkSpeech.Speak(Localization.Language.T(
                $"Santé : {health} sur {maxHealth}. Mana : {mana} sur {maxMana}. {Purse()}",
                $"Health: {health} of {maxHealth}. Mana: {mana} of {maxMana}. {Purse()}"), true);
        }

        /// <summary>
        /// Les monnaies détenues. La bourse est lue dans `GameSave` (statique) et non sur le
        /// joueur, qui n'expose que des méthodes d'ajout.
        /// </summary>
        private static string Purse()
        {
            try
            {
                var parts = new System.Collections.Generic.List<string>();

                int coins = GameSave.Coins;
                int orbs = GameSave.Orbs;
                int tickets = GameSave.Tickets;

                // Les pièces sont toujours dites, même à zéro : « je n'ai plus rien » est
                // précisément l'information qu'on vient chercher. Orbes et tickets, eux, restent
                // à zéro pendant toute la première partie du jeu.
                parts.Add(Localization.Language.T(
                    $"{coins} pièce{(coins > 1 ? "s" : "")}",
                    $"{coins} coin{(coins > 1 ? "s" : "")}"));
                if (orbs > 0) parts.Add(Localization.Language.T(
                    $"{orbs} orbe{(orbs > 1 ? "s" : "")}",
                    $"{orbs} orb{(orbs > 1 ? "s" : "")}"));
                if (tickets > 0) parts.Add($"{tickets} ticket{(tickets > 1 ? "s" : "")}");

                return string.Join(", ", parts) + ".";
            }
            catch
            {
                // Sauvegarde pas encore chargée : mieux vaut annoncer santé et mana seuls que
                // de faire échouer toute la touche.
                return string.Empty;
            }
        }
    }
}
