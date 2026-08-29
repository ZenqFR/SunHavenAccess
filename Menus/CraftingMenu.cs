using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using Wish;
using SunHavenAccess.Info;
using SunHavenAccess.Patches;
using SunHavenAccess.Speech;
using SunHavenAccess.Util;

namespace SunHavenAccess.Menus
{
    /// <summary>
    /// L'établi en liste : ce qu'on peut fabriquer, avec quoi, et combien.
    ///
    /// CE QUE C'ÉTAIT. L'écran d'artisanat est une grille de vignettes. Chaque recette y montre son
    /// résultat, ses ingrédients en petites cases à côté, et trois boutons pour en fabriquer un,
    /// cinq ou vingt. Tout repose sur le regard : quelle vignette va avec quels ingrédients, et
    /// lesquels on possède. Sans la vue, on ne savait ni ce qu'on pouvait faire, ni ce qu'il
    /// manquait, ni combien on allait en produire.
    ///
    /// CE QUE C'EST. Une liste de recettes. Chacune dit son résultat et ses ingrédients dans la
    /// même phrase — c'est ce qu'on veut savoir avant de choisir, pas après. Valider une recette
    /// ouvre ce qu'on peut en faire : en fabriquer un, cinq, vingt, ou une quantité qu'on tape.
    ///
    /// LA QUANTITÉ LIBRE existe parce que un, cinq et vingt sont des raccourcis d'interface, pas
    /// des besoins réels : on veut trois planches, ou douze. Le jeu n'offre pas ce champ ; on
    /// enchaîne donc ses propres appels, ce qu'il accepte parfaitement puisque c'est ce que ferait
    /// un joueur en cliquant plusieurs fois.
    ///
    /// AUCUN SONDAGE. `CraftingTable.Interact` prévient à l'ouverture. Les vignettes, elles, sont
    /// construites en différé : on attend qu'elles existent, pendant deux secondes au plus, puis on
    /// renonce en silence plutôt que de guetter indéfiniment.
    /// </summary>
    internal static class CraftingMenu
    {
        private const string OwnerTag = "artisanat";

        /// <summary>Instant limite d'attente des vignettes ; zéro quand on n'attend rien.</summary>
        private static float _waitUntil;

        [HarmonyPatch(typeof(CraftingTable), nameof(CraftingTable.Interact))]
        public static class OpenPatch
        {
            private static void Postfix() =>
                PatchGuard.Run("ArtisanatOuvert", () => _waitUntil = Time.unscaledTime + 2f);
        }

        internal static void Tick()
        {
            if (_waitUntil == 0f) return; // rien en attente : coût nul

            if (Time.unscaledTime > _waitUntil)
            {
                _waitUntil = 0f;
                return;
            }

            List<CraftingPanel> panels = Panels();
            if (panels.Count == 0) return; // pas encore construites, on repasse

            _waitUntil = 0f;
            Open(panels);
        }

        private static List<CraftingPanel> Panels()
        {
            try
            {
                return Object.FindObjectsOfType<CraftingPanel>()
                    .Where(p => p != null && p.gameObject.activeInHierarchy && p.outputImage != null)
                    .OrderBy(p => Label(p))
                    .ToList();
            }
            catch { return new List<CraftingPanel>(); }
        }

        private static void Open(List<CraftingPanel> panels)
        {
            var entries = panels.Select(Describe).ToList();

            ListMenu.Open(Localization.Language.T("Fabrication", "Crafting"), entries,
                chosen =>
                {
                    if (chosen >= 0 && chosen < panels.Count) OpenRecipe(panels, panels[chosen]);
                },
                owner: OwnerTag);
        }

        /// <summary>
        /// Une recette en une phrase : ce qu'elle produit, puis ce qu'elle consomme. L'ordre compte
        /// — on cherche d'abord ce qu'on veut obtenir, et seulement ensuite si l'on peut.
        /// </summary>
        private static string Describe(CraftingPanel panel)
        {
            string output = Label(panel);
            string inputs = Ingredients(panel);

            return string.IsNullOrEmpty(inputs)
                ? output
                : Localization.Language.T($"{output}, avec {inputs}", $"{output}, from {inputs}");
        }

