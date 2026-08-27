using System.Collections.Generic;
using Wish;
using SunHavenAccess.Speech;

namespace SunHavenAccess.Info
{
    /// <summary>
    /// Niveaux de compétence (`Wish.ProfessionType` : Combat, Farming, Fishing, Mining,
    /// Exploration — "Exploration" couvre aussi la coupe de bois côté jeu). Données publiques
    /// (`Wish.CharacterData.Professions`, `Wish.Profession.level`/`experience`), et surtout
    /// `Profession.GetLevelPercentFromExp` : une méthode statique du jeu qui formate déjà le
    /// texte de progression ("1234/5678") tout seule — on la réutilise telle quelle plutôt que
    /// de recalculer, comme pour le texte de quête ou de courrier ailleurs dans le mod.
    /// Ne couvre PAS l'arbre de compétences lui-même (dépense de points sur une grille de
    /// nœuds, touche K par défaut du jeu) : une grille 2D spatiale, un chantier à part entière,
    /// pas encore commencé.
    /// </summary>
    public static class ProfessionAnnouncer
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

            Dictionary<ProfessionType, Profession> professions = GameSave.CurrentCharacter.Professions;
            var parts = new List<string>();

            foreach (KeyValuePair<ProfessionType, string> entry in Names)
            {
                if (!professions.TryGetValue(entry.Key, out Profession profession)) continue;

                Profession.GetLevelPercentFromExp(profession.experience, out string expString);
                // Le nom du métier passe par la table de traduction, qui les porte déjà pour
                // l'arbre de compétences : une seule liste de noms pour tout le mod.
                string name = Localization.Translator.Translate(entry.Value);
                parts.Add(Localization.Language.T(
                    $"{name}, niveau {profession.level} ({expString}).",
                    $"{name}, level {profession.level} ({expString})."));
            }

            if (parts.Count == 0)
            {
                TolkSpeech.Speak("Aucune donnée de compétence disponible.", true);
                return;
            }

            TolkSpeech.Speak(string.Join(" ", parts), true);
        }
    }
}
