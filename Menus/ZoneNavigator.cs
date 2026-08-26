using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Wish;
using SunHavenAccess.Speech;

namespace SunHavenAccess.Menus
{
    /// <summary>
    /// Navigation directionnelle RÉELLE dans les menus : les flèches suivent la disposition
    /// visuelle (ligne/colonne), pas une liste plate triée. Remplace le parcours linéaire de
    /// MenuNavigator, qui n'avait aucune notion de grille (« droite » pouvait sauter à la ligne
    /// suivante, « haut » atterrir n'importe où) ni de frontière entre panneaux.
    ///
    /// Principe en deux temps, chacun utilisant la source la plus fiable disponible :
    ///
    /// 1. **À quelle ZONE appartient un élément → arithmétique**, donc exact. `Wish.PlayerInventory`
    ///    assigne des `slotNumber` dans un ordre qui fait autorité (`SetUpInventoryData`) :
    ///    0-9 = barre d'action, 10-49 = sac à dos, 50-62 = équipement (confirmé par
    ///    `GetIndexByArmorType`/`GetVanityIndexByArmorType` : chapeau 50/51, torse 52/53,
    ///    gants 54/55, jambes 56/57, dos 58/59, puis souvenir 60, amulette 61, anneau 62 — d'où
    ///    les DEUX colonnes de l'équipement : index pair = porté, impair = apparence).
    ///
    /// 2. **Où se trouve l'élément DANS sa zone → géométrique**, donc adapté à la réalité de
    ///    l'écran. Les lignes sont reconstruites en regroupant les éléments de hauteur voisine,
    ///    plutôt qu'en supposant une largeur de grille théorique : ça reste juste si un panneau
    ///    n'est que partiellement rempli, et ça marche tel quel pour les écrans SANS grille
    ///    connue (options, boutique, artisanat, arbre de compétences...) qui forment alors une
    ///    zone « générique » unique.
    ///
    /// Flèche seule = déplacement DANS la zone, jamais au-delà : gauche/droite restent sur la
    /// ligne et butent en bout (bip de bord, voir Speech/UiSound.cs), haut/bas changent de ligne.
    /// Ctrl+flèche = saut vers une zone voisine, via une table d'adjacence explicite calquée sur
    /// la disposition réelle du menu Tab (onglets en haut, équipement à gauche, sac à droite,
    /// barre d'action en bas) — pas de géométrie ici, pour que le résultat soit toujours le même.
    /// </summary>
    public static class ZoneNavigator
    {
        public enum Zone { None, Tabs, Equipment, Backpack, ActionBar, Chest, Generic }

        /// <summary>
        /// Adjacence entre zones pour Ctrl+flèche. Absence d'entrée = bord (bip, aucun
        /// déplacement) : c'est ce qui « bloque » proprement en haut de l'équipement, à droite du
        /// sac, etc., comme demandé.
        /// </summary>
        private static readonly Dictionary<(Zone from, int dx, int dy), Zone> Adjacency =
            new Dictionary<(Zone, int, int), Zone>
            {
                { (Zone.Tabs,      0, -1), Zone.Backpack },

                { (Zone.Equipment, 0,  1), Zone.Tabs },
                { (Zone.Equipment, 1,  0), Zone.Backpack },
                { (Zone.Equipment, 0, -1), Zone.ActionBar },

                { (Zone.Backpack,  0,  1), Zone.Tabs },
                { (Zone.Backpack, -1,  0), Zone.Equipment },
                { (Zone.Backpack,  0, -1), Zone.ActionBar },

                { (Zone.ActionBar, 0,  1), Zone.Backpack },

                // Un coffre ouvert s'affiche à côté de l'inventaire du joueur : on passe de
                // l'un à l'autre horizontalement, dans les deux sens.
                { (Zone.Chest,     1,  0), Zone.Backpack },
                { (Zone.Chest,    -1,  0), Zone.Backpack },
                { (Zone.Chest,     0, -1), Zone.ActionBar },
            };

        private static readonly Dictionary<Zone, string> ZoneNames = new Dictionary<Zone, string>
        {
            { Zone.Tabs, "Onglets" },
            { Zone.Equipment, "Équipement" },
            { Zone.Backpack, "Sac à dos" },
            { Zone.ActionBar, "Barre d'action" },
            { Zone.Chest, "Coffre" },
        };

        private static FieldInfo _tabsField;

        // ---------------------------------------------------------------- API

