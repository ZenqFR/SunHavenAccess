using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Wish;
using SunHavenAccess.Speech;
using SunHavenAccess.Util;

namespace SunHavenAccess.Menus
{
    /// <summary>
    /// Navigation directionnelle RÉELLE (haut/bas/gauche/droite selon la position à l'écran, pas
    /// une simple liste triée) dans le sac à dos/équipement/barre d'action, demandée
    /// explicitement pour remplacer le parcours plat de MenuNavigator sur cet écran précis.
    ///
    /// `Wish.Slot`/`ArmorSlot` (inventaire/équipement) implémentent `ISelectHandler`/
    /// `ISubmitHandler` mais PAS `IPointerClickHandler` — `MenuNavigator.Activate()` (qui simule
    /// un pointerClick en repli générique) ne fait donc probablement rien dessus. Cette classe
    /// évite le problème : elle ne clique jamais, elle déplace juste la sélection Unity
    /// (`EventSystem.SetSelectedGameObject`), qui déclenche déjà nativement l'infobulle/le texte
    /// lus par TooltipReader/FocusReader — mêmes annonces que la navigation normale du jeu.
    ///
    /// Catégorisation d'un slot (pas besoin de connaître la hiérarchie Unity réelle, jamais vue) :
    /// - `ArmorSlot` → Équipement.
    /// - `Slot` simple avec `slotNumber &lt; 10` → Barre d'action (confirmé en décompilant
    ///   `Wish.ItemIcon.MoveBetweenActionBar` : `MoveToAndFromIndex = 10` sépare exactement les
    ///   10 premiers slots — mêmes `Wish.Inventory`, juste un sous-ensemble d'index — du reste du
    ///   sac, cohérent avec `Button.InventorySlot1..10` = touches 1-0 par défaut du jeu).
    /// - `Slot` simple avec `slotNumber &gt;= 10` → Sac à dos (reste des emplacements).
    /// - Boutons d'onglet majeurs (même détection que `MenuNavigator.SwitchMajorTab`) → Onglets.
    ///
    /// Flèche seule : voisin géométrique le plus proche dans la direction pressée, PARMI LA MÊME
    /// catégorie que le slot actuel (delta d'écran réel des RectTransform, comme la navigation
    /// spatiale native d'Unity — pas de comptage de colonnes deviné, jamais vérifiable sans jeu
    /// ouvert). Ctrl+flèche : même algorithme, mais sur les catégories DIFFÉRENTES de la
    /// courante, pour sauter direct au panneau adjacent plutôt que de parcourir toute la grille.
    ///
    /// Touches 1-0 (rangée de chiffres, même position physique en AZERTY qu'en QWERTY pour
    /// `KeyCode.Alpha1`..`Alpha0`) sur un slot du sac ou de la barre d'action : échange
    /// directement avec le slot de barre d'action correspondant
    /// (`Wish.Inventory.SwapItems`, PUBLIQUE, opération symétrique — couvre "envoyer vers la
    /// barre d'action" ET "récupérer depuis la barre d'action" avec le même code). Le jeu
    /// désactive lui-même le changement d'outil actif par ces touches tant que l'inventaire est
    /// ouvert (`!UIHandler.InventoryOpen`, vu en décompilation) : pas de conflit avec leur usage
    /// normal en jeu.
    ///
    /// **Jamais testé en jeu** : aucun moyen de vérifier les positions RectTransform réelles ni
    /// si la navigation native du jeu répond DÉJÀ (en partie ou en double) aux mêmes flèches sur
    /// cet écran précis (voir le même risque déjà documenté pour l'arbre de compétences).
    /// </summary>
    public static class InventoryGridNavigator
    {
        private enum PanelKind { ActionBar, Backpack, Equipment, Tab }

        /// <summary>Vrai si un Slot (sac/équipement) est actuellement sélectionné nativement — sert de garde à HotkeyManager.</summary>
        public static bool IsSlotFocused() => ResolveCurrentSlot() != null;

