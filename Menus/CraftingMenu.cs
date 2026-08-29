using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Wish;
using SunHavenAccess.Info;
using SunHavenAccess.Patches;
using SunHavenAccess.Speech;

namespace SunHavenAccess.Menus
{
    /// <summary>
    /// L'établi en liste : ce qu'on peut fabriquer, avec quoi, et combien.
    ///
    /// CE QUE C'ÉTAIT. L'écran d'artisanat est une grille de vignettes. Chaque recette y montre son
    /// résultat, ses ingrédients en petites cases à côté, et trois boutons pour en fabriquer un,
    /// cinq ou vingt. Tout repose sur le regard : quelle vignette va avec quels ingrédients, et
    /// lesquels on possède.
    ///
    /// ON LIT LES RECETTES, PAS LES VIGNETTES. La première version cherchait les `CraftingPanel`
    /// affichés — et n'en trouvait aucun : cette liste est VIRTUELLE, le jeu ne construit que les
    /// quelques vignettes réellement visibles à l'écran et les recycle en défilant. Chercher des
    /// vignettes, c'était chercher un affichage plutôt qu'un contenu. Les recettes, elles, sont
    /// toutes là, dans l'établi, dès son ouverture.
    ///
    /// Cela vaut mieux à tous égards : la liste est complète même sans défiler, elle ne dépend
    /// d'aucun détail de mise en page, et `CraftingTable.Craft` comme `CanCraft` sont publiques —
    /// on fabrique donc par le même chemin que le jeu, et l'on sait dire ce qui est réalisable.
    ///
    /// LA QUANTITÉ LIBRE existe parce qu'un, cinq et vingt sont des raccourcis d'interface, pas des
    /// besoins réels : on veut trois planches, ou douze.
    /// </summary>
    internal static class CraftingMenu
    {
        private const string OwnerTag = "artisanat";

        private static CraftingTable _table;
        private static float _waitUntil;
        private static FieldInfo _recipesField;

        [HarmonyPatch(typeof(CraftingTable), nameof(CraftingTable.Interact))]
        public static class OpenPatch
        {
            private static void Postfix(CraftingTable __instance) =>
                PatchGuard.Run("ArtisanatOuvert", () =>
                {
                    _table = __instance;
                    _waitUntil = Time.unscaledTime + 2f;
                });
        }

        internal static void Tick()
        {
            if (_waitUntil == 0f) return; // rien en attente : coût nul

            if (Time.unscaledTime > _waitUntil || _table == null)
            {
                Give();
                return;
            }

            List<Recipe> recipes = Recipes(_table);
            if (recipes.Count == 0) return; // pas encore chargées, on repasse

            CraftingTable table = _table;
            _waitUntil = 0f;
            _table = null;

            Open(table, recipes);
        }

        /// <summary>
        /// On renonce en le DISANT. Un établi qui s'ouvre en silence laisse croire que le mod n'a
        /// rien vu, et l'on reste devant un écran muet sans savoir s'il faut insister.
        /// </summary>
        private static void Give()
        {
            bool wasWaiting = _waitUntil != 0f;
            _waitUntil = 0f;
            _table = null;

            if (!wasWaiting) return;

            Plugin.Log?.LogInfo("Artisanat : aucune recette lue sur cet établi.");
            TolkSpeech.Speak(Localization.Language.T(
                "Aucune recette lue sur cet établi.", "No recipe read on this table."), true);
        }

