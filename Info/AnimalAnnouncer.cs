using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Wish;
using SunHavenAccess.Speech;

namespace SunHavenAccess.Info
{
    /// <summary>
    /// État des animaux de la ferme.
    ///
    /// S'occuper des bêtes est une routine quotidienne — nourrir, caresser, ramasser ce qu'elles
    /// produisent — et tout y est signalé visuellement : une icône au-dessus de la tête de
    /// l'animal, un objet posé au sol. Sans la vue, la seule méthode était d'aller au contact de
    /// chaque animal l'un après l'autre pour découvrir son état, dans un enclos où l'on ne sait
    /// même pas combien de bêtes on possède.
    ///
    /// D'où deux niveaux : chaque animal repéré au scanner dit désormais son état, et une touche
    /// donne le bilan de tout le troupeau présent. Ce bilan est ce qui manquait le plus — il
    /// répond en une phrase à « me reste-t-il quelque chose à faire avant de me coucher ? », une
    /// question qu'un joueur voyant règle d'un coup d'œil sur l'enclos.
    /// </summary>
    public static class AnimalAnnouncer
    {
        /// <summary>
        /// Décrit un animal : son nom, puis ce qu'il reste à faire pour lui. Utilisée par le
        /// scanner, donc volontairement brève — c'est une ligne parmi d'autres dans une liste.
        /// </summary>
        public static string Describe(Animal animal)
        {
            string name = SafeName(animal);
            AnimalPositionData data = DataOf(animal);
            if (data == null) return name;

            var todo = new List<string>();
            if (data.Hungry) todo.Add(Localization.Language.T("à nourrir", "to feed"));
            if (!data.hasPetted) todo.Add(Localization.Language.T("à caresser", "to pet"));
            if (data.droppedItem) todo.Add(Localization.Language.T("produit au sol", "product on the ground"));

            return todo.Count == 0
                ? Localization.Language.T($"{name}, rien à faire", $"{name}, nothing to do")
                : $"{name}, {string.Join(", ", todo)}";
        }

        /// <summary>
        /// Bilan de tous les animaux de la scène courante.
        ///
        /// On compte plutôt qu'on n'énumère : dans une grange pleine, réciter quinze bêtes une à
        /// une serait plus long que d'aller les voir. Les noms ne sont donnés que pour le petit
        /// troupeau, où ils aident vraiment à savoir de qui on parle.
        /// </summary>
        public static void AnnounceHerdStatus()
        {
            Animal[] animals = Object.FindObjectsOfType<Animal>()
                .Where(IsInActiveScene)
                .ToArray();

            if (animals.Length == 0)
            {
                TolkSpeech.Speak("Aucun animal ici.", true);
                return;
            }

            var hungry = new List<string>();
            var unpetted = new List<string>();
            int products = 0;
            int unknown = 0;

            foreach (Animal animal in animals)
            {
                AnimalPositionData data = DataOf(animal);
                if (data == null) { unknown++; continue; }

                if (data.Hungry) hungry.Add(SafeName(animal));
                if (!data.hasPetted) unpetted.Add(SafeName(animal));
                if (data.droppedItem) products++;
            }

            var parts = new List<string>
            {
                Localization.Language.T(
                    $"{animals.Length} animal{(animals.Length > 1 ? "ux" : "")}",
                    $"{animals.Length} animal{(animals.Length > 1 ? "s" : "")}")
            };

            parts.Add(hungry.Count == 0
                ? Localization.Language.T("tous nourris", "all fed")
                : Count(hungry.Count, Localization.Language.T("à nourrir", "to feed")) + Names(hungry));

            parts.Add(unpetted.Count == 0
                ? Localization.Language.T("tous caressés", "all petted")
                : Count(unpetted.Count, Localization.Language.T("à caresser", "to pet")) + Names(unpetted));

            if (products > 0)
                parts.Add(Localization.Language.T(
                    $"{products} produit{(products > 1 ? "s" : "")} au sol",
                    $"{products} product{(products > 1 ? "s" : "")} on the ground"));

            // Un animal sans données n'est pas encore initialisé, ou n'appartient pas au joueur.
            // Le dire plutôt que de le compter comme « rien à faire » : un compte faussement
            // rassurant est pire que pas de compte du tout.
            if (unknown > 0)
                parts.Add(Localization.Language.T(
                    $"{unknown} dont l'état est inconnu",
                    $"{unknown} whose state is unknown"));

            TolkSpeech.Speak(string.Join(", ", parts) + ".", true);
        }

        private static string Count(int n, string label) => $"{n} {label}";

        /// <summary>
        /// Les noms ne sont cités qu'en petit nombre : au-delà, la phrase devient une liste qu'on
        /// ne retient pas, alors que le compte, lui, reste utile.
        /// </summary>
        private static string Names(List<string> names) =>
            names.Count <= 3 ? $" ({string.Join(", ", names)})" : string.Empty;

        private static string SafeName(Animal animal)
        {
            AnimalPositionData data = DataOf(animal);
            if (data != null && !string.IsNullOrWhiteSpace(data.name)) return data.name;

            try
            {
                string species = animal.AnimalName;
                if (!string.IsNullOrWhiteSpace(species)) return species;
            }
            catch { }

            return "Animal";
        }

        private static AnimalPositionData DataOf(Animal animal)
        {
            try { return animal?.animalItem?.animalData; }
            catch { return null; }
        }

        /// <summary>
        /// Sun Haven charge chaque carte dans une scène Unity ADDITIVE : sans ce filtre, on
        /// compterait les bêtes des cartes voisines encore chargées en mémoire. Même règle que
        /// partout ailleurs dans le mod (Scanner, NPCFinder).
        /// </summary>
        private static bool IsInActiveScene(Animal animal)
        {
            if (animal == null || !animal.gameObject.activeInHierarchy) return false;
            try { return animal.Scene == ScenePortalManager.ActiveSceneName; }
            catch { return animal.gameObject.scene.name == ScenePortalManager.ActiveSceneName; }
        }
    }
}
