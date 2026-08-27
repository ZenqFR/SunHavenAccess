using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Wish;

namespace SunHavenAccess.Menus
{
    /// <summary>
    /// Pose la sélection sur le PREMIER bouton du menu principal dès qu'il apparaît.
    ///
    /// Le jeu n'y sélectionne rien de lui-même : il attend la souris. Le mod ne prenait donc la
    /// main qu'à la première flèche, et cette première pression déplaçait déjà la sélection —
    /// depuis nulle part, donc vers un endroit qui n'avait aucune raison d'être « Jouer ». On
    /// arrivait en bas de la liste sans comprendre pourquoi.
    ///
    /// Poser la sélection d'emblée règle les deux problèmes à la fois : on sait où l'on est sans
    /// avoir rien pressé, et la première flèche fait un déplacement d'UN cran, comme partout
    /// ailleurs.
    ///
    /// Le choix du bouton est volontairement géométrique — le plus haut, puis le plus à gauche —
    /// et non un nom recherché dans le texte : « Jouer » devient « Continuer » quand une partie
    /// existe, et se traduit dans chaque langue. La position, elle, ne change pas.
    /// </summary>
    public static class MainMenuFocus
    {
        private static bool _placed;

        public static void Tick()
        {
            MainMenuController menu = MainMenuController.Instance;

            if (menu == null || !menu.isActiveAndEnabled)
            {
                _placed = false;
                return;
            }

            // Une seule fois par apparition de l'écran : replacer la sélection à chaque image
            // empêcherait toute navigation.
            if (_placed) return;

            // Quelque chose est déjà sélectionné — par le jeu, ou parce qu'on vient de naviguer :
            // on n'y touche pas.
            if (EventSystem.current == null) return;
            if (EventSystem.current.currentSelectedGameObject != null) { _placed = true; return; }

            var candidates = MenuNavigator.VisibleSelectables()
                .OrderByDescending(s => s.transform.position.y)
                .ThenBy(s => s.transform.position.x)
                .ToList();

            Selectable first = candidates.FirstOrDefault();
            if (first == null) return; // écran pas encore construit : on réessaiera à la frame suivante

            _placed = true;
            LogCandidates(candidates);
            EventSystem.current.SetSelectedGameObject(first.gameObject);
            // Pas d'annonce ici : FocusReader annonce l'élément sélectionné de lui-même, et le
            // doubler ne ferait que se couper la parole.
        }

        /// <summary>
        /// Journalise les premiers candidats à la sélection, une seule fois par session.
        ///
        /// Deux tentatives ont échoué à écarter le bouton du studio de la tête de liste, faute de
        /// savoir ce qu'il est réellement : je raisonnais sur des suppositions. Ces quelques lignes
        /// donnent son nom, son chemin dans la hiérarchie et ses composants — de quoi trancher en
        /// une lecture au lieu d'un aller-retour de plus. Rien n'est prononcé : c'est pour le
        /// journal, pas pour le joueur.
        /// </summary>
        private static readonly System.Collections.Generic.HashSet<string> _logged =
            new System.Collections.Generic.HashSet<string>();

        private static void LogCandidates(System.Collections.Generic.List<Selectable> candidates)
        {
            try
            {
                // Une trace par ÉCRAN, pas une par session : la première fois, tout est parti sur
                // l'écran de chargement et l'écran d'accueil — celui qui posait problème — n'a
                // jamais été journalisé.
                string screen = candidates.Count > 0 ? RootName(candidates[0].transform) : "?";
                if (!_logged.Add(screen)) return;

                var lines = candidates.Take(6).Select(s =>
                {
                    string path = s.name;
                    for (Transform t = s.transform.parent; t != null; t = t.parent) path = t.name + "/" + path;

                    string components = string.Join(", ", s.GetComponents<Component>()
                        .Where(c => c != null)
                        .Select(c => c.GetType().Name));

                    // Le mode de navigation dit si le jeu a câblé lui-même l'ordre de parcours :
                    // c'est ce qui décide si l'on peut suivre son enchaînement ou non.
                    return $"  {path}  [{components}]  nav={s.navigation.mode}";
                });

                Plugin.Log?.LogInfo($"Menu principal ({screen}), premiers candidats à la sélection :\n"
                                    + string.Join("\n", lines));
            }
            catch { }
        }

        /// <summary>Le nom du panneau d'écran auquel appartient cet élément, pour distinguer les traces.</summary>
        private static string RootName(Transform t)
        {
            string name = t.name;
            for (Transform p = t.parent; p != null; p = p.parent)
                if (p.name.StartsWith("[")) return p.name; // convention du jeu : [HomeMenu], [LoadCharacterMenu]…
                else name = p.name;
            return name;
        }
    }
}
