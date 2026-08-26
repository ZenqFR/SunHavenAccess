using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SunHavenAccess.Config;
using SunHavenAccess.Speech;
using SunHavenAccess.Util;

namespace SunHavenAccess.Menus
{
    /// <summary>
    /// Les réglages en liste : le nom de l'option d'un côté, sa valeur qu'on change aux flèches
    /// gauche et droite.
    ///
    /// Un panneau d'options est une grille de curseurs, de cases à cocher et de listes déroulantes
    /// dont l'étiquette est posée à côté du contrôle. Retrouver quelle étiquette va avec quel
    /// contrôle demande de voir la mise en page. Or une option n'est qu'un couple : un nom et une
    /// valeur. C'est donc ainsi qu'on la présente.
    ///
    /// Deux sources dans la même liste, parce que la distinction n'intéresse personne au moment de
    /// régler quelque chose : d'abord les réglages du MOD, ensuite ceux du JEU actuellement à
    /// l'écran. Les premiers ne demandaient jusqu'ici rien de moins que d'éditer un fichier de
    /// configuration à la main.
    ///
    /// Les contrôles du jeu sont pilotés directement — cocher la case, déplacer le curseur — donc
    /// c'est le jeu lui-même qui applique et enregistre le changement, sans qu'aucune règle ne
    /// soit redupliquée ici.
    /// </summary>
    public static class SettingsMenu
    {
        /// <summary>Contrôles du jeu retenus, dans l'ordre de la liste affichée.</summary>
        private static readonly List<Selectable> _controls = new List<Selectable>();

        /// <summary>Réglages du mod, dans l'ordre de la liste affichée.</summary>
        private static readonly List<BepInEx.Configuration.ConfigEntry<bool>> _modSettings =
            new List<BepInEx.Configuration.ConfigEntry<bool>>();

        private static readonly List<string> _modLabels = new List<string>();

        public static void Open()
        {
            _controls.Clear();
            _modSettings.Clear();
            _modLabels.Clear();

            var entries = new List<string>();

            AddModSetting(entries, ModConfig.EdgeSound, "Bip de bord");
            AddModSetting(entries, ModConfig.TakeOverMenuArrows, "Navigation directionnelle du mod");
            AddModSetting(entries, ModConfig.BriefMode, "Mode bref dans l'inventaire");

            foreach (Selectable control in GameControls())
            {
                string label = LabelOf(control);
                if (label == null) continue;
                _controls.Add(control);
                entries.Add(Describe(label, control));
            }

            ListMenu.Open("Réglages", entries, onAdjust: Adjust);
        }

        // ------------------------------------------------------------------ Réglages du mod

        private static void AddModSetting(List<string> entries,
                                          BepInEx.Configuration.ConfigEntry<bool> setting,
                                          string label)
        {
            if (setting == null) return;
            _modSettings.Add(setting);
            _modLabels.Add(label);
            entries.Add($"{label} : {OnOff(setting.Value)}");
        }

        private static string OnOff(bool value) => value ? "activé" : "désactivé";

        // ------------------------------------------------------------------ Réglages du jeu

        /// <summary>
        /// Les contrôles réglables actuellement à l'écran. On ne retient que ce qui a une VALEUR —
        /// case à cocher, curseur, liste déroulante — et non les boutons, qui déclenchent une
        /// action plutôt que de porter un état, et qui restent atteignables par la navigation
        /// ordinaire.
        /// </summary>
        private static IEnumerable<Selectable> GameControls()
        {
            try
            {
                return MenuNavigator.VisibleSelectables()
                    .Where(s => s is Toggle || s is Slider || s is TMP_Dropdown || s is Dropdown);
            }
            catch { return Enumerable.Empty<Selectable>(); }
        }

        /// <summary>
        /// L'étiquette d'un contrôle : son propre texte, sinon celui d'un frère.
        ///
        /// C'est exactement le problème que l'affichage pose — l'étiquette est POSÉE à côté du
        /// contrôle, pas dedans. On réutilise l'extracteur du mod, déjà chargé de retrouver le
        /// libellé d'un élément qui n'en porte pas lui-même.
        /// </summary>
        private static string LabelOf(Selectable control)
        {
            try
            {
                string text = UiTextExtractor.ExtractAll(control.gameObject);
                text = TextUtil.Clean(text);
                return string.IsNullOrWhiteSpace(text) ? null : text;
            }
            catch { return null; }
        }

