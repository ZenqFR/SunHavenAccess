using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Wish;
using SunHavenAccess.Speech;

namespace SunHavenAccess.Navigation
{
    /// <summary>
    /// Un trajet qui traverse plusieurs zones, sans rien demander en route.
    ///
    /// Le cheminement d'origine s'arrête au bord de la zone où l'on se trouve : c'est sa nature, il
    /// calcule un chemin dans une grille, et une grille s'arrête où la carte s'arrête. Demander
    /// « emmène-moi au café » depuis la ferme n'avait donc pas de réponse — il fallait choisir soi-
    /// même une sortie, marcher, rouvrir la carte, redemander.
    ///
    /// Ici, le plan appris ([[WorldLinks]]) donne la suite de zones à traverser, et ce module la
    /// parcourt : marcher jusqu'à la bonne porte, la franchir, recommencer de l'autre côté. Les
    /// portes du jeu s'ouvrent au simple contact — vérifié dans `ScenePortalSpot.OnTriggerEnter2D`
    /// — donc il n'y a rien à « actionner » : y arriver suffit.
    ///
    /// COÛT EN FOND : AUCUN. `Tick` sort à la première ligne tant qu'aucun trajet n'est en cours.
    /// C'est la règle apprise à la dure sur ce mod : ce qui tourne en permanence doit d'abord
    /// prouver qu'il a une raison de tourner.
    ///
    /// CE QUE LE MOD DIT PLUTÔT QUE DE S'ENTÊTER. Le jeu refuse un passage dans deux cas — une
    /// boutique fermée à cette heure, une zone pas encore débloquée. Rester planté devant une porte
    /// sans rien dire serait le pire des comportements pour qui ne voit pas : au bout de quelques
    /// secondes sans progrès, le trajet s'arrête et l'annonce.
    /// </summary>
    internal static class Journey
    {
        /// <summary>Zones restant à traverser, la prochaine en tête. Null quand rien n'est en cours.</summary>
        private static List<string> _remaining;

        /// <summary>Nom lisible de la destination finale, pour les annonces.</summary>
        private static string _label;

        /// <summary>Zone où l'on était au dernier relevé, pour détecter une arrivée.</summary>
        private static string _lastScene;

        /// <summary>
        /// Instant au-delà duquel on considère l'étape en échec. Sans cela, une porte fermée
        /// laisserait le trajet « en cours » indéfiniment, et le mod muet.
        /// </summary>
        private static float _stepDeadline;

        /// <summary>Point exact à rejoindre dans la zone d'arrivée, quand il est connu.</summary>
        private static Vector3? _finalPosition;

        /// <summary>
        /// Marge par étape. Large : une traversée de ville prend du temps, et abandonner trop tôt
        /// serait plus pénible qu'attendre un peu.
        /// </summary>
        private const float StepTimeoutSeconds = 45f;

        internal static bool InProgress => _remaining != null;

        /// <summary>
        /// Lance un trajet vers une zone, en traversant celles qu'il faut. Renvoie false si aucun
        /// chemin connu n'y mène — l'appelant explique alors, il en sait plus sur le contexte.
        /// </summary>
        /// <param name="finalPosition">
        /// Point précis à rejoindre une fois arrivé dans la zone, quand on le connaît.
        ///
        /// Une quête ne dit pas « va en ville », elle dit « rends ceci ICI » — le jeu range la
        /// carte ET les coordonnées dans son propre descriptif. S'arrêter au seuil de la zone
        /// serait s'arrêter juste avant la partie qu'on ne peut pas faire sans voir. Quand le
        /// point est connu, le trajet va donc jusqu'au bout.
        /// </param>
        internal static bool Start(string targetScene, string label, Vector3? finalPosition = null)
        {
            Stop();

            string here = WorldLinks.CurrentScene;
            if (string.IsNullOrWhiteSpace(here) || string.IsNullOrWhiteSpace(targetScene)) return false;

            List<string> route = WorldLinks.Route(here, targetScene);
            if (route == null) return false;

            _label = label;
            _finalPosition = finalPosition;

            if (route.Count == 0)
            {
                // Déjà dans la bonne zone : plus rien à traverser. S'il reste un point précis à
                // rejoindre, on marche directement ; sinon l'appelant prend le relais.
                if (finalPosition.HasValue) PathingController.TravelTo(finalPosition.Value, label);
                return true;
            }

            _remaining = route;
            _lastScene = here;
            _stepDeadline = Time.unscaledTime + StepTimeoutSeconds;

            TolkSpeech.Speak(Localization.Language.T(
                $"Trajet vers {label}, {route.Count} zone{(route.Count > 1 ? "s" : "")} à traverser.",
                $"Travelling to {label}, {route.Count} area{(route.Count > 1 ? "s" : "")} to cross."), true);

            StepToNextDoor();
            return true;
        }

