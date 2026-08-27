using System.Collections.Generic;
using Wish;
using SunHavenAccess.Speech;

namespace SunHavenAccess.Info
{
    /// <summary>
    /// Résumé des points de compétence disponibles par arbre (`Wish.Skills`, méthodes PUBLIQUES
    /// statiques : NumberOfSkillPoints = total gagné, NumberOfSkillPointsSpent = déjà dépensé).
    ///
    /// Ne remplace PAS une vraie navigation dans la grille de nœuds elle-même. Contrairement à
    /// la carte du monde et à l'apparence en création de personnage (voir MapNavigator/
    /// CharacterAppearanceNavigator), `Wish.SkillNode` a un champ `navElement`
    /// (`Wish.NavigationElement`) qui AJOUTE lui-même un composant `Selectable` nu sur son
    /// GameObject au démarrage (vu en décompilant NavigationElement.Start) — ce qui le rend
    /// probablement déjà repérable par le scan générique de MenuNavigator
    /// (`FindObjectsOfType&lt;Selectable&gt;()`), sans le travail dédié qu'a demandé la carte.
    /// `Wish.LocationName`/`ClothingImageButton` n'ont PAS cette échappatoire (confirmé pour
    /// LocationName ; ClothingImageButton EN a un aussi en fait, donc peut-être déjà partiellement
    /// couvert lui aussi — le travail dédié qu'on lui a donné reste plus fiable, lui, puisqu'il
    /// lit les données du jeu directement plutôt que d'espérer que la lecture générique tombe
    /// juste). Jamais vérifié en jeu si la navigation aux flèches marche vraiment ici — cette
    /// touche est un premier pas sûr en attendant confirmation, pas une solution complète.
    /// </summary>
    public static class SkillPointsAnnouncer
    {
        private static readonly Dictionary<ProfessionType, string> Names = new Dictionary<ProfessionType, string>
        {
            { ProfessionType.Combat, "Combat" },
            { ProfessionType.Farming, "Agriculture" },
            { ProfessionType.Fishing, "Pêche" },
            { ProfessionType.Mining, "Minage" },
            { ProfessionType.Exploration, "Exploration" },
        };

        public static void AnnounceAll()
        {
            if (SingletonBehaviour<GameSave>.Instance == null || GameSave.CurrentCharacter == null)
            {
                TolkSpeech.Speak("Le jeu n'est pas encore chargé.", true);
                return;
            }

            var parts = new List<string>();
            foreach (KeyValuePair<ProfessionType, string> entry in Names)
            {
                int total = Skills.NumberOfSkillPoints(entry.Key);
                int spent = Skills.NumberOfSkillPointsSpent(entry.Key);
                int available = total - spent;
                string name = Localization.Translator.Translate(entry.Value);
                parts.Add(Localization.Language.T(
                    $"{name} : {available} point{(available > 1 ? "s" : "")} disponible{(available > 1 ? "s" : "")} sur {total}.",
                    $"{name}: {available} point{(available > 1 ? "s" : "")} available of {total}."));
            }

            TolkSpeech.Speak(string.Join(" ", parts), true);
        }
    }
}
