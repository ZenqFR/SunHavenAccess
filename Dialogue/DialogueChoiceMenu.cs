using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine.Events;
using Wish;
using SunHavenAccess.Menus;
using SunHavenAccess.Speech;
using SunHavenAccess.Util;

namespace SunHavenAccess.Dialogue
{
    /// <summary>
    /// Une bulle de dialogue à choix devient une liste : le message en tête, les réponses ensuite.
    ///
    /// TROIS DÉFAUTS D'UN SEUL COUP, tous signalés en jeu.
    ///
    /// Le message n'avait pas fini d'être lu que les réponses s'annonçaient déjà par-dessus. Le jeu
    /// affiche les deux ensemble : à l'œil, on lit le texte puis on baisse le regard, mais à
    /// l'oreille tout arrive dans le même flux et se coupe la parole.
    ///
    /// On ne pouvait pas relire ce qui venait d'être dit. Une réponse mal entendue obligeait à
    /// choisir au hasard.
    ///
    /// Et les flèches ne servaient à rien : le jeu câble bien une navigation entre ses options,
    /// mais le mod neutralise la navigation native d'Unity quand il pilote les menus — sans quoi
    /// chaque flèche compterait double. Les options se retrouvaient donc entre deux systèmes, et
    /// aucun ne s'en occupait.
    ///
    /// LA RÉPONSE. Le message devient la PREMIÈRE ENTRÉE de la liste, avant les réponses. Y
    /// remonter le relit, autant de fois qu'on veut, sans rien engager — puisque parcourir n'est
    /// pas choisir, la règle déjà en place partout ailleurs dans ce mod. Les flèches marchent parce
    /// que c'est notre liste. Et rien ne s'annonce par-dessus le message, puisque plus rien ne
    /// s'annonce tout seul.
    ///
    /// Choisir se fait comme le jeu le fait lui-même : on pose l'index de l'option, puis on appelle
    /// `Next()` — exactement les deux gestes que déclenche un clic. Rien n'est réimplémenté, donc
    /// rien ne peut diverger de ce que le jeu attend.
    ///
    /// COÛT EN FOND. `DialogueOnGoing` est une propriété du jeu, gratuite. Tant qu'aucun dialogue
    /// n'est en cours, ce module s'arrête à la première ligne.
    /// </summary>
    internal static class DialogueChoiceMenu
    {
        private const string OwnerTag = "dialogue";

        private static FieldInfo _optionsField;
        private static FieldInfo _optionIndexField;
        private static FieldInfo _bustField;

        /// <summary>Textes des options déjà présentées, pour ne rouvrir qu'au VRAI changement.</summary>
        private static string _shownSignature;

        internal static void Tick()
        {
            DialogueController controller = DialogueController.Instance;
            if (controller == null || !controller.DialogueOnGoing)
            {
                // Le dialogue s'est terminé : la liste part avec lui, sinon elle resterait ouverte
                // par-dessus le jeu et continuerait de capter les flèches.
                if (_shownSignature != null)
                {
                    _shownSignature = null;
                    ListMenu.CloseIfOwner(OwnerTag, false);
                }
                return;
            }

            List<(TextMeshProUGUI, UnityAction, UnityEngine.UI.Image, string, bool)> options = Options(controller);
            if (options == null || options.Count == 0)
            {
                // Une ligne sans choix : le jeu la lit déjà via le patch sur TextScroll, et
                // l'espace ou Entrée la fait avancer comme d'habitude. Rien à ouvrir.
                if (_shownSignature != null)
                {
                    _shownSignature = null;
                    ListMenu.CloseIfOwner(OwnerTag, false);
                }
                return;
            }

            string signature = Signature(options);
            if (signature == _shownSignature) return; // mêmes choix qu'à l'image précédente

            _shownSignature = signature;
            Open(controller, options);
        }

        private static void Open(DialogueController controller,
            List<(TextMeshProUGUI, UnityAction, UnityEngine.UI.Image, string, bool)> options)
        {
            string message = Message(controller);

            // La première entrée est un RAPPEL COURT, pas le message entier.
            //
            // Le message vient d'être lu par le patch sur TextScroll ; le remettre en toutes
            // lettres ici le ferait entendre deux fois de suite. Une entrée brève dit qu'on peut
            // le réentendre, et le relit seulement si on le demande.
            var entries = new List<string>
            {
                Localization.Language.T("Relire le message", "Read the message again"),
            };

            foreach (var option in options)
            {
                string text = TextUtil.Clean(option.Item4);
                if (string.IsNullOrWhiteSpace(text)) text = Localization.Language.T("Réponse", "Reply");

                // Le jeu grise les réponses indisponibles en rouge ; à l'oreille il faut le dire,
                // sans quoi on la choisit et rien ne se passe.
                entries.Add(option.Item5
                    ? text
                    : text + Localization.Language.T(" — indisponible", " — unavailable"));
            }

            ListMenu.Open(Localization.Language.T("Réponse", "Reply"), entries,
                chosen => Choose(controller, chosen, options.Count),
                owner: OwnerTag, announce: false);
        }

        /// <summary>
        /// Valide l'entrée choisie. L'entrée zéro est le message : la valider ne fait que le
        /// relire, et surtout n'engage rien — c'est la différence entre consulter et répondre.
        /// </summary>
        private static void Choose(DialogueController controller, int index, int optionCount)
        {
            if (index <= 0)
            {
                TolkSpeech.Speak(Message(controller), true);
                // La liste se referme à la validation : on la rouvre pour rester dans la bulle.
                _shownSignature = null;
                return;
            }

            int option = index - 1;
            if (option >= optionCount) return;

            try
            {
                // Les deux gestes exacts d'un clic sur l'option : poser l'index, puis avancer.
                _optionIndexField?.SetValue(controller, option);
                controller.Next();
            }
            catch (Exception e)
            {
                Plugin.Log?.LogWarning("Choix de dialogue impossible : " + e.Message);
                TolkSpeech.Speak(Localization.Language.T(
                    "Impossible de valider cette réponse.",
                    "Cannot select this reply."), true);
            }
        }

        private static string Message(DialogueController controller)
        {
            try
            {
                _bustField ??= typeof(DialogueController).GetField("_bustTMP",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                var tmp = _bustField?.GetValue(controller) as TextMeshProUGUI;
                return TextUtil.Clean(tmp?.text);
            }
            catch { return null; }
        }

        private static List<(TextMeshProUGUI, UnityAction, UnityEngine.UI.Image, string, bool)> Options(
            DialogueController controller)
        {
            try
            {
                _optionsField ??= typeof(DialogueController).GetField("_options",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                _optionIndexField ??= typeof(DialogueController).GetField("_optionIndex",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                return _optionsField?.GetValue(controller)
                    as List<(TextMeshProUGUI, UnityAction, UnityEngine.UI.Image, string, bool)>;
            }
            catch { return null; }
        }

        /// <summary>
        /// De quoi reconnaître « ce sont les mêmes choix ». Le jeu reconstruit ses options à chaque
        /// ligne : comparer les textes évite de rouvrir la liste à chaque image, ce qui la rendrait
        /// impossible à parcourir.
        /// </summary>
        private static string Signature(
            List<(TextMeshProUGUI, UnityAction, UnityEngine.UI.Image, string, bool)> options)
        {
            var parts = new string[options.Count];
            for (int i = 0; i < options.Count; i++) parts[i] = options[i].Item4;
            return string.Join("", parts);
        }
    }
}
