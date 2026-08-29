using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Wish;
using SunHavenAccess.Speech;
using SunHavenAccess.Localization;
using SunHavenAccess.Util;
using SunHavenAccess.Cursor;

namespace SunHavenAccess.Navigation
{
    /// <summary>
    /// Scanner par catégories façon stardew-access (Object Tracker) : Page précédente/suivante
    /// parcourt les éléments de la catégorie actuelle (du plus proche au plus loin), Ctrl+Page
    /// précédente/suivante change de catégorie (personnages, plantations, ressources,
    /// bâtiments/portails), Origine annonce l'élément actuellement sélectionné (ou le plus
    /// proche si aucun ne l'est encore), Ctrl+Origine lance un cheminement automatique vers lui
    /// (voir PathingController), Fin annonce le nombre trouvé dans la catégorie.
    /// </summary>
    public static class Scanner
    {
        private static readonly string[] CategoryNames =
        {
            "Personnages", "Plantations", "Ressources", "Entrées de bâtiment",
            "Animaux et compagnons", "Ennemis", "Mobilier et rangement", "Services et repères",
            "Changements de zone", "Favoris"
        };

        private const float Radius = 55f; // agrandi (était 40) : plusieurs objets légitimes tombaient hors champ

        private static int _categoryIndex;
        private static int _itemIndex = -1;
        private static readonly List<(Component Obj, string Label, float Distance, string Bearing)> _items = new();

        public static void NextCategory() => ChangeCategory(1);
        public static void PreviousCategory() => ChangeCategory(-1);

        /// <summary>
        /// Change de catégorie en sautant automatiquement celles où rien n'est trouvé (masquées
        /// tant qu'elles sont vides, comme demandé), pour ne pas avoir à parcourir des
        /// catégories vides une par une. Si TOUTES les catégories sont vides, revient sur celle
        /// de départ après un tour complet plutôt que de tourner indéfiniment.
        /// </summary>
        private static void ChangeCategory(int direction)
        {
            int startIndex = _categoryIndex;
            int attempts = 0;
            do
            {
                _categoryIndex = ((_categoryIndex + direction) % CategoryNames.Length + CategoryNames.Length) % CategoryNames.Length;
                Rescan();
                attempts++;
            }
            while (_items.Count == 0 && _categoryIndex != startIndex && attempts < CategoryNames.Length);

            // Pas de préfixe « Catégorie : » : on en change plusieurs fois de suite pour trouver
            // la bonne, et le mot se répétait à chaque fois sans jamais rien apprendre — le nom de
            // la catégorie dit déjà de quoi il s'agit.
            TolkSpeech.Speak(Localization.Language.T(
                $"{CategoryName()}, {_items.Count} élément trouvé{(_items.Count > 1 ? "s" : "")}.",
                $"{CategoryName()}, {_items.Count} found."), true);
        }

        /// <summary>
        /// Le nom de la catégorie courante, dans la langue des annonces. Les noms sont écrits en
        /// français dans <see cref="CategoryNames"/> et traduits ici plutôt que dupliqués : c'est
        /// la même liste qui sert à parcourir les catégories, et deux listes en parallèle
        /// finiraient par diverger.
        /// </summary>
        private static string CategoryName() =>
            Localization.Translator.Translate(CategoryNames[_categoryIndex]);

        public static void NextItem() => Step(1);
        public static void PreviousItem() => Step(-1);

        /// <summary>
        /// Passe à l'élément suivant ou précédent, en RETROUVANT d'abord où l'on en était.
        ///
        /// Le balayage remet l'index à zéro : enchaîner balayage puis déplacement repartait donc
        /// toujours du premier élément, et parcourir la liste était impossible — on réentendait le
        /// même, ou l'on sautait au hasard dès que le décor changeait. C'est exactement le défaut
        /// déjà corrigé dans MenuNavigator, jamais reporté ici.
        ///
        /// On repère donc l'OBJET courant avant de rebalayer, puis on repart de sa nouvelle
        /// position. Le monde bouge — on marche, les bêtes se déplacent — mais l'objet qu'on
        /// écoutait, lui, reste le même.
        /// </summary>
        private static void Step(int direction)
        {
            Component previous = (_itemIndex >= 0 && _itemIndex < _items.Count) ? _items[_itemIndex].Obj : null;

            Rescan();
            if (_items.Count == 0)
            {
                TolkSpeech.Speak(Localization.Language.T($"Rien trouvé en {CategoryName()}.",
                                                          $"Nothing found in {CategoryName()}."), true);
                return;
            }

            int baseIndex = previous != null ? _items.FindIndex(i => i.Obj == previous) : -1;
            _itemIndex = ((baseIndex + direction) % _items.Count + _items.Count) % _items.Count;
            AnnounceCurrent();
        }

