using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Wish;
using SunHavenAccess.Speech;

namespace SunHavenAccess.Info
{
    /// <summary>
    /// Accompagne la création de personnage, premier écran d'une nouvelle partie.
    ///
    /// C'est le tout premier obstacle : un joueur aveugle qui n'en sort pas ne joue jamais. Or
    /// l'écran ne dit rien de lui-même — ni comment il est organisé, ni surtout ce qui empêche de
    /// valider. Le jeu affiche « Veuillez saisir un nom ! » et « Veuillez définir votre
    /// anniversaire ! » en rouge à côté du bouton, et grise celui-ci ; sans la vue, on appuie sur
    /// Valider et il ne se passe rien, sans la moindre explication.
    ///
    /// Deux annonces, donc, et aucune touche à retenir :
    /// - à l'ouverture, comment l'écran est fait et par où commencer ;
    /// - dès que la liste de ce qui manque change, ce qui manque encore.
    ///
    /// Le mod ne redéfinit aucune règle : il lit l'état que le jeu recalcule à chaque image dans
    /// `NewCharacterCreator.Update` — nom saisi, anniversaire défini, bouton actif.
    /// </summary>
    public static class CharacterCreationGuide
    {
        private static bool _onScreen;
        private static string _lastRemaining;

        private static FieldInfo _nameField;
        private static FieldInfo _inSalonField;
        private static bool _resolved;

        public static void Tick()
        {
            // L'assistant pose les questions une par une : décrire en même temps la disposition de
            // l'écran, que l'assistant dispense justement d'explorer, ne ferait que parler par
            // dessus. Ce guide reprend la main dès qu'on ferme l'assistant pour régler l'écran
            // soi-même — c'est exactement le cas où il sert.
            if (Menus.CharacterCreationWizard.IsRunning) return;

            // Le jeu expose son singleton : le chercher dans toute la scène à chaque image coûterait
            // cher pour un écran qui n'existe qu'une fois.
            NewCharacterCreator creator = Util.ScreenPresence<NewCharacterCreator>.Instance;

            if (creator == null || !creator.isActiveAndEnabled)
            {
                _onScreen = false;
                _lastRemaining = null;
                return;
            }

            if (!_onScreen)
            {
                _onScreen = true;
                _lastRemaining = null;
                AnnounceOrientation();
                return;
            }

            AnnounceRemaining(creator);
        }

        /// <summary>
        /// Ce qu'il faut savoir en arrivant. Volontairement dit d'un bloc et une seule fois : c'est
        /// une prise de repères, pas une information qu'on consulte — et elle est de toute façon
        /// reprise dans l'aide.
        /// </summary>
        private static void AnnounceOrientation()
        {
            TolkSpeech.Speak(Localization.Language.T(
                "Création de personnage. L'écran a trois colonnes : à gauche les catégories — race, corps, cheveux, " +
                "métier, anniversaire — au centre les choix de la catégorie courante, à droite votre personnage, " +
                "le champ du nom et le bouton Valider. " +
                "Contrôle plus gauche ou droite change de colonne, les flèches parcourent la colonne, Entrée choisit. " +
                "Un nom et une date d'anniversaire sont obligatoires pour commencer la partie.",

                "Character creation. The screen has three columns: on the left the categories — race, body, hair, " +
                "profession, birthday — in the centre the current category's choices, on the right your character, " +
                "the name field and the Confirm button. " +
                "Control plus left or right changes column, the arrows browse the column, Enter chooses. " +
                "A name and a birthday are required to start the game."),
                true);
        }

        /// <summary>
        /// Ce qui manque encore, annoncé UNIQUEMENT quand la liste change.
        ///
        /// Le jeu recalcule cet état à chaque image : le répéter au même rythme rendrait l'écran
        /// inutilisable. En revanche, franchir la dernière exigence mérite d'être dit — c'est
        /// l'information qu'on attend.
        /// </summary>
        private static void AnnounceRemaining(NewCharacterCreator creator)
        {
            Resolve();

            // En salon de coiffure, on modifie un personnage existant : ni nom ni anniversaire à
            // fournir, donc rien à réclamer.
            if (ReadBool(_inSalonField, creator)) return;

            bool nameMissing = NameMissing(creator);
            bool birthdayMissing = !creator.isBirthdaySet;

            string remaining;
            if (nameMissing && birthdayMissing) remaining = Localization.Language.T(
                "Il reste à saisir un nom et à définir votre anniversaire.",
                "Still to do: enter a name and set your birthday.");
            else if (nameMissing) remaining = Localization.Language.T(
                "Il reste à saisir un nom.", "Still to do: enter a name.");
            else if (birthdayMissing) remaining = Localization.Language.T(
                "Il reste à définir votre anniversaire.", "Still to do: set your birthday.");
            else remaining = Localization.Language.T(
                "Tout est prêt : le bouton Valider lance la partie.",
                "Everything is ready: the Confirm button starts the game.");

            if (remaining == _lastRemaining) return;

            // La première évaluation sert de référence sans être dite : elle suit immédiatement
            // l'orientation, qui vient déjà d'annoncer que nom et anniversaire sont obligatoires.
            bool first = _lastRemaining == null;
            _lastRemaining = remaining;
            if (first) return;

            TolkSpeech.Speak(remaining, interrupt: false);
        }

        /// <summary>
        /// Le champ du nom est-il encore vide ? Même test que le jeu : un champ désactivé ne
        /// réclame rien.
        ///
        /// Le type est `TMPro.SunHavenInputField`, un fork maison qui dérive de `Selectable` et
        /// NON de `TMP_InputField` — malgré son espace de noms. Le typer en `TMP_InputField`
        /// compilait sans broncher et renvoyait null à l'exécution : le nom manquant n'aurait
        /// jamais été signalé, sans le moindre message d'erreur.
        /// </summary>
        private static bool NameMissing(NewCharacterCreator creator)
        {
            if (_nameField == null) return false;

            try
            {
                var field = _nameField.GetValue(creator) as SunHavenInputField;
                if (field == null || !field.isActiveAndEnabled) return false;
                return string.IsNullOrWhiteSpace(field.text);
            }
            catch { return false; }
        }

        private static bool ReadBool(FieldInfo field, object target)
        {
            if (field == null) return false;
            try { return (bool)field.GetValue(target); }
            catch { return false; }
        }

        private static void Resolve()
        {
            if (_resolved) return;
            _resolved = true;

            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
            _nameField = typeof(NewCharacterCreator).GetField("nameInputField", flags);
            _inSalonField = typeof(NewCharacterCreator).GetField("inSalon", flags);

            if (_nameField == null)
                Plugin.Log?.LogWarning("NewCharacterCreator.nameInputField introuvable : le rappel de ce qui manque en création de personnage sera incomplet.");
        }
    }
}
