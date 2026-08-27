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
    /// La liste n'est pas une impasse. Signalé en jeu : arriver sur l'arbre de compétences y
    /// enfermait, puisqu'une liste ouverte capte tout le clavier — la barre d'onglets devenait
    /// inatteignable. **Ctrl+haut en ressort vers les onglets, Ctrl+bas y entre à nouveau**, le
    /// geste que le mod emploie déjà partout ailleurs pour changer de zone. Et tant qu'on est
    /// ressorti, changer d'onglet ne rouvre plus rien : on parcourt les onglets tranquillement,
    /// puis on entre dans celui qu'on veut.
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

        /// <summary>
        /// L'utilisateur a demandé à rester sur la barre d'onglets (Ctrl+haut depuis une liste).
        ///
        /// Tant que ce drapeau tient, changer d'onglet n'ouvre plus la liste automatiquement. Sans
        /// lui, quitter une liste par le haut ne servait à rien : la flèche suivante changeait
        /// d'onglet, la liste se rouvrait aussitôt et reprenait le clavier — impossible de
        /// parcourir les onglets pour voir ce qu'ils contiennent. Ctrl+bas lève le drapeau.
        /// </summary>
        private static bool _stayOnTabs;

        public static void Tick()
        {
            if (!MenuOpen())
            {
                // Le menu vient de se fermer : on referme la liste avec lui, sans quoi elle
                // resterait ouverte par-dessus le jeu et continuerait de capter les flèches.
                if (_lastTab >= 0)
                {
                    _lastTab = -1;
                    _stayOnTabs = false; // la prochaine ouverture du menu repart sur l'automatisme
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

            if (_stayOnTabs) return; // on parcourt les onglets : Ctrl+bas pour entrer dans celui-ci

            OpenListFor(tab);
        }

        /// <summary>Appelée quand on quitte une liste d'onglet par le haut.</summary>
        private static void ExitToTabs()
        {
            _stayOnTabs = true;
            ZoneNavigator.FocusTabs();
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
        /// Rouvre la liste de l'onglet courant, à la demande — c'est ce que fait Ctrl+bas depuis la
        /// barre d'onglets. Renvoie false si cet onglet n'a pas de liste (le sac à dos), auquel cas
        /// l'appelant reprend son comportement habituel et descend dans le panneau.
        ///
        /// Sans ce chemin, quitter une liste par Ctrl+haut était sans retour : Ctrl+bas ramenait
        /// dans le panneau brut, celui-là même que la liste remplace.
        /// </summary>
        public static bool OpenForCurrentTab()
        {
            if (!MenuOpen() || ListMenu.IsOpen) return false;

            int tab = CurrentTab();
            if (tab <= BackpackTab) return false;

            _stayOnTabs = false; // demande explicite d'entrer : l'automatisme reprend
            _lastTab = tab;
            OpenListFor(tab);
            return ListMenu.IsOpen; // une liste vide ne s'ouvre pas : ne pas prétendre le contraire
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

            // Toute liste ouverte depuis un onglet se quitte par le haut vers la barre d'onglets.
            // On le pose après coup : les modules qui construisent ces listes n'ont pas à savoir
            // d'où on les a ouvertes, et ils servent aussi aux touches directes (G, V, Z…), où il
            // n'y a pas d'onglet au-dessus.
            if (ListMenu.IsOpen) ListMenu.SetExitUp(ExitToTabs);
        }
    }
}