        /// <summary>
        /// Touche "Origine" du scanner : annonce l'élément actuellement sélectionné (comme
        /// l'info objet de stardew-access), ou le plus proche si on n'a encore rien sélectionné
        /// dans cette catégorie. Ne change pas l'index si un élément est déjà sélectionné.
        /// </summary>
        public static void AnnounceInfo()
        {
            Rescan();
            if (_items.Count == 0)
            {
                TolkSpeech.Speak(Localization.Language.T($"Rien trouvé en {CategoryName()}.", $"Nothing found in {CategoryName()}."), true);
                return;
            }
            if (_itemIndex < 0 || _itemIndex >= _items.Count) _itemIndex = 0;
            AnnounceCurrent();
        }

        /// <summary>
        /// Ctrl+Origine : lance un cheminement automatique (voir PathingController) vers
        /// l'élément actuellement sélectionné par le scanner.
        /// </summary>
        public static void TravelToCurrent()
        {
            if (_itemIndex < 0 || _itemIndex >= _items.Count)
            {
                TolkSpeech.Speak("Sélectionnez d'abord un élément avec le scanner.", true);
                return;
            }
            var item = _items[_itemIndex];
            if (item.Obj == null)
            {
                TolkSpeech.Speak("Cet élément n'existe plus, nouvelle recherche.", true);
                Rescan();
                return;
            }
            PathingController.TravelTo(item.Obj.transform.position, item.Label);
        }

        public static void AnnounceCount()
        {
            Rescan();
            TolkSpeech.Speak(Localization.Language.T(
                $"{_items.Count} élément{(_items.Count > 1 ? "s" : "")} trouvé{(_items.Count > 1 ? "s" : "")} " +
                $"en {CategoryName()}.",
                $"{_items.Count} found in {CategoryName()}."), true);
        }

        private static void AnnounceCurrent()
        {
            var item = _items[_itemIndex];

            // Le scanner sert à VISER : c'est le seul endroit où la direction reste par défaut.
            string bearing = Strings.WantBearing(forTargeting: true) ? $"{item.Bearing}, " : string.Empty;

            TolkSpeech.Speak(Localization.Language.T(
                $"{item.Label}, {bearing}{Mathf.Round(item.Distance)} case{(item.Distance > 1 ? "s" : "")}. " +
                $"Élément {_itemIndex + 1} sur {_items.Count}.",
                $"{item.Label}, {bearing}{Mathf.Round(item.Distance)} tile{(item.Distance > 1 ? "s" : "")}. " +
                $"Item {_itemIndex + 1} of {_items.Count}."), true);
        }

        private static void Rescan()
        {
            _items.Clear();
            _itemIndex = -1;

            Player player = Player.Instance;
            if (player == null) return;
            Vector3 ppos = player.transform.position;

            IEnumerable<Component> candidates = _categoryIndex switch
            {
                0 => FindNpcs(),
                1 => Object.FindObjectsOfType<Crop>(),
                2 => FindResources(),
                3 => FindPortalsAndDoors(interiors: true),
                8 => FindPortalsAndDoors(interiors: false),
                9 => Favorites.MarkersHere(),
                4 => FindAnimalsAndPets(),
                5 => FindEnemies(),
                6 => FindFurniture(),
                7 => FindServices(),
                _ => System.Array.Empty<Component>()
            };

            foreach (Component c in candidates)
            {
                if (c == null || c.gameObject == null) continue;
                if (!BelongsToActiveScene(c)) continue;

                Vector3 delta = c.transform.position - ppos;
                float distanceTiles = new Vector2(delta.x, delta.y / 1.4142135f).magnitude;
                if (distanceTiles > Radius) continue;

                string label = Describe(c);
                if (string.IsNullOrWhiteSpace(label)) continue;

                _items.Add((c, label, distanceTiles, Strings.BearingName(delta)));
            }

            _items.Sort((a, b) => a.Distance.CompareTo(b.Distance));
            KeepNearestOfEachKind();

            LogEmptyCategory(candidates, ppos);
        }

