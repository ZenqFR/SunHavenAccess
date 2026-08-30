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

        /// <summary>Terme de recherche en cours ; vide quand on voit tout.</summary>
        private static string _filter = string.Empty;

        private static void Open(CraftingTable table, List<Recipe> recipes) =>
            Open(table, recipes, announce: true);

        /// <summary>
        /// UN ÉTABLI PEUT PORTER DES CENTAINES DE RECETTES.
        ///
        /// Les parcourir aux flèches pour en trouver une dont on connaît le nom, c'est écouter
        /// cent annonces pour une seule qui compte. Un joueur voyant balaie la grille du regard et
        /// s'arrête ; sans la vue, il n'existait aucun équivalent. Le jeu a d'ailleurs son propre
        /// champ de recherche, dont on ne pouvait pas se servir.
        ///
        /// La recherche est donc la PREMIÈRE ligne de la liste, avant les recettes : c'est le geste
        /// qu'on veut faire en arrivant, pas au bout de trois minutes de défilement. Elle porte sur
        /// le résultat ET sur les ingrédients, ce qui permet aussi de demander « que puis-je faire
        /// avec du bois ».
        /// </summary>
        private static void Open(CraftingTable table, List<Recipe> recipes, bool announce)
        {
            // Le réalisable d'abord : c'est ce qu'on cherche neuf fois sur dix, et le reste n'est
            // qu'un pense-bête pour la prochaine fois.
            var ordered = recipes
                .Where(r => Matches(table, r))
                .OrderByDescending(r => CanMake(table, r))
                .ThenBy(Output)
                .ToList();

            var entries = new List<string>
            {
                string.IsNullOrEmpty(_filter)
                    ? Localization.Language.T("Rechercher une recette", "Search for a recipe")
                    : Localization.Language.T($"Recherche : {_filter}. Entrée pour changer, Ctrl+Entrée pour tout revoir",
                                              $"Search: {_filter}. Enter to change, Ctrl+Enter to show all"),
            };
            entries.AddRange(ordered.Select(r => Describe(table, r)));

            // Le terme cherché passe DANS le titre, plutôt qu'en phrase séparée suivie d'une liste
            // muette : une liste qui ne s'annonce pas est indiscernable d'une liste absente, comme
            // la boutique vient de le montrer. Une seule phrase dit le contexte et le contenu.
            string title = string.IsNullOrEmpty(_filter)
                ? Localization.Language.T("Fabrication", "Crafting")
                : Localization.Language.T($"Fabrication, recherche {_filter}", $"Crafting, search {_filter}");

            ListMenu.Open(title, entries,
                chosen =>
                {
                    if (chosen == 0) { AskFilter(table, recipes); return; }

                    int index = chosen - 1;
                    if (index >= 0 && index < ordered.Count) OpenRecipe(table, ordered, ordered[index]);
                },
                owner: OwnerTag,
                announce: announce);
        }

        /// <summary>
        /// La recherche porte sur le nom du résultat ET sur celui des ingrédients : « que puis-je
        /// faire avec du cuivre » est une question aussi légitime que « où est la planche ».
        /// Accents et casse ignorés — taper « ble » doit trouver « Blé ».
        /// </summary>
        private static bool Matches(CraftingTable table, Recipe recipe)
        {
            if (string.IsNullOrEmpty(_filter)) return true;

            try
            {
                if (Flatten(Output(recipe)).Contains(_filter)) return true;

                foreach (SerializedItemDataNamedAmount input in recipe.Input ?? new List<SerializedItemDataNamedAmount>())
                {
                    if (input == null) continue;
                    string name = ItemNames.Get(input.id) ?? input.name;
                    if (!string.IsNullOrEmpty(name) && Flatten(name).Contains(_filter)) return true;
                }
            }
            catch { }

            return false;
        }

        private static string Flatten(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;

            string normalised = s.Normalize(System.Text.NormalizationForm.FormD);
            var builder = new System.Text.StringBuilder(normalised.Length);

            foreach (char c in normalised)
            {
                // On retire les accents plutôt que de les exiger : personne ne tape « Blé » avec
                // son accent dans un champ de recherche, et surtout pas en écoutant.
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                    != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(char.ToLowerInvariant(c));
                }
            }

            return builder.ToString();
        }

        private static void AskFilter(CraftingTable table, List<Recipe> recipes)
        {
            TextPrompt.Ask(
                Localization.Language.T("Rechercher quoi ? Entrée vide pour tout revoir.",
                                        "Search for what? Empty to show all."),
                null,
                typed =>
                {
                    _filter = Flatten(typed);
                    Open(table, recipes, announce: true);
                },
                allowEmpty: true);
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
            string inputs = Ingredients(table, recipe);

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

        /// <summary>
        /// Les ingrédients, avec CE QU'ON A SUR CE QU'IL FAUT.
        ///
        /// « Ingrédients manquants » dit qu'on ne peut pas ; cela ne dit pas ce qui manque, ni
        /// combien. On repartait donc chercher sans savoir quoi, ni quand s'arrêter. L'écran du jeu
        /// l'affiche pourtant, en petit sous chaque case : « 3/5 ». Signalé en jeu.
        ///
        /// On reprend la règle du jeu à la lettre plutôt que d'en inventer une : le mana et la vie
        /// se lisent sur le joueur, les monnaies dans la bourse, le reste se compte dans TOUS les
        /// inventaires que l'établi accepte — y compris les coffres voisins quand l'option est
        /// active. Une règle maison aurait annoncé « il vous en manque » devant un coffre plein.
        /// </summary>
        private static string Ingredients(CraftingTable table, Recipe recipe)
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

                        int needed = Needed(recipe, i);
                        int owned = Owned(table, i);

                        // On ne dit « x sur y » que lorsqu'il en manque : quand tout est là, le
                        // compte n'apprend rien et double la longueur de chaque ligne.
                        return owned < needed
                            ? Localization.Language.T($"{name} {owned} sur {needed}", $"{name} {owned} of {needed}")
                            : (needed > 1 ? $"{needed} {name}" : name);
                    })
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToArray();

                return parts.Length == 0 ? null : string.Join(", ", parts);
            }
            catch { return null; }
        }

        /// <summary>Ce que le jeu dit du résultat de cette recette.</summary>
        private static string Description(Recipe recipe)
        {
            try { return ItemNames.Description(recipe.output2.id); }
            catch { return null; }
        }

        private static int Needed(Recipe recipe, SerializedItemDataNamedAmount input)
        {
            try { return recipe.ModifiedAmount(input.amount, input.id, recipe.output2.id, recipe.isFood); }
            catch { return input.amount; }
        }

        /// <summary>Ce qu'on possède de cet ingrédient, selon la règle exacte du jeu.</summary>
        private static int Owned(CraftingTable table, SerializedItemDataNamedAmount input)
        {
            try
            {
                if (input.name == "Mana") return (int)Player.Instance.Mana;
                if (input.name == "Health") return (int)Player.Instance.Health;

                switch (input.id)
                {
                    case 60000: return GameSave.Coins;
                    case 60001: return GameSave.Orbs;
                    case 60002: return GameSave.Tickets;
                }

                int total = 0;
                foreach (Inventory inventory in table.allInventories)
                {
                    if (inventory != null) total += inventory.GetAmount(input.id);
                }
                return total;
            }
            catch { return 0; }
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
            string inputs = Ingredients(table, recipe);

            // LA DESCRIPTION DE CE QU'ON FABRIQUE.
            //
            // Le nom d'une recette ne dit pas à quoi sert ce qu'elle produit. Un joueur voyant
            // survole la vignette et lit l'infobulle ; c'est ainsi qu'on apprend qu'un objet
            // restaure de la vie, augmente une statistique, ou n'est qu'un ingrédient pour la
            // suite. Sans cela, on fabrique sans savoir pourquoi.
            //
            // Elle est annoncée D'EMBLÉE en ouvrant la recette, pas cachée derrière une ligne de
            // plus : c'est ce qu'on veut savoir au moment où l'on hésite, et une hésitation ne
            // supporte pas trois gestes.
            string description = Description(recipe);
            if (!string.IsNullOrWhiteSpace(description)) TolkSpeech.Speak(description, true);

            var actions = new List<string>
            {
                Localization.Language.T("Fabriquer 1", "Craft 1"),
                Localization.Language.T("Fabriquer 5", "Craft 5"),
                Localization.Language.T("Fabriquer 20", "Craft 20"),
                Localization.Language.T("Fabriquer une autre quantité", "Craft another amount"),
                Localization.Language.T("Ingrédients", "Ingredients"),
                Localization.Language.T("Description", "Description"),
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

                        case 4:
                            TolkSpeech.Speak(inputs ?? Localization.Language.T("Aucun ingrédient.", "No ingredients."), true);
                            break;

                        default:
                            TolkSpeech.Speak(Description(recipe)
                                ?? Localization.Language.T("Pas de description.", "No description."), true);
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