        /// <summary>
        /// Vrai si le mod doit prendre la main sur les flèches. Volontairement basé sur « un menu
        /// est-il ouvert », PAS sur « un élément est-il sélectionné » : c'est exactement ce qui
        /// avait causé une régression (Tab est aussi la touche de cycle de focus par défaut
        /// d'Unity, ce qui rendait vrai un test de sélection même hors de tout menu, et volait
        /// alors toutes les flèches à la navigation normale).
        /// </summary>
        public static bool IsActive()
        {
            // Pendant un dialogue ou une cinématique, le mod LAISSE les flèches au jeu.
            //
            // Les options d'une bulle de dialogue ne sont pas des éléments sélectionnables mais de
            // simples textes (`DialogueController._options`), que le jeu pilote lui-même. Le mod
            // s'en emparait quand même, ne trouvait rien à sélectionner, et rejouait son annonce
            // de repli à chaque appui — tout en empêchant le jeu de changer d'option. D'où le
            // symptôme rapporté : choisir une réponse aux flèches devenait pénible et un message
            // se répétait.
            //
            // Les réponses possibles sont désormais énoncées d'emblée avec la question (voir
            // Patches/DialogueLinePatch.cs), donc rien n'est perdu à rendre les flèches au jeu.
            if (Dialogue.DialogueReader.DialogueOnGoing) return false;

            // Hors partie (menu principal, sélection et création de personnage) : aucun UIHandler
            // ni joueur, mais on est forcément dans un menu.
            //
            // On teste « y a-t-il quelque chose à parcourir », PAS « quelque chose est-il
            // sélectionné ». Ce second test créait un blocage circulaire : la création de
            // personnage ne sélectionne rien d'elle-même, donc le mod refusait les flèches, donc
            // rien n'était jamais sélectionné — l'écran restait sourd à toute navigation. C'est
            // exactement le symptôme rapporté en jeu.
            //
            // Le risque qui avait motivé le test de sélection ne s'applique qu'EN PARTIE (Unity y
            // garde une sélection résiduelle hors menu) : ici il n'y a pas de « hors menu ».
            if (Player.Instance == null) return MenuNavigator.VisibleSelectables().Any();

            // En partie : on s'appuie UNIQUEMENT sur les signaux du jeu « une interface est
            // ouverte ». Se fier à « un élément est-il sélectionné » avait causé une régression
            // (Unity garde une sélection résiduelle même en jeu normal, ce qui volait les flèches
            // et cassait toute la navigation ailleurs).
            if (UIHandler.InventoryOpen) return true;
            if (UIHandler.UIWasOpenThisFrame) return true;
            UIHandler handler = UIHandler.Instance;
            return handler != null && handler.AnotherUIOpen;
        }

        /// <summary>
        /// Libellé d'un onglet du menu principal à partir de son GameObject, via sa position dans
        /// la liste `tabs` du jeu — qui EST l'index de panneau (`OpenMajorPanel(i)` sélectionne
        /// `tabs[i]`). Source autoritative, contrairement au rang deviné dans la hiérarchie Unity
        /// qu'utilisait UiTextExtractor jusqu'ici. Renvoie null si ce n'est pas un onglet.
        /// </summary>
        public static string TabLabelFor(GameObject go)
        {
            if (go == null) return null;
            List<Image> tabs = Tabs();
            if (tabs == null) return null;
            for (int i = 0; i < tabs.Count; i++)
            {
                if (tabs[i] != null && tabs[i].gameObject == go) return TabLabel(i);
            }
            return null;
        }

        public static void Move(int dx, int dy, bool crossZone)
        {
            GameObject currentGo = CurrentSelection();
            Zone zone = ClassifyZone(currentGo);

            // Rien de sélectionné mais un menu est ouvert : premier appui = point d'entrée.
            if (currentGo == null || zone == Zone.None)
            {
                EnterDefault();
                return;
            }

            // Écrans sans zones connues du jeu (création de personnage, arbre de compétences,
            // relations...) : Ctrl+flèche y navigue entre bandes et colonnes déduites de la
            // disposition, puisqu'aucune table d'adjacence ne peut les décrire d'avance.
            if (crossZone && zone == Zone.Generic && MoveAcrossBands(currentGo, dx, dy)) return;

            if (crossZone) MoveAcrossZones(currentGo, zone, dx, dy);
            else MoveWithinZone(currentGo, zone, dx, dy);
        }

        /// <summary>
        /// Saute à la colonne voisine sur un écran qui en comporte plusieurs. Renvoie false si
        /// l'écran n'est pas en colonnes, auquel cas le comportement habituel reprend la main.
        ///
        /// Les colonnes sont déduites GÉOMÉTRIQUEMENT, par les grands écarts horizontaux entre
        /// éléments : c'est ce que fait l'œil, et surtout ça ne dépend d'aucune supposition sur la
        /// hiérarchie Unity de l'écran — laquelle varie d'un écran à l'autre et n'est pas
        /// vérifiable sans lancer le jeu.
        ///
        /// Volontairement ADDITIF : les flèches seules continuent de parcourir tout l'écran comme
        /// avant. Si le découpage se trompe, on atterrit au mauvais endroit avec Ctrl, mais rien
        /// de ce qui fonctionnait déjà ne change.
        /// </summary>
        /// <summary>
        /// La colonne qui contient cet élément, ou null si l'écran n'est pas en colonnes.
        /// </summary>
        private static List<GameObject> ColumnContaining(List<GameObject> members, GameObject go)
        {
            List<List<GameObject>> columns = BuildColumns(members);
            if (columns.Count < 2) return null;
            return columns.FirstOrDefault(c => c.Contains(go));
        }