        public static void Move(Vector2Int direction, bool crossPanel)
        {
            Slot current = ResolveCurrentSlot();
            if (current == null) return;

            PanelKind currentKind = Classify(current);
            var candidates = new List<(Component obj, PanelKind kind, Vector3 pos)>();

            foreach (Slot s in Object.FindObjectsOfType<Slot>())
            {
                if (s == null || s == current || !s.gameObject.activeInHierarchy || !IsVisible(s.gameObject)) continue;
                candidates.Add((s, Classify(s), s.transform.position));
            }
            if (crossPanel || currentKind == PanelKind.Tab)
            {
                foreach (Transform tab in FindMajorTabs())
                {
                    if (tab == null || !tab.gameObject.activeInHierarchy) continue;
                    candidates.Add((tab, PanelKind.Tab, tab.position));
                }
            }

            IEnumerable<(Component obj, PanelKind kind, Vector3 pos)> pool = crossPanel
                ? candidates.Where(c => c.kind != currentKind)
                : candidates.Where(c => c.kind == currentKind);

            Component best = FindNearest(current.transform.position, direction, pool);
            if (best == null)
            {
                TolkSpeech.Speak(crossPanel ? "Aucun panneau dans cette direction." : "Aucun élément dans cette direction.", true);
                return;
            }

            EventSystem.current.SetSelectedGameObject(best.gameObject);
        }

        /// <summary>Touches 1 à 0 : échange le slot actuel avec le slot de barre d'action correspondant (index 0-9).</summary>
        public static void QuickAssign(int hotbarIndex)
        {
            Slot current = ResolveCurrentSlot();
            if (current == null || current is ArmorSlot || current.inventory == null) return;
            if (hotbarIndex < 0 || hotbarIndex > 9) return;

            current.inventory.SwapItems(current.slotNumber, hotbarIndex, out _, out _);
            current.inventory.UpdateInventory();

            // Re-sélectionner le MÊME GameObject slot annonce automatiquement son nouveau
            // contenu via FocusReader/TooltipReader (comme n'importe quel autre changement de
            // sélection), sans avoir à reconstruire nous-mêmes le texte.
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(current.gameObject);
        }

        private static Slot ResolveCurrentSlot()
        {
            GameObject go = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (go == null || !go.activeInHierarchy) return null;
            return go.GetComponent<Slot>() ?? go.GetComponentInParent<Slot>() ?? go.GetComponentInChildren<Slot>();
        }

        private static PanelKind Classify(Slot slot)
        {
            if (slot is ArmorSlot) return PanelKind.Equipment;
            return slot.slotNumber < 10 ? PanelKind.ActionBar : PanelKind.Backpack;
        }

        private static List<Transform> FindMajorTabs()
        {
            Selectable[] all = Object.FindObjectsOfType<Selectable>();
            Selectable anyMajorTab = all.FirstOrDefault(s =>
                s != null && s.gameObject.activeInHierarchy && UiNameTranslator.IsMajorTabName(s.gameObject.name));
            if (anyMajorTab == null) return new List<Transform>();
            Transform parent = anyMajorTab.transform.parent;
            if (parent == null) return new List<Transform>();

            var result = new List<Transform>();
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (UiNameTranslator.IsMajorTabName(child.name) && child.gameObject.activeInHierarchy)
                    result.Add(child);
            }
            return result;
        }

        private static bool IsVisible(GameObject go)
        {
            CanvasGroup group = go.GetComponentInParent<CanvasGroup>();
            return group == null || group.alpha > 0.01f;
        }

        /// <summary>
        /// Voisin géométrique le plus proche dans la direction demandée : privilégie ce qui est
        /// bien ALIGNÉ dans cette direction (faible écart perpendiculaire) plutôt que juste le
        /// plus proche à vol d'oiseau — même principe que la navigation spatiale native d'Unity.
        /// </summary>
        private static Component FindNearest(Vector3 from, Vector2Int direction, IEnumerable<(Component obj, PanelKind kind, Vector3 pos)> candidates)
        {
            Vector2 dir = new Vector2(direction.x, direction.y).normalized;
            Vector2 perp = new Vector2(-dir.y, dir.x);

            Component best = null;
            float bestScore = float.MaxValue;
            foreach (var (obj, _, pos) in candidates)
            {
                Vector2 delta = pos - from;
                float along = Vector2.Dot(delta, dir);
                if (along <= 0.01f) continue; // doit être réellement dans cette direction, pas derrière
                float perpendicular = Mathf.Abs(Vector2.Dot(delta, perp));
                float score = along + perpendicular * 3f; // pénalise fortement le désalignement
                if (score < bestScore)
                {
                    bestScore = score;
                    best = obj;
                }
            }
            return best;
        }
    }
}