        /// <summary>
        /// Quand une catégorie ne rend rien alors que des objets existaient, écrire POURQUOI.
        ///
        /// Une catégorie vide n'apprend rien : on ne sait pas si le monde est vide, si le filtre de
        /// zone a tout écarté, ou si le rayon était trop court. Deux fois de suite, il a fallu une
        /// relance et un rapport pour découvrir laquelle des trois — dont « je suis devant Anne et
        /// le scanner ne la trouve pas ». Une ligne de journal répond en une lecture.
        ///
        /// Une seule fois par catégorie et par session : ces relevés se font à chaque pression de
        /// touche, et le journal ne doit pas devenir le prochain problème.
        /// </summary>
        private static readonly HashSet<int> _loggedEmpty = new HashSet<int>();

        private static void LogEmptyCategory(IEnumerable<Component> candidates, Vector3 ppos)
        {
            if (_items.Count > 0 || !_loggedEmpty.Add(_categoryIndex)) return;

            try
            {
                var rejected = candidates
                    .Where(c => c != null && c.gameObject != null)
                    .Take(6)
                    .Select(c =>
                    {
                        Vector3 delta = c.transform.position - ppos;
                        float tiles = new Vector2(delta.x, delta.y / 1.4142135f).magnitude;
                        string scene = c is AI ai ? ai.Scene : c.gameObject.scene.name;
                        return $"    {c.GetType().Name} « {c.gameObject.name} » zone={scene} distance={tiles:F0}";
                    })
                    .ToArray();

                if (rejected.Length == 0)
                {
                    Plugin.Log?.LogInfo($"Scanner, {CategoryNames[_categoryIndex]} : le jeu ne contient aucun objet de ce type ici.");
                    return;
                }

                Plugin.Log?.LogInfo(
                    $"Scanner, {CategoryNames[_categoryIndex]} : rien retenu alors que des objets existent. " +
                    $"Zone active = « {ScenePortalManager.ActiveSceneName} », rayon = {Radius} cases.\n" +
                    string.Join("\n", rejected));
            }
            catch { }
        }

        /// <summary>
        /// Un seul représentant par sorte : le plus proche.
        ///
        /// Une forêt donnait quarante entrées « Chêne », un filon en donnait douze « Pierre ». Pour
        /// qui voit, c'est un décor qu'on embrasse d'un regard ; pour qui écoute, c'est une liste
        /// qu'il faut parcourir jusqu'au bout pour découvrir qu'elle ne contenait qu'une seule
        /// chose. Trouver le bois quand on cherche du bois devenait plus long que d'aller le
        /// couper. Signalé en jeu : « n'afficher que le plus proche ».
        ///
        /// La liste est déjà triée par distance, donc le premier de chaque sorte EST le plus
        /// proche : il suffit de ne garder que lui. On ne perd rien — une fois le chêne le plus
        /// proche abattu, le suivant prend sa place au relevé d'après. La liste dit désormais ce
        /// qu'il y a autour, une fois chaque, ce qui est la question qu'on lui pose.
        /// </summary>
        private static void KeepNearestOfEachKind()
        {
            // On parcourt du plus proche au plus loin et l'on garde la PREMIÈRE occurrence de
            // chaque nom : la liste étant triée par distance, c'est bien la plus proche. Parcourir
            // en sens inverse aurait gardé la plus lointaine — l'exact contraire de ce qu'on veut.
            var seen = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < _items.Count; )
            {
                // On regroupe sur le NOM, pas sur le type : deux minerais différents restent deux
                // entrées, alors qu'ils partagent la même classe. C'est bien ce qu'on cherche —
                // savoir ce qu'il y a, pas combien d'exemplaires.
                if (seen.Add(_items[i].Label)) i++;
                else _items.RemoveAt(i);
            }
        }