        /// <summary>
        /// Ctrl+flèche sur un écran sans zones connues du jeu (création de personnage, arbre de
        /// compétences, relations...).
        ///
        /// L'écran y est vu comme un empilement de BANDES horizontales — barre d'onglets, barre de
        /// sous-onglets, grille de contenu, bandeau d'informations — chacune découpée en COLONNES.
        /// Ctrl+haut/bas change de bande, Ctrl+gauche/droite change de colonne dans la bande. Deux
        /// Ctrl+bas depuis les onglets amènent donc dans la barre de métiers puis dans la grille,
        /// exactement comme demandé.
        ///
        /// Les colonnes sont recalculées DANS la bande d'arrivée, pas sur tout l'écran : une
        /// grille de contenu et un bandeau du bas n'ont aucune raison d'avoir le même découpage.
        /// </summary>
        private static bool MoveAcrossBands(GameObject currentGo, int dx, int dy)
        {
            List<GameObject> all = ZoneMembers(Zone.Generic);
            List<List<GameObject>> bands = BuildBands(all);
            if (bands.Count == 0) return false;

            int band = bands.FindIndex(b => b.Contains(currentGo));
            if (band < 0) return false;

            if (dy != 0)
            {
                // dy positif = vers le haut de l'écran ; les bandes sont ordonnées du haut vers le bas.
                int target = band + (dy > 0 ? -1 : 1);

                // Au SOMMET de l'écran, on ne bute pas : on laisse la main à la logique de zones,
                // dont la règle « Ctrl+haut ramène toujours à la barre d'onglets » est la sortie
                // de secours du menu principal. Renvoyer true ici l'avalait — c'est ce qui rendait
                // la barre d'onglets introuvable depuis l'arbre de compétences.
                if (target < 0) return false;

                if (target >= bands.Count) { UiSound.EdgeBump(); return true; }

                float x = currentGo.transform.position.x;
                GameObject entry = bands[target].OrderBy(g => Mathf.Abs(g.transform.position.x - x)).FirstOrDefault();
                if (entry == null) return false;

                FocusReader.SetPendingPrefix($"Bande {target + 1} sur {bands.Count}");
                Select(entry);
                return true;
            }

            List<List<GameObject>> columns = BuildColumns(bands[band]);
            if (columns.Count < 2) { UiSound.EdgeBump(); return true; }

            int col = columns.FindIndex(c => c.Contains(currentGo));
            if (col < 0) return false;

            int targetCol = col + (dx > 0 ? 1 : -1);
            if (targetCol < 0 || targetCol >= columns.Count) { UiSound.EdgeBump(); return true; }

            // On vise l'élément le plus proche EN HAUTEUR : changer de colonne ne doit pas faire
            // perdre sa place verticale.
            float y = currentGo.transform.position.y;
            GameObject best = columns[targetCol].OrderBy(g => Mathf.Abs(g.transform.position.y - y)).FirstOrDefault();
            if (best == null) return false;

            // Repère posé en PRÉFIXE plutôt qu'annoncé à part : FocusReader annonce l'élément à la
            // frame suivante en coupant la parole, ce qui avalerait une annonce faite ici. On
            // obtient « Colonne 2 sur 3, Cheveux » d'une seule traite.
            FocusReader.SetPendingPrefix($"Colonne {targetCol + 1} sur {columns.Count}");
            Select(best);
            return true;
        }

        /// <summary>
        /// Regroupe les éléments en colonnes visuelles. Seuls les écarts horizontaux NETTEMENT
        /// plus grands que l'écart courant coupent une colonne : une grille dense d'icônes reste
        /// ainsi d'un seul tenant, alors qu'un panneau séparé par du vide s'en détache.
        /// </summary>
        private static List<List<GameObject>> BuildColumns(List<GameObject> members) =>
            Cluster(members, g => g.transform.position.x);

        /// <summary>
        /// Regroupe les éléments en BANDES horizontales : barre d'onglets, barre de sous-onglets,
        /// grille de contenu, bandeau d'informations en bas. C'est la structure que suit
        /// Ctrl+haut/bas.
        /// </summary>
        private static List<List<GameObject>> BuildBands(List<GameObject> members) =>
            Cluster(members, g => g.transform.position.y)
                .AsEnumerable().Reverse().ToList(); // du HAUT de l'écran vers le bas

