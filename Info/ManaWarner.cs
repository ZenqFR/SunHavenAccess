using UnityEngine;
using Wish;
using SunHavenAccess.Speech;

namespace SunHavenAccess.Info
{
    /// <summary>
    /// Alerte quand le mana baisse.
    ///
    /// Le « mana » de Sun Haven n'est pas une réserve de sorts : c'est la jauge que consomment les
    /// outils, et à zéro la pioche, la houe et l'arrosoir cessent simplement d'agir. Un joueur
    /// voyant voit la barre se vider et rentre se coucher avant la panne ; sans la vue, l'outil
    /// s'arrête d'un coup sans que rien n'explique pourquoi — le pire des symptômes, puisqu'il
    /// ressemble à un bug du mod plutôt qu'à une mécanique du jeu.
    ///
    /// On garde le mot du jeu plutôt que de parler d'« énergie » : c'est celui qu'emploient
    /// l'interface et le wiki, et en inventer un autre obligerait à traduire mentalement chaque
    /// fois qu'on lit une aide extérieure.
    ///
    /// La santé en combat était déjà annoncée progressivement ; le mana ne l'était qu'à la
    /// demande. C'est pourtant lui qui décide de la longueur d'une journée de travail.
    /// </summary>
    public static class ManaWarner
    {
        /// <summary>
        /// Seuils d'alerte, du plus haut au plus bas. Espacés largement : prévenir tous les dix
        /// pour cent transformerait une séance de minage en compte à rebours permanent. Ceux-ci
        /// correspondent aux moments où la décision change — « il me reste de quoi finir »,
        /// « je ferais mieux de rentrer », « c'est fini ».
        /// </summary>
        private static readonly (float Fraction, string Message)[] Thresholds =
        {
            (0.50f, "Mana à la moitié."),
            (0.25f, "Mana à un quart."),
            (0.10f, "Mana presque épuisé."),
            (0.00f, "Plus de mana : les outils ne fonctionnent plus."),
        };

        /// <summary>
        /// Dernier seuil franchi, en indice dans le tableau ; -1 quand le mana est au-dessus de
        /// tous. Mémorisé pour n'annoncer qu'au FRANCHISSEMENT : la valeur est relue à chaque
        /// image, et répéter l'alerte tant qu'on reste sous le seuil rendrait la synthèse
        /// inutilisable.
        /// </summary>
        private static int _lastCrossed = -1;

        /// <summary>
        /// Marge de remontée avant de réarmer un seuil. Sans elle, un mana qui oscille juste
        /// autour d'un seuil — ce qui arrive en permanence, puisque les outils le consomment par
        /// petits paquets et que certains objets le rendent — réannoncerait sans fin.
        /// </summary>
        private const float Hysteresis = 0.03f;

        public static void Tick()
        {
            Player player = Player.Instance;
            if (player == null) { _lastCrossed = -1; return; }

            float fraction;
            try { fraction = player.ManaPercentage; }
            catch { return; }

            // Écran de chargement, personnage pas encore initialisé : une valeur aberrante ne doit
            // pas déclencher « plus de mana » au moment où la partie s'ouvre.
            if (float.IsNaN(fraction) || fraction < 0f || fraction > 1.5f) return;

            int crossed = CurrentThreshold(fraction);

            if (crossed > _lastCrossed)
            {
                // On peut sauter plusieurs seuils d'un coup (un gros coup d'outil, un sort coûteux) :
                // seul le plus bas atteint est annoncé, les intermédiaires n'apprendraient rien.
                _lastCrossed = crossed;
                TolkSpeech.Speak(Thresholds[crossed].Message, interrupt: false);
                return;
            }

            if (crossed < _lastCrossed)
            {
                // Le mana remonte : on réarme, mais seulement une fois la marge franchie.
                if (_lastCrossed >= 0 && fraction < Thresholds[_lastCrossed].Fraction + Hysteresis) return;
                _lastCrossed = crossed;
            }
        }

        // Pas de remise à zéro explicite au changement de journée : le mana y remonte au
        // maximum, ce qui le fait repasser au-dessus de tous les seuils et réarme le suivi.

        /// <summary>Indice du seuil le plus bas actuellement franchi, ou -1 si aucun.</summary>
        private static int CurrentThreshold(float fraction)
        {
            int result = -1;
            for (int i = 0; i < Thresholds.Length; i++)
            {
                if (fraction <= Thresholds[i].Fraction) result = i;
            }
            return result;
        }
    }
}
