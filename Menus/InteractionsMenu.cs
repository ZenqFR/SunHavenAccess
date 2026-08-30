using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Wish;
using SunHavenAccess.Speech;
using SunHavenAccess.Util;

namespace SunHavenAccess.Menus
{
    /// <summary>
    /// Tout ce avec quoi on peut interagir autour de soi, et de quoi le faire directement.
    ///
    /// DEUX PROBLÈMES, UNE SEULE CAUSE. Devant le guichet du magasin, le mod annonçait le comptoir
    /// et non la vendeuse derrière. Devant une entrée de bâtiment, la touche d'interaction ne
    /// faisait rien : il fallait reculer d'un pas puis revenir pour qu'elle réponde. Dans les deux
    /// cas, ce n'est pas la description qui échoue — c'est le CIBLAGE du jeu, qui décide seul de ce
    /// qu'on vise selon l'orientation, la distance et l'ordre où les choses sont entrées dans sa
    /// liste. Un joueur voyant corrige d'un micro-déplacement, sans même s'en rendre compte.
    ///
    /// LA RÉPONSE EST DE S'EN PASSER. On cherche nous-mêmes ce qui est interactif alentour, on le
    /// présente en liste, et valider APPELLE l'interaction directement. Plus rien ne dépend d'être
    /// bien placé ni bien orienté : ce qui est là est atteignable.
    ///
    /// C'est ce qui avait été écarté au profit de « nommer les gens avant le mobilier ». Cette
    /// correction-là reste juste, mais elle ne suffisait pas : elle améliore ce qu'on ENTEND, pas
    /// ce qu'on peut FAIRE. Le retour de jeu était sans appel — « toujours pas suffisant ».
    /// </summary>
    internal static class InteractionsMenu
    {
        private const string OwnerTag = "interactions";

        /// <summary>
        /// Rayon de recherche, en unités de monde. Généreux à dessein : un comptoir large, une
        /// porte dont le point d'entrée est décalé, un habitant qui vient de faire un pas — tout
        /// cela met facilement deux ou trois cases entre soi et ce qu'on croit avoir devant soi.
        /// </summary>
        private const float Radius = 3.5f;

        internal static void Open()
        {
            Player player = Player.Instance;
            if (player == null)
            {
                TolkSpeech.Speak(Localization.Language.T(
                    "Le jeu n'est pas encore chargé.", "The game is not loaded yet."), true);
                return;
            }

            List<(IInteractable Target, string Label, float Distance)> found = Nearby(player);

            if (found.Count == 0)
            {
                TolkSpeech.Speak(Localization.Language.T(
                    "Rien avec quoi interagir autour de vous.",
                    "Nothing to interact with around you."), true);
                return;
            }

            var entries = found
                .Select(f => Localization.Language.T(
                    $"{f.Label}, {Mathf.Round(f.Distance)} case{(f.Distance > 1 ? "s" : "")}",
                    $"{f.Label}, {Mathf.Round(f.Distance)} tile{(f.Distance > 1 ? "s" : "")}"))
                .ToList();

            ListMenu.Open(Localization.Language.T("Interagir", "Interact"), entries,
                chosen =>
                {
                    if (chosen < 0 || chosen >= found.Count) return;
                    Interact(found[chosen].Target, found[chosen].Label);
                },
                owner: OwnerTag);
        }

        /// <summary>
        /// Ce qui est interactif autour, du plus proche au plus loin, sans doublon.
        ///
        /// On part de la PHYSIQUE et non de la liste du jeu : c'est justement cette liste qui
        /// oublie une porte tant qu'on n'a pas fait un pas en arrière. Les déclencheurs sont inclus
        /// — un point d'interaction est très souvent un déclencheur, et les écarter reviendrait à
        /// reproduire l'oubli qu'on cherche à corriger.
        /// </summary>
        private static List<(IInteractable, string, float)> Nearby(Player player)
        {
            var found = new List<(IInteractable, string, float)>();
            var seen = new HashSet<Object>();

            try
            {
                Vector3 from = player.transform.position;

                foreach (Collider2D hit in Physics2D.OverlapCircleAll(from, Radius))
                {
                    if (hit == null) continue;
                    if (hit.transform.IsChildOf(player.transform)) continue;

                    // Le composant interactif est rarement porté par le collisionneur lui-même :
                    // il est au-dessus, parfois deux ou trois crans plus haut.
                    var target = hit.GetComponentInParent<Component>() as IInteractable
                                 ?? FindInteractable(hit.transform);
                    if (target is not Component component) continue;
                    if (!seen.Add(component)) continue;

                    string label = Navigation.Scanner.Describe(component, allowGenericName: false);
                    if (string.IsNullOrWhiteSpace(label)) label = Navigation.Scanner.Describe(component);
                    if (string.IsNullOrWhiteSpace(label)) continue;

                    float distance = Vector2.Distance(component.transform.position, from);
                    found.Add((target, TextUtil.Clean(label), distance));
                }
            }
            catch { }

            return found.OrderBy(f => f.Item3).ToList();
        }

        private static IInteractable FindInteractable(Transform from)
        {
            for (Transform t = from; t != null; t = t.parent)
            {
                foreach (Component c in t.GetComponents<Component>())
                {
                    if (c is IInteractable interactable) return interactable;
                }
            }
            return null;
        }

        /// <summary>
        /// Déclenche l'interaction comme le ferait la touche du jeu. `Target()` d'abord : plusieurs
        /// objets s'attendent à être visés avant d'être actionnés, et l'omettre laisse certains
        /// refuser en silence — exactement le défaut qu'on corrige.
        /// </summary>
        private static void Interact(IInteractable target, string label)
        {
            try
            {
                target.Target();
                target.Interact(0);

                TolkSpeech.Speak(Localization.Language.T($"{label}.", $"{label}."), true);
            }
            catch
            {
                TolkSpeech.Speak(Localization.Language.T(
                    $"{label} n'a pas répondu.", $"{label} did not respond."), true);
            }
        }
    }
}