        private static string Label(CraftingPanel panel)
        {
            string named = Name(panel.outputImage);
            if (!string.IsNullOrWhiteSpace(named)) return named;

            // Repli sur le texte affiché : certaines recettes n'ont pas encore chargé leur objet.
            string text = TextUtil.Clean(panel.outputTMP?.text);
            return string.IsNullOrWhiteSpace(text) ? Localization.Language.T("Recette", "Recipe") : text;
        }

        private static string Ingredients(CraftingPanel panel)
        {
            var parts = new[] { panel.input1Image, panel.input2Image, panel.input3Image }
                .Select(Name)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToArray();

            return parts.Length == 0 ? null : string.Join(", ", parts);
        }

        /// <summary>Quantité et nom d'une case, par l'identifiant — la source du jeu, donc en français.</summary>
        private static string Name(ItemImage image)
        {
            try
            {
                if (image == null || !image.gameObject.activeInHierarchy) return null;

                int id = image.Item?.ID() ?? 0;
                if (id == 0) return null;

                string name = ItemNames.Get(id);
                if (string.IsNullOrWhiteSpace(name)) return null;

                return image.Amount > 1 ? $"{image.Amount} {name}" : name;
            }
            catch { return null; }
        }

        private static void OpenRecipe(List<CraftingPanel> all, CraftingPanel panel)
        {
            string label = Label(panel);
            string inputs = Ingredients(panel);
            string time = TextUtil.Clean(panel.craftTimeTMP?.text);

            var actions = new List<string>
            {
                Localization.Language.T("Fabriquer 1", "Craft 1"),
                Localization.Language.T("Fabriquer 5", "Craft 5"),
                Localization.Language.T("Fabriquer 20", "Craft 20"),
                Localization.Language.T("Fabriquer une autre quantité", "Craft another amount"),
                Localization.Language.T("Ingrédients et durée", "Ingredients and time"),
            };

            ListMenu.Open(label, actions,
                chosen =>
                {
                    switch (chosen)
                    {
                        case 0: Craft(panel, 1, label); break;
                        case 1: Craft(panel, 5, label); break;
                        case 2: Craft(panel, 20, label); break;
                        case 3: AskAmount(panel, label); break;
                        default:
                            TolkSpeech.Speak(Localization.Language.T(
                                $"{label}. {(inputs ?? "Aucun ingrédient")}. {time}",
                                $"{label}. {(inputs ?? "No ingredients")}. {time}"), true);
                            OpenRecipe(all, panel);
                            break;
                    }
                },
                onExitUp: () => Open(all),
                owner: OwnerTag);
        }

        private static void AskAmount(CraftingPanel panel, string label)
        {
            TextPrompt.Ask(
                Localization.Language.T($"Combien de {label} ?", $"How many {label}?"),
                null,
                typed =>
                {
                    if (!int.TryParse(typed.Trim(), out int amount) || amount <= 0)
                    {
                        TolkSpeech.Speak(Localization.Language.T(
                            "Ce n'est pas un nombre.", "That is not a number."), true);
                        return;
                    }

                    // Une borne haute, parce qu'une faute de frappe ne doit pas lancer mille
                    // fabrications qu'on ne pourrait plus arrêter.
                    if (amount > 200)
                    {
                        TolkSpeech.Speak(Localization.Language.T(
                            "Deux cents au maximum.", "Two hundred at most."), true);
                        return;
                    }

                    Craft(panel, amount, label);
                });
        }

        private static void Craft(CraftingPanel panel, int amount, string label)
        {
            try
            {
                panel.Craft(amount);
                TolkSpeech.Speak(Localization.Language.T(
                    $"{amount} {label} lancé{(amount > 1 ? "s" : "")}.",
                    $"{amount} {label} started."), true);
            }
            catch
            {
                // Le jeu refuse quand les ingrédients manquent ou que la file est pleine. On le dit
                // plutôt que de laisser croire que c'est parti.
                TolkSpeech.Speak(Localization.Language.T(
                    $"Impossible de fabriquer {label} : ingrédients manquants ou file pleine.",
                    $"Cannot craft {label}: missing ingredients or the queue is full."), true);
            }
        }
    }
}
