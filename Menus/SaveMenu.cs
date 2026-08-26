using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Wish;
using SunHavenAccess.Speech;
using SunHavenAccess.Util;

namespace SunHavenAccess.Menus
{
    /// <summary>
    /// Le choix d'une sauvegarde en liste : un personnage par ligne, puis Charger ou Supprimer.
    ///
    /// L'écran affiche chaque sauvegarde comme une fiche — portrait, cinq jauges de métier, or,
    /// saison — avec deux boutons quelque part dedans. Les parcourir dans l'ordre de la mise en
    /// page mélange les fiches et les boutons, et rien ne dit à quelle sauvegarde appartient le
    /// bouton où l'on se trouve. C'est précisément le genre d'écran où il ne faut pas suivre
    /// l'affichage : une sauvegarde est une ligne, et deux actions en dépendent.
    ///
    /// D'où deux temps. La liste identifie les parties — nom et date, ce qui suffit à les
    /// distinguer. Valider ouvre une seconde liste, courte et sans ambiguïté, où l'action porte le
    /// nom de la partie choisie : on ne peut pas supprimer la mauvaise en se trompant de ligne.
    ///
    /// Les deux boutons sont ceux du jeu (`SavePanel.selectButton` et `deleteButton`, publics) :
    /// c'est donc son propre code qui charge ou supprime, avec la confirmation qu'il impose.
    /// </summary>
    public static class SaveMenu
    {
        public static void Open()
        {
            List<SavePanel> panels = Panels();
            if (panels.Count == 0)
            {
                TolkSpeech.Speak("Aucune sauvegarde à afficher ici.", true);
                return;
            }

            var labels = panels.Select(Describe).ToList();
            ListMenu.Open("Sauvegardes", labels, chosen => OpenActions(panels, chosen));
        }

        /// <summary>
        /// Une sauvegarde en une ligne : de quoi la reconnaître, rien de plus.
        ///
        /// Les niveaux de métier et l'or restent hors de la liste — on les consulte pour se
        /// souvenir d'une partie, pas pour la choisir, et les réciter à chaque déplacement
        /// rendrait le parcours interminable. La touche de description complète les redit.
        /// </summary>
        private static string Describe(SavePanel panel)
        {
            string name = TextUtil.Clean(panel.playerNameText?.text);
            if (string.IsNullOrWhiteSpace(name)) return "Emplacement vide";

            string day = TextUtil.Clean(panel.dayYearText?.text);
            return string.IsNullOrWhiteSpace(day) ? name : $"{name}, {day}";
        }

        /// <summary>
        /// Les deux actions possibles sur la sauvegarde choisie, chacune nommée avec elle : « ...
        /// pour Camille » plutôt que « Supprimer », qui laisserait un doute sur la cible.
        /// </summary>
        private static void OpenActions(List<SavePanel> panels, int index)
        {
            if (index < 0 || index >= panels.Count) return;

            SavePanel panel = panels[index];
            if (panel == null) return;

            string name = TextUtil.Clean(panel.playerNameText?.text);
            bool empty = string.IsNullOrWhiteSpace(name);

            if (empty)
            {
                // Un emplacement libre n'a qu'un usage : y commencer une partie. Proposer
                // « Supprimer » n'aurait aucun sens.
                Press(panel.selectButton, "Nouvelle partie");
                return;
            }

            var actions = new List<string>
            {
                $"Charger la partie de {name}",
                $"Supprimer la partie de {name}",
            };

            ListMenu.Open($"Que faire pour {name}", actions, chosen =>
            {
                if (chosen == 0) Press(panel.selectButton, $"Chargement de {name}");
                else Press(panel.deleteButton, $"Suppression de {name}");
            });
        }

        /// <summary>
        /// Déclenche le bouton du jeu. On ne supprime ni ne charge rien nous-mêmes : le jeu garde
        /// la main, y compris sur la confirmation qu'il demande avant d'effacer une partie.
        /// </summary>
        private static void Press(UnityEngine.UI.Button button, string what)
        {
            if (button == null || !button.gameObject.activeInHierarchy)
            {
                TolkSpeech.Speak("Cette action n'est pas disponible.", true);
                return;
            }

            TolkSpeech.Speak(what + ".", true);
            button.onClick.Invoke();
        }

        /// <summary>
        /// Les fiches de sauvegarde affichées, de haut en bas — l'ordre à l'écran, donc celui des
        /// emplacements. Les gabarits désactivés que le jeu garde dans la hiérarchie sont écartés.
        /// </summary>
        private static List<SavePanel> Panels()
        {
            try
            {
                return Object.FindObjectsOfType<SavePanel>()
                    .Where(p => p != null && p.gameObject.activeInHierarchy)
                    .OrderByDescending(p => p.transform.position.y)
                    .ThenBy(p => p.transform.position.x)
                    .ToList();
            }
            catch { return new List<SavePanel>(); }
        }
    }
}
