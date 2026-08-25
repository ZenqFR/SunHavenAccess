using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Wish;
using SunHavenAccess.Speech;
using SunHavenAccess.Util;

namespace SunHavenAccess.Info
{
    /// <summary>
    /// Actions de confort sur l'inventaire, toutes basées sur des méthodes PUBLIQUES du jeu :
    /// trier, résumer le contenu, ranger dans les coffres proches. Sans elles, la seule façon de
    /// savoir ce qu'on possède est de parcourir les 40 emplacements un par un — ce qui reste
    /// lisible, mais très lent.
    /// </summary>
    public static class InventoryActions
    {
        /// <summary>
        /// Rayon de rangement, en unités de monde (≈ cases). Volontairement court : le joueur ne
        /// voit pas où partent ses objets, un rangement à distance serait impossible à retrouver.
        /// </summary>
        private const float ChestRadius = 12f;

        // ------------------------------------------------------------------ Tri

        /// <summary>
        /// `SortPlayerInventory()` = `Sort(10, 50)` côté jeu : ne trie QUE le sac à dos, sans
        /// toucher à la barre d'action (0-9) ni à l'équipement (50+) — exactement ce qu'on veut,
        /// réorganiser la barre d'action dans le dos du joueur serait déroutant.
        /// Effet secondaire recherché : les emplacements occupés deviennent contigus, donc la
        /// navigation directionnelle devient nettement plus courte.
        /// </summary>
        public static void SortBackpack()
        {
            Inventory inventory = PlayerInventory();
            if (inventory == null) return;

            inventory.SortPlayerInventory();
            inventory.UpdateInventory();
            TolkSpeech.Speak("Sac à dos trié.", true);
        }

        // -------------------------------------------------------------- Résumé

        /// <summary>
        /// Annonce ce que contient le sac, agrégé par nom d'objet. Lit les `ItemIcon` présents
        /// plutôt que `Inventory.Items` : leur champ public `itemData` est DÉJÀ résolu par le jeu,
        /// ce qui évite de passer par la base de données d'objets (chargement asynchrone, et une
        /// référence d'assembly en plus). Le résumé n'a de sens qu'inventaire ouvert, donc les
        /// icônes existent forcément à ce moment-là.
        /// </summary>
        public static void AnnounceContents()
        {
            Inventory inventory = PlayerInventory();
            if (inventory == null) return;

            var totals = new Dictionary<string, int>();
            int occupied = 0;

            foreach (Slot slot in Object.FindObjectsOfType<Slot>())
            {
                if (slot == null || slot is ArmorSlot || !slot.gameObject.activeInHierarchy) continue;
                if (slot.slotNumber < 10 || slot.slotNumber >= 50) continue; // sac à dos uniquement

                ItemIcon icon = slot.GetComponentInChildren<ItemIcon>();
                if (icon == null || icon.item == null) continue;

                string name;
                try
                {
                    if (icon.item.ID() == 0) continue;
                    name = TextUtil.Clean(icon.itemData?.UnformattedDisplayName);
                }
                catch { continue; }

                if (string.IsNullOrWhiteSpace(name)) name = "objet inconnu";
                occupied++;
                totals.TryGetValue(name, out int already);
                totals[name] = already + Mathf.Max(1, icon.amount);
            }

            if (totals.Count == 0)
            {
                TolkSpeech.Speak("Sac à dos vide.", true);
                return;
            }

            // Du plus abondant au moins abondant : ce qu'on cherche en priorité est en général
            // ce dont on a beaucoup (ressources), et ça évite de finir par les objets uniques.
            string list = string.Join(", ", totals
                .OrderByDescending(kv => kv.Value)
                .Select(kv => $"{kv.Value} {kv.Key}"));

            int free = 40 - occupied; // 40 emplacements de sac (index 10 à 49)
            TolkSpeech.Speak($"{list}. {free} emplacement{(free > 1 ? "s" : "")} libre{(free > 1 ? "s" : "")}.", true);
        }

        // ------------------------------------------------------------ Rangement

        /// <summary>
        /// Dépose dans les coffres PROCHES tout ce dont ils contiennent déjà un exemplaire
        /// (sémantique de `TransferSimilarToOtherInventory`, publique côté jeu, qui renvoie la
        /// liste des inventaires réellement modifiés — d'où un vrai compte-rendu possible).
        ///
        /// La méthode `Inventory.TransferToNearbyChests()` du jeu N'EST PAS utilisée malgré son
        /// nom : elle parcourt `ChestManager.inventories`, c'est-à-dire TOUS les coffres chargés,
        /// sans aucun filtre de distance. Des objets partiraient dans un coffre d'un autre
        /// bâtiment sans que le joueur puisse le constater — inacceptable ici. On refait donc la
        /// boucle nous-mêmes en filtrant sur la distance réelle au joueur.
        /// </summary>
        public static void StoreInNearbyChests()
        {
            Inventory inventory = PlayerInventory();
            Player player = Player.Instance;
            if (inventory == null || player == null) return;

            Vector3 playerPos = player.transform.position;
            var touched = new HashSet<Inventory>();
            int nearby = 0;

            foreach (KeyValuePair<Inventory, Chest> pair in ChestManager.associatedChests)
            {
                Chest chest = pair.Value;
                Inventory chestInventory = pair.Key;
                if (chest == null || chestInventory == null || !chest.gameObject.activeInHierarchy) continue;
                if (Vector3.Distance(chest.transform.position, playerPos) > ChestRadius) continue;

                nearby++;
                touched.UnionWith(inventory.TransferSimilarToOtherInventory(chestInventory));
            }

            if (nearby == 0)
            {
                TolkSpeech.Speak("Aucun coffre à proximité.", true);
                return;
            }

            if (touched.Count == 0)
            {
                TolkSpeech.Speak("Rien à ranger dans les coffres proches.", true);
                return;
            }

            // Comme le fait le jeu : prévenir les autres joueurs des coffres modifiés.
            foreach (Inventory modified in touched)
            {
                if (ChestManager.associatedChests.TryGetValue(modified, out Chest chest) && chest != null)
                {
                    try { chest.UpdateChestForMultiplayer(); } catch { }
                }
            }

            inventory.UpdateInventory();
            TolkSpeech.Speak($"{touched.Count} coffre{(touched.Count > 1 ? "s" : "")} rempli{(touched.Count > 1 ? "s" : "")}.", true);
        }

        // --------------------------------------------------------------- Commun

        private static Inventory PlayerInventory()
        {
            Player player = Player.Instance;
            if (player == null || player.Inventory == null)
            {
                TolkSpeech.Speak("Le jeu n'est pas encore chargé.", true);
                return null;
            }
            return player.Inventory;
        }
    }
}
