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
        /// <summary>
        /// Vrai tant que l'écran de choix affiche des fiches. Sert à n'ouvrir la liste qu'À
        /// L'ARRIVÉE sur l'écran, et à la refermer au départ.
        /// </summary>
        private static bool _onScreen;

        private static float _nextCheck;

        /// <summary>
        /// Ouvre la liste dès que l'écran de choix apparaît, sans qu'aucune touche soit à connaître.
        ///
        /// C'est le premier écran du jeu après le menu principal : y arriver et devoir se souvenir
        /// d'un raccourci pour pouvoir choisir sa partie, c'est buter dès la première minute. La
        /// touche dédiée reste néanmoins active, pour rouvrir la liste après l'avoir fermée avec
        /// Échap.
        /// </summary>
        public static void Tick()
        {
            // Repérer l'écran demande un balayage complet de la scène (FindObjectsOfType), bien
            // trop coûteux soixante fois par seconde pour une question dont la réponse ne change
            // qu'au changement d'écran. Un quart de seconde reste imperceptible à l'arrivée.
            if (Time.unscaledTime < _nextCheck) return;
            _nextCheck = Time.unscaledTime + 0.25f;

            bool present = Panels().Count > 0;

            if (present == _onScreen) return;
            _onScreen = present;

            if (present) Open();
            else ListMenu.Close(false); // on quitte l'écran : rien à annoncer, le jeu parle déjà
        }

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

            string day = UiNameTranslator.Date(TextUtil.Clean(panel.dayYearText?.text));
            return string.IsNullOrWhiteSpace(day) ? name : $"{name}, {day}";
        }

        /// <summary>
        /// Tout ce que la fiche affiche, en une liste que l'on parcourt à son rythme : les cinq
        /// niveaux de métier, l'or, les orbes, les tickets, l'emplacement et l'heure.
        ///
        /// Ces informations existent bien à l'écran, mais les entendre récitées d'un bloc à chaque
        /// déplacement dans la liste des parties serait insupportable. Les mettre à part, sous une
        /// action explicite, laisse le choix : reconnaître une partie d'un mot, ou l'examiner.
        /// </summary>
        private static void OpenDetails(SavePanel panel, string name)
        {
            var lines = new List<string>();

            void Add(string label, TMPro.TextMeshProUGUI text)
            {
                string value = TextUtil.Clean(text?.text);
                if (!string.IsNullOrWhiteSpace(value)) lines.Add(Localization.Language.Pair(label, value));
            }

            // La date passe par la traduction, les autres champs sont des nombres.
            string date = UiNameTranslator.Date(TextUtil.Clean(panel.dayYearText?.text));
            if (!string.IsNullOrWhiteSpace(date)) lines.Add(Localization.Language.Pair("Date", date));

            Add(Localization.Language.T("Heure", "Time"), panel.timeTMP);
            Add(Localization.Language.T("Emplacement", "Slot"), panel.slotTMP);
            Add(Localization.Language.T("Pièces", "Coins"), panel.coinText);
            Add(Localization.Language.T("Orbes", "Orbs"), panel.orbText);
            Add("Tickets", panel.ticketText);
            Add(Localization.Language.T("Agriculture", "Farming"), panel.farmingLevelText);
            Add(Localization.Language.T("Minage", "Mining"), panel.miningLevelText);
            Add("Combat", panel.combatLevelText);
            Add(Localization.Language.T("Pêche", "Fishing"), panel.fishingLevelText);
            Add("Exploration", panel.explorationLevelText);
            Add(Localization.Language.T("Quête en cours", "Current quest"), panel.questTMP);

            if (lines.Count == 0) lines.Add(Localization.Language.T("Aucun détail disponible pour cette partie.", "No details available for this save."));

            ListMenu.Open($"Détails de {name}", lines);
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

            // « Supprimer » vient en dernier, derrière une action inoffensive : la validation d'une
            // liste est un geste réflexe, et la première entrée est celle qu'on active par erreur.
            var actions = new List<string>
            {
                $"Charger la partie de {name}",
                $"Détails complets de {name}",
                $"Supprimer la partie de {name}",
            };

            ListMenu.Open($"Que faire pour {name}", actions, chosen =>
            {
                if (chosen == 0) Press(panel.selectButton, $"Chargement de {name}");
                else if (chosen == 1) OpenDetails(panel, name);
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
