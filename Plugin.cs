using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using SunHavenAccess.Speech;
using SunHavenAccess.Patches;
using SunHavenAccess.Config;
using SunHavenAccess.Localization;
using SunHavenAccess.Farming;
using SunHavenAccess.Info;

namespace SunHavenAccess
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.kleitz.sunhavenaccess";
        public const string PluginName = "Sun Haven Access";
        public const string PluginVersion = "0.1.0";

        internal static ManualLogSource Log;
        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            // Sun Haven détruit lui-même les objets qu'il ne reconnaît pas (confirmé
            // empiriquement), y compris l'objet hébergeant BepInEx et donc notre plugin.
            // On indique au plus tôt quel objet protéger, avant même que Harmony ne soit
            // initialisé, pour réduire au minimum la fenêtre de vulnérabilité.
            NoKillPatch.ProtectedRoot = gameObject.transform.root.gameObject;

            ModConfig.Init(Config);
            // Le bip de bord de la navigation lit son état ici plutôt que d'interroger la config
            // à chaque pression (il peut être déclenché plusieurs fois par seconde).
            Speech.UiSound.Enabled = ModConfig.EdgeSound.Value;
            ModConfig.EdgeSound.SettingChanged += (_, _) => Speech.UiSound.Enabled = ModConfig.EdgeSound.Value;
            TolkSpeech.Init(Log);
            FarmingAnnouncer.Init();
            CombatStateAnnouncer.Init();
            QuestAnnouncer.Init();

            _harmony = new Harmony(PluginGuid);
            try
            {
                _harmony.PatchAll();
            }
            catch (System.Exception e)
            {
                Log.LogError("PatchAll() a levé une exception : " + e);
            }

            Log.LogInfo($"{PluginName} {PluginVersion} chargé.");
            TolkSpeech.Speak(
                "Mod d'accessibilité Sun Haven chargé. " +
                $"Touche {Strings.KeyName(ModConfig.Help.Value)} pour l'aide.", true);
        }

        private void OnDestroy()
        {
            // IMPORTANT : on NE dépatch PAS ici. Sun Haven détruit l'objet hébergeant le
            // plugin très tôt en conditions normales (indépendamment de notre volonté,
            // malgré NoKillPatch) ; si on désinstalle nos patches Harmony à ce moment-là,
            // plus rien ne fonctionne jamais, y compris nos points d'accroche censés faire
            // tourner le mod à chaque frame (voir TickDriverPatch). On laisse donc les
            // patches actifs pour le reste de la session — ils ne dépendent pas de la survie
            // de cet objet particulier.
        }
    }
}
