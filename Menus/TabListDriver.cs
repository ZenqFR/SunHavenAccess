using Wish;
using SunHavenAccess.Info;
using SunHavenAccess.Navigation;

namespace SunHavenAccess.Menus
{
    /// <summary>
    /// Chaque onglet du menu principal présente son contenu sous forme de liste, tout seul.
    ///
    /// Les listes vocales existaient déjà, mais derrière des touches séparées — G, V, Z, X,
    /// Pavé 8. Ouvrir l'onglet Relations avec Tab donnait donc toujours le panneau brut : la liste
    /// n'existait que pour qui savait qu'une touche la cachait. Une fonctionnalité qu'il faut
    /// deviner n'existe pas vraiment.
    ///
    /// Désormais le geste tient en trois choses, et rien d'autre n'est à retenir :
    /// Tab ouvre le menu, gauche et droite changent d'onglet, et l'onglet choisi ouvre sa liste.
    ///
    /// Le sac à dos fait exception : c'est une vraie grille, sa navigation en grille lui convient,
    /// et elle a été confirmée fluide en jeu. La remplacer par une liste serait un recul.
    ///
    /// Les touches d'origine restent : elles servent à consulter ces mêmes listes SANS ouvrir le
    /// menu, ce que l'aiguillage ne remplace pas.
    /// </summary>
    public static class TabListDriver
    {
        private const int BackpackTab = 0;

        /// <summary>
        /// Dernier onglet pour lequel une liste a été ouverte ; -1 quand le menu est fermé.
        /// Sert à n'agir qu'au CHANGEMENT : le jeu expose l'onglet courant à chaque image, et
        /// rouvrir la liste en boucle la rendrait impossible à parcourir.
        /// </summary>
        private static int _lastTab = -1;

        public static void Tick()
        {
            if (!MenuOpen())
            {
                // Le menu vient de se fermer : on referme la liste avec lui, sans quoi elle
                // resterait ouverte par-dessus le jeu et continuerait de capter les flèches.
                if (_lastTab >= 0)
                {
                    _lastTab = -1;
                    ListMenu.Close();
                }
                return;
            }

            int tab = CurrentTab();
            if (tab < 0 || tab == _lastTab) return;

            _lastTab = tab;

            // Le sac à dos garde sa grille.
            if (tab == BackpackTab)
            {
                ListMenu.Close();
                return;
            }

            OpenListFor(tab);
        }

        private static bool MenuOpen()
        {
            try { return UIHandler.InventoryOpen; }
            catch { return false; }
        }

        private static int CurrentTab()
        {
            try
            {
                PlayerInventory inventory = Player.Instance?.PlayerInventory;
                return inventory != null ? inventory.majorTabIndex : -1;
            }
            catch { return -1; }
        }

        /// <summary>
        /// Ouvre la liste correspondant à l'onglet. Les index suivent la table de libellés déjà
        /// confirmée en jeu (voir UiNameTranslator.MajorTabLabelsByIndex) : cette table fait
        /// autorité et ne doit pas être redéduite du code décompilé, ce qui a déjà conduit deux
        /// fois à une erreur.
        /// </summary>
        private static void OpenListFor(int tab)
        {
            switch (tab)
            {
                case 1: SkillTreeMenu.Open(); break;              // Arbre de compétences
                case 2: RelationshipAnnouncer.AnnounceAll(); break; // Relations
                case 3: QuestAnnouncer.AnnounceActiveQuests(); break; // Quêtes
                case 4: MapNavigator.OpenList(); break;           // Carte
                case 5: StatisticsMenu.Open(); break;             // Statistiques
                case 6: SettingsMenu.Open(); break;               // Paramètres
                default: ListMenu.Close(); break;
            }
        }
    }
}
