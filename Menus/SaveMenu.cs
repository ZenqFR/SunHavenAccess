using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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
    /// LE CONTENU VIENT DES DONNÉES, PAS DE L'AFFICHAGE. `GameSave.Saves` est la liste des parties
    /// elle-même, publique et complète. Trois défauts d'affilée ont été signalés tant que j'ai
    /// voulu déduire cette liste des fiches à l'écran : elle ne s'ouvrait pas, puis la touche
    /// dédiée ne répondait plus sur l'une des deux routes, puis seules les deux premières parties
    /// apparaissaient — le reste avait défilé hors du cadre, ou n'était même pas construit. Aucune
    /// de ces pannes n'aurait existé en lisant les données dès le départ.
    ///
    /// L'affichage garde exactement deux rôles, qu'il est seul à pouvoir tenir : dire QUAND on est
    /// devant cet écran, et fournir le bouton de suppression, pour que ce soit le jeu qui efface,
    /// avec sa propre confirmation.
    /// </summary>
    public static class SaveMenu
    {
        /// <summary>
        /// Vrai tant que l'écran de choix est affiché. Sert à n'ouvrir la liste qu'À L'ARRIVÉE sur
        /// l'écran, et à la refermer au départ.
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
            // Un quart de seconde suffit : la réponse ne change qu'au changement d'écran, et le
            // délai reste imperceptible à l'arrivée.
            if (Time.unscaledTime < _nextCheck) return;
            _nextCheck = Time.unscaledTime + 0.25f;

            bool present = OnLoadScreen();
            if (present == _onScreen) return;
            _onScreen = present;

            if (present) Open();
            else ListMenu.Close(false); // on quitte l'écran : rien à annoncer, le jeu parle déjà
        }

        /// <summary>
        /// Sommes-nous devant l'écran de choix ?
        ///
        /// C'est la seule question à laquelle les données ne répondent pas : `GameSave.Saves` est
        /// rempli en permanence, dès le lancement. Il faut donc bien un signal d'affichage, et une
        /// fiche RÉELLEMENT à l'écran en est un fiable — le menu principal en garde d'autres en
        /// mémoire, mais rangées hors champ.
        /// </summary>
        private static bool OnLoadScreen()
        {
            try
            {
                return UnityEngine.Object.FindObjectsOfType<SavePanel>()
                    .Any(p => p != null && p.gameObject.activeInHierarchy && MenuNavigator.IsOnScreen(p));
            }
            catch { return false; }
        }

        public static void Open()
        {
            List<GameSaveData> saves = Saves();
            if (saves.Count == 0)
            {
                TolkSpeech.Speak("Aucune sauvegarde à afficher ici.", true);
                return;
            }

            var labels = saves.Select(Describe).ToList();
            ListMenu.Open("Sauvegardes", labels, chosen => OpenActions(chosen));
        }

        /// <summary>
        /// Toutes les parties enregistrées, dans l'ordre où le jeu les indexe — celui-là même
        /// qu'attendent `PlayGame(int)` et `DeleteCharacter(int)`. Ne dépend ni du défilement, ni
        /// de la route suivie pour arriver sur l'écran, ni de ce que l'interface a construit.
        /// </summary>
        private static List<GameSaveData> Saves()
        {
            try
            {
                List<GameSaveData> saves = SingletonBehaviour<GameSave>.Instance?.Saves;
                return saves == null
                    ? new List<GameSaveData>()
                    : saves.Where(s => s?.characterData != null).ToList();
            }
            catch { return new List<GameSaveData>(); }
        }

        /// <summary>
        /// Une sauvegarde en une ligne : de quoi la reconnaître, rien de plus.
        ///
        /// Les niveaux de métier et l'or restent hors de la liste — on les consulte pour se
        /// souvenir d'une partie, pas pour la choisir, et les réciter à chaque déplacement
        /// rendrait le parcours interminable. L'action « Détails complets » les redit.
        /// </summary>
        private static string Describe(GameSaveData save)
        {
            string name = TextUtil.Clean(save.characterData?.characterName);
            if (string.IsNullOrWhiteSpace(name))
                return Localization.Language.T("Emplacement vide", "Empty slot");

            int day = save.worldData?.day ?? 0;
            return day > 0
                ? Localization.Language.T($"{name}, jour {day}", $"{name}, day {day}")
                : name;
        }

        /// <summary>
        /// Tout ce que la fiche affiche, en une liste que l'on parcourt à son rythme.
        ///
        /// Ces informations existent bien à l'écran, mais les entendre récitées d'un bloc à chaque
        /// déplacement dans la liste des parties serait insupportable. Les mettre à part, sous une
        /// action explicite, laisse le choix : reconnaître une partie d'un mot, ou l'examiner.
        /// </summary>
        private static void OpenDetails(GameSaveData save, string name)
        {
            var lines = new List<string>();

            void Add(string label, string value)
            {
                if (!string.IsNullOrWhiteSpace(value)) lines.Add(Localization.Language.Pair(label, value));
            }

            int day = save.worldData?.day ?? 0;
            if (day > 0) Add(Localization.Language.T("Jour", "Day"), day.ToString());

            try
            {
                DateTime time = save.worldData.time;
                Add(Localization.Language.T("Heure", "Time"), $"{time.Hour}:{time.Minute:00}");
            }
            catch { }

            // Les niveaux de métier viennent du dictionnaire du jeu : un métier absent est un
            // métier jamais pratiqué, et il n'y a rien à en dire.
            try
            {
                foreach (var pair in save.characterData.Professions)
                {
                    if (pair.Value == null) continue;
                    Add(ProfessionName(pair.Key), pair.Value.level.ToString());
                }
            }
            catch { }

            if (lines.Count == 0)
                lines.Add(Localization.Language.T("Aucun détail disponible pour cette partie.",
                                                  "No details available for this save."));

            ListMenu.Open(Localization.Language.T($"Détails de {name}", $"Details for {name}"), lines);
        }

        private static string ProfessionName(ProfessionType profession)
        {
            switch (profession)
            {
                case ProfessionType.Farming:     return Localization.Language.T("Agriculture", "Farming");
                case ProfessionType.Mining:      return Localization.Language.T("Minage", "Mining");
                case ProfessionType.Combat:      return "Combat";
                case ProfessionType.Fishing:     return Localization.Language.T("Pêche", "Fishing");
                case ProfessionType.Exploration: return "Exploration";
                default:                         return profession.ToString();
            }
        }

        /// <summary>
        /// Les actions possibles sur la sauvegarde choisie, chacune nommée avec elle : « ... pour
        /// Camille » plutôt que « Supprimer », qui laisserait un doute sur la cible.
        /// </summary>
        private static void OpenActions(int index)
        {
            List<GameSaveData> saves = Saves();
            if (index < 0 || index >= saves.Count) return;

            GameSaveData save = saves[index];
            string name = TextUtil.Clean(save.characterData?.characterName);
            if (string.IsNullOrWhiteSpace(name)) name = Localization.Language.T("cette partie", "this game");

            // « Supprimer » vient en dernier, derrière une action inoffensive : la validation d'une
            // liste est un geste réflexe, et la première entrée est celle qu'on active par erreur.
            var actions = new List<string>
            {
                Localization.Language.T($"Charger la partie de {name}", $"Load {name}'s game"),
                Localization.Language.T($"Détails complets de {name}", $"Full details for {name}"),
                Localization.Language.T($"Supprimer la partie de {name}", $"Delete {name}'s game"),
            };

            ListMenu.Open(Localization.Language.T($"Que faire pour {name}", $"What to do for {name}"),
                actions, chosen =>
                {
                    if (chosen == 0) Load(index, name);
                    else if (chosen == 1) OpenDetails(save, name);
                    else Delete(name);
                });
        }

        /// <summary>
        /// Charge la partie par son index — exactement ce que fait le bouton du jeu, via la même
        /// méthode publique. Aucune règle n'est redupliquée : c'est le jeu qui charge.
        /// </summary>
        private static void Load(int index, string name)
        {
            try
            {
                MainMenuController menu = MainMenuController.Instance;
                if (menu == null)
                {
                    TolkSpeech.Speak("Cette action n'est pas disponible.", true);
                    return;
                }

                TolkSpeech.Speak(Localization.Language.T($"Chargement de {name}.", $"Loading {name}."), true);
                menu.PlayGame(index);
            }
            catch
            {
                TolkSpeech.Speak("Cette action n'est pas disponible.", true);
            }
        }

        /// <summary>
        /// Supprime en actionnant le bouton du JEU, jamais en appelant `DeleteCharacter` nous-mêmes.
        ///
        /// Cette méthode-là efface le fichier sur-le-champ, sans rien demander. Le bouton, lui,
        /// passe par la confirmation que le jeu impose. Pour un geste irréversible, la confirmation
        /// n'est pas un détail : si le bouton reste introuvable — fiche défilée hors du cadre,
        /// écran construit autrement — on refuse et on le dit, plutôt que d'effacer une partie
        /// sans avoir rien demandé.
        /// </summary>
        private static void Delete(string name)
        {
            UnityEngine.UI.Button button = DeleteButtonFor(name);
            if (button == null || !button.gameObject.activeInHierarchy)
            {
                TolkSpeech.Speak(Localization.Language.T(
                    "Suppression indisponible ici : faites défiler jusqu'à cette partie sur l'écran du jeu, la confirmation du jeu étant nécessaire pour effacer.",
                    "Delete unavailable here: scroll to this save on the game's own screen, since the game's confirmation is required to erase."), true);
                return;
            }

            TolkSpeech.Speak(Localization.Language.T($"Suppression de {name}.", $"Deleting {name}."), true);
            button.onClick.Invoke();
        }

        /// <summary>Le bouton de suppression de la fiche portant ce nom, s'il est présent à l'écran.</summary>
        private static UnityEngine.UI.Button DeleteButtonFor(string name)
        {
            try
            {
                return UnityEngine.Object.FindObjectsOfType<SavePanel>()
                    .Where(p => p != null && p.gameObject.activeInHierarchy)
                    .Where(p => string.Equals(TextUtil.Clean(p.playerNameText?.text), name,
                                              StringComparison.CurrentCultureIgnoreCase))
                    .Select(p => p.deleteButton)
                    .FirstOrDefault(b => b != null);
            }
            catch { return null; }
        }
    }
}
