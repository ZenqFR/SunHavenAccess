using System;
using System.Text;
using UnityEngine;
using SunHavenAccess.Speech;

namespace SunHavenAccess.Menus
{
    /// <summary>
    /// Demander du texte, sans champ de saisie à l'écran.
    ///
    /// Le mod savait déjà lire un champ du JEU pendant qu'on tape dedans. Mais nommer un point
    /// favori n'a pas de champ : il fallait pouvoir taper alors que rien, à l'écran, n'attend de
    /// texte. `Input.inputString` donne exactement cela — les caractères réellement frappés, tels
    /// que le système les produit, accents et clavier AZERTY compris. Rien à traduire, rien à
    /// deviner sur la disposition du clavier.
    ///
    /// CE QUI SE DIT PENDANT LA FRAPPE. Chaque caractère est annoncé au moment où il est tapé, et
    /// chaque effacement dit ce qu'on vient de retirer — sans quoi on écrit à l'aveugle et l'on ne
    /// découvre sa faute qu'à la fin, quand il est trop tard pour la corriger simplement. Le texte
    /// entier se réentend d'une touche.
    ///
    /// PENDANT UNE SAISIE, LE MOD SE TAIT. Tant que ce module tient le clavier, aucune autre touche
    /// du mod n'agit : sinon taper « p » annoncerait la position et « o » l'horloge, comme cela
    /// arrivait avant que le lecteur de champ du jeu n'existe. La règle est déjà celle du reste du
    /// mod ; elle vaut ici aussi.
    /// </summary>
    internal static class TextPrompt
    {
        private static readonly StringBuilder _text = new StringBuilder();
        private static string _question;
        private static Action<string> _onDone;

        internal static bool IsOpen => _onDone != null;

        /// <summary>
        /// Ouvre la saisie. <paramref name="initial"/> pré-remplit le texte — c'est ce qui permet
        /// de RENOMMER sans tout retaper.
        /// </summary>
        internal static void Ask(string question, string initial, Action<string> onDone)
        {
            _question = question;
            _onDone = onDone;
            _text.Length = 0;
            if (!string.IsNullOrEmpty(initial)) _text.Append(initial);

            string current = _text.Length > 0
                ? Localization.Language.T($" Texte actuel : {_text}.", $" Current text: {_text}.")
                : string.Empty;

            TolkSpeech.Speak(Localization.Language.T(
                $"{question}{current} Tapez, Entrée pour valider, Échap pour annuler.",
                $"{question}{current} Type, Enter to confirm, Escape to cancel."), true);
        }

        internal static void Close()
        {
            _onDone = null;
            _question = null;
            _text.Length = 0;
        }

        internal static void Tick()
        {
            if (_onDone == null) return; // aucune saisie : coût nul

            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
            {
                Close();
                TolkSpeech.Speak(Localization.Language.T("Annulé.", "Cancelled."), true);
                return;
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.Return) || UnityEngine.Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                string result = _text.ToString().Trim();
                Action<string> done = _onDone;
                Close();

                if (string.IsNullOrWhiteSpace(result))
                {
                    TolkSpeech.Speak(Localization.Language.T(
                        "Texte vide, rien n'a été fait.", "Empty text, nothing was done."), true);
                    return;
                }

                done(result);
                return;
            }

            // Relire ce qu'on a écrit jusqu'ici, sans rien changer. Indispensable sur un nom un peu
            // long : on perd le fil, et l'alternative serait de tout effacer pour recommencer.
            if (UnityEngine.Input.GetKeyDown(KeyCode.F2))
            {
                TolkSpeech.Speak(_text.Length > 0
                    ? _text.ToString()
                    : Localization.Language.T("Vide.", "Empty."), true);
                return;
            }

            foreach (char c in UnityEngine.Input.inputString)
            {
                if (c == '\b')
                {
                    if (_text.Length == 0)
                    {
                        UiSound.EdgeBump();
                        continue;
                    }

                    // Dire ce qu'on RETIRE, pas seulement qu'on a retiré quelque chose : c'est la
                    // seule façon de savoir où l'on en est sans relire le tout.
                    char removed = _text[_text.Length - 1];
                    _text.Length--;
                    TolkSpeech.Speak(Localization.Language.T($"{Spoken(removed)} effacé", $"{Spoken(removed)} deleted"), true);
                    continue;
                }

                // Entrée et retour chariot sont déjà traités plus haut ; les ignorer ici évite de
                // les écrire dans le texte.
                if (c == '\n' || c == '\r') continue;

                _text.Append(c);
                TolkSpeech.Speak(Spoken(c), true);
            }
        }

        /// <summary>
        /// L'espace ne s'entend pas : annoncé tel quel, il passe pour un silence et l'on croit que
        /// la touche n'a pas été prise.
        /// </summary>
        private static string Spoken(char c) =>
            c == ' ' ? Localization.Language.T("espace", "space") : c.ToString();
    }
}
