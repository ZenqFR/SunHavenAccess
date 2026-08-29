using System.Linq;
using UnityEngine;
using Wish;
using SunHavenAccess.Speech;

namespace SunHavenAccess.Info
{
    /// <summary>
    /// Les trois silences qui coûtent cher : le sac plein, la vie basse, la nuit qui tombe.
    ///
    /// CE QUE L'ÉCRAN DIT SANS LE DIRE. Un joueur voyant apprend ces trois choses sans y penser :
    /// la barre de vie rougit, les cases du sac se remplissent, le ciel s'assombrit. Rien de tout
    /// cela ne s'entend. On récolte pendant dix minutes en croyant engranger alors que le sac est
    /// plein depuis le début ; on se fait tuer par un ennemi qu'on ne voyait pas venir ; on
    /// s'évanouit à deux heures du matin en perdant sa journée et une part de son argent.
    ///
    /// LA RÈGLE : PRÉVENIR AU FRANCHISSEMENT, PAS EN CONTINU. Chaque seuil ne parle qu'une fois, et
    /// ne se réarme qu'en repassant au-dessus. Un avertissement répété devient un bruit de fond
    /// qu'on cesse d'écouter — c'est-à-dire exactement l'inverse d'un avertissement. Même principe
    /// que [[ManaWarner]], dont ce module est le prolongement pour tout ce qui n'est pas le mana.
    ///
    /// COÛT : un relevé toutes les demi-secondes, et une sortie immédiate hors partie. Ces trois
    /// valeurs ne changent pas assez vite pour mériter davantage.
    /// </summary>
    internal static class SurvivalWarnings
    {
        private const float Interval = 0.5f;
        private static float _nextCheck;

        /// <summary>Seuils de vie déjà annoncés, du plus haut au plus bas.</summary>
        private static readonly float[] HealthThresholds = { 0.5f, 0.25f, 0.1f };
        private static int _healthStage = -1;

        private static bool _warnedFull;
        private static int _nightStage = -1;

        /// <summary>Heures auxquelles prévenir. Sun Haven fait s'évanouir à deux heures.</summary>
        private static readonly int[] NightHours = { 0, 1 };

        internal static void Tick()
        {
            if (Time.unscaledTime < _nextCheck) return;
            _nextCheck = Time.unscaledTime + Interval;

            Player player = Player.Instance;
            if (player == null)
            {
                // Hors partie : on repart d'une page blanche, sinon le retour en jeu hériterait des
                // seuils de la partie précédente et resterait muet là où il faudrait parler.
                _healthStage = -1;
                _nightStage = -1;
                _warnedFull = false;
                return;
            }

            CheckHealth(player);
            CheckInventory(player);
            CheckNight();
        }

        /// <summary>
        /// La vie, par paliers. On annonce la part restante plutôt qu'un nombre : « un quart de vie »
        /// se comprend sans connaître son maximum, qui change avec l'équipement.
        /// </summary>
        private static void CheckHealth(Player player)
        {
            float part;
            try { part = player.HealthPercentage; }
            catch { return; }

            // Remonté au-dessus du dernier seuil franchi : on réarme, et l'on se tait. Se soigner
            // n'a pas à être commenté, c'est déjà ce qu'on voulait.
            if (_healthStage >= 0 && part > HealthThresholds[_healthStage])
            {
                _healthStage = -1;
            }

            for (int i = HealthThresholds.Length - 1; i >= 0; i--)
            {
                if (part > HealthThresholds[i]) continue;
                if (_healthStage >= i) return; // déjà dit, ou pire déjà dit

                _healthStage = i;
                TolkSpeech.Speak(Localization.Language.T(
                    Warning(i, "Vie à la moitié.", "Vie au quart.", "Vie critique."),
                    Warning(i, "Health at half.", "Health at a quarter.", "Health critical.")), true);
                return;
            }
        }

        private static string Warning(int stage, string half, string quarter, string critical) =>
            stage == 0 ? half : stage == 1 ? quarter : critical;

        /// <summary>
        /// LE SAC PLEIN EST LE PIRE DES SILENCES. On continue de récolter en croyant engranger, et
        /// tout ce qu'on ramasse tombe par terre pour disparaître. C'est du travail perdu sans
        /// aucun signe.
        ///
        /// On compte les emplacements libres du sac plutôt que d'attendre l'échec d'un ramassage :
        /// prévenir AVANT que ça déborde laisse le temps de faire quelque chose. Le dernier
        /// emplacement libre est annoncé aussi — c'est le moment utile, pas celui d'après.
        /// </summary>
        private static void CheckInventory(Player player)
        {
            int free;
            try
            {
                var items = player.Inventory?.Items;
                if (items == null) return;

                // Un emplacement est libre s'il ne porte pas d'objet. Les emplacements verrouillés
                // du sac, non encore achetés, portent une donnée : ils ne comptent donc pas comme
                // libres, ce qui est le comportement voulu.
                free = items.Count(s => s?.item == null);
            }
            catch { return; }

            if (free > 1)
            {
                _warnedFull = false;
                return;
            }

            if (_warnedFull) return;
            _warnedFull = true;

            TolkSpeech.Speak(free == 0
                ? Localization.Language.T(
                    "Sac plein. Ce que vous ramassez sera perdu.",
                    "Backpack full. Anything you pick up will be lost.")
                : Localization.Language.T(
                    "Sac presque plein : un seul emplacement libre.",
                    "Backpack nearly full: one free slot left."), false);
        }

        /// <summary>
        /// La nuit. S'évanouir coûte une journée et une part de son argent ; savoir qu'il est
        /// minuit passé permet encore de rentrer.
        /// </summary>
        private static void CheckNight()
        {
            int hour;
            try { hour = SingletonBehaviour<DayCycle>.Instance?.Time.Hour ?? -1; }
            catch { return; }

            if (hour < 0) return;

            // Le jour a tourné : on réarme pour la nuit suivante.
            if (hour > 2 && hour < 23)
            {
                _nightStage = -1;
                return;
            }

            for (int i = NightHours.Length - 1; i >= 0; i--)
            {
                if (hour != NightHours[i]) continue;
                if (_nightStage >= i) return;

                _nightStage = i;
                TolkSpeech.Speak(i == 0
                    ? Localization.Language.T(
                        "Minuit passé. Vous vous évanouirez à deux heures.",
                        "Past midnight. You will pass out at two.")
                    : Localization.Language.T(
                        "Une heure du matin. Il reste une heure avant l'évanouissement.",
                        "One in the morning. One hour left before you pass out."), false);
                return;
            }
        }
    }
}
