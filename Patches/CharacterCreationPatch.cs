using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TMPro;
using Wish;
using SunHavenAccess.Speech;
using SunHavenAccess.Util;

namespace SunHavenAccess.Patches
{
    /// <summary>
    /// Écran de création de personnage (`Wish.NewCharacterCreator`) : un écran très visuel
    /// (grilles de vignettes pour corps/cheveux/yeux/visage/torse/jambes/tête/ailes/queue,
    /// sélecteurs de couleur) — potentiellement un vrai bloqueur, personne ne peut commencer une
    /// partie sans passer par là. Pas de solution complète pour l'instant (naviguer la grille
    /// d'apparence demanderait un vrai système spatial dédié, jamais testé sans retour humain).
    /// Première étape : annoncer automatiquement la race et sa description/capacité à chaque
    /// changement (`SetRace`, méthode PUBLIQUE), au moins aussi lisible que le reste du jeu pour
    /// cette partie précise de l'écran. `raceDescriptionText`/`raceAbilityText` sont des champs
    /// PRIVÉS (TextMeshProUGUI) : lus par réflexion, comme ailleurs dans le mod.
    /// </summary>
    [HarmonyPatch(typeof(NewCharacterCreator), nameof(NewCharacterCreator.SetRace), new[] { typeof(Race), typeof(int) })]
    public static class CharacterCreationPatch
    {
        private static readonly Dictionary<Race, string> RaceNames = new Dictionary<Race, string>
        {
            { Race.Human, "Humain" },
            { Race.Elf, "Elfe" },
            { Race.Amari, "Amari" },
            { Race.Naga, "Naga" },
            { Race.Elemental, "Élémentaire" },
            { Race.Angel, "Ange" },
            { Race.Demon, "Démon" },
        };

        private static FieldInfo _descriptionField;
        private static FieldInfo _abilityField;

        private static void Postfix(NewCharacterCreator __instance, Race race)
        {
            _descriptionField ??= typeof(NewCharacterCreator).GetField("raceDescriptionText", BindingFlags.NonPublic | BindingFlags.Instance);
            _abilityField ??= typeof(NewCharacterCreator).GetField("raceAbilityText", BindingFlags.NonPublic | BindingFlags.Instance);

            string raceName = SunHavenAccess.Localization.Language.IsEnglish
                ? race.ToString() // les noms de races sont déjà des mots anglais
                : (RaceNames.TryGetValue(race, out string name) ? name : race.ToString());
            string description = TextUtil.Clean((_descriptionField?.GetValue(__instance) as TextMeshProUGUI)?.text);
            string ability = TextUtil.Clean((_abilityField?.GetValue(__instance) as TextMeshProUGUI)?.text);

            var parts = new List<string> { raceName };
            if (!string.IsNullOrWhiteSpace(description)) parts.Add(description);
            if (!string.IsNullOrWhiteSpace(ability)) parts.Add(ability);

            TolkSpeech.Speak(string.Join(". ", parts) + ".", interrupt: true);
        }
    }
}