        /// <summary>
        /// Le joueur et les décorations/PNJ de la carte ne vivent PAS forcément dans la même
        /// scène Unity (Sun Haven charge la carte active en scène additive séparée du joueur) :
        /// comparer `gameObject.scene.name` du joueur à celui des objets trouvait donc
        /// systématiquement zéro résultat. Le jeu lui-même ne fait jamais cette comparaison : les
        /// décorations (cultures, rochers, arbres à cueillette, coffres, boîtes aux lettres...)
        /// portent un `sceneID` comparé à `ScenePortalManager.ActiveSceneIndex` (voir
        /// Grid.UpdateDecoration en décompilation), et tout ce qui hérite de `Wish.AI` (PNJ,
        /// animaux, compagnons, ennemis) porte un champ `Scene` (string) comparé à
        /// `ScenePortalManager.ActiveSceneName` (voir NPCManager.ManageGraphicsForNPC). On
        /// reproduit exactement cette logique ici — testée sur la classe DE BASE (`AI`), pas
        /// seulement `NPCAI`, pour couvrir aussi Animal/Pet/EnemyAI qui en héritent directement.
        /// </summary>
        private static bool BelongsToActiveScene(Component c)
        {
            if (c is Decoration deco) return deco.sceneID == ScenePortalManager.ActiveSceneIndex;
            // UN HABITANT SOUS LES YEUX N'A PAS À PROUVER SON APPARTENANCE.
            //
            // On comparait `AI.Scene` au nom de la zone courante, caractère pour caractère. Signalé
            // en jeu : debout devant Anne, le scanner ne la trouvait pas. Une égalité stricte de
            // chaînes échoue pour trois fois rien — une casse, un espace, un champ que le jeu n'a
            // pas encore mis à jour après un changement de zone — et l'habitant disparaît alors
            // complètement, alors qu'il est là, à trois pas.
            //
            // On accepte donc trois preuves plutôt qu'une : le nom de zone du personnage, la scène
            // Unity de son objet, ou simplement un champ vide. Le vrai garde-fou reste le filtre de
            // distance, qui écarte de toute façon ce qui est à cinquante-cinq cases. Rater
            // quelqu'un qu'on a devant soi est bien plus grave que d'en annoncer un de trop.
            if (c is AI ai)
            {
                string active = ScenePortalManager.ActiveSceneName;
                if (string.IsNullOrWhiteSpace(ai.Scene)) return true;
                if (string.Equals(ai.Scene.Trim(), active?.Trim(), System.StringComparison.OrdinalIgnoreCase)) return true;
                return string.Equals(c.gameObject.scene.name?.Trim(), active?.Trim(), System.StringComparison.OrdinalIgnoreCase);
            }

            // Les joueurs ne vivent PAS dans la scène de la carte : Sun Haven charge chaque carte
            // en scène additive, alors que les joueurs sont dans une scène persistante. Le repli
            // par nom de scène ci-dessous les aurait donc tous écartés en silence, et l'ajout des
            // autres joueurs au scanner n'aurait rien donné du tout.
            //
            // Le filtre de distance suffit à les cantonner : un partenaire sur une autre carte est
            // à des centaines de cases, donc hors rayon.
            if (c is Player) return true;

            // Un repère de favori est CRÉÉ par le mod : il n'appartient à aucune scène du jeu, et
            // le repli par nom l'écarterait systématiquement. Il n'est de toute façon construit
            // que pour la zone courante.
            if (c is FavoriteMarker) return true;

            return c.gameObject.scene.name == ScenePortalManager.ActiveSceneName;
        }

