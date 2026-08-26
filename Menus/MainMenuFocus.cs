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

            Selectable first = MenuNavigator.VisibleSelectables()
                .OrderByDescending(s => s.transform.position.y)
                .ThenBy(s => s.transform.position.x)
                .FirstOrDefault();

            if (first == null) return; // écran pas encore construit : on réessaiera à la frame suivante

            _placed = true;
            EventSystem.current.SetSelectedGameObject(first.gameObject);
            // Pas d'annonce ici : FocusReader annonce l'élément sélectionné de lui-même, et le
            // doubler ne ferait que se couper la parole.
        }
    }
}