        /// <summary>
        /// Découpe une liste d'éléments en paquets, là où l'espace entre deux voisins devient
        /// nettement plus grand qu'ailleurs — ce que fait l'œil pour distinguer deux panneaux.
        ///
        /// Le seuil est proportionnel à l'ÉTENDUE TOTALE occupée par les éléments, et non une
        /// distance absolue. C'est le correctif du défaut rapporté en jeu sur le choix du métier :
        /// dans une grille, beaucoup d'éléments partagent exactement la même coordonnée, la
        /// médiane des écarts tombe donc à zéro et n'importe quel plancher fixe finissait par
        /// couper entre chaque colonne d'icônes. Un vrai vide entre deux panneaux occupe une
        /// fraction visible de l'écran ; l'espacement d'une grille, non.
        /// </summary>
        private static List<List<GameObject>> Cluster(List<GameObject> members, System.Func<GameObject, float> axis)
        {
            var groups = new List<List<GameObject>>();
            if (members == null) return groups;

            var sorted = members.Where(m => m != null).OrderBy(axis).ToList();
            if (sorted.Count < 2) return groups;

            float span = axis(sorted[sorted.Count - 1]) - axis(sorted[0]);
            if (span <= 0f) return groups;

            var gaps = new List<float>();
            for (int i = 1; i < sorted.Count; i++) gaps.Add(axis(sorted[i]) - axis(sorted[i - 1]));

            float median = gaps.OrderBy(g => g).ElementAt(gaps.Count / 2);

            // Deux conditions à remplir : dépasser largement l'écart courant ET représenter une
            // part visible de l'écran. La première seule se fait piéger par une médiane nulle, la
            // seconde seule découperait une grille très étalée.
            float threshold = Mathf.Max(median * 4f, span * 0.08f);

            var current = new List<GameObject> { sorted[0] };
            for (int i = 1; i < sorted.Count; i++)
            {
                if (gaps[i - 1] > threshold)
                {
                    groups.Add(current);
                    current = new List<GameObject>();
                }
                current.Add(sorted[i]);
            }
            groups.Add(current);

            return groups;
        }

        /// <summary>
        /// Onglets du menu Tab. Utilise l'API PUBLIQUE du jeu (`PlayerInventory.OpenMajorPanel`)
        /// qui fait tout correctement d'un coup : active le bon panneau, met à jour
        /// `majorTabIndex`, et déplace la sélection Unity sur l'onglet (donc FocusReader annonce
        /// tout seul). Remplace l'ancienne approche — clic sur un GameObject trouvé par préfixe de
        /// nom « Major », avec un compteur de rang maintenu par le mod qui DÉRIVAIT dès que
        /// l'onglet changeait autrement (souris, action du jeu) : c'était la cause la plus
        /// probable du comportement « bancale » signalé.
        /// </summary>
        public static void SwitchTab(int direction, bool wrap)
        {
            PlayerInventory inv = Player.Instance != null ? Player.Instance.PlayerInventory : null;
            if (inv == null)
            {
                TolkSpeech.Speak("Le menu n'est pas ouvert.", true);
                return;
            }

            const int tabCount = 7; // le jeu lui-même cycle en Mod(±1, 7) dans UIHandler.Update
            int target = inv.majorTabIndex + direction;

            if (wrap)
            {
                target = ((target % tabCount) + tabCount) % tabCount;
            }
            else if (target < 0 || target >= tabCount)
            {
                UiSound.EdgeBump();
                return;
            }

            // Pas de préfixe à poser ici : OpenMajorPanel sélectionne lui-même l'onglet, et
            // UiTextExtractor sait maintenant en tirer le bon libellé (via TabLabelFor), donc
            // FocusReader l'annonce correctement tout seul.
            inv.OpenMajorPanel(target);
        }

        /// <summary>
        /// Touches 1 à 0 sur un emplacement : échange son contenu avec le slot de barre d'action
        /// correspondant (index 0-9). `Wish.Inventory.SwapItems` est publique et SYMÉTRIQUE, donc
        /// le même appel couvre « envoyer vers la barre d'action » et « récupérer depuis la barre
        /// d'action ». Le jeu bloque de lui-même le changement d'outil actif par ces touches tant
        /// que l'inventaire est ouvert (`!UIHandler.InventoryOpen`, vu en décompilation d'ItemIcon),
        /// donc pas de conflit avec leur usage normal en jeu.
        /// </summary>
        public static void QuickAssign(int hotbarIndex)
        {
            if (!UIHandler.InventoryOpen) return;
            if (hotbarIndex < 0 || hotbarIndex > 9) return;

            Slot slot = ResolveSlot(CurrentSelection());
            // Un emplacement de COFFRE est exclu : `SwapItems` opère au sein d'un même inventaire,
            // donc sur un coffre il échangerait deux cases DU COFFRE au lieu d'envoyer l'objet
            // vers la barre d'action du joueur — silencieusement faux, et destructeur du rangement.
            if (slot == null || slot is ArmorSlot || slot.inventory == null
                || ClassifyZone(slot.gameObject) == Zone.Chest)
            {
                UiSound.EdgeBump();
                return;
            }

            slot.inventory.SwapItems(slot.slotNumber, hotbarIndex, out _, out _);
            slot.inventory.UpdateInventory();

            // Re-sélectionner le même emplacement force FocusReader à réannoncer son nouveau
            // contenu (il ne réagit qu'aux CHANGEMENTS de sélection, d'où le passage par null).
            if (EventSystem.current == null) return;
            EventSystem.current.SetSelectedGameObject(null);
            Select(slot.gameObject); // repasse par Select pour retrouver l'infobulle du nouveau contenu
        }

