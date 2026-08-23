using Wish;
using SunHavenAccess.Speech;

namespace SunHavenAccess.Farming
{
    /// <summary>
    /// Confirmations vocales des actions agricoles, pour ne pas avoir à re-vérifier la case à
    /// chaque fois avec la touche dédiée. Le jeu expose déjà des évènements statiques pour la
    /// plupart de ces actions (Wish.Hoe.onHoe, Wish.WateringCan.onWater, Wish.Seeds.onPlant,
    /// Wish.Crop.onCropPickedUp...) : on s'y abonne directement, plus simple et plus fiable
    /// qu'un patch Harmony.
    /// </summary>
    public static class FarmingAnnouncer
    {
        private static bool _initialized;

        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;

            Hoe.onHoe += OnHoe;
            WateringCan.onWater += OnWater;
            WateringCan.onWateringCanEmpty += OnWateringCanEmpty;
            WateringCan.onFillUpWateringCan += OnFillUpWateringCan;
            Seeds.onPlant += OnPlant;
            Crop.onCropPickedUp += OnCropPickedUp;
            Crop.onCropInfused += OnCropInfused;
        }

        private static void OnHoe() =>
            TolkSpeech.Speak("Terre labourée.", interrupt: false);

        private static void OnWater(int cropId) =>
            TolkSpeech.Speak("Arrosé.", interrupt: false);

        private static void OnWateringCanEmpty() =>
            TolkSpeech.Speak("Arrosoir vide.", interrupt: false);

        private static void OnFillUpWateringCan() =>
            TolkSpeech.Speak("Arrosoir rempli.", interrupt: false);

        private static void OnPlant(int seedId) =>
            TolkSpeech.Speak("Planté.", interrupt: false);

        private static void OnCropPickedUp(int seedId) =>
            TolkSpeech.Speak("Récolté.", interrupt: false);

        private static void OnCropInfused(int seedId) =>
            TolkSpeech.Speak("Culture infusée de mana.", interrupt: false);
    }
}