        /// <summary>
        /// Les recettes de l'établi. Le champ est privé mais c'est la source complète ; la méthode
        /// publique de tri, elle, joue un son à chaque appel et rend une liste vide dans son cas
        /// par défaut — inutilisable pour simplement lire.
        /// </summary>
        private static List<Recipe> Recipes(CraftingTable table)
        {
            try
            {
                _recipesField ??= typeof(CraftingTable).GetField("_craftingRecipes",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                return (_recipesField?.GetValue(table) as List<Recipe>)?
                    .Where(r => r != null && r.output2 != null)
                    .ToList() ?? new List<Recipe>();
            }
            catch { return new List<Recipe>(); }
        }

        private static void Open(CraftingTable table, List<Recipe> recipes)
        {
            // Le réalisable d'abord : c'est ce qu'on cherche neuf fois sur dix, et le reste n'est
            // qu'un pense-bête pour la prochaine fois.
            var ordered = recipes
                .OrderByDescending(r => CanMake(table, r))
                .ThenBy(Output)
                .ToList();

            var entries = ordered.Select(r => Describe(table, r)).ToList();

            ListMenu.Open(Localization.Language.T("Fabrication", "Crafting"), entries,
                chosen =>
                {
                    if (chosen >= 0 && chosen < ordered.Count) OpenRecipe(table, ordered, ordered[chosen]);
                },
                owner: OwnerTag);
        }

        private static bool CanMake(CraftingTable table, Recipe recipe)
        {
            try { return table.CanCraft(recipe, 1); }
            catch { return false; }
        }

        /// <summary>
        /// Une recette en une phrase : ce qu'elle produit, si on peut la faire, et avec quoi.
        /// L'ordre suit la décision qu'on prend — quoi, puis est-ce possible, puis à quel prix.
        /// </summary>
        private static string Describe(CraftingTable table, Recipe recipe)
        {
            string output = Output(recipe);
            string inputs = Ingredients(recipe);

            string state = CanMake(table, recipe)
                ? string.Empty
                : Localization.Language.T(" — ingrédients manquants", " — missing ingredients");

            return string.IsNullOrEmpty(inputs)
                ? output + state
                : Localization.Language.T($"{output}{state}, avec {inputs}", $"{output}{state}, from {inputs}");
        }

        private static string Output(Recipe recipe)
        {
            try
            {
                string name = ItemNames.Get(recipe.output2.id);
                if (string.IsNullOrWhiteSpace(name)) return Localization.Language.T("Recette", "Recipe");

                int amount = recipe.output2.amount;
                return amount > 1 ? $"{amount} {name}" : name;
            }
            catch { return Localization.Language.T("Recette", "Recipe"); }
        }

        private static string Ingredients(Recipe recipe)
        {
            try
            {
                var parts = (recipe.Input ?? new List<SerializedItemDataNamedAmount>())
                    .Where(i => i != null)
                    .Select(i =>
                    {
                        string name = ItemNames.Get(i.id);

                        // Le mana et la vie sont des ingrédients comme les autres pour le jeu, mais
                        // sans objet derrière : leur nom écrit est alors la seule source.
                        if (string.IsNullOrWhiteSpace(name)) name = i.name;
                        if (string.IsNullOrWhiteSpace(name)) return null;

                        return i.amount > 1 ? $"{i.amount} {name}" : name;
                    })
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToArray();

                return parts.Length == 0 ? null : string.Join(", ", parts);
            }
            catch { return null; }
        }

        /// <summary>
        /// FABRIQUER N'EST PAS PARTIR.
        ///
        /// Une liste se referme quand on valide, ce qui est juste pour une action qui emmène
        /// ailleurs. Fabriquer n'emmène nulle part : on enchaîne presque toujours — cinq planches,
        /// puis cinq autres, puis on regarde ce qu'il reste. Le menu disparaissait à chaque lot et
        /// il fallait rouvrir l'établi. Signalé en jeu.
        ///
        /// On remet donc la recette EN SILENCE après chaque fabrication : elle est de nouveau sous
        /// les flèches, sans parler par-dessus l'annonce de ce qui vient d'être lancé.
        /// </summary>
        private static void OpenRecipe(CraftingTable table, List<Recipe> all, Recipe recipe) =>
            OpenRecipe(table, all, recipe, announce: true);

        private static void Reopen(CraftingTable table, List<Recipe> all, Recipe recipe) =>
            OpenRecipe(table, all, recipe, announce: false);

        private static void OpenRecipe(CraftingTable table, List<Recipe> all, Recipe recipe, bool announce)
        {
            string label = Output(recipe);
            string inputs = Ingredients(recipe);

            var actions = new List<string>
            {
                Localization.Language.T("Fabriquer 1", "Craft 1"),
                Localization.Language.T("Fabriquer 5", "Craft 5"),
                Localization.Language.T("Fabriquer 20", "Craft 20"),
                Localization.Language.T("Fabriquer une autre quantité", "Craft another amount"),
                Localization.Language.T("Ingrédients", "Ingredients"),
            };

            ListMenu.Open(label, actions,
                chosen =>
                {
                    switch (chosen)
                    {
                        case 0: Craft(table, recipe, 1, label); break;
                        case 1: Craft(table, recipe, 5, label); break;
                        case 2: Craft(table, recipe, 20, label); break;

                        // La saisie de quantité remet la recette elle-même, une fois le nombre
                        // connu : la rouvrir ici la refermerait aussitôt sous la saisie.
                        case 3: AskAmount(table, all, recipe, label); return;

                        default:
                            TolkSpeech.Speak(inputs ?? Localization.Language.T("Aucun ingrédient.", "No ingredients."), true);
                            break;
                    }

                    Reopen(table, all, recipe);
                },
                onExitUp: () => Open(table, all),
                owner: OwnerTag,
                announce: announce);
        }

        private static void AskAmount(CraftingTable table, List<Recipe> all, Recipe recipe, string label)
        {
            TextPrompt.Ask(
                Localization.Language.T($"Combien de {label} ?", $"How many {label}?"),
                null,
                typed =>
                {
                    // Quoi qu'il advienne — nombre valide, faute de frappe, refus — on revient à la
                    // recette. Se retrouver hors de l'établi après une faute serait doublement puni.
                    if (!int.TryParse(typed.Trim(), out int amount) || amount <= 0)
                    {
                        TolkSpeech.Speak(Localization.Language.T(
                            "Ce n'est pas un nombre.", "That is not a number."), true);
                        Reopen(table, all, recipe);
                        return;
                    }

                    // Une borne haute : une faute de frappe ne doit pas lancer mille fabrications
                    // qu'on ne pourrait plus arrêter.
                    if (amount > 200)
                    {
                        TolkSpeech.Speak(Localization.Language.T(
                            "Deux cents au maximum.", "Two hundred at most."), true);
                        Reopen(table, all, recipe);
                        return;
                    }

                    Craft(table, recipe, amount, label);
                    Reopen(table, all, recipe);
                });
        }

        private static void Craft(CraftingTable table, Recipe recipe, int amount, string label)
        {
            // On DEMANDE d'abord, parce que le jeu refuse en silence : sans cela, on croirait avoir
            // lancé une fabrication qui n'a jamais commencé.
            if (!CanMake(table, recipe) || !CanMake(table, recipe, amount))
            {
                TolkSpeech.Speak(Localization.Language.T(
                    $"Impossible de fabriquer {amount} {label} : ingrédients insuffisants.",
                    $"Cannot craft {amount} {label}: not enough ingredients."), true);
                return;
            }

            try
            {
                table.Craft(recipe, amount);
                TolkSpeech.Speak(Localization.Language.T(
                    $"{amount} {label} lancé{(amount > 1 ? "s" : "")}.",
                    $"{amount} {label} started."), true);
            }
            catch
            {
                TolkSpeech.Speak(Localization.Language.T(
                    $"L'établi a refusé {label}.", $"The table refused {label}."), true);
            }
        }

        private static bool CanMake(CraftingTable table, Recipe recipe, int amount)
        {
            try { return table.CanCraft(recipe, amount); }
            catch { return false; }
        }
    }
}
