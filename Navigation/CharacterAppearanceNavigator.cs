using I2.Loc;
using Wish;
using SunHavenAccess.Speech;
using SunHavenAccess.Util;

namespace SunHavenAccess.Navigation
{
    /// <summary>
    /// Écran de création de personnage : la grille d'apparence (corps, cheveux, yeux, visage,
    /// torse, jambes, tête, queue) est faite de vignettes (`Wish.ClothingImageButton`) qui,
    /// comme la carte du monde (voir MapNavigator), répondent à la sélection/au clic mais
    /// N'HÉRITENT PAS de `Selectable` — injoignables au clavier sans ce système dédié.
    ///
    /// Contrairement à la carte, PAS BESOIN de lire la grille visuelle instanciée à l'écran :
    /// le jeu expose déjà tout ce qu'il faut en DONNÉES pures et PUBLIQUES —
    /// `NewCharacterCreator.CycleLayer(direction, layer)` avance/recule la sélection d'une
    /// catégorie (gère déjà lui-même la compatibilité avec la race choisie), et
    /// `CharacterClothingStyles.ClothingStyles[layer][id]` (statique) donne le nom lisible de
    /// l'option actuellement choisie (`CurrentCharacter.StyleData[(byte)layer]`). On pilote la
    /// logique du jeu directement plutôt que de deviner la hiérarchie Unity de la grille.
    ///
    /// Catégories couvertes : les 7 dont le nom du bouton correspond directement et sans
    /// ambiguïté à une valeur de `Wish.ClothingLayer`, plus Queue (Tail, confirmé). "Ailes"
    /// (wingsButton) n'a PAS d'équivalent évident dans l'énum ClothingLayer (pas de valeur
    /// "Wings") — non couvert pour l'instant plutôt que de deviner au hasard (ex. "Back")
    /// et risquer d'annoncer des informations fausses.
    /// </summary>
    public static class CharacterAppearanceNavigator
    {
        private static readonly (ClothingLayer Layer, string Label)[] Categories =
        {
            (ClothingLayer.Body, "Corps"),
            (ClothingLayer.Hair, "Cheveux"),
            (ClothingLayer.Eyes, "Yeux"),
            (ClothingLayer.Face, "Visage"),
            (ClothingLayer.Chest, "Torse"),
            (ClothingLayer.Pants, "Jambes"),
            (ClothingLayer.Head, "Tête"),
            (ClothingLayer.Tail, "Queue"),
        };

        private static int _categoryIndex;

        public static void NextCategory() => ChangeCategory(1);
        public static void PreviousCategory() => ChangeCategory(-1);
        public static void NextOption() => CycleOption(1);
        public static void PreviousOption() => CycleOption(-1);

        private static void ChangeCategory(int direction)
        {
            NewCharacterCreator creator = SingletonBehaviour<NewCharacterCreator>.Instance;
            if (creator == null || !creator.gameObject.activeInHierarchy)
            {
                TolkSpeech.Speak("La création de personnage n'est pas ouverte.", true);
                return;
            }

            _categoryIndex = ((_categoryIndex + direction) % Categories.Length + Categories.Length) % Categories.Length;
            (ClothingLayer layer, string label) = Categories[_categoryIndex];

            // Repeuple la grille visuelle correspondante (même effet que cliquer le bouton de
            // catégorie) pour que l'état du jeu reste cohérent, même si on ne la lit jamais.
            creator.SetClothingImages(layer);
            AnnounceCurrent(creator, layer, label);
        }

        private static void CycleOption(int direction)
        {
            NewCharacterCreator creator = SingletonBehaviour<NewCharacterCreator>.Instance;
            if (creator == null || !creator.gameObject.activeInHierarchy)
            {
                TolkSpeech.Speak("La création de personnage n'est pas ouverte.", true);
                return;
            }

            (ClothingLayer layer, string label) = Categories[_categoryIndex];
            creator.CycleLayer(direction, layer, cycleBody: layer == ClothingLayer.Body);
            AnnounceCurrent(creator, layer, label);
        }

        private static void AnnounceCurrent(NewCharacterCreator creator, ClothingLayer layer, string label)
        {
            string name = "Aucun";
            if (creator.CurrentCharacter != null
                && creator.CurrentCharacter.StyleData.TryGetValue((byte)layer, out string id)
                && !string.IsNullOrEmpty(id)
                && CharacterClothingStyles.ClothingStyles.TryGetValue(layer, out var styles)
                && styles.TryGetValue(id, out ClothingLayerData data))
            {
                string translated = TextUtil.Clean(LocalizeText.TranslateText(data.keyDisplayName, data.menuName));
                if (!string.IsNullOrWhiteSpace(translated)) name = translated;
            }

            TolkSpeech.Speak($"{label} : {name}.", true);
        }
    }
}