        public static string TabLabel(int panelIndex) =>
            Util.UiNameTranslator.MajorTabLabelsByIndex.TryGetValue(panelIndex, out string label)
                ? label
                : $"Onglet {panelIndex + 1}";

        // ------------------------------------------------------- Déplacements

        private static void MoveWithinZone(GameObject currentGo, Zone zone, int dx, int dy)
        {
            // Les onglets sont un cas à part : ce n'est pas une sélection libre, changer d'onglet
            // change tout l'écran — on passe par l'API du jeu plutôt que par la grille.
            if (zone == Zone.Tabs)
            {
                if (dx != 0) SwitchTab(dx, wrap: false);
                else MoveAcrossZones(currentGo, zone, dx, dy);
                return;
            }

            // Sur un écran en colonnes, on ne parcourt QUE la colonne courante. Sans ça, les
            // lignes sont reconstruites en groupant par hauteur sur tout l'écran : une « ligne »
            // traverse alors les trois colonnes, et gauche/droite vagabonde du menu de catégories
            // au panneau d'informations en passant par la personnalisation. C'est précisément ce
            // qui rendait la création de personnage incompréhensible.
            List<GameObject> members = ZoneMembers(zone);
            if (zone == Zone.Generic)
            {
                List<GameObject> column = ColumnContaining(members, currentGo);
                if (column != null) members = column;
            }

            List<List<GameObject>> rows = BuildRows(members);
            if (!Locate(rows, Anchor(currentGo), out int row, out int col))
            {
                UiSound.EdgeBump();
                return;
            }

            GameObject target = dx != 0
                ? StepHorizontally(rows, row, col, dx)
                : StepVertically(rows, row, col, dy);

            if (target != null)
            {
                Select(target);
                return;
            }

            // Une zone d'une SEULE ligne (barre d'onglets, barre d'action) n'a aucun déplacement
            // vertical qui lui soit propre : y buter reviendrait à rendre haut et bas définitivement
            // morts, alors que la disposition à l'écran leur donne un sens évident — la première
            // demande était « Tab me met sur les onglets, puis flèche du bas m'amène dans
            // l'inventaire ». On suit donc l'adjacence, comme le ferait Contrôle+flèche.
            //
            // Ce n'est PAS le cas d'un simple bord de grille : en haut du sac ou de l'équipement,
            // la flèche continue de buter avec un bip, comme demandé. La distinction est nette —
            // on ne redirige une touche que lorsqu'elle serait sans effet dans TOUTE la zone,
            // jamais quand elle bloque un vrai bord à l'intérieur d'une grille.
            if (dy != 0 && rows.Count <= 1)
            {
                MoveAcrossZones(currentGo, zone, dx, dy);
                return;
            }

            UiSound.EdgeBump();
        }

        /// <summary>Gauche/droite : reste sur la MÊME ligne, bute en bout (pas de retour à la ligne).</summary>
        private static GameObject StepHorizontally(List<List<GameObject>> rows, int row, int col, int dx)
        {
            int next = col + dx;
            if (next < 0 || next >= rows[row].Count) return null;
            return rows[row][next];
        }

