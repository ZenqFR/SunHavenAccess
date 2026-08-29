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

    }
}
