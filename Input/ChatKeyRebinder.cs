using System.Reflection;
using UnityEngine;
using QFSW.QC;
using SunHavenAccess.Config;

namespace SunHavenAccess.Input
{
    /// <summary>
    /// Le tchat/console de debug du jeu (l'asset tiers "Quantum Console", voir
    /// Wish.QuantumConsoleManager) s'ouvre par défaut sur la touche Entrée — exactement la
    /// touche que ce mod utilise déjà pour valider un élément de menu et faire avancer les
    /// dialogues, d'où un conflit permanent ("j'appuie sur Entrée, ça ouvre le tchat"). La
    /// touche d'activation vient d'un champ PRIVÉ (`_keyConfig`, type QFSW.QC.QuantumKeyConfig)
    /// sur l'instance de QuantumConsole : on la récupère par réflexion une seule fois, puis on
    /// réassigne directement ses champs publics (ShowConsoleKey/ToggleConsoleVisibilityKey, eux
    /// accessibles normalement) vers la touche choisie dans la config du mod.
    /// </summary>
    public static class ChatKeyRebinder
    {
        private static bool _applied;
        private static FieldInfo _keyConfigField;

        public static void Tick()
        {
            if (_applied) return;

            QuantumConsole console = QuantumConsole.Instance;
            if (console == null) return; // pas encore instancié par le jeu, on réessaiera au prochain tick

            _keyConfigField ??= typeof(QuantumConsole).GetField("_keyConfig", BindingFlags.NonPublic | BindingFlags.Instance);
            if (_keyConfigField?.GetValue(console) is not QuantumKeyConfig config) return;

            KeyCode newKey = ModConfig.ChatOpenKey.Value;
            bool changed = false;

            if (config.ShowConsoleKey.Key == KeyCode.Return || config.ShowConsoleKey.Key == KeyCode.KeypadEnter)
            {
                config.ShowConsoleKey = new ModifierKeyCombo { Key = newKey };
                changed = true;
            }
            if (config.ToggleConsoleVisibilityKey.Key == KeyCode.Return || config.ToggleConsoleVisibilityKey.Key == KeyCode.KeypadEnter)
            {
                config.ToggleConsoleVisibilityKey = new ModifierKeyCombo { Key = newKey };
                changed = true;
            }

            _applied = true;
            if (changed)
            {
                Plugin.Log.LogInfo($"ChatKeyRebinder : touche d'ouverture du tchat/console changée pour {newKey} (elle entrait en conflit avec Entrée).");
            }
        }
    }
}
