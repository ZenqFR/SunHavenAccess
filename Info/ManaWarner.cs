using UnityEngine;
using Wish;
using SunHavenAccess.Speech;

namespace SunHavenAccess.Info
{
    /// <summary>
    /// Alerte quand l'énergie baisse.
    ///
    /// Dans Sun Haven, ce que l'interface appelle « mana » est la jauge que consomment les outils :
    /// à zéro, la pioche, la houe et l'arrosoir cessent simplement d'agir. Un joueur voyant voit la
    /// barre se vider et rentre se coucher avant la panne ; sans la vue, l'outil s'arrête d'un coup
    /// sans que rien n'explique pourquoi — le pire des symptômes, puisqu'il ressemble à un bug du
    /// mod plutôt qu'à une mécanique du jeu.
    ///
    /// La santé en combat était déjà annoncée progressivement ; l'énergie ne l'était qu'à la
    /// demande. C'est pourtant elle qui décide de la longueur d'une journée de travail.
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
            (0.50f, "Énergie à la moitié."),
            (0.25f, "Énergie à un quart."),
            (0.10f, "Énergie presque épuisée."),
            (0.00f, "Plus d'énergie : les outils ne fonctionnent plus."),
        };

        /// <summary>
        /// Dernier seuil franchi, en indice dans le tableau ; -1 quand l'énergie est au-dessus de
        /// tous. Mémorisé pour n'annoncer qu'au FRANCHISSEMENT : la valeur est relue à chaque
        /// image, et répéter l'alerte tant qu'on reste sous le seuil rendrait la synthèse
        /// inutilisable.
        /// </summary>
        private static int _lastCrossed = -1;

        /// <summary>
        /// Marge de remontée avant de réarmer un seuil. Sans elle, une énergie qui oscille juste
        /// autour d'un seuil — ce qui arrive en permanence, puisque les outils la consomment par
        /// petits paquets et que certains objets la rendent — réannoncerait sans fin.
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
            // pas déclencher « plus d'énergie » au moment où la partie s'ouvre.
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
                // L'énergie remonte : on réarme, mais seulement une fois la marge franchie.
                if (_lastCrossed >= 0 && fraction < Thresholds[_lastCrossed].Fraction + Hysteresis) return;
                _lastCrossed = crossed;
            }
        }

        // Pas de remise à zéro explicite au changement de journée : l'énergie y remonte au
        // maximum, ce qui la fait repasser au-dessus de tous les seuils et réarme le suivi.

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