        /// <summary>
        /// Les personnages : PNJ du jeu ET autres joueurs.
        ///
        /// `NPCManager._npcsList` ne contient que les PNJ. En coopération, un joueur aveugle ne
        /// pouvait donc pas localiser son partenaire — le seul personnage de la carte qu'on
        /// cherche vraiment à rejoindre. Les autres joueurs sont ajoutés ici, et distingués à
        /// l'annonce (voir Describe) : « joueur » plutôt qu'un nom de villageois.
        ///
        /// Le joueur local est évidemment écarté : s'annoncer soi-même à trois cases de distance
        /// n'apprendrait rien.
        /// </summary>
        private static IEnumerable<Component> FindNpcs()
        {
            // ON REGARDE LA SCÈNE, PAS SEULEMENT LE REGISTRE.
            //
            // On se fiait au seul `NPCManager._npcsList`. C'est un registre : il contient qui le
            // jeu a bien voulu y inscrire, pas forcément qui se tient devant vous. Un personnage
            // absent de cette liste — arrivé par une scène, lié à une quête, pas encore enregistré
            // — n'existait tout simplement pas pour le scanner.
            //
            // Balayer la scène en plus du registre garantit que ce qui est là est trouvé. Les
            // doublons entre les deux sources ne coûtent rien : ils portent le même nom, et le
            // regroupement par nom n'en garde qu'un.
            NPCManager mgr = NPCManager.Instance;
            IEnumerable<Component> registered = mgr?._npcsList != null
                ? mgr._npcsList.Where(n => n != null).Cast<Component>()
                : System.Array.Empty<Component>();

            IEnumerable<Component> present;
            try { present = Object.FindObjectsOfType<NPCAI>().Cast<Component>(); }
            catch { present = System.Array.Empty<Component>(); }

            IEnumerable<Component> npcs = registered.Concat(present).Distinct();

            IEnumerable<Component> others;
            try
            {
                others = Object.FindObjectsOfType<Player>()
                    .Where(p => p != null && p != Player.Instance)
                    .Cast<Component>();
            }
            catch { others = System.Array.Empty<Component>(); }

            return npcs.Concat(others);
        }

        private static IEnumerable<Component> FindResources()
        {
            // LES ARBRES MANQUAIENT — les vrais, ceux qu'on abat.
            //
            // `ForageTree` était présent depuis le début et donnait l'illusion que les arbres
            // étaient couverts : c'est l'arbre à cueillette, celui dont on ramasse les fruits. Les
            // arbres à couper sont `Wish.Tree`, et les souches et rondins `Wish.Wood` — deux
            // classes distinctes, absentes de cette liste. Le bûcheronnage était donc entièrement
            // invisible au scanner, alors que le mod annonce « Arbre abattu » depuis toujours et
            // corrige même les patches Wood.Hit et Wood.Die. La preuve était sous les yeux.
            //
            // Toutes héritent de `Decoration`, donc elles portent un identifiant, donc elles sont
            // nommées en français par la base du jeu sans rien à ajouter.
            return Object.FindObjectsOfType<Rock>()
                .Cast<Component>()
                .Concat(Object.FindObjectsOfType<Tree>())
                .Concat(Object.FindObjectsOfType<Wood>())
                .Concat(Object.FindObjectsOfType<ForageTree>())
                .Concat(Object.FindObjectsOfType<Forageable>())
                .Concat(Object.FindObjectsOfType<Ore>());
        }

        private static IEnumerable<Component> FindAnimalsAndPets()
        {
            return Object.FindObjectsOfType<Animal>()
                .Cast<Component>()
                .Concat(Object.FindObjectsOfType<Pet>());
        }

        /// <summary>
        /// Wish.EnemyAI est aussi la classe DE BASE de Wish.NPCAI (tous les PNJ EN héritent) :
        /// sans filtrage, cette catégorie afficherait aussi tous les PNJ en double avec la
        /// catégorie Personnages. On exclut donc explicitement les instances de NPCAI.
        /// </summary>
        private static IEnumerable<Component> FindEnemies()
        {
            return Object.FindObjectsOfType<EnemyAI>()
                .Where(e => e is not NPCAI)
                .Cast<Component>();
        }

        private static IEnumerable<Component> FindFurniture()
        {
            // Wish.OneTimeChest et Wish.ForageableChest N'héritent PAS de Wish.Chest (vérifié en
            // décompilation, toutes deux héritent directement de Decoration) : sans ça, les
            // coffres à butin uniques et ceux trouvés en cueillette manquaient à l'appel.
            return Object.FindObjectsOfType<Chest>()
                .Cast<Component>()
                .Concat(Object.FindObjectsOfType<OneTimeChest>())
                .Concat(Object.FindObjectsOfType<ForageableChest>());
        }

