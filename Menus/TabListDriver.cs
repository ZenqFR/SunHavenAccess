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
    /// Le geste tient en quatre choses, et rien d'autre n'est à retenir : Tab ouvre le menu,
    /// gauche et droite passent d'un onglet à l'autre, **Entrée ouvre la liste de l'onglet où l'on
    /// est**, et Ctrl+haut en ressort vers les onglets.
    ///
    /// PARCOURIR N'EST PAS CHOISIR. La liste s'ouvrait autrefois d'elle-même au simple passage sur
    /// un onglet, et captait aussitôt les flèches : arriver sur l'arbre de compétences interdisait
    /// d'atteindre Relations ou la suite, puisque la flèche suivante parcourait la liste au lieu
    /// de changer d'onglet. Signalé deux fois en jeu — d'abord comme une impasse, puis comme un
    /// obstacle à la simple lecture des onglets. Une ouverture ne se déclenche donc plus que sur
    /// demande explicite.
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

        /// <summary>Signe ce module sur la liste qu'il ouvre, pour ne refermer que la sienne.</summary>
        private const string OwnerTag = "onglets";

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
                    ListMenu.CloseIfOwner(OwnerTag);
                }
                return;
            }

            int tab = CurrentTab();
            if (tab < 0 || tab == _lastTab) return;

            _lastTab = tab;

            // On CHANGE d'onglet : on referme la liste du précédent, sans en ouvrir de nouvelle.
            //
            // Elle s'ouvrait autrefois d'elle-même à l'arrivée sur chaque onglet, et captait
            // aussitôt les flèches. Signalé en jeu : arriver sur l'arbre de compétences
            // interdisait d'atteindre Relations ou la suite, puisque la flèche suivante parcourait
            // la liste au lieu de changer d'onglet. Parcourir n'est pas choisir — on passe devant
            // les onglets librement, et Entrée ouvre celui qu'on veut.
            ListMenu.CloseIfOwner(OwnerTag, false);
        }

        /// <summary>Appelée quand on quitte une liste d'onglet par le haut.</summary>
        private static void ExitToTabs()
        {
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
                case 3: Info.QuestMenu.Open(); break;               // Quêtes
                case 4: MapNavigator.OpenList(); break;           // Carte
                case 5: StatisticsMenu.Open(); break;             // Statistiques
                case 6: SettingsMenu.Open(); break;               // Paramètres
                default: ListMenu.CloseIfOwner(OwnerTag); break;
            }

            // Toute liste ouverte depuis un onglet se quitte par le haut vers la barre d'onglets.
            // On le pose après coup : les modules qui construisent ces listes n'ont pas à savoir
            // d'où on les a ouvertes, et ils servent aussi aux touches directes (G, V, Z…), où il
            // n'y a pas d'onglet au-dessus.
            if (!ListMenu.IsOpen) return;
            ListMenu.Claim(OwnerTag);
            ListMenu.SetExitUp(ExitToTabs);
        }
    }
}
