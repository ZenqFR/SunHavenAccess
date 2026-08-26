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
            "Personnages", "Plantations", "Ressources", "Bâtiments et portails",
            "Animaux et compagnons", "Ennemis", "Mobilier et rangement"
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

            TolkSpeech.Speak($"Catégorie : {CategoryNames[_categoryIndex]}. {_items.Count} élément trouvé{(_items.Count > 1 ? "s" : "")}.", true);
        }

        public static void NextItem()
        {
            Rescan();
            Move(1);
        }

        public static void PreviousItem()
        {
            Rescan();
            Move(-1);
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
                TolkSpeech.Speak($"Rien trouvé dans la catégorie {CategoryNames[_categoryIndex]}.", true);
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
            TolkSpeech.Speak(
                $"{_items.Count} élément{(_items.Count > 1 ? "s" : "")} trouvé{(_items.Count > 1 ? "s" : "")} " +
                $"dans la catégorie {CategoryNames[_categoryIndex]}.", true);
        }

        private static void Move(int direction)
        {
            if (_items.Count == 0)
            {
                TolkSpeech.Speak($"Rien trouvé dans la catégorie {CategoryNames[_categoryIndex]}.", true);
                return;
            }
            _itemIndex = ((_itemIndex + direction) % _items.Count + _items.Count) % _items.Count;
            AnnounceCurrent();
        }

        private static void AnnounceCurrent()
        {
            var item = _items[_itemIndex];
            TolkSpeech.Speak(
                $"{item.Label}, {item.Bearing}, {Mathf.Round(item.Distance)} case{(item.Distance > 1 ? "s" : "")}. " +
                $"Élément {_itemIndex + 1} sur {_items.Count}.", true);
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
                3 => FindPortalsAndDoors(),
                4 => FindAnimalsAndPets(),
                5 => FindEnemies(),
                6 => FindFurniture(),
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
            if (c is AI ai) return ai.Scene == ScenePortalManager.ActiveSceneName;
            return c.gameObject.scene.name == ScenePortalManager.ActiveSceneName;
        }

        private static IEnumerable<Component> FindNpcs()
        {
            NPCManager mgr = NPCManager.Instance;
            return mgr != null ? mgr._npcsList.Cast<Component>() : System.Array.Empty<Component>();
        }

        private static IEnumerable<Component> FindResources()
        {
            // Wish.Ore (filons de minerai) manquait complètement à l'appel — trouvé en
            // recherchant les classes proches de Rock/ForageTree dans le listing des types.
            return Object.FindObjectsOfType<Rock>()
                .Cast<Component>()
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
                .Concat(Object.FindObjectsOfType<ForageableChest>())
                .Concat(Object.FindObjectsOfType<Mailbox>())
                .Concat(Object.FindObjectsOfType<Bed>());
        }

        private static IEnumerable<Component> FindPortalsAndDoors()
        {
            // Wish.ScenePortalSpot est la VRAIE classe des entrées de bâtiment (maison, grange,
            // boutiques, donjons...) — trouvée en décompilant Wish.NPCManager/Player à la
            // recherche de "sceneToLoadString". Wish.Door et Wish.PortalSpot (composants
            // marqueurs quasi vides, sans texte d'interaction) sont conservés en repli mais ne
            // décrivent presque jamais rien d'utile à eux seuls.
            return Object.FindObjectsOfType<ScenePortalSpot>()
                .Cast<Component>()
                .Concat(Object.FindObjectsOfType<Door>())
                .Concat(Object.FindObjectsOfType<PortalSpot>());
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
                string label = portalType != null ? UiNameTranslator.Translate(portalType.ToString()) : "bâtiment";
                return $"Entrée, {label}.";
            }

            string sceneToLoad = SceneToLoadField?.GetValue(portal) as string;
            if (!string.IsNullOrWhiteSpace(sceneToLoad))
            {
                return $"Entrée, {UiNameTranslator.Translate(sceneToLoad)}.";
            }

            return "Entrée.";
        }

        private static string Describe(Component c)
        {
            if (c is NPCAI npc) return npc.LocalizedActualNPCName;
            if (c is Crop crop) return TileCursor.DescribeCrop(crop);
            if (c is ScenePortalSpot portal) return DescribeBuildingEntrance(portal);
            if (c is Animal animal) return Info.AnimalAnnouncer.Describe(animal);
            if (c is EnemyAI enemy && !string.IsNullOrWhiteSpace(enemy.enemyName)) return UiNameTranslator.Translate(enemy.enemyName);

            if (c is IInteractable interactable)
            {
                InteractionInfo info = interactable.InteractionPoint;
                if (info?.interactionText != null && info.interactionText.Count > 0)
                {
                    string text = TextUtil.Clean(info.interactionText[0]);
                    if (!string.IsNullOrWhiteSpace(text)) return text;
                }
            }

            return UiNameTranslator.Translate(c.gameObject.name);
        }
    }
}