        /// <summary>
        /// Les points de service de la journée : lit, boîte aux lettres, puits, panneau
        /// d'affichage, établi.
        ///
        /// Ce sont les endroits où l'on RETOURNE — dormir pour finir la journée, relever le
        /// courrier, remplir l'arrosoir, prendre les tâches du jour, fabriquer. Le lit et la boîte
        /// aux lettres étaient rangés avec le mobilier : les y chercher obligeait à parcourir tous
        /// les coffres de la maison, alors que ce sont justement les repères qu'on veut atteindre
        /// le plus vite. Ils sortent donc du mobilier plutôt que d'y être dupliqués — une même
        /// chose dans deux catégories ne fait que rallonger les deux.
        /// </summary>
        private static IEnumerable<Component> FindServices()
        {
            return Object.FindObjectsOfType<Bed>()
                .Cast<Component>()
                .Concat(Object.FindObjectsOfType<Mailbox>())
                .Concat(Object.FindObjectsOfType<Well>())
                .Concat(Object.FindObjectsOfType<BulletinBoard>())
                .Concat(Object.FindObjectsOfType<CraftingTable>());
        }

        /// <summary>
        /// Les entrées de bâtiment présentes dans la zone courante.
        ///
        /// Exposée pour la carte : un lieu de carte est une icône sans position dans le monde, et
        /// la seule façon d'y « aller » est de marcher jusqu'à son entrée réelle, qui est ici.
        /// </summary>
        internal static IEnumerable<ScenePortalSpot> PortalsInScene()
        {
            try
            {
                return Object.FindObjectsOfType<ScenePortalSpot>().Where(BelongsToActiveScene);
            }
            catch { return System.Array.Empty<ScenePortalSpot>(); }
        }

        /// <summary>La scène vers laquelle mène ce portail, telle que le jeu la nomme.</summary>
        internal static string PortalDestination(ScenePortalSpot portal)
        {
            try { return SceneToLoadField?.GetValue(portal) as string; }
            catch { return null; }
        }

        /// <summary>
        /// Les passages, séparés selon ce qu'ils font vraiment : entrer quelque part, ou changer
        /// de zone.
        ///
        /// Ils étaient tous dans le même sac, et ce sac ne répondait à aucune question réelle. On
        /// cherche soit « où est la boutique », soit « comment je sors d'ici » — jamais les deux
        /// en même temps, et il fallait pourtant parcourir les deux mélangés à chaque fois.
        ///
        /// Le jeu fait déjà la distinction : chaque zone déclare `SceneSettings.interior`. Une
        /// porte qui mène à un intérieur est une entrée de bâtiment ; une qui mène à l'extérieur
        /// est une sortie vers une autre zone. Aucune liste à tenir, et la règle vaudra encore
        /// pour les zones ajoutées plus tard.
        ///
        /// `Wish.ScenePortalSpot` est la VRAIE classe de ces passages — trouvée en décompilant
        /// Wish.NPCManager/Player à la recherche de « sceneToLoadString ». `Wish.Door` et
        /// `Wish.PortalSpot` sont des marqueurs quasi vides, sans texte d'interaction ; ils ne
        /// savent pas où ils mènent, donc on ne peut pas les classer. On les laisse avec les
        /// entrées de bâtiment, où ils sont le plus souvent, plutôt que de les faire disparaître.
        /// </summary>
        private static IEnumerable<Component> FindPortalsAndDoors(bool interiors)
        {
            var portals = Object.FindObjectsOfType<ScenePortalSpot>()
                .Where(p => p != null && LeadsInside(p) == interiors)
                .Cast<Component>();

            if (!interiors) return portals;

            return portals
                .Concat(Object.FindObjectsOfType<Door>())
                .Concat(Object.FindObjectsOfType<PortalSpot>());
        }

