using System.Collections.Generic;
using System.Linq;
using SunHavenAccess.Speech;

namespace SunHavenAccess.Menus
{
    /// <summary>
    /// Tout ce qui a été annoncé récemment, en liste.
    ///
    /// La touche de répétition remonte le fil d'un cran par appui, ce qui va bien pour rattraper
    /// une phrase manquée. Elle ne vaut plus rien dès qu'on cherche quelque chose de précis : une
    /// notification reçue il y a une minute, ce qu'un habitant a dit avant qu'on l'interrompe, le
    /// dernier message d'un partenaire de jeu. Reculer quinze fois pour retrouver une phrase, c'est
    /// la réentendre quinze fois d'abord.
    ///
    /// Les notifications et le tchat passent DÉJÀ par la parole du mod : ils sont donc dans cet
    /// historique sans qu'aucun module séparé n'ait à les collecter. Une seule liste, une seule
    /// vérité, et rien qui puisse diverger.
    ///
    /// Valider une ligne la relit — c'est ce qu'on vient chercher, et rien d'autre ne serait sûr :
    /// une annonce est un texte passé, pas une action qu'on pourrait rejouer.
    /// </summary>
    internal static class HistoryMenu
    {
        internal static void Open()
        {
            IReadOnlyList<string> history = TolkSpeech.History;

            if (history == null || history.Count == 0)
            {
                TolkSpeech.Speak(Localization.Language.T(
                    "Rien n'a encore été annoncé.", "Nothing has been announced yet."), true);
                return;
            }

            // La plus récente en tête : c'est presque toujours celle qu'on cherche, et l'on
            // descend vers le passé, ce qui est le sens naturel d'une liste qu'on parcourt.
            var entries = history.ToList();

            ListMenu.Open(Localization.Language.T("Annonces récentes", "Recent announcements"),
                entries,
                chosen =>
                {
                    if (chosen >= 0 && chosen < entries.Count) TolkSpeech.Speak(entries[chosen], true);
                },
                owner: "historique");
        }
    }
}
