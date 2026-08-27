using System;
using System.Runtime.InteropServices;
using BepInEx.Logging;

namespace SunHavenAccess.Speech
{
    /// <summary>
    /// Sortie vocale directe, en P/Invoke natif pur (pas d'assembly .NET manquante, pas
    /// d'activation COM managée — les deux se sont révélées non supportées par le runtime
    /// Mono embarqué dans Unity) :
    /// 1) NVDA en direct, via son propre client officiel (nvdaControllerClient64.dll) —
    ///    aucune détection préalable nécessaire, la fonction échoue proprement si NVDA
    ///    n'est pas lancé.
    /// 2) SAPI en repli garanti, via le pilote SAPI natif de Tolk.dll (la création de la
    ///    voix COM se fait entièrement en C++ à l'intérieur de la DLL ; côté C#, ce ne sont
    ///    que de simples appels P/Invoke, ce qui fonctionne très bien sous Mono).
    /// </summary>
    public static class TolkSpeech
    {
        private static ManualLogSource _log;
        private static string _lastMessage = "";
        private static bool _nvdaAvailable;
        private static bool _sapiAvailable;

        /// <summary>Une seule trace par session : Speak est appelée des milliers de fois.</summary>
        private static bool _translationFailed;

        // --- Client officiel NVDA ---
        [DllImport("nvdaControllerClient64.dll")]
        private static extern int nvdaController_testIfRunning();

        [DllImport("nvdaControllerClient64.dll", CharSet = CharSet.Unicode)]
        private static extern int nvdaController_speakText(string text);

        [DllImport("nvdaControllerClient64.dll")]
        private static extern int nvdaController_cancelSpeech();

        // --- Pilote SAPI natif de Tolk (aucune dépendance managée) ---
        [DllImport("Tolk.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void Tolk_Load();

        [DllImport("Tolk.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void Tolk_Unload();

        [DllImport("Tolk.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void Tolk_TrySAPI([MarshalAs(UnmanagedType.I1)] bool trySAPI);

        [DllImport("Tolk.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void Tolk_PreferSAPI([MarshalAs(UnmanagedType.I1)] bool preferSAPI);

        [DllImport("Tolk.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool Tolk_HasSpeech();

        [DllImport("Tolk.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool Tolk_Output([MarshalAs(UnmanagedType.LPWStr)] string str,
            [MarshalAs(UnmanagedType.I1)] bool interrupt);

        [DllImport("Tolk.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool Tolk_Silence();

        public static bool Available => _nvdaAvailable || _sapiAvailable;

        public static void Init(ManualLogSource log)
        {
            _log = log;

            try
            {
                int running = nvdaController_testIfRunning();
                _nvdaAvailable = running == 0;
                _log.LogInfo(_nvdaAvailable
                    ? "NVDA détecté directement via nvdaControllerClient."
                    : $"NVDA non détecté par nvdaControllerClient (code {running}).");
            }
            catch (Exception e)
            {
                _nvdaAvailable = false;
                _log.LogWarning("nvdaControllerClient64.dll indisponible : " + e.Message);
            }

            try
            {
                Tolk_Load();
                // On force SAPI en priorité : la détection automatique NVDA/JAWS/etc. de Tolk
                // n'est pas fiable ici, et on gère déjà NVDA nous-mêmes juste au-dessus.
                Tolk_TrySAPI(true);
                Tolk_PreferSAPI(true);
                _sapiAvailable = Tolk_HasSpeech();
                _log.LogInfo(_sapiAvailable
                    ? "Synthèse SAPI (via Tolk) prête."
                    : "Tolk_HasSpeech() a renvoyé false : aucune voix SAPI disponible.");
            }
            catch (Exception e)
            {
                _sapiAvailable = false;
                _log.LogWarning("Impossible d'initialiser le pilote SAPI de Tolk.dll : " + e);
            }

            if (!Available)
            {
                _log.LogError("Aucune sortie vocale disponible (ni NVDA, ni SAPI). Le mod restera muet.");
            }
        }

        /// <summary>Parle le texte. interrupt=true coupe la parole en cours (par défaut).</summary>
        public static void Speak(string text, bool interrupt = true)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            // La traduction se fait ici, au dernier moment, plutôt qu'aux cent soixante-quinze
            // endroits d'où l'on parle : le français écrit dans le code reste la seule version
            // vérifiée à l'oreille, et l'anglais ne peut donc rien casser de ce qui marche.
            // En français, Translate rend la chaîne inchangée sans rien parcourir.
            //
            // Sous garde, et sans exception près : traduire est un CONFORT, parler est la raison
            // d'être du mod. Une erreur ici — table mal formée, expression régulière refusée par
            // le Mono d'Unity — ne doit jamais coûter la parole à qui n'a que ça. On dit alors la
            // phrase française telle quelle, ce qui est très exactement ce qu'on faisait avant.
            try { text = SunHavenAccess.Localization.Translator.Translate(text); }
            catch (Exception e)
            {
                if (!_translationFailed)
                {
                    _translationFailed = true;
                    _log?.LogWarning("Traduction désactivée pour la session : " + e);
                }
            }

            _lastMessage = text;
            bool spoken = false;

            if (_nvdaAvailable)
            {
                try
                {
                    if (interrupt) nvdaController_cancelSpeech();
                    int result = nvdaController_speakText(text);
                    spoken = result == 0;
                    if (!spoken)
                    {
                        _log?.LogWarning($"nvdaController_speakText a échoué (code {result}) pour : \"{text}\"");
                    }
                }
                catch (Exception e)
                {
                    _log?.LogWarning("Erreur en parlant via NVDA : " + e.Message);
                    _nvdaAvailable = false; // NVDA a probablement été fermé : bascule sur SAPI dès maintenant
                }
            }

            if (!spoken && _sapiAvailable)
            {
                try
                {
                    spoken = Tolk_Output(text, interrupt);
                    if (!spoken)
                    {
                        _log?.LogWarning("Tolk_Output (SAPI) a renvoyé false pour : \"" + text + "\"");
                    }
                }
                catch (Exception e)
                {
                    _log?.LogWarning("Erreur en parlant via SAPI : " + e.Message);
                }
            }

            if (!spoken)
            {
                _log?.LogInfo("[muet, aucune sortie vocale n'a fonctionné] " + text);
            }
        }

        /// <summary>Répète la dernière phrase annoncée (touche dédiée).</summary>
        public static void Repeat()
        {
            if (!string.IsNullOrEmpty(_lastMessage)) Speak(_lastMessage, true);
        }

        public static void Silence()
        {
            try { if (_nvdaAvailable) nvdaController_cancelSpeech(); } catch { }
            try { if (_sapiAvailable) Tolk_Silence(); } catch { }
        }

        public static void Shutdown()
        {
            try { if (_sapiAvailable) Tolk_Unload(); } catch { }
            _sapiAvailable = false;
        }
    }
}
