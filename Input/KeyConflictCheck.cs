using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Rewired;
using SunHavenAccess.Config;

namespace SunHavenAccess.Input
{
    /// <summary>
    /// Les touches du mod contre celles du jeu — vérifiées auprès du jeu, jamais supposées.
    ///
    /// LE PROBLÈME QUE ÇA REMPLACE. On choisissait une touche « qui a l'air libre », on la
    /// livrait, et l'on découvrait en jouant qu'elle déclenchait aussi un sort ou ouvrait les
    /// quêtes. Chaque collision coûtait une relance, un rapport, une correction — et rien
    /// n'empêchait la suivante, puisque la liste des touches du jeu n'existait nulle part de notre
    /// côté. Une liste écrite à la main aurait de toute façon vieilli à la première mise à jour,
    /// ou dès qu'on change ses propres touches dans les options.
    ///
    /// CE QUE FAIT CE MODULE. Rewired, le système d'entrées du jeu, expose ses associations :
    /// `ActionElementMap` donne la touche ET l'action à laquelle elle est liée. On lit donc la
    /// configuration RÉELLE — celle du joueur, options comprises — et l'on signale toute touche du
    /// mod qui tombe sur une touche du jeu.
    ///
    /// CE QU'IL NE FAIT PAS. Il ne corrige rien tout seul. Déplacer une touche dans le dos de
    /// quelqu'un qui a appris ses raccourcis est pire que le conflit lui-même : on signale, on
    /// explique, et le choix reste à qui joue — le menu des raccourcis permet de rebrancher.
    ///
    /// Une seule vérification par session, au premier passage en jeu : les associations ne
    /// changent qu'en passant par les options, et relire à chaque image serait exactement le
    /// travers qu'on a passé la journée à corriger.
    /// </summary>
    internal static class KeyConflictCheck
    {
        private static bool _done;

        /// <summary>
        /// Touches que le mod se réserve DÉLIBÉRÉMENT en doublon du jeu, avec la raison.
        ///
        /// Toutes les collisions ne sont pas des défauts. Entrée vaut clic gauche hors menu, et
        /// c'est voulu. Échap annule un trajet ET ferme les menus du jeu, et c'est voulu aussi :
        /// le mod ne consomme la touche que s'il a réellement un trajet à annuler.
        /// </summary>
        private static readonly HashSet<KeyCode> Deliberate = new HashSet<KeyCode>
        {
            KeyCode.Return, KeyCode.KeypadEnter, KeyCode.Escape,
            KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow,
        };

        internal static void RunOnce()
        {
            if (_done) return;

            // On attend d'être EN PARTIE. Au menu principal, Rewired peut se déclarer prêt avant
            // que le jeu ait chargé ses associations : on lirait alors une liste incomplète, on la
            // retiendrait comme définitive, et l'on conclurait qu'il n'y a aucun conflit là où il
            // y en a. Mieux vaut répondre tard que répondre faux.
            if (Wish.Player.Instance == null) return;

            Dictionary<KeyCode, List<string>> gameKeys = ReadGameBindings();
            if (gameKeys == null) return; // Rewired pas encore prêt : on retentera plus tard

            _done = true;

            var collisions = new List<string>();

            foreach (var binding in ModConfig.All)
            {
                KeyCode key = binding.Entry?.Value ?? KeyCode.None;
                string label = binding.Label;
                if (key == KeyCode.None || Deliberate.Contains(key)) continue;
                if (!gameKeys.TryGetValue(key, out List<string> actions)) continue;

                collisions.Add($"{key} : « {label} » du mod, et « {string.Join(", ", actions.Distinct().ToArray())} » du jeu");
            }

            if (collisions.Count == 0)
            {
                Plugin.Log?.LogInfo($"Raccourcis : aucun conflit avec les {gameKeys.Count} touches du jeu.");
                return;
            }

            Plugin.Log?.LogWarning(
                $"Raccourcis : {collisions.Count} conflit(s) avec les touches du jeu.\n  " +
                string.Join("\n  ", collisions.ToArray()) +
                "\nCes touches déclencheront les deux actions à la fois. Le menu des raccourcis " +
                "(Suppr) permet d'en changer.");
        }

        /// <summary>
        /// Toutes les touches que le jeu a réellement associées, avec le nom de leur action.
        ///
        /// On passe par TOUS les joueurs Rewired et TOUTES leurs associations clavier, y compris
        /// les catégories inactives : une touche qui n'agit que dans un menu reste une touche
        /// prise. Renvoie null tant que Rewired n'est pas initialisé — au démarrage, il ne l'est
        /// pas encore, et prendre son silence pour « aucune touche » ferait conclure à tort qu'il
        /// n'y a aucun conflit.
        /// </summary>
        private static Dictionary<KeyCode, List<string>> ReadGameBindings()
        {
            try
            {
                if (!ReInput.isReady) return null;

                var result = new Dictionary<KeyCode, List<string>>();
                bool anyPlayer = false;

                foreach (Rewired.Player player in ReInput.players.AllPlayers)
                {
                    if (player == null) continue;
                    anyPlayer = true;

                    foreach (KeyboardMap map in player.controllers.maps.GetAllMaps<KeyboardMap>())
                    {
                        if (map?.AllMaps == null) continue;

                        foreach (ActionElementMap element in map.AllMaps)
                        {
                            if (element == null || element.keyCode == KeyCode.None) continue;

                            string action = ActionName(element);
                            if (!result.TryGetValue(element.keyCode, out List<string> names))
                            {
                                names = new List<string>();
                                result[element.keyCode] = names;
                            }
                            names.Add(action);
                        }
                    }
                }

                return anyPlayer ? result : null;
            }
            catch (Exception e)
            {
                Plugin.Log?.LogWarning("Touches du jeu illisibles : " + e.Message);
                return null;
            }
        }

        private static string ActionName(ActionElementMap element)
        {
            try
            {
                InputAction action = ReInput.mapping.GetAction(element.actionId);
                string name = action?.descriptiveName;
                if (string.IsNullOrWhiteSpace(name)) name = action?.name;
                return string.IsNullOrWhiteSpace(name) ? $"action {element.actionId}" : name;
            }
            catch { return $"action {element.actionId}"; }
        }
    }
}