        private static string Describe(string label, Selectable control)
        {
            switch (control)
            {
                case Toggle toggle:
                    return $"{label} : {(toggle.isOn ? "coché" : "décoché")}";
                case Slider slider:
                    return $"{label} : {Percent(slider)} pour cent";
                case TMP_Dropdown dropdown:
                    return $"{label} : {OptionText(dropdown)}";
                case Dropdown legacy:
                    return $"{label} : {LegacyOptionText(legacy)}";
                default:
                    return label;
            }
        }

        /// <summary>
        /// Un curseur s'annonce en POUR CENT de sa course, pas en valeur brute : « 0,73 » ne dit
        /// rien, « 73 pour cent » situe immédiatement.
        /// </summary>
        private static int Percent(Slider slider)
        {
            float range = slider.maxValue - slider.minValue;
            if (range <= 0f) return 0;
            return Mathf.RoundToInt((slider.value - slider.minValue) / range * 100f);
        }

        private static string OptionText(TMP_Dropdown dropdown) =>
            dropdown.value >= 0 && dropdown.value < dropdown.options.Count
                ? TextUtil.Clean(dropdown.options[dropdown.value].text)
                : "inconnu";

        private static string LegacyOptionText(Dropdown dropdown) =>
            dropdown.value >= 0 && dropdown.value < dropdown.options.Count
                ? TextUtil.Clean(dropdown.options[dropdown.value].text)
                : "inconnu";

        // ------------------------------------------------------------------ Modification

        /// <summary>
        /// Change la valeur d'une ligne et renvoie son nouveau libellé, ou null si elle ne peut pas
        /// bouger — bout de course d'un curseur, dernière option d'une liste — pour que l'appelant
        /// le signale par le bip de bord.
        /// </summary>
        private static string Adjust(int index, int direction)
        {
            if (index < _modSettings.Count) return AdjustModSetting(index, direction);

            int controlIndex = index - _modSettings.Count;
            if (controlIndex < 0 || controlIndex >= _controls.Count) return null;

            Selectable control = _controls[controlIndex];
            if (control == null || !control.gameObject.activeInHierarchy) return null;

            string label = LabelOf(control) ?? "Réglage";

            try
            {
                switch (control)
                {
                    case Toggle toggle:
                        // Une case n'a que deux états : les deux directions la basculent, plutôt
                        // que d'en réserver une à chaque sens et de laisser l'autre sans effet.
                        toggle.isOn = !toggle.isOn;
                        return Describe(label, toggle);

                    case Slider slider:
                    {
                        float step = slider.wholeNumbers ? 1f : (slider.maxValue - slider.minValue) / 20f;
                        float wanted = Mathf.Clamp(slider.value + step * direction, slider.minValue, slider.maxValue);
                        if (Mathf.Approximately(wanted, slider.value)) return null;
                        slider.value = wanted;
                        return Describe(label, slider);
                    }

                    case TMP_Dropdown dropdown:
                    {
                        int wanted = dropdown.value + direction;
                        if (wanted < 0 || wanted >= dropdown.options.Count) return null;
                        dropdown.value = wanted;
                        return Describe(label, dropdown);
                    }

                    case Dropdown legacy:
                    {
                        int wanted = legacy.value + direction;
                        if (wanted < 0 || wanted >= legacy.options.Count) return null;
                        legacy.value = wanted;
                        return Describe(label, legacy);
                    }
                }
            }
            catch
            {
                TolkSpeech.Speak("Ce réglage n'a pas pu être modifié.", true);
            }

            return null;
        }

        private static string AdjustModSetting(int index, int direction)
        {
            var setting = _modSettings[index];
            setting.Value = !setting.Value; // BepInEx enregistre le fichier de config tout seul
            return $"{_modLabels[index]} : {OnOff(setting.Value)}";
        }
    }
}