        /// <summary>
        /// Haut/bas : change de ligne en gardant la colonne la plus proche. Les lignes plus
        /// courtes (typiquement souvenir/amulette/anneau, seuls sur leur ligne sous les deux
        /// colonnes de l'équipement) sont donc atteignables au lieu d'être des culs-de-sac.
        /// </summary>
        private static GameObject StepVertically(List<List<GameObject>> rows, int row, int col, int dy)
        {
            // dy = +1 signifie "vers le haut de l'écran", or les lignes sont ordonnées du haut
            // vers le bas : on descend donc dans l'index quand dy est positif.
            int step = dy > 0 ? -1 : 1;
            for (int r = row + step; r >= 0 && r < rows.Count; r += step)
            {
                if (rows[r].Count == 0) continue;
                return rows[r][Mathf.Clamp(col, 0, rows[r].Count - 1)];
            }
            return null;
        }
        /// <summary>
        /// Zone voisine dans une direction. La table d'adjacence est statique, mais deux zones
        /// n'existent que par intermittence et doivent être résolues à l'exécution :
        ///
        /// - **Le coffre**, qui n'est là que quand un coffre est ouvert. L'inscrire en dur ferait
        ///   buter Ctrl+gauche sur du vide depuis le sac le reste du temps.
        /// - **Les panneaux « génériques »** (arbre de compétences, relations, quêtes, carte,
        ///   statistiques, paramètres). BUG CORRIGÉ : la table ne contenait AUCUNE sortie depuis
        ///   Zone.Generic, donc une fois dans un de ces onglets il était impossible de remonter à
        ///   la barre d'onglets avec Ctrl+haut — on restait bloqué dans le panneau. Seul
        ///   l'inventaire fonctionnait, parce que lui seul a ses zones nommées dans la table.
        /// </summary>
        private static bool ResolveAdjacent(Zone from, int dx, int dy, out Zone target)
        {
            if (ChestOpen() && dx != 0 && (from == Zone.Backpack || from == Zone.ActionBar))
            {
                target = Zone.Chest;
                return true;
            }

            // Depuis n'importe quel panneau d'onglet, Ctrl+haut ramène toujours à la barre
            // d'onglets : c'est la sortie de secours qui manquait.
            if (from == Zone.Generic && dy > 0)
            {
                target = Zone.Tabs;
                return true;
            }

            // Depuis la barre d'onglets, Ctrl+bas descend dans le CONTENU de l'onglet courant —
            // qui n'est le sac à dos que sur le premier onglet. Ailleurs, c'est un panneau
            // générique : on choisit celui qui a réellement des éléments plutôt que de renvoyer
            // aveuglément vers un sac à dos absent de cet écran.
            if (from == Zone.Tabs && dy < 0)
            {
                target = HasMembers(Zone.Backpack) ? Zone.Backpack : Zone.Generic;
                return true;
            }

            return Adjacency.TryGetValue((from, dx, dy), out target);
        }

        private static bool HasMembers(Zone zone)
        {
            List<GameObject> members = ZoneMembers(zone);
            return members != null && members.Count > 0;
        }

        private static bool ChestOpen() => ItemIcon.ExternalInventory != null;

        private static void MoveAcrossZones(GameObject currentGo, Zone zone, int dx, int dy)
        {
            if (!ResolveAdjacent(zone, dx, dy, out Zone target))
            {
                UiSound.EdgeBump();
                return;
            }

            if (target == Zone.Tabs)
            {
                PlayerInventory inv = Player.Instance != null ? Player.Instance.PlayerInventory : null;
                if (inv == null) { UiSound.EdgeBump(); return; }
                FocusReader.SetPendingPrefix(ZoneNames[Zone.Tabs]);
                inv.OpenMajorPanel(inv.majorTabIndex); // re-sélectionne l'onglet courant
                return;
            }

            List<List<GameObject>> targetRows = BuildRows(ZoneMembers(target));
            if (targetRows.Count == 0) { UiSound.EdgeBump(); return; }

            // On garde la cohérence spatiale : en entrant par la gauche on arrive à gauche, en
            // entrant par le haut on arrive en haut, et on conserve au mieux l'autre coordonnée.
            List<List<GameObject>> currentRows = BuildRows(ZoneMembers(zone));
            Locate(currentRows, Anchor(currentGo), out int row, out int col);

            int newRow, newCol;
            if (dy != 0)
            {
                newRow = dy > 0 ? targetRows.Count - 1 : 0; // vers le haut = dernière ligne de la zone au-dessus
                newCol = Mathf.Clamp(col, 0, targetRows[newRow].Count - 1);
            }
            else
            {
                newRow = Mathf.Clamp(row, 0, targetRows.Count - 1);
                newCol = dx > 0 ? 0 : targetRows[newRow].Count - 1;
            }

            GameObject entry = targetRows[newRow][Mathf.Clamp(newCol, 0, targetRows[newRow].Count - 1)];
            FocusReader.SetPendingPrefix(ZoneNames.TryGetValue(target, out string n) ? n : null);
            Select(entry);
        }

        private static void EnterDefault()
        {
            // Si un coffre est ouvert, c'est très probablement ce qu'on vient d'ouvrir : on y entre
            // en premier plutôt que dans le sac.
            Zone[] order = ChestOpen()
                ? new[] { Zone.Chest, Zone.Backpack, Zone.ActionBar, Zone.Equipment, Zone.Generic }
                : new[] { Zone.Backpack, Zone.ActionBar, Zone.Equipment, Zone.Generic };

            foreach (Zone z in order)
            {
                List<List<GameObject>> rows = BuildRows(ZoneMembers(z));
                if (rows.Count == 0 || rows[0].Count == 0) continue;
                FocusReader.SetPendingPrefix(ZoneNames.TryGetValue(z, out string n) ? n : null);
                Select(rows[0][0]);
                return;
            }
            UiSound.EdgeBump();
        }

