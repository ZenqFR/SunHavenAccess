using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Wish;
using SunHavenAccess.Menus;
using SunHavenAccess.Navigation;
using SunHavenAccess.Speech;
using SunHavenAccess.Util;

namespace SunHavenAccess.Info
{
    /// <summary>
    /// Les quêtes, et surtout : comment s'y rendre.
    ///
    /// CE QUI MANQUAIT. Le mod savait déjà réciter les quêtes en cours. Mais une quête ne dit pas
    /// seulement quoi faire, elle dit OÙ — « apporte ceci à Wesley », « rends-toi au musée ». Pour
    /// qui voit, la carte pose un marqueur et le reste est de la marche. Pour qui n'y voit pas, ce
    /// marqueur n'existe pas : on savait ce qu'il fallait faire sans avoir aucun moyen d'y aller.
    ///
    /// CE QUE LE JEU RANGE DÉJÀ. `QuestAsset` porte trois champs qui règlent tout : `turnInMap` (la
    /// zone), `turnInLocation` (les coordonnées exactes) et `npcToTurnInTo` (la personne). Le jeu
    /// s'en sert pour poser son marqueur ; on s'en sert pour marcher. Rien à deviner, rien à
    /// écrire à la main, et cela restera juste quand le jeu ajoutera des quêtes.
    ///
    /// COMMENT ÇA S'UTILISE. La touche des quêtes ouvre la liste ; valider une quête ouvre ce
    /// qu'on peut en faire — s'y rendre, réécouter la description, connaître la progression.
    /// « S'y rendre » traverse les zones s'il le faut et s'arrête sur le point exact, pas au seuil.
    ///
    /// CE QU'ON DIT PLUTÔT QUE DE FAIRE SEMBLANT. Une quête sans lieu de rendu — tuer des
    /// créatures, récolter — n'a nulle part où aller, et on le dit. Un lieu jamais visité non plus,
    /// puisque le plan du monde ne connaît que ce qui a été exploré.
    /// </summary>
    internal static class QuestMenu
    {
        private const string OwnerTag = "quetes";

        internal static void Open()
        {
            Player player = Player.Instance;
            List<QuestBundle> quests = player?.QuestList?.quests;

            if (quests == null || quests.Count == 0)
            {
                TolkSpeech.Speak(Localization.Language.T("Aucune quête active.", "No active quest."), true);
                return;
            }

            var usable = quests.Where(b => b?.quest?.questAsset != null).ToList();
            if (usable.Count == 0)
            {
                TolkSpeech.Speak(Localization.Language.T("Aucune quête active.", "No active quest."), true);
                return;
            }

            var labels = usable.Select(b => Label(b)).ToList();
            ListMenu.Open(Localization.Language.T("Quêtes", "Quests"), labels,
                chosen => OpenQuest(usable[chosen]), owner: OwnerTag);
        }

        /// <summary>
        /// L'intitulé d'une quête dans la liste : son nom, et un mot sur ce qui l'attend. Savoir
        /// AVANT d'entrer qu'une quête a un lieu de rendu évite d'ouvrir dix quêtes pour trouver
        /// celle où l'on peut aller quelque part.
        /// </summary>
        private static string Label(QuestBundle bundle)
        {
            QuestAsset asset = bundle.quest.questAsset;
            string name = TextUtil.Clean(asset.LocalizedQuestName);
            if (string.IsNullOrWhiteSpace(name)) name = Localization.Language.T("Quête", "Quest");

            string who = TurnInName(asset);
            return string.IsNullOrWhiteSpace(who)
                ? name
                : $"{name}, {Localization.Language.T("à rendre à", "hand in to")} {who}";
        }