        internal static void Stop()
        {
            _remaining = null;
            _label = null;
            _lastScene = null;
            _finalPosition = null;
        }

        internal static void Tick()
        {
            if (_remaining == null) return; // aucun trajet : coût nul

            string here = WorldLinks.CurrentScene;

            // On a changé de zone : c'est le seul signal d'avancement qui compte.
            if (!string.Equals(here, _lastScene, StringComparison.OrdinalIgnoreCase))
            {
                _lastScene = here;

                // Toute arrivée enrichit le plan : on est dans une zone, ses sorties sont lisibles
                // maintenant et jamais gratuitement plus tard.
                WorldLinks.Learn();

                if (_remaining.Count > 0 && string.Equals(here, _remaining[0], StringComparison.OrdinalIgnoreCase))
                {
                    _remaining.RemoveAt(0);
                }
                else
                {
                    // On a atterri ailleurs que prévu — une porte partagée, un passage inattendu.
                    // Plutôt que d'insister sur un itinéraire devenu faux, on le recalcule d'ici.
                    string destination = _remaining.Count > 0 ? _remaining[_remaining.Count - 1] : null;
                    List<string> route = destination != null ? WorldLinks.Route(here, destination) : null;
                    if (route == null)
                    {
                        Fail(Localization.Language.T(
                            "Trajet interrompu : je ne connais pas de chemin depuis ici.",
                            "Journey stopped: I know no route from here."));
                        return;
                    }
                    _remaining = route;
                }

                if (_remaining.Count == 0)
                {
                    // Dernière ligne droite : quand on sait OÙ exactement, on y va, plutôt que de
                    // laisser quelqu'un chercher à tâtons dans la bonne zone. C'est le cas des
                    // quêtes, qui rangent leurs coordonnées de rendu avec leur carte.
                    Vector3? destination = _finalPosition;
                    string label = _label;
                    Stop();

                    if (destination.HasValue)
                    {
                        PathingController.TravelTo(destination.Value, label);
                        return;
                    }

                    TolkSpeech.Speak(Localization.Language.T(
                        $"Arrivé dans la zone de {label}.",
                        $"Arrived in the area of {label}."), true);
                    return;
                }

                _stepDeadline = Time.unscaledTime + StepTimeoutSeconds;
                StepToNextDoor();
                return;
            }

            // Toujours dans la même zone. Tant que le personnage marche, on le laisse faire.
            if (PathingController.IsPathing)
            {
                return;
            }

            // Il s'est arrêté sans avoir changé de zone : soit il est encore loin de la porte et le
            // chemin s'est interrompu, soit la porte refuse. On relance une fois, puis on renonce.
            if (Time.unscaledTime < _stepDeadline)
            {
                StepToNextDoor();
                return;
            }

            Fail(Localization.Language.T(
                $"Impossible d'atteindre {_label} : le passage est refusé ou bloqué.",
                $"Cannot reach {_label}: the way is refused or blocked."));
        }

        /// <summary>
        /// Marche vers la porte de la zone courante qui mène à l'étape suivante. On la retrouve à
        /// chaque fois plutôt que de la retenir : entre deux zones, les objets ne sont plus les
        /// mêmes, et une référence conservée ne désignerait plus rien.
        /// </summary>
        private static void StepToNextDoor()
        {
            if (_remaining == null || _remaining.Count == 0) return;

            string next = _remaining[0];

            var door = Scanner.PortalsInScene()
                .Where(p => p != null && string.Equals(Scanner.PortalDestination(p), next, StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => Player.Instance != null
                    ? Vector3.Distance(p.transform.position, Player.Instance.transform.position)
                    : 0f)
                .FirstOrDefault();

            if (door == null)
            {
                Fail(Localization.Language.T(
                    $"Trajet interrompu : aucune sortie vers {Util.SceneNames.Translate(next)} ici.",
                    $"Journey stopped: no exit to {Util.SceneNames.Translate(next)} here."));
                return;
            }

            PathingController.TravelTo(door.transform.position, Util.SceneNames.Translate(next));
        }

        private static void Fail(string message)
        {
            Stop();
            TolkSpeech.Speak(message, true);
        }
    }
}
