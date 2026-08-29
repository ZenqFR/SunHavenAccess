using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Wish;
using SunHavenAccess.Menus;
using SunHavenAccess.Patches;
using SunHavenAccess.Speech;
using SunHavenAccess.Util;

namespace SunHavenAccess.Info
{
    /// <summary>
    /// Les récompenses de quête : savoir ce qu'on reçoit, et choisir quand il y a un choix.
    ///
    /// CE QUI SE PASSAIT. Rendre une quête ouvre une fenêtre de récompenses. Deux sortes s'y
    /// côtoient : celles qu'on reçoit dans tous les cas, et celles entre lesquelles il faut
    /// CHOISIR. Rien de tout cela ne s'entendait. On validait à l'aveugle, sans savoir ce qu'on
    /// gagnait ni qu'un choix venait d'être fait à notre place — et un choix de récompense ne se
    /// refait pas.
    ///
    /// CE QU'ON EN FAIT. La fenêtre s'annonce d'elle-même et s'ouvre en liste : d'abord ce qui est
    /// acquis, ensuite les options entre lesquelles trancher, enfin « Accepter ». Valider une
    /// option la sélectionne et le dit ; valider « Accepter » clôt l'affaire.
    ///
    /// AUCUN SONDAGE. `EnableRewardsPopup` et `DisableRewardsPopup` sont appelées par le jeu au
    /// moment exact où la fenêtre paraît et disparaît. On écoute plutôt que de demander à chaque
    /// image si elle est là — la règle du mod depuis qu'un guet permanent a fait tomber le jeu à
    /// vingt-cinq images par seconde.
    /// </summary>
    internal static class RewardsMenu
    {
        private const string OwnerTag = "recompenses";

        private static FieldInfo _choiceField;
        private static FieldInfo _guaranteedField;

        [HarmonyPatch(typeof(QuestRewards), nameof(QuestRewards.EnableRewardsPopup))]
        public static class OpenPatch
        {
            private static void Postfix(QuestRewards __instance) =>
                PatchGuard.Run("RecompensesOuvertes", () => Open(__instance));
        }

        [HarmonyPatch(typeof(QuestRewards), nameof(QuestRewards.DisableRewardsPopup))]
        public static class ClosePatch
        {
            private static void Postfix() =>
                PatchGuard.Run("RecompensesFermees", () => ListMenu.CloseIfOwner(OwnerTag, false));
        }

        private static void Open(QuestRewards rewards)
        {
            List<ItemImage> guaranteed = Images(rewards, ref _guaranteedField, "guaranteedRewards");
            List<ItemImage> choices = Images(rewards, ref _choiceField, "choiceRewards");

            var entries = new List<string>();
            var actions = new List<System.Action>();

            foreach (ItemImage image in guaranteed)
            {
                string label = Describe(image);
                if (label == null) continue;

                // Acquis : on le dit, et valider ne fait que le redire. Il n'y a rien à décider,
                // et laisser croire le contraire ferait chercher un choix qui n'existe pas.
                entries.Add(Localization.Language.T($"Reçu : {label}", $"Received: {label}"));
                actions.Add(() => TolkSpeech.Speak(label, true));
            }

            foreach (ItemImage image in choices)
            {
                string label = Describe(image);
                if (label == null) continue;

                ItemImage target = image;
                entries.Add(Localization.Language.T($"Au choix : {label}", $"Choice: {label}"));
                actions.Add(() =>
                {
                    try
                    {
                        rewards.SetSelectedItem(target);
                        TolkSpeech.Speak(Localization.Language.T($"{label} choisi.", $"{label} selected."), true);
                    }
                    catch
                    {
                        TolkSpeech.Speak(Localization.Language.T(
                            "Impossible de choisir cette récompense.", "Cannot select this reward."), true);
                    }
                });
            }

            entries.Add(Localization.Language.T("Accepter les récompenses", "Accept the rewards"));
            actions.Add(() =>
            {
                try { rewards.AcceptRewards(); }
                catch
                {
                    TolkSpeech.Speak(Localization.Language.T(
                        "Impossible d'accepter pour le moment.", "Cannot accept right now."), true);
                }
            });

            // Un choix à faire se dit d'emblée : c'est la seule chose qui distingue cette fenêtre
            // d'une simple confirmation, et la manquer coûte une récompense qu'on ne reprendra pas.
            if (choices.Count > 0)
            {
                TolkSpeech.Speak(Localization.Language.T(
                    $"Récompenses de quête, {choices.Count} au choix.",
                    $"Quest rewards, {choices.Count} to choose from."), true);
            }

            ListMenu.Open(Localization.Language.T("Récompenses", "Rewards"), entries,
                chosen =>
                {
                    if (chosen >= 0 && chosen < actions.Count) actions[chosen]();

                    // Consulter ne doit pas faire sortir : tant que la fenêtre du jeu est là, la
                    // liste doit rester sous les flèches. Seul « Accepter » la referme, et c'est le
                    // jeu qui s'en charge en fermant sa fenêtre.
                    if (chosen < entries.Count - 1) Open(rewards);
                },
                owner: OwnerTag,
                announce: choices.Count == 0);
        }

        /// <summary>
        /// Le contenu d'une case de récompense : sa quantité et son nom, traduit par le jeu via son
        /// identifiant — la même source que partout ailleurs dans le mod.
        /// </summary>
        private static string Describe(ItemImage image)
        {
            try
            {
                int id = image?.Item?.ID() ?? 0;
                if (id == 0) return null;

                string name = ItemNames.Get(id);
                if (string.IsNullOrWhiteSpace(name)) return null;

                int amount = image.Amount;
                return amount > 1 ? $"{amount} {name}" : name;
            }
            catch { return null; }
        }

        private static List<ItemImage> Images(QuestRewards rewards, ref FieldInfo cached, string fieldName)
        {
            try
            {
                cached ??= typeof(QuestRewards).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);

                // Les cases inutilisées restent dans la liste, simplement désactivées : les garder
                // ferait annoncer des récompenses vides.
                return (cached?.GetValue(rewards) as List<ItemImage>)?
                    .Where(i => i != null && i.gameObject.activeInHierarchy)
                    .ToList() ?? new List<ItemImage>();
            }
            catch { return new List<ItemImage>(); }
        }
    }
}