        private static void OpenQuest(QuestBundle bundle)
        {
            QuestAsset asset = bundle.quest.questAsset;
            string name = TextUtil.Clean(asset.LocalizedQuestName);
            bool hasDestination = !string.IsNullOrWhiteSpace(asset.turnInMap);

            var actions = new List<string>
            {
                hasDestination
                    ? Localization.Language.T("S'y rendre", "Go there")
                    : Localization.Language.T("S'y rendre : cette quête n'a pas de lieu de rendu",
                                              "Go there: this quest has no hand-in place"),
                Localization.Language.T("Description", "Description"),
                Localization.Language.T("Progression", "Progress"),
            };

            ListMenu.Open(string.IsNullOrWhiteSpace(name) ? Localization.Language.T("Quête", "Quest") : name,
                actions,
                chosen =>
                {
                    switch (chosen)
                    {
                        case 0: GoTo(asset); break;
                        case 1: TolkSpeech.Speak(Description(bundle, asset), true); break;
                        default: TolkSpeech.Speak(Progress(bundle), true); break;
                    }
                },
                onExitUp: Open,
                owner: OwnerTag);
        }

        /// <summary>
        /// Marche jusqu'au lieu de rendu, en traversant les zones s'il le faut.
        ///
        /// `turnInLocation` est une position dans la zone `turnInMap`. On la donne au trajet comme
        /// destination finale : on arrive donc SUR le point, pas à l'entrée de la ville. C'est la
        /// différence entre « tu es dans le bon quartier » et « tu y es ».
        /// </summary>
        private static void GoTo(QuestAsset asset)
        {
            if (string.IsNullOrWhiteSpace(asset.turnInMap))
            {
                TolkSpeech.Speak(Localization.Language.T(
                    "Cette quête n'indique aucun lieu de rendu : elle se termine en faisant ce qu'elle demande.",
                    "This quest gives no hand-in place: it completes by doing what it asks."), true);
                return;
            }

            var target = new Vector3(asset.turnInLocation.x, asset.turnInLocation.y, 0f);
            string label = TurnInName(asset);
            if (string.IsNullOrWhiteSpace(label)) label = SceneNames.Translate(asset.turnInMap);

            if (Journey.Start(asset.turnInMap, label, target)) return;

            TolkSpeech.Speak(Localization.Language.T(
                $"Je ne connais pas encore le chemin vers {SceneNames.Translate(asset.turnInMap)}. Allez-y une première fois, et il sera retenu.",
                $"I don't know the way to {SceneNames.Translate(asset.turnInMap)} yet. Go there once, and it will be remembered."), true);
        }

        /// <summary>
        /// La personne à qui rendre la quête, dans la langue du jeu. Le nom brut est en anglais ;
        /// sa clé de traduction est juste à côté, et c'est elle qui fait foi.
        /// </summary>
        private static string TurnInName(QuestAsset asset)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(asset.npcToTurnInTo)) return null;
                string translated = LocalizeText.TranslateText(asset.keyNPCToTurnInTo, asset.npcToTurnInTo);
                return TextUtil.Clean(string.IsNullOrWhiteSpace(translated) ? asset.npcToTurnInTo : translated);
            }
            catch { return null; }
        }

        private static string Description(QuestBundle bundle, QuestAsset asset)
        {
            try
            {
                // Le panneau du jeu, quand il existe, porte le texte déjà mis en forme avec les
                // valeurs de la partie. On le préfère au descriptif brut, qui garde ses jetons de
                // remplacement. Les quêtes cachées n'ont pas de panneau : on retombe sur le brut.
                string fromPanel = TextUtil.Clean(bundle.questPanel?.questDescription?.text);
                if (!string.IsNullOrWhiteSpace(fromPanel)) return fromPanel;

                string raw = LocalizeText.TranslateText(asset.keyQuestDescription, asset.questDescription);
                string clean = TextUtil.Clean(raw);
                return string.IsNullOrWhiteSpace(clean)
                    ? Localization.Language.T("Pas de description.", "No description.")
                    : clean;
            }
            catch { return Localization.Language.T("Pas de description.", "No description."); }
        }

        private static string Progress(QuestBundle bundle)
        {
            try
            {
                string progress = TextUtil.Clean(bundle.questPanel?.questProgress?.text);
                string complete = TextUtil.Clean(bundle.questPanel?.questCompleteTMP?.text);

                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(complete)) parts.Add(complete);
                if (!string.IsNullOrWhiteSpace(progress)) parts.Add(progress);

                return parts.Count > 0
                    ? string.Join(". ", parts.ToArray())
                    : Localization.Language.T("Aucune progression indiquée.", "No progress shown.");
            }
            catch { return Localization.Language.T("Aucune progression indiquée.", "No progress shown."); }
        }
    }
}