        /// <summary>
        /// Ce passage mène-t-il à un intérieur ? Faute de savoir — zone inconnue du jeu, table pas
        /// encore chargée — on répond « intérieur » : c'est le cas de loin le plus fréquent, et
        /// mieux vaut un passage rangé au mauvais endroit qu'un passage qui disparaît des deux.
        /// </summary>
        private static bool LeadsInside(ScenePortalSpot portal)
        {
            try
            {
                string destination = PortalDestination(portal);
                if (string.IsNullOrWhiteSpace(destination)) return true;

                var manager = SceneSettingsManager.Instance;
                if (manager?.sceneNameDictionary == null) return true;

                return !manager.sceneNameDictionary.TryGetValue(destination, out SceneSettings settings)
                    || settings == null
                    || settings.interior;
            }
            catch { return true; }
        }

        private static readonly FieldInfo SceneToLoadField =
            typeof(ScenePortalSpot).GetField("sceneToLoadString", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo BuildingPortalField =
            typeof(ScenePortalSpot).GetField("buildingPortal", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo PortalTypeField =
            typeof(ScenePortalSpot).GetField("portalType", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo PlayerHousePortalField =
            typeof(ScenePortalSpot).GetField("_playerHousePortal", BindingFlags.NonPublic | BindingFlags.Instance);

        /// <summary>
        /// L'infobulle d'interaction de ScenePortalSpot dit toujours juste "Entrer" (texte
        /// générique), pas très utile pour savoir VERS QUOI. Les champs qui donnent la vraie
        /// destination (nom de maison/grange/boutique...) sont privés : lus par réflexion, comme
        /// pour la quantité d'objets dans TooltipReader.cs.
        /// </summary>
        private static string DescribeBuildingEntrance(ScenePortalSpot portal)
        {
            bool isPlayerHouse = (bool)(PlayerHousePortalField?.GetValue(portal) ?? false);
            if (isPlayerHouse) return "Entrée, votre maison.";

            bool isBuildingPortal = (bool)(BuildingPortalField?.GetValue(portal) ?? false);
            if (isBuildingPortal)
            {
                object portalType = PortalTypeField?.GetValue(portal);
                string label = portalType != null ? UiNameTranslator.Translate(portalType.ToString()) : Localization.Language.T("bâtiment", "building");
                return $"Entrée, {label}.";
            }

            string sceneToLoad = SceneToLoadField?.GetValue(portal) as string;
            if (!string.IsNullOrWhiteSpace(sceneToLoad))
            {
                return $"Entrée, {Util.SceneNames.Translate(sceneToLoad)}.";
            }

            return "Entrée.";
        }

        /// <param name="allowGenericName">
        /// Autorise le dernier repli — le nom technique de l'objet, mis en mots.
        ///
        /// À passer FAUX quand on interroge plusieurs composants d'un même objet pour trouver le
        /// plus parlant : ce repli répond pour n'importe quel composant, donc le premier venu
        /// gagnerait avant qu'on atteigne celui qui sait vraiment nommer la chose. C'est ce qui
        /// faisait annoncer « Enemy bloque le passage » au lieu du nom de la créature.
        /// </param>
        internal static string Describe(Component c, bool allowGenericName = true, bool followSiblings = true)
        {
            // Un autre joueur AVANT le test NPCAI : c'est la distinction qui compte en
            // coopération, et on ne veut surtout pas qu'un partenaire soit annoncé comme un
            // villageois.
            if (c is Player other && other != Player.Instance)
            {
                string playerName = TextUtil.Clean(other.name);
                return string.IsNullOrWhiteSpace(playerName) ? "Autre joueur" : $"{playerName}, joueur";
            }

            if (c is FavoriteMarker favorite) return favorite.FavoriteName;
            if (c is NPCAI npc) return npc.LocalizedActualNPCName;
            if (c is Crop crop) return TileCursor.DescribeCrop(crop);
            if (c is ScenePortalSpot portal) return DescribeBuildingEntrance(portal);
            if (c is Animal animal) return Info.AnimalAnnouncer.Describe(animal);
            if (c is EnemyAI enemy && !string.IsNullOrWhiteSpace(enemy.enemyName))
                return Info.ItemNames.ByEnglishName(enemy.enemyName) ?? UiNameTranslator.Translate(enemy.enemyName);

            // Le nom de la chose AVANT ce qu'on peut en faire.
            //
            // Une décoration — rocher, arbre, filon, meuble — porte son propre nom, et c'est
            // l'information qu'on cherche : sans la vue, savoir qu'on peut « Miner » ne dit pas
            // s'il s'agit de pierre, de cuivre ou de fer. Le texte d'interaction ne nomme que le
            // geste ; il vient donc après, en repli.
            if (c is Decoration decoration)
            {
                // L'identifiant d'abord, le nom affiché ensuite.
                //
                // Une décoration porte un identifiant, et le jeu sait déjà traduire chaque
                // identifiant en un nom, dans la langue où l'on joue. C'est la seule source qui ne
                // laisse jamais de trou : elle couvre tout ce qui existe, y compris ce que le jeu
                // ajoutera. `decorationName`, lui, est un texte d'auteur souvent vide ou resté en
                // anglais — bon en repli, mauvais en premier choix.
                string byId = Info.ItemNames.Get(decoration.id);
                if (!string.IsNullOrWhiteSpace(byId)) return UiNameTranslator.Translate(byId);

                // Beaucoup d'objets du décor n'ont PAS d'identifiant : ceux que la carte contient
                // d'origine valent -1, n'ayant jamais été posés par personne. Leur nom d'auteur est
                // alors le seul point d'entrée — mais il est en anglais. On le repasse à la base du
                // jeu, qui sait retrouver un identifiant depuis un nom, et donc rendre la version
                // française. Sans ce détour, une forêt entière s'annonçait « Oak Tree ».
                string raw = TextUtil.Clean(decoration.decorationName);
                string byName = Info.ItemNames.ByEnglishName(raw);
                if (!string.IsNullOrWhiteSpace(byName)) return byName;

                string decorationName = UiNameTranslator.Translate(raw);
                if (!string.IsNullOrWhiteSpace(decorationName)) return decorationName;
            }

            if (c is IInteractable interactable)
            {
                InteractionInfo info = interactable.InteractionPoint;
                if (info?.interactionText != null && info.interactionText.Count > 0)
                {
                    string text = TextUtil.Clean(info.interactionText[0]);
                    if (!string.IsNullOrWhiteSpace(text)) return text;
                }
            }

            // LE NOM EST PARFOIS SUR LE VOISIN, PAS SUR CE QU'ON REGARDE.
            //
            // `Wish.Ore` est une classe entièrement VIDE : un simple marqueur posé sur l'objet pour
            // dire « ceci est un filon ». Elle ne porte ni nom, ni identifiant, ni texte. Le nom
            // réel est sur la `Decoration` du même objet — mais comme le scanner collectait les
            // deux composants séparément, le filon apparaissait deux fois : une fois nommé
            // correctement, une fois en « Ore ». C'est exactement l'anglais qui restait dans les
            // ressources.
            //
            // On regarde donc les autres composants du MÊME objet avant d'abandonner. Écrit une
            // fois ici plutôt que classe par classe : tout marqueur vide que le jeu ajoutera un
            // jour sera traité pareil, sans qu'on y revienne.
            // `followSiblings` coupe la récursion : sans lui, deux composants sans nom se
            // renverraient l'un à l'autre indéfiniment et gèleraient le jeu. Un seul saut suffit —
            // le nom cherché est sur l'objet lui-même, pas deux niveaux plus loin.
            if (followSiblings)
            {
                foreach (Component sibling in c.gameObject.GetComponents<Component>())
                {
                    if (sibling == null || sibling == c) continue;
                    if (sibling is Transform || sibling is Collider2D || sibling is Renderer) continue;

                    string named = Describe(sibling, allowGenericName: false, followSiblings: false);
                    if (!string.IsNullOrWhiteSpace(named)) return named;
                }
            }

            // Le nom technique de l'objet Unity, en dernier recours. Il est en anglais par nature ;
            // on le repasse quand même à la base du jeu, qui reconnaît parfois le nom d'origine
            // d'un objet et rend alors sa version française.
            string objectName = TextUtil.Clean(c.gameObject.name);
            string fromDatabase = Info.ItemNames.ByEnglishName(objectName);
            if (!string.IsNullOrWhiteSpace(fromDatabase)) return fromDatabase;

            return allowGenericName ? UiNameTranslator.Translate(objectName) : null;
        }
    }
}