        /// <summary>
        /// Sélectionne un emplacement — mais en visant son ICÔNE D'OBJET quand il en contient une.
        ///
        /// C'est LE point qui manquait : le nom et la description d'un objet ne vivent QUE dans
        /// l'infobulle native du jeu, et celle-ci est déclenchée par `Wish.ItemIcon.Select()` →
        /// `SetupTooltip()`. Or l'ItemIcon est un ENFANT du `Slot` : sélectionner l'emplacement
        /// ne réveillait donc jamais l'infobulle, et on n'entendait que ce qui est écrit dans la
        /// case (la quantité), sans savoir de quel objet il s'agissait. Vaut pour le sac à dos,
        /// l'équipement ET la barre d'action, qui utilisent tous la même paire Slot/ItemIcon.
        ///
        /// `SetupTooltip` est appelée explicitement en plus de la sélection : ça ne dépend pas de
        /// la bonne propagation de l'évènement de sélection, et c'est la méthode publique que le
        /// jeu utilise lui-même partout ailleurs pour ça.
        /// </summary>
        private static void Select(GameObject go)
        {
            if (EventSystem.current == null || go == null) return;

            ItemIcon icon = FilledIcon(go);
            if (icon == null)
            {
                // Emplacement vide : pas d'infobulle possible, FocusReader annonce
                // « Emplacement vide » (ou le type d'armure attendu) comme avant.
                EventSystem.current.SetSelectedGameObject(go);
                return;
            }

            // L'infobulle porte déjà nom + description + quantité (voir Menus/TooltipReader.cs) :
            // laisser FocusReader annoncer en plus donnerait deux annonces concurrentes dont la
            // première serait coupée en plein mot.
            FocusReader.SuppressNextAnnouncement();
            EventSystem.current.SetSelectedGameObject(icon.gameObject);
            try { icon.SetupTooltip(overrideCurrentIcon: true); }
            catch { /* objet en cours de destruction : la sélection reste valide, on n'insiste pas */ }
        }

        /// <summary>
        /// Icône d'objet d'un emplacement, ou null si l'emplacement est vide. Exige un ID d'objet
        /// VALIDE (0 = case vide, même test que partout ailleurs dans le mod) : c'est aussi la
        /// condition que `SetupTooltip` vérifie de son côté avant d'afficher quoi que ce soit.
        /// Sans ce contrôle, on renverrait une icône pour laquelle aucune infobulle n'apparaîtra —
        /// or on coupe l'annonce de FocusReader en comptant dessus : ce serait le silence total
        /// sur cette case.
        /// </summary>
        private static ItemIcon FilledIcon(GameObject slotGo)
        {
            Slot slot = slotGo != null ? slotGo.GetComponent<Slot>() : null;
            if (slot == null) return null;

            ItemIcon icon = slot.GetComponentInChildren<ItemIcon>();
            if (icon == null || !icon.gameObject.activeInHierarchy || icon.item == null) return null;

            try { return icon.item.ID() != 0 ? icon : null; }
            catch { return null; }
        }

        /// <summary>
        /// Emplacement correspondant à un objet sélectionné. Nécessaire depuis que la sélection
        /// peut porter sur l'ICÔNE plutôt que sur l'emplacement lui-même (voir Select) : sans ça,
        /// la navigation perdrait sa position dès le premier objet non vide rencontré.
        /// </summary>
        private static Slot ResolveSlot(GameObject go)
        {
            if (go == null) return null;
            Slot slot = go.GetComponent<Slot>();
            if (slot != null) return slot;
            ItemIcon icon = go.GetComponent<ItemIcon>();
            if (icon != null && icon.slot != null) return icon.slot;
            return go.GetComponentInParent<Slot>();
        }

        /// <summary>GameObject servant de repère dans la grille : toujours celui de l'emplacement.</summary>
        private static GameObject Anchor(GameObject selected)
        {
            Slot slot = ResolveSlot(selected);
            return slot != null ? slot.gameObject : selected;
        }

        // ------------------------------------------------------------- Zones

        private static GameObject CurrentSelection()
        {
            GameObject go = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            return (go != null && go.activeInHierarchy) ? go : null;
        }

        public static Zone ClassifyZone(GameObject go)
        {
            if (go == null) return Zone.None;

            if (IsTab(go)) return Zone.Tabs;

            // ResolveSlot et pas GetComponent : la sélection peut porter sur l'icône d'objet,
            // enfant de l'emplacement (voir Select).
            Slot slot = ResolveSlot(go);
            if (slot != null)
            {
                // ORDRE IMPORTANT : le test « coffre » passe AVANT les plages de slotNumber. Les
                // emplacements d'un coffre ont eux aussi des slotNumber partant de 0, ils seraient
                // donc pris pour la barre d'action ou le sac à dos. Le jeu distingue les deux via des
                // statiques posées par `Chest.Interact()` : l'inventaire externe est celui du coffre.
                if (slot.inventory != null && ItemIcon.ExternalInventory != null
                    && ReferenceEquals(slot.inventory, ItemIcon.ExternalInventory))
                {
                    return Zone.Chest;
                }

                if (slot is ArmorSlot) return Zone.Equipment;
                if (slot.slotNumber >= 0 && slot.slotNumber < 10) return Zone.ActionBar;
                if (slot.slotNumber >= 10 && slot.slotNumber < 50) return Zone.Backpack;
                if (slot.slotNumber >= 50) return Zone.Equipment;
                return Zone.Backpack;
            }

            return go.GetComponent<Selectable>() != null ? Zone.Generic : Zone.None;
        }

