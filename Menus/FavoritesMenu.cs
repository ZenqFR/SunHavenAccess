using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SunHavenAccess.Navigation;
using SunHavenAccess.Speech;
using SunHavenAccess.Util;

namespace SunHavenAccess.Menus
{
    /// <summary>
    /// Créer, renommer, supprimer et rejoindre ses points favoris.
    ///
    /// Le scanner trouve ce que le JEU connaît. Il ne connaît pas « l'endroit où je plante mes
    /// navets » ni « le coin où je pêche » : ces repères-là sont dans la tête de qui joue, et pour
    /// qui voit ils tiennent à un coup d'œil sur le paysage. Ce menu leur donne une existence.
    ///
    /// LES FAVORIS D'AILLEURS SONT LISTÉS AUSSI, pas seulement ceux de la zone courante — sinon on
    /// ne pourrait ni les renommer ni les supprimer sans y retourner, et l'on ne saurait même pas
    /// qu'ils existent. Chacun dit sa zone quand il n'est pas ici, et « s'y rendre » traverse ce
    /// qu'il faut pour l'atteindre.
    /// </summary>
    internal static class FavoritesMenu
    {
        private const string OwnerTag = "favoris";

        internal static void Open()
        {
            if (Wish.Player.Instance == null)
            {
                TolkSpeech.Speak(Localization.Language.T(
                    "Les points favoris ne servent qu'en jeu.", "Favourite points only work in game."), true);
                return;
            }

            string here = WorldLinks.CurrentScene;

            // Ceux d'ici d'abord, du plus proche au plus loin, puis les autres : on cherche presque
            // toujours un point de la zone où l'on se trouve.
            List<Favorites.Point> points = Favorites.Here();
            points.AddRange(Favorites.All()
                .Where(p => !string.Equals(p.Scene, here, System.StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p.Name));

            var entries = new List<string>
            {
                Localization.Language.T("Ajouter un point ici", "Add a point here"),
            };
            entries.AddRange(points.Select(p => Label(p, here)));

            ListMenu.Open(Localization.Language.T("Points favoris", "Favourite points"), entries,
                chosen =>
                {
                    if (chosen == 0) { AddHere(); return; }

                    int index = chosen - 1;
                    if (index >= 0 && index < points.Count) OpenPoint(points[index]);
                },
                owner: OwnerTag);
        }

        private static string Label(Favorites.Point point, string here)
        {
            if (string.Equals(point.Scene, here, System.StringComparison.OrdinalIgnoreCase))
                return point.Name;

            // Dire la zone évite de croire qu'un point est à deux pas alors qu'il est à deux
            // chargements de là.
            return $"{point.Name}, {SceneNames.Translate(point.Scene)}";
        }

        private static void AddHere()
        {
            TextPrompt.Ask(
                Localization.Language.T("Nom du point favori ?", "Name of the favourite point?"),
                null,
                name =>
                {
                    if (Favorites.AddHere(name))
                    {
                        TolkSpeech.Speak(Localization.Language.T(
                            $"{name} ajouté ici.", $"{name} added here."), true);
                    }
                    else
                    {
                        TolkSpeech.Speak(Localization.Language.T(
                            "Impossible d'ajouter ce point.", "Cannot add this point."), true);
                    }
                });
        }

        private static void OpenPoint(Favorites.Point point)
        {
            var actions = new List<string>
            {
                Localization.Language.T("S'y rendre", "Go there"),
                Localization.Language.T("Renommer", "Rename"),
                Localization.Language.T("Supprimer", "Delete"),
            };

            ListMenu.Open(point.Name, actions,
                chosen =>
                {
                    switch (chosen)
                    {
                        case 0: GoTo(point); break;
                        case 1: Rename(point); break;
                        default: ConfirmDelete(point); break;
                    }
                },
                onExitUp: Open,
                owner: OwnerTag);
        }

        private static void GoTo(Favorites.Point point)
        {
            var target = new Vector3(point.X, point.Y, 0f);

            // Dans la même zone, on marche ; ailleurs, le trajet traverse ce qu'il faut et
            // s'arrête sur le point exact — le favori EST une coordonnée, on peut donc y aller
            // jusqu'au bout plutôt que de s'arrêter au seuil.
            if (string.Equals(point.Scene, WorldLinks.CurrentScene, System.StringComparison.OrdinalIgnoreCase))
            {
                PathingController.TravelTo(target, point.Name);
                return;
            }

            if (Journey.Start(point.Scene, point.Name, target)) return;

            TolkSpeech.Speak(Localization.Language.T(
                $"Je ne connais pas encore le chemin vers {SceneNames.Translate(point.Scene)}. Allez-y une première fois, et il sera retenu.",
                $"I don't know the way to {SceneNames.Translate(point.Scene)} yet. Go there once, and it will be remembered."), true);
        }

        private static void Rename(Favorites.Point point)
        {
            // Le nom actuel pré-remplit la saisie : corriger une faute ne doit pas obliger à tout
            // retaper.
            TextPrompt.Ask(
                Localization.Language.T("Nouveau nom ?", "New name?"),
                point.Name,
                name =>
                {
                    Favorites.Rename(point, name);
                    TolkSpeech.Speak(Localization.Language.T($"Renommé en {name}.", $"Renamed to {name}."), true);
                });
        }

        /// <summary>
        /// Supprimer demande confirmation. Un point favori se repose en quelques secondes, mais
        /// seulement si l'on est SUR place : effacé par erreur depuis l'autre bout de la carte, il
        /// est perdu pour de bon. La confirmation coûte une touche ; l'erreur coûte un aller-retour.
        /// </summary>
        private static void ConfirmDelete(Favorites.Point point)
        {
            var choices = new List<string>
            {
                Localization.Language.T("Non, garder ce point", "No, keep this point"),
                Localization.Language.T("Oui, supprimer", "Yes, delete"),
            };

            ListMenu.Open(
                Localization.Language.T($"Supprimer {point.Name} ?", $"Delete {point.Name}?"),
                choices,
                chosen =>
                {
                    if (chosen != 1) { Open(); return; }

                    string name = point.Name;
                    Favorites.Remove(point);
                    TolkSpeech.Speak(Localization.Language.T($"{name} supprimé.", $"{name} deleted."), true);
                },
                owner: OwnerTag);
        }
    }
}
