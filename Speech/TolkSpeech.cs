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
            RecordInHistory(text);
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

        /// <summary>
        /// Les dernières annonces, la plus récente en tête. Vingt suffisent : au-delà, on ne
        /// remonte plus, on cherche.
        /// </summary>
        private static readonly System.Collections.Generic.List<string> _history =
            new System.Collections.Generic.List<string>();

        private const int HistorySize = 20;

        /// <summary>Où l'on en est dans la remontée ; -1 quand on n'a pas commencé.</summary>
        private static int _historyIndex = -1;

        /// <summary>Instant de la dernière répétition, pour distinguer une série d'un nouvel appui.</summary>
        private static float _lastRepeat;

        /// <summary>
        /// Répète ce qui vient d'être dit, et REMONTE LE FIL si l'on insiste.
        ///
        /// Ne redire que la dernière phrase ne sert qu'à une chose : réentendre ce qu'on a mal
        /// saisi. Or ce qu'on a manqué est souvent l'avant-dernière chose — une annonce en a
        /// couvert une autre, on s'est absenté deux secondes, un avertissement est passé pendant
        /// qu'on écoutait autre chose. Des appuis rapprochés remontent donc d'un cran à chaque
        /// fois, comme le fait stardew-access ; un appui isolé repart de la plus récente.
        ///
        /// Trois secondes séparent les deux gestes : assez pour enchaîner sans se presser, assez
        /// court pour qu'un appui plus tard soit compris comme une nouvelle demande.
        /// </summary>
        public static void Repeat()
        {
            if (_history.Count == 0)
            {
                if (!string.IsNullOrEmpty(_lastMessage)) Speak(_lastMessage, true);
                return;
            }

            bool continuing = UnityEngine.Time.unscaledTime - _lastRepeat < 3f;
            _lastRepeat = UnityEngine.Time.unscaledTime;

            _historyIndex = continuing ? _historyIndex + 1 : 0;

            if (_historyIndex >= _history.Count)
            {
                // Bout du fil : on le dit et l'on y reste, plutôt que de reboucler en silence sur
                // la plus récente — on croirait que rien ne s'est passé.
                _historyIndex = _history.Count - 1;
                UiSound.EdgeBump();
            }

            string message = _history[_historyIndex];
            SpeakWithoutRecording(_historyIndex == 0
                ? message
                : Localization.Language.T($"{_historyIndex + 1} en arrière : {message}",
                                          $"{_historyIndex + 1} back: {message}"));
        }

        /// <summary>
        /// Annonce SANS entrer dans l'historique. Sans cela, remonter le fil y ajouterait ses
        /// propres répétitions et l'on tournerait en rond au lieu de remonter.
        /// </summary>
        private static void SpeakWithoutRecording(string text)
        {
            _suspendHistory = true;
            try { Speak(text, true); }
            finally { _suspendHistory = false; }
        }

        private static bool _suspendHistory;

        /// <summary>
        /// Retient une annonce. Les répétitions immédiates ne sont pas gardées : la case devant soi
        /// est décrite à chaque pas, et sans ce filtre l'historique se remplirait de vingt fois la
        /// même herbe, ne remontant plus que trois secondes de jeu.
        /// </summary>
        private static void RecordInHistory(string text)
        {
            if (_suspendHistory || string.IsNullOrWhiteSpace(text)) return;
            if (_history.Count > 0 && _history[0] == text) return;

            _history.Insert(0, text);
            if (_history.Count > HistorySize) _history.RemoveAt(_history.Count - 1);

            // Une nouvelle annonce recommence le fil : remonter doit repartir d'ici, pas de l'endroit
            // où l'on s'était arrêté il y a dix minutes.
            _historyIndex = -1;
        }

        /// <summary>Toutes les annonces conservées, la plus récente en tête, pour une liste.</summary>
        public static System.Collections.Generic.IReadOnlyList<string> History => _history;

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
