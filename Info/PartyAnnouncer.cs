using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Wish;
using SunHavenAccess.Speech;
using SunHavenAccess.Util;

namespace SunHavenAccess.Info
{
    /// <summary>
    /// Annonce l'arrivée et le départ des autres joueurs.
    ///
    /// En coopération, un joueur voyant sait d'un coup d'œil s'il est seul. Sans cette annonce, on
    /// ne l'apprend qu'en le heurtant au scanner — ou pas du tout, s'il repart entre-temps. C'est
    /// une information de présence, la plus élémentaire qui soit, et elle n'existait pas.
    ///
    /// La détection se fait par comparaison des joueurs présents d'un instant à l'autre, sans
    /// s'accrocher à un événement réseau du jeu : aucun n'est exposé publiquement, et un point
    /// d'accroche deviné se briserait à la première mise à jour. Comparer ce qui est réellement là
    /// fonctionne quel que soit le chemin qu'a pris le jeu pour l'y mettre.
    /// </summary>
    public static class PartyAnnouncer
    {
        private const float Interval = 1.5f;

        /// <summary>
        /// Nombre de relevés d'absence avant d'annoncer un départ, soit un peu plus de quatre
        /// secondes.
        ///
        /// Sun Haven charge chaque carte comme une scène additionnelle et reconstruit les joueurs
        /// au passage : un partenaire disparaît donc réellement, quelques secondes, chaque fois
        /// qu'il franchit une porte. Annoncer son départ aussitôt donnerait « untel a quitté la
        /// partie » suivi de « untel a rejoint la partie » à chaque bâtiment qu'il traverse.
        /// </summary>
        private const int MissesBeforeLeaving = 3;

        /// <summary>
        /// Joueurs connus, par NOM et non par identifiant d'objet : le changement de carte détruit
        /// puis recrée le joueur, donc son identifiant Unity change alors que c'est la même
        /// personne. Deux partenaires portant le même nom se confondraient, mais la conséquence se
        /// limite à une annonce manquée.
        /// </summary>
        private static readonly HashSet<string> _known = new HashSet<string>();

        /// <summary>Relevés consécutifs où un joueur connu n'a pas été retrouvé.</summary>
        private static readonly Dictionary<string, int> _missing = new Dictionary<string, int>();

        /// <summary>
        /// Vrai une fois le premier relevé fait. À l'arrivée dans une partie déjà peuplée, les
        /// joueurs présents ne « rejoignent » pas : ils sont là. Les annoncer comme des arrivées
        /// serait faux, et bruyant au pire moment — celui où le jeu parle déjà beaucoup.
        /// </summary>
        private static bool _seeded;

        private static float _nextCheck;

        public static void Tick()
        {
            if (Time.unscaledTime < _nextCheck) return;
            _nextCheck = Time.unscaledTime + Interval;

            // Hors partie — menus, écran de chargement — il n'y a personne à suivre, et le prochain
            // retour en jeu doit repartir d'une page blanche plutôt que d'annoncer le départ de
            // toute une partie qu'on vient simplement de quitter.
            if (Player.Instance == null)
            {
                _seeded = false;
                _known.Clear();
                _missing.Clear();
                return;
            }

            HashSet<string> present = Present();

            if (!_seeded)
            {
                _seeded = true;
                foreach (string name in present) _known.Add(name);
                return;
            }

            foreach (string name in present)
            {
                _missing.Remove(name);
                if (!_known.Add(name)) continue; // déjà connu

                TolkSpeech.Speak($"{name} a rejoint la partie.", false);
            }

            foreach (string name in _known.ToList())
            {
                if (present.Contains(name)) continue;

                _missing.TryGetValue(name, out int misses);
                misses++;
                _missing[name] = misses;
                if (misses < MissesBeforeLeaving) continue;

                TolkSpeech.Speak($"{name} a quitté la partie.", false);
                _known.Remove(name);
                _missing.Remove(name);
            }
        }

        private static HashSet<string> Present()
        {
            var result = new HashSet<string>();
            try
            {
                foreach (Player p in Object.FindObjectsOfType<Player>())
                {
                    if (p == null || p == Player.Instance) continue;
                    string name = TextUtil.Clean(p.name);
                    result.Add(string.IsNullOrWhiteSpace(name) ? "Un autre joueur" : name);
                }
            }
            catch { /* scène en cours de chargement : on réessaiera au prochain relevé */ }
            return result;
        }
    }
}