        private static bool IsTab(GameObject go)
        {
            List<Image> tabs = Tabs();
            return tabs != null && tabs.Any(t => t != null && t.gameObject == go);
        }

        private static List<Image> Tabs()
        {
            PlayerInventory inv = Player.Instance != null ? Player.Instance.PlayerInventory : null;
            if (inv == null) return null;
            _tabsField ??= typeof(PlayerInventory).GetField("tabs", BindingFlags.NonPublic | BindingFlags.Instance);
            return _tabsField?.GetValue(inv) as List<Image>;
        }

        private static List<GameObject> ZoneMembers(Zone zone)
        {
            if (zone == Zone.Tabs)
            {
                List<Image> tabs = Tabs();
                return tabs == null
                    ? new List<GameObject>()
                    : tabs.Where(t => t != null && t.gameObject.activeInHierarchy)
                          .Select(t => t.gameObject).ToList();
            }

            if (zone == Zone.Generic)
            {
                // Écrans sans grille connue : on réutilise exactement le filtrage déjà éprouvé de
                // MenuNavigator (interactable + visible + hors CanvasGroup transparent).
                return MenuNavigator.VisibleSelectables()
                    .Where(s => ClassifyZone(s.gameObject) == Zone.Generic)
                    .Select(s => s.gameObject).ToList();
            }

            return Object.FindObjectsOfType<Slot>()
                .Where(s => s != null && s.gameObject.activeInHierarchy && ClassifyZone(s.gameObject) == zone)
                .Select(s => s.gameObject)
                .ToList();
        }

        // ------------------------------------------------------------ Grille

        /// <summary>
        /// Reconstruit les lignes visuelles en regroupant les éléments de hauteur voisine, puis
        /// trie chaque ligne de gauche à droite. Résultat ordonné du HAUT vers le BAS.
        /// Volontairement géométrique plutôt que basé sur une largeur de grille supposée : ça
        /// reste juste quel que soit le remplissage réel du panneau, et ça sert tel quel aux
        /// écrans dont on ne connaît pas la disposition.
        /// </summary>
        private static List<List<GameObject>> BuildRows(List<GameObject> members)
        {
            var rows = new List<List<GameObject>>();
            if (members == null || members.Count == 0) return rows;

            var sorted = members
                .Where(m => m != null)
                .OrderByDescending(m => m.transform.position.y)
                .ToList();
            if (sorted.Count == 0) return rows;

            float tolerance = RowTolerance(sorted);
            var currentRow = new List<GameObject> { sorted[0] };
            float rowY = sorted[0].transform.position.y;

            for (int i = 1; i < sorted.Count; i++)
            {
                float y = sorted[i].transform.position.y;
                if (Mathf.Abs(y - rowY) <= tolerance)
                {
                    currentRow.Add(sorted[i]);
                }
                else
                {
                    rows.Add(currentRow.OrderBy(g => g.transform.position.x).ToList());
                    currentRow = new List<GameObject> { sorted[i] };
                    rowY = y;
                }
            }
            rows.Add(currentRow.OrderBy(g => g.transform.position.x).ToList());
            return rows;
        }

        /// <summary>
        /// Deux éléments sont sur la même ligne si leur écart vertical est inférieur à la moitié
        /// de la hauteur d'un élément — dérivé de la taille réelle à l'écran plutôt que d'une
        /// constante en pixels, pour rester correct quelle que soit la résolution ou l'échelle
        /// d'interface choisie par le joueur.
        /// </summary>
        private static float RowTolerance(List<GameObject> members)
        {
            var heights = new List<float>();
            foreach (GameObject go in members)
            {
                if (go.transform is RectTransform rt)
                {
                    float h = Mathf.Abs(rt.rect.height * rt.lossyScale.y);
                    if (h > 0.0001f) heights.Add(h);
                }
            }
            if (heights.Count == 0) return 0.5f;
            heights.Sort();
            return Mathf.Max(0.0001f, heights[heights.Count / 2] * 0.5f);
        }

        private static bool Locate(List<List<GameObject>> rows, GameObject go, out int row, out int col)
        {
            for (int r = 0; r < rows.Count; r++)
            {
                int c = rows[r].IndexOf(go);
                if (c >= 0) { row = r; col = c; return true; }
            }
            row = 0; col = 0;
            return false;
        }
    }
}
