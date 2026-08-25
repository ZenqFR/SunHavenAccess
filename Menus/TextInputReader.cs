using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SunHavenAccess.Speech;
using SunHavenAccess.Util;

namespace SunHavenAccess.Menus
{
    /// <summary>
    /// Lecture des champs de saisie de texte. Sans ça, taper du texte se fait dans le SILENCE
    /// TOTAL — ce qui bloquait purement et simplement la création de personnage : impossible de
    /// savoir ce qu'on a tapé comme nom, ni même si la frappe était prise en compte.
    ///
    /// Le champ du jeu est `TMPro.SunHavenInputField` — attention, il est dans le namespace
    /// `TMPro` et NON `Wish` (c'est une copie modifiée de `TMP_InputField` embarquée dans
    /// SunHaven.Core.dll). Tout ce qu'il faut est public : `text`, `isFocused`, `placeholder`,
    /// `inputType`.
    ///
    /// Sondage à chaque tick plutôt qu'abonnement à `onValueChanged` : c'est le même patron que
    /// FocusReader/TooltipReader, et surtout les champs sont créés/détruits dynamiquement selon
    /// l'écran — s'abonner supposerait de les découvrir au bon moment, alors que le sondage
    /// fonctionne quel que soit le moment d'apparition.
    ///
    /// Couvre le nom du personnage, le champ de tri de l'artisanat et le tchat, sans code
    /// spécifique à chacun.
    /// </summary>
    public static class TextInputReader
    {
        private static SunHavenInputField _lastField;
        private static string _lastText = "";

        /// <summary>
        /// Un champ de saisie capte-t-il actuellement le clavier ? Utilisé par HotkeyManager pour
        /// SUSPENDRE toutes les touches du mod pendant la frappe — sans ça, taper « p » dans le
        /// nom de son personnage déclencherait l'annonce de position, « o » l'horloge, etc.
        /// On combine notre propre détection et le signal du jeu (`UIHandler.IsInputFieldFocused`),
        /// qui couvre aussi d'éventuels champs d'un autre type.
        /// </summary>
        public static bool IsTyping()
        {
            if (FocusedField() != null) return true;
            try { return Wish.UIHandler.IsInputFieldFocused(); }
            catch { return false; }
        }

        public static void Tick()
        {
            SunHavenInputField field = FocusedField();

            if (field == null)
            {
                _lastField = null;
                _lastText = "";
                return;
            }

            string current = field.text ?? "";

            if (field != _lastField)
            {
                // Prise de focus : on annonce à quoi sert le champ et ce qu'il contient déjà.
                _lastField = field;
                _lastText = current;
                FocusReader.SuppressNextAnnouncement();
                string label = DescribeField(field);
                string content = string.IsNullOrEmpty(current) ? "vide" : Spoken(field, current);
                TolkSpeech.Speak($"{label} : {content}.", interrupt: true);
                return;
            }

            if (current == _lastText) return;

            AnnounceDifference(field, _lastText, current);
            _lastText = current;
        }

        /// <summary>
        /// Écho de frappe : on annonce ce qui a CHANGÉ, pas tout le champ. Relire la chaîne
        /// entière à chaque lettre rendrait la saisie d'un nom insupportable.
        /// </summary>
        private static void AnnounceDifference(SunHavenInputField field, string before, string after)
        {
            if (IsHidden(field))
            {
                // Champ masqué (mot de passe) : ne jamais prononcer le contenu à voix haute.
                TolkSpeech.Speak(after.Length > before.Length ? "étoile" : "supprimé", interrupt: true);
                return;
            }

            if (after.Length > before.Length && after.StartsWith(before))
            {
                TolkSpeech.Speak(after.Substring(before.Length), interrupt: true);
                return;
            }

            if (after.Length < before.Length && before.StartsWith(after))
            {
                string removed = before.Substring(after.Length);
                TolkSpeech.Speak($"supprimé {removed}", interrupt: true);
                return;
            }

            // Changement non trivial (collage, insertion au milieu, effacement total) : on relit
            // tout, c'est le seul moyen de rester juste.
            TolkSpeech.Speak(string.IsNullOrEmpty(after) ? "vide" : Spoken(field, after), interrupt: true);
        }

        /// <summary>
        /// Le champ réellement en train de recevoir la frappe. On part de la sélection Unity
        /// (le jeu sélectionne le champ en l'activant) et on confirme avec `isFocused`, qui est
        /// le vrai signal « ce champ capte le clavier ».
        /// </summary>
        private static SunHavenInputField FocusedField()
        {
            GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (selected == null || !selected.activeInHierarchy) return null;

            SunHavenInputField field = selected.GetComponent<SunHavenInputField>()
                ?? selected.GetComponentInParent<SunHavenInputField>();

            return (field != null && field.isFocused) ? field : null;
        }

        /// <summary>
        /// Libellé du champ. Le texte d'invite (placeholder) est ce qui décrit le mieux son rôle
        /// ("Nom du personnage", "Rechercher"...) ; à défaut on reste générique plutôt que
        /// d'annoncer un nom technique d'objet Unity.
        /// </summary>
        private static string DescribeField(SunHavenInputField field)
        {
            try
            {
                if (field.placeholder is TextMeshProUGUI tmp)
                {
                    string hint = TextUtil.Clean(tmp.text);
                    if (!string.IsNullOrWhiteSpace(hint)) return hint;
                }
                if (field.placeholder is Text legacy)
                {
                    string hint = TextUtil.Clean(legacy.text);
                    if (!string.IsNullOrWhiteSpace(hint)) return hint;
                }
            }
            catch { /* structure inattendue : on retombe sur le libellé générique */ }

            return "Champ de saisie";
        }

        private static string Spoken(SunHavenInputField field, string text) =>
            IsHidden(field) ? "masqué" : text;

        private static bool IsHidden(SunHavenInputField field)
        {
            try { return field.inputType.ToString().IndexOf("Password", System.StringComparison.OrdinalIgnoreCase) >= 0; }
            catch { return false; }
        }
    }
}
