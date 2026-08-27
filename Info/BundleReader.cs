using System.Collections.Generic;
using System.Linq;
using Wish;
using SunHavenAccess.Speech;

namespace SunHavenAccess.Info
{
    /// <summary>
    /// Les paquets à compléter : musée, autel de Dynus, Snaccoon, aquarium.
    ///
    /// C'est le système de progression au long cours de Sun Haven — on y dépose des objets précis,
    /// en quantité précise, sur toute une partie, pour débloquer des récompenses. Visuellement, un
    /// paquet est une grille d'emplacements montrant chacun l'objet attendu en grisé et un
    /// compteur. Sans la vue, il était impossible de savoir ce qu'un paquet réclamait encore : il
    /// fallait déposer au hasard et voir ce que le jeu acceptait.
    ///
    /// Techniquement, un paquet est un coffre (`Wish.HungryMonster : Chest`) dont chaque
    /// emplacement n'accepte qu'un seul objet (`Slot.onlyAcceptSpecificItem`) en une quantité
    /// donnée (`Slot.numberOfItemToAccept`). C'est ce qui le distingue d'un coffre ordinaire, et
    /// ce que ce fichier lit — sans redéfinir la moindre règle du jeu.
    /// </summary>
    public static class BundleReader
    {
        /// <summary>
        /// Un paquet est-il ouvert ? Un coffre ordinaire accepte n'importe quoi ; un paquet a des
        /// emplacements à objet imposé. On teste donc la nature des emplacements, pas le type de
        /// l'objet ouvert — ce qui couvre du même coup toutes les variantes de paquets sans avoir
        /// à les énumérer.
        /// </summary>
        public static bool IsOpen() => Slots().Count > 0;

        /// <summary>
        /// Ce qu'il manque au paquet ouvert. Répond à la seule question qui se pose devant lui :
        /// « qu'est-ce que je dois encore rapporter ? »
        /// </summary>
        public static void AnnounceStatus()
        {
            List<SlotItemData> slots = Slots();

            if (slots.Count == 0)
            {
                TolkSpeech.Speak("Aucun paquet ouvert.", true);
                return;
            }

            var missing = new List<string>();
            int filled = 0;

            foreach (SlotItemData data in slots)
            {
                Slot slot = data.slot;
                int needed = slot.numberOfItemToAccept;
                int have = data.amount;

                if (have >= needed) { filled++; continue; }
                missing.Add(Localization.Language.T($"{NameOf(slot)}, {have} sur {needed}",
                                                    $"{NameOf(slot)}, {have} of {needed}"));
            }

            if (missing.Count == 0)
            {
                TolkSpeech.Speak(Localization.Language.T(
                    $"Paquet complet : {slots.Count} emplacement{Plural(slots.Count)} rempli{Plural(slots.Count)}.",
                    $"Bundle complete: {slots.Count} slot{Plural(slots.Count)} filled."), true);
                return;
            }

            string head = Localization.Language.T(
                $"Paquet, {filled} emplacement{Plural(filled)} rempli{Plural(filled)} sur {slots.Count}. Il manque : ",
                $"Bundle, {filled} slot{Plural(filled)} filled of {slots.Count}. Still missing: ");
            TolkSpeech.Speak(head + string.Join(" ; ", missing) + ".", true);
        }

        /// <summary>
        /// Description d'un emplacement de paquet, pour la lecture au fil de la navigation.
        /// Retourne null si cet emplacement n'appartient pas à un paquet, auquel cas la lecture
        /// habituelle des emplacements reprend la main.
        ///
        /// Un emplacement de paquet doit s'annoncer même quand il est vide — c'est justement dans
        /// ce cas qu'il porte l'information utile : ce qu'il attend.
        /// </summary>
        public static string DescribeSlot(Slot slot)
        {
            if (slot == null || !slot.onlyAcceptSpecificItem) return null;

            int needed = slot.numberOfItemToAccept;
            int have = AmountIn(slot);

            if (have >= needed)
                return Localization.Language.T($"{NameOf(slot)}, complet, {needed} sur {needed}.",
                                               $"{NameOf(slot)}, complete, {needed} of {needed}.");

            return Localization.Language.T(
                $"{NameOf(slot)}, {have} sur {needed} déposé{Plural(have)}.",
                $"{NameOf(slot)}, {have} of {needed} handed in.");
        }

        // ------------------------------------------------------------------ Interne

        private static string Plural(int n) => n > 1 ? "s" : string.Empty;

        /// <summary>
        /// Les emplacements à objet imposé du conteneur actuellement ouvert. Liste vide s'il n'y
        /// en a pas — coffre ordinaire, ou rien d'ouvert.
        /// </summary>
        private static List<SlotItemData> Slots()
        {
            try
            {
                Inventory external = ItemIcon.ExternalInventory;
                if (external?.Items == null) return new List<SlotItemData>();

                return external.Items
                    .Where(d => d?.slot != null && d.slot.onlyAcceptSpecificItem && d.slot.numberOfItemToAccept > 0)
                    .ToList();
            }
            catch
            {
                return new List<SlotItemData>();
            }
        }

        private static int AmountIn(Slot slot)
        {
            try
            {
                Inventory inv = slot.inventory;
                if (inv?.Items == null) return 0;
                if (slot.slotNumber < 0 || slot.slotNumber >= inv.Items.Count) return 0;
                return inv.Items[slot.slotNumber]?.amount ?? 0;
            }
            catch { return 0; }
        }

        /// <summary>
        /// Nom de l'objet attendu par un emplacement.
        ///
        /// Deux sources, essayées dans cet ordre : la référence directe de l'emplacement, puis
        /// l'icône grisée que le jeu y affiche en attendant le dépôt. Si aucune ne répond, on
        /// annonce l'identifiant brut plutôt que « objet » : un numéro se recherche, alors qu'un
        /// mot vague ne mène nulle part.
        /// </summary>
        private static string NameOf(Slot slot)
        {
            try
            {
                string direct = slot.itemToAccept?.UnformattedDisplayName;
                if (!string.IsNullOrWhiteSpace(direct)) return direct;
            }
            catch { }

            try
            {
                string icon = slot.GetComponentInChildren<ItemIcon>()?.itemData?.UnformattedDisplayName;
                if (!string.IsNullOrWhiteSpace(icon)) return icon;
            }
            catch { }

            try { return Localization.Language.T($"objet numéro {slot.serializedItemToAccept.id}", $"item number {slot.serializedItemToAccept.id}"); }
            catch { return Localization.Language.T("objet inconnu", "unknown item"); }
        }
    }
}
