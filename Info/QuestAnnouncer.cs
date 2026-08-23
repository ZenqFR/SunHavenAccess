using System.Collections.Generic;
using Wish;
using SunHavenAccess.Speech;
using SunHavenAccess.Util;

namespace SunHavenAccess.Info
{
    /// <summary>
    /// Quêtes (`Wish.QuestList`/`Wish.Quest`/`Wish.QuestAsset`) : totalement absent du mod
    /// jusqu'ici. La touche L par défaut du jeu (Button.Quests, table
    /// Wish.UserSettings.DefaultKeybinds) ouvre le journal de quêtes, mais c'est un écran
    /// purement visuel (liste défilante, pas de Selectable navigable au clavier) — les systèmes
    /// génériques du mod (FocusReader, MenuNavigator...) ne peuvent donc rien en tirer. Cette
    /// classe lit directement les données de quête (Player.Instance.QuestList), sans dépendre de
    /// cet écran :
    /// - Annonce automatique dès qu'une nouvelle quête est acceptée
    ///   (`QuestList.OnAcceptQuest`, UnityAction STATIQUE, abonnement une seule fois — même
    ///   principe que CombatStateAnnouncer).
    /// - Annonce automatique d'une quête terminée (voir Patches/QuestCompletePatch.cs).
    /// - Touche dédiée pour lister toutes les quêtes actives à la demande (voir
    ///   Config/ModConfig.cs, HotkeyManager.cs).
    /// Toutes les données lues (QuestAsset.LocalizedQuestName, QuestPanel.questDescription/
    /// questProgress/questCompleteTMP) sont des champs PUBLICS déjà remplis par le jeu lui-même
    /// (confirmé en décompilation de QuestList.StartQuest et Quest.CheckForCompletion) — aucune
    /// réflexion nécessaire, contrairement à d'autres systèmes du mod.
    /// </summary>
    public static class QuestAnnouncer
    {
        private static bool _initialized;

        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;

            QuestList.OnAcceptQuest += OnQuestAccepted;
        }

        private static void OnQuestAccepted(Quest quest)
        {
            if (quest?.questAsset == null) return;
            string name = TextUtil.Clean(quest.questAsset.LocalizedQuestName);
            TolkSpeech.Speak($"Nouvelle quête : {name}.", interrupt: false);
        }

        /// <summary>Touche dédiée : liste toutes les quêtes actives (nom, description, progression).</summary>
        public static void AnnounceActiveQuests()
        {
            Player player = Player.Instance;
            if (player == null || player.QuestList == null)
            {
                TolkSpeech.Speak("Le jeu n'est pas encore chargé.", true);
                return;
            }

            List<QuestBundle> quests = player.QuestList.quests;
            if (quests == null || quests.Count == 0)
            {
                TolkSpeech.Speak("Aucune quête active.", true);
                return;
            }

            var parts = new List<string>();
            int total = quests.Count;
            int index = 0;
            foreach (QuestBundle bundle in quests)
            {
                index++;
                if (bundle?.quest?.questAsset == null) continue;

                string name = TextUtil.Clean(bundle.quest.questAsset.LocalizedQuestName);
                string sentence = total > 1 ? $"Quête {index} sur {total} : {name}." : $"Quête : {name}.";

                // Les quêtes "cachées" (QuestAsset.hidden) n'ont pas de panneau visuel — le jeu
                // ne leur en crée jamais un (voir QuestList.StartQuest) — juste le nom dans ce cas.
                if (bundle.questPanel != null)
                {
                    string description = TextUtil.Clean(bundle.questPanel.questDescription?.text);
                    string progress = TextUtil.Clean(bundle.questPanel.questProgress?.text);
                    string complete = TextUtil.Clean(bundle.questPanel.questCompleteTMP?.text);

                    if (!string.IsNullOrWhiteSpace(description)) sentence += $" {description}.";
                    if (!string.IsNullOrWhiteSpace(complete)) sentence += $" Progression : {complete}.";
                    if (!string.IsNullOrWhiteSpace(progress)) sentence += $" {progress}.";
                }

                parts.Add(sentence);
            }

            TolkSpeech.Speak(string.Join(" ", parts), true);
        }
    }
}
