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
            _openedFrame = UnityEngine.Time.frameCount;
            _text.Length = 0;
            if (!string.IsNullOrEmpty(initial)) _text.Append(initial);

            LockGameInput(true);

            string current = _text.Length > 0
                ? Localization.Language.T($" Texte actuel : {_text}.", $" Current text: {_text}.")
                : string.Empty;

            TolkSpeech.Speak(Localization.Language.T(
                $"{question}{current} Tapez, Entrée pour valider, Échap pour annuler.",
                $"{question}{current} Type, Enter to confirm, Escape to cancel."), true);
        }

        /// <summary>
        /// Étiquette de notre blocage d'entrées. Le jeu en accepte plusieurs, chacune nommée, et ne
        /// rend la main que lorsque toutes sont retirées : la nôtre ne peut donc pas débloquer par
        /// erreur ce qu'un dialogue ou une cinématique avait bloqué de son côté.
        /// </summary>
        private const string InputLock = "SunHavenAccess.saisie";

        /// <summary>
        /// PENDANT LA FRAPPE, LE JEU NE DOIT PAS ÉCOUTER.
        ///
        /// Le mod se taisait déjà, mais le jeu, lui, continuait : taper le nom d'un point favori
        /// faisait marcher le personnage, changer d'objet en main, ouvrir des menus. Signalé en jeu
        /// — « je ne suis pas dans une zone de texte bloquée ». Une saisie de texte doit se
        /// comporter comme un champ de texte : tant qu'elle est ouverte, les touches lui
        /// appartiennent.
        /// </summary>
        private static void LockGameInput(bool locked)
        {
            try
            {
                if (locked) Wish.PlayerInput.DisableInput(InputLock);
                else Wish.PlayerInput.EnableInput(InputLock);
            }
            catch { }
        }

        internal static void Close()
        {
            // On rend la main au jeu AVANT tout le reste : si quoi que ce soit échouait ensuite, le
            // clavier resterait confisqué et il faudrait quitter la partie pour s'en sortir.
            LockGameInput(false);

            _onDone = null;
            _question = null;
            _text.Length = 0;
        }

        /// <summary>Image où la saisie s'est ouverte. Voir le garde ci-dessous.</summary>
        private static int _openedFrame = -1;

        internal static void Tick()
        {
            if (_onDone == null) return; // aucune saisie : coût nul

            // LA TOUCHE QUI OUVRE NE DOIT PAS VALIDER.
            //
            // On ouvre cette saisie en validant « Ajouter un point ici » — donc avec Entrée. Or
            // `GetKeyDown` reste vrai pendant TOUTE l'image, et ce module tourne après celui qui a
            // ouvert la saisie : dans la même image, il voyait la même Entrée, validait un texte
            // vide et refermait aussitôt. Vu de l'extérieur, la saisie ne s'ouvrait jamais et rien
            // ne s'enregistrait — exactement ce qui a été signalé en jeu.
            //
            // On laisse donc passer l'image d'ouverture. Le même piège vaut pour Échap et pour les
            // caractères, d'où un garde placé avant toute lecture.
            if (UnityEngine.Time.frameCount == _openedFrame) return;

            // Filet de sécurité : si l'on quitte la partie pendant une saisie, elle se referme et
            // rend les touches. Un clavier confisqué par un module qui n'existe plus obligerait à
            // relancer le jeu.
            if (Wish.Player.Instance == null)
            {
                Close();
                return;
            }

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
