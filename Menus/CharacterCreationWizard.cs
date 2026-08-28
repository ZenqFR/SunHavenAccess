using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using I2.Loc;
using UnityEngine;
using UnityEngine.UI;
using Wish;
using SunHavenAccess.Speech;
using SunHavenAccess.Util;

namespace SunHavenAccess.Menus
{
    /// <summary>
    /// La création de personnage refaite en questions successives, plutôt qu'en écran à explorer.
    ///
    /// L'écran du jeu est fait de trois colonnes et d'une grille d'apparence : des cheveux, des
    /// yeux, des couleurs. Pour un joueur aveugle, l'essentiel de cet écran ne sert à rien — on ne
    /// choisit pas une coiffure qu'on ne verra jamais — et le peu qui compte vraiment y est noyé.
    /// Or trois choix seulement changent la partie : la RACE, qui donne un atout ; le MÉTIER, qui
    /// donne l'équipement de départ ; et l'ANNIVERSAIRE, que les habitants retiennent.
    ///
    /// L'assistant ne pose donc que ces questions-là, une par une, chacune avec ce qu'il faut pour
    /// décider — la description de chaque race et son atout, les objets de départ de chaque métier.
    /// L'apparence garde ses valeurs par défaut ; qui veut la régler peut toujours fermer
    /// l'assistant et parcourir l'écran du jeu comme avant.
    ///
    /// Rien n'est réimplémenté : chaque réponse appelle la méthode publique correspondante du jeu
    /// (`SetRace`, `UpdateProfession`, `SetBirthdayMonthDay`, `AddNewCharacter`). Ce que l'assistant
    /// construit, c'est l'ORDRE des questions, pas les règles.
    /// </summary>
    public static class CharacterCreationWizard
    {
        /// <summary>Une saison compte 28 jours — `DayCycle.MonthDay` en fait foi.</summary>
        private const int DaysPerSeason = 28;

        /// <summary>Signe ce module sur les listes qu'il ouvre, pour ne refermer que les siennes.</summary>
        private const string OwnerTag = "création de personnage";

        /// <summary>Ouvre une liste au nom de l'assistant, pour qu'aucun autre module ne la ferme.</summary>
        private static void OpenOwned(string title, List<string> entries, Action<int> onActivate = null) =>
            ListMenu.Open(title, entries, onActivate, owner: OwnerTag);

        private enum Step { Idle, Race, Profession, BirthSeason, BirthDay, Name, SkipIntro, Confirm }

        private static Step _step = Step.Idle;
        private static bool _onScreen;
        private static float _nextCheck;

        // Retenus au fil des questions, pour le récapitulatif final.
        private static string _raceLabel;
        private static string _professionLabel;
        private static Season _season;
        private static int _day;

        public static bool IsRunning => _step != Step.Idle;

        // ------------------------------------------------------------------ Déclenchement

        /// <summary>
        /// Ouvre l'assistant dès que l'écran de création apparaît. C'est le seul écran du jeu où
        /// l'on ne peut rien faire tant qu'on n'a pas compris sa disposition : le proposer d'emblée
        /// évite d'avoir à connaître une touche pour commencer sa partie.
        /// </summary>
        public static void Tick()
        {
            // La saisie du nom est lue à CHAQUE IMAGE, jamais au rythme espacé du reste.
            //
            // `GetKeyDown` n'est vrai qu'une seule image. En le consultant quatre fois par
            // seconde, on avait environ une chance sur quinze de tomber sur la bonne : l'Entrée
            // qui valide le nom, et l'Échap qui referme l'assistant, étaient ignorées presque à
            // chaque fois. Espacer une détection d'écran est sans conséquence ; espacer une
            // lecture de touche, c'est perdre la touche.
            if (_step == Step.Name) { TickNameEntry(); return; }

            if (Time.unscaledTime < _nextCheck) return;
            _nextCheck = Time.unscaledTime + 0.25f;

            bool present = Creator() != null;
            if (present != _onScreen)
            {
                _onScreen = present;
                if (present) Start();
                else Cancel();
            }

            // Échap ferme la liste : l'assistant s'arrête avec elle. Sans cela il resterait en
            // attente d'une réponse à une question qui n'est plus posée, et il faudrait quitter
            // l'écran pour s'en sortir. C'est aussi la porte de sortie annoncée à l'ouverture, pour
            // qui veut régler l'apparence à la main.
            if (_step != Step.Idle && !ListMenu.IsOpen)
            {
                _step = Step.Idle;
                TolkSpeech.Speak(Localization.Language.T(
                    "Assistant fermé. L'écran de création reste utilisable aux flèches.",
                    "Wizard closed. The creation screen is still usable with the arrows."), true);
            }
        }

        private static NewCharacterCreator Creator()
        {
            try
            {
                NewCharacterCreator creator = SingletonBehaviour<NewCharacterCreator>.Instance;
                return creator != null && creator.isActiveAndEnabled ? creator : null;
            }
            catch { return null; }
        }

        public static void Start()
        {
            if (Creator() == null)
            {
                TolkSpeech.Speak("La création de personnage n'est pas ouverte.", true);
                return;
            }

            TolkSpeech.Speak(Localization.Language.T(
                "Création de personnage. Quatre questions : la race, le métier, l'anniversaire et le nom. " +
                "L'apparence garde ses valeurs par défaut — Échap à tout moment pour fermer l'assistant et régler l'écran vous-même.",
                "Character creation. Four questions: race, profession, birthday and name. " +
                "Appearance keeps its defaults — Escape at any time to close this and set the screen up yourself."), true);

            AskRace();
        }

        private static void Cancel()
        {
            _step = Step.Idle;
            ListMenu.CloseIfOwner(OwnerTag, false);
        }

        // ------------------------------------------------------------------ 1. La race

        private static void AskRace()
        {
            _step = Step.Race;

            List<Race> races = Enum.GetValues(typeof(Race)).Cast<Race>().ToList();


            var labels = races
                .Select(r => DescribeRace(r))
                .ToList();

            OpenOwned(Localization.Language.T("Choisir une race", "Choose a race"), labels, chosen =>
            {
                Race race = races[chosen];
                _raceLabel = RaceName(race);
                Apply(() => Creator().SetRace(race));
                AskProfession();
            });
        }

        /// <summary>
        /// Une race avec de quoi la choisir : son nom, ce qu'elle est, et l'atout qu'elle donne.
        ///
        /// L'atout est la seule chose qui change réellement la partie — le reste est du décor. On
        /// le dit donc systématiquement, même quand la description est longue : c'est précisément
        /// l'information qu'on vient chercher.
        /// </summary>
        private static string DescribeRace(Race race)
        {
            var parts = new List<string>
            {
                RaceName(race)
            };

            try
            {
                if (Creator().raceInfo.TryGetValue(race, out RaceInfo info) && info != null)
                {
                    string description = TextUtil.Clean(LocalizeText.TranslateText(info.keyDescription, info.description));
                    if (!string.IsNullOrWhiteSpace(description)) parts.Add(description);

                    string perk = TextUtil.Clean(LocalizeText.TranslateText(info.keyPerkDescription, info.perkDescription));
                    if (!string.IsNullOrWhiteSpace(perk))
                        parts.Add(Localization.Language.T("Atout : ", "Perk: ") + perk);
                }
            }
            catch { }

            return string.Join(". ", parts) + ".";
        }

        private static string RaceName(Race race)
        {
            // En anglais, les noms de l'énumération sont déjà les bons mots.
            if (Localization.Language.IsEnglish) return race.ToString();

            switch (race)
            {
                case Race.Human:     return "Humain";
                case Race.Elf:       return "Elfe";
                case Race.Amari:     return "Amari";
                case Race.Naga:      return "Naga";
                case Race.Elemental: return "Élémentaire";
                case Race.Angel:     return "Ange";
                case Race.Demon:     return "Démon";
                default:             return race.ToString();
            }
        }

        // ------------------------------------------------------------------ 2. Le métier

        private static void AskProfession()
        {
            _step = Step.Profession;

            StartingProfessionInfo[] professions;
            try { professions = Creator().professionInfos ?? new StartingProfessionInfo[0]; }
            catch { professions = new StartingProfessionInfo[0]; }

            if (professions.Length == 0) { AskBirthSeason(); return; } // rien à choisir : on passe

            // Sept métiers se traduisent par la table du jeu ; les trois autres sont relevés sur son
            // écran (voir MissingFrenchNames). Lire les boutons directement s'est révélé impossible :
            // le panneau central n'affiche que la catégorie sélectionnée, donc les boutons de métier
            // ne sont pas présents au moment où l'assistant pose la question.
            var labels = professions
                .Select(p => DescribeProfession(p))
                .ToList();

            OpenOwned(Localization.Language.T("Choisir un métier de départ", "Choose a starting profession"),
                labels, chosen =>
                {
                    _professionLabel = ProfessionName(TextUtil.Clean(professions[chosen].name));
                    int index = chosen;
                    Apply(() => Creator().UpdateProfession(index));
                    AskBirthSeason();
                });
        }

        /// <summary>
        /// Un métier se distingue par son ÉQUIPEMENT DE DÉPART, et par rien d'autre à ce stade :
        /// aucun n'est verrouillé, aucun ne ferme de voie. Les citer est donc la seule façon
        /// honnête d'expliquer la différence.
        /// </summary>
        private static string DescribeProfession(StartingProfessionInfo profession)
        {
            string name = ProfessionName(TextUtil.Clean(profession.name));

            try
            {
                var items = (profession.startingItems ?? new List<ItemInfo>())
                    .Where(i => i?.item != null)
                    .Select(i => i.amount > 1
                        ? $"{i.amount} {i.item.UnformattedDisplayName}"
                        : i.item.UnformattedDisplayName)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();

                if (items.Count > 0)
                    return name + Localization.Language.T(". Objets de départ : ", ". Starting items: ")
                                + string.Join(", ", items) + ".";
            }
            catch { }

            return name;
        }

        /// <summary>
        /// Le nom d'un métier de départ, tel que le JEU l'affiche.
        ///
        /// `StartingProfessionInfo.name` est un libellé interne, en anglais ; le nom traduit est
        /// porté par les boutons de l'écran, en dehors de ce que cette classe expose. On demande
        /// donc sa traduction au jeu, en passant le libellé interne comme clé — c'est ainsi que
        /// ses propres boutons la retrouvent.
        ///
        /// Sept métiers sur dix se retrouvent ainsi. Les trois autres portent une clé que rien ne
        /// permet de deviner, et lire leur libellé sur les boutons de l'écran s'est révélé
        /// impossible : le panneau central n'affiche que la catégorie sélectionnée, donc les
        /// boutons de métier ne sont pas là au moment où l'assistant pose la question.
        ///
        /// Ils sont donc écrits ci-dessous, RELEVÉS SUR L'ÉCRAN DU JEU et non traduits par moi.
        /// C'est un pis-aller assumé : si une mise à jour renomme ces métiers, ces trois lignes
        /// deviendront fausses là où les sept autres suivront toutes seules.
        /// </summary>
        private static readonly Dictionary<string, string> MissingFrenchNames =
            new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase)
            {
                { "Orchard Farmer", "Arboriculteur" },
                { "Duelist", "Duelliste" },
                { "Royalty in Your Last Life", "Royauté" },
            };

        private static string ProfessionName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return raw;

            LogProfessionNames();

            // Les trois que la table du jeu ne rend pas, relevés sur son propre écran.
            if (!Localization.Language.IsEnglish
                && MissingFrenchNames.TryGetValue(raw.Trim(), out string known)) return known;

            string key = TranslationKeyFor(raw);
            if (key != null)
            {
                try
                {
                    string translated = TextUtil.Clean(LocalizeText.TranslateText(key, Missing));
                    if (!string.IsNullOrWhiteSpace(translated) && translated != Missing) return translated;
                }
                catch { }
            }

            return raw; // aucune clé ne répond : le libellé interne, plutôt qu'une invention
        }

        /// <summary>
        /// Sentinelle de repli : le traducteur du jeu renvoie ce qu'on lui donne par défaut quand
        /// il ne connaît pas la clé. Une valeur qu'aucune traduction ne peut valoir rend donc
        /// l'échec reconnaissable, ce qu'une chaîne vide ne permettrait pas.
        /// </summary>
        private const string Missing = "?";

        private static readonly Dictionary<string, string> _keyCache = new Dictionary<string, string>();

        /// <summary>
        /// La clé de traduction d'un métier, cherchée par essais successifs.
        ///
        /// Le libellé interne n'est pas la clé — passer « Orchard Farmer » ne donne rien. Les clés
        /// de ce jeu suivent des conventions visibles ailleurs dans sa table (« ..._Name »), mais
        /// rien dans le code décompilé ne dit laquelle s'applique ici : ces boutons portent leur
        /// terme dans la scène, hors de portée. On essaie donc les formes plausibles et on retient
        /// la première qui répond — puis on la journalise, pour qu'un seul essai en jeu suffise à
        /// trancher au lieu d'un aller-retour par hypothèse.
        /// </summary>
        private static string TranslationKeyFor(string raw)
        {
            if (_keyCache.TryGetValue(raw, out string cached)) return cached;

            string tight = raw.Replace(" ", "").Replace("'", "");
            string[] attempts =
            {
                raw,
                tight,
                tight + "_Name",
                raw + "_Name",
                "Profession_" + tight,
                "StartingProfession_" + tight,
                "Profession/" + tight,
            };

            string found = null;
            foreach (string attempt in attempts)
            {
                try
                {
                    string result = LocalizeText.TranslateText(attempt, Missing);
                    if (!string.IsNullOrWhiteSpace(result) && result != Missing) { found = attempt; break; }
                }
                catch { }
            }

            // Aucune convention ne répond — c'est le cas de trois métiers sur dix, dont « Orchard
            // Farmer » et « Royalty in Your Last Life ». Plutôt qu'une hypothèse de plus sur la
            // forme du nom, on retourne le problème : le libellé interne EST le texte anglais de
            // la traduction cherchée. On demande donc au jeu la liste de ses termes, et on retient
            // celui dont la version anglaise est exactement ce libellé. Ce n'est plus une
            // supposition mais une correspondance.
            if (found == null) found = TermWithEnglishValue(raw);

            _keyCache[raw] = found;
            Plugin.Log?.LogInfo(found != null
                ? $"Métier « {raw} » : clé de traduction trouvée, « {found} »."
                : $"Métier « {raw} » : aucune clé de traduction ne répond, le libellé interne sera lu.");

            return found;
        }

        /// <summary>
        /// Le terme de traduction dont la version ANGLAISE vaut ce texte.
        ///
        /// Le parcours complet de la table est coûteux, mais il n'a lieu que pour les libellés
        /// qu'aucune convention de nom ne retrouve, une seule fois par session et sur un écran de
        /// menu. C'est le prix d'une réponse exacte, contre une suite d'hypothèses qui ont déjà
        /// coûté plusieurs essais en jeu.
        /// </summary>
        private static string TermWithEnglishValue(string englishText)
        {
            try
            {
                LocalizationManager.InitializeIfNeeded();

                List<string> terms = LocalizationManager.GetTermsList();
                if (terms == null || terms.Count == 0)
                {
                    Plugin.Log?.LogInfo("Table de traduction du jeu vide ou illisible : recherche de terme impossible.");
                    return null;
                }

                string wanted = englishText.Trim();
                string flattened = Flatten(englishText);

                // Deux passes, de la plus sûre à la plus tolérante.
                //
                // D'abord le NOM du terme, comparé sans espaces ni ponctuation ni casse :
                // « Orchard Farmer » retrouve « OrchardFarmer », « orchard_farmer » ou
                // « ORCHARDFARMER ». C'est indépendant des langues chargées, donc fiable même si
                // la colonne anglaise ne l'est pas.
                foreach (string term in terms)
                    if (!string.IsNullOrEmpty(term) && Flatten(term) == flattened) return term;

                // La recherche par VALEUR anglaise a été retirée : elle demandait au jeu la version
                // anglaise de chacun des quarante-huit mille termes, alors qu'en partie française
                // c'est la valeur française qui revient. Elle ne pouvait donc jamais correspondre —
                // elle coûtait cher et m'a fait conclure à tort que trois métiers n'étaient pas
                // traduits, alors qu'ils le sont bel et bien à l'écran.
                Plugin.Log?.LogInfo($"Aucun terme ne correspond à « {englishText} » parmi {terms.Count} termes lus.");
            }
            catch (Exception e)
            {
                Plugin.Log?.LogInfo("Recherche de terme impossible : " + e.Message);
            }

            return null;
        }

        /// <summary>Réduit un libellé à ses lettres et chiffres, en minuscules, pour comparer des
        /// variantes d'écriture sans se soucier des espaces, tirets ou majuscules.</summary>
        private static string Flatten(string s) =>
            new string((s ?? string.Empty).Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

        private static bool _loggedProfessions;

        /// <summary>
        /// Journalise une fois les noms bruts que le jeu expose. Ils ne sont pas devinables depuis
        /// le code décompilé — ce sont des chaînes posées dans la scène — et cette trace évite un
        /// aller-retour de plus si l'un d'eux manque à la table ci-dessus.
        /// </summary>
        private static void LogProfessionNames()
        {
            if (_loggedProfessions) return;
            _loggedProfessions = true;

            try
            {
                var raw = (Creator()?.professionInfos ?? new StartingProfessionInfo[0])
                    .Where(p => p != null)
                    .Select(p => "\"" + p.name + "\"");

                Plugin.Log?.LogInfo("Métiers de départ, noms bruts du jeu : " + string.Join(", ", raw));
            }
            catch { }
        }

        // ------------------------------------------------------------------ 3. L'anniversaire

        private static void AskBirthSeason()
        {
            _step = Step.BirthSeason;

            var seasons = new[] { Season.Spring, Season.Summer, Season.Fall, Season.Winter };
            var labels = seasons.Select(SeasonName).ToList();

            OpenOwned(Localization.Language.T("Saison de votre anniversaire", "Season of your birthday"),
                labels, chosen =>
                {
                    _season = seasons[chosen];
                    AskBirthDay();
                });
        }

        private static void AskBirthDay()
        {
            _step = Step.BirthDay;

            var labels = Enumerable.Range(1, DaysPerSeason)
                .Select(d => Localization.Language.T($"Jour {d}", $"Day {d}"))
                .ToList();

            OpenOwned(Localization.Language.T($"Jour de votre anniversaire, {SeasonName(_season)}",
                                                  $"Day of your birthday, {SeasonName(_season)}"),
                labels, chosen =>
                {
                    _day = chosen + 1;
                    Apply(() =>
                    {
                        Creator().SetBirthdayMonthDay(_season, _day);
                        Creator().SetBirthdayConfirmed();
                    });
                    AskName();
                });
        }

        private static string SeasonName(Season season)
        {
            try
            {
                string translated = TextUtil.Clean(Utilities.TranslateSeason(season));
                if (!string.IsNullOrWhiteSpace(translated)) return translated;
            }
            catch { }
            return season.ToString();
        }

        // ------------------------------------------------------------------ 4. Le nom

        /// <summary>
        /// Le seul moment où l'on quitte la liste : taper un nom demande le clavier entier.
        ///
        /// On donne le champ du jeu au joueur plutôt que d'inventer une saisie à nous : la lecture
        /// caractère par caractère et la suspension des touches du mod pendant la frappe existent
        /// déjà et fonctionnent (voir Menus/TextInputReader.cs).
        /// </summary>
        private static void AskName()
        {
            _step = Step.Name;
            ListMenu.CloseIfOwner(OwnerTag, false);

            TMPro.SunHavenInputField field = NameField();
            if (field == null) { AskSkipIntro(); return; }

            try { field.Select(); field.ActivateInputField(); } catch { }

            TolkSpeech.Speak(Localization.Language.T(
                "Nom du personnage : tapez-le, puis Entrée pour continuer.",
                "Character name: type it, then press Enter to continue."), true);
        }

        private static void TickNameEntry()
        {
            // La détection d'écran est court-circuitée pendant cette étape : on la refait ici, sans
            // quoi quitter l'écran en pleine saisie laisserait l'assistant à attendre une touche
            // pour un écran qui n'existe plus.
            if (Creator() == null) { Cancel(); _onScreen = false; return; }

            // Pendant la saisie, la liste est fermée : la sortie par Échap doit donc être gérée
            // ici, sinon l'assistant n'aurait plus aucune porte de sortie à cette étape — la
            // seule où il ne tient pas le clavier.
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
            {
                _step = Step.Idle;
                TolkSpeech.Speak(Localization.Language.T(
                    "Assistant fermé. L'écran de création reste utilisable aux flèches.",
                    "Wizard closed. The creation screen is still usable with the arrows."), true);
                return;
            }

            if (!UnityEngine.Input.GetKeyDown(KeyCode.Return) && !UnityEngine.Input.GetKeyDown(KeyCode.KeypadEnter))
                return;

            TMPro.SunHavenInputField field = NameField();
            string typed = field != null ? TextUtil.Clean(field.text) : null;

            if (string.IsNullOrWhiteSpace(typed))
            {
                TolkSpeech.Speak(Localization.Language.T(
                    "Le nom ne peut pas être vide. Tapez un nom, puis Entrée.",
                    "The name cannot be empty. Type a name, then press Enter."), true);
                return;
            }

            Apply(() => Creator().SetCharacterName());
            TolkSpeech.Speak(Localization.Language.T($"Nom : {typed}.", $"Name: {typed}."), true);
            AskSkipIntro();
        }

        private static FieldInfo _nameFieldInfo;
        private static bool _nameFieldResolved;

        private static TMPro.SunHavenInputField NameField()
        {
            try
            {
                if (!_nameFieldResolved)
                {
                    _nameFieldResolved = true;
                    _nameFieldInfo = typeof(NewCharacterCreator).GetField("nameInputField",
                        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                    if (_nameFieldInfo == null)
                        Plugin.Log?.LogWarning("NewCharacterCreator.nameInputField introuvable : l'assistant sautera l'étape du nom.");
                }

                return _nameFieldInfo?.GetValue(Creator()) as TMPro.SunHavenInputField;
            }
            catch { return null; }
        }

        // ------------------------------------------------------------------ 5. L'introduction

        private static void AskSkipIntro()
        {
            _step = Step.SkipIntro;

            Toggle toggle;
            try { toggle = Creator().skipIntroToggle; } catch { toggle = null; }

            if (toggle == null) { AskConfirm(); return; }

            var labels = new List<string>
            {
                Localization.Language.T("Voir l'introduction", "Watch the introduction"),
                Localization.Language.T("Passer l'introduction", "Skip the introduction"),
            };

            OpenOwned(Localization.Language.T("L'introduction du jeu", "The game's introduction"),
                labels, chosen =>
                {
                    Apply(() => toggle.isOn = chosen == 1);
                    AskConfirm();
                });
        }

        // ------------------------------------------------------------------ 6. Confirmer

        /// <summary>
        /// Le récapitulatif avant de lancer : c'est le dernier moment où l'on peut se raviser, et
        /// la seule occasion d'entendre ses choix d'affilée plutôt qu'un par un.
        /// </summary>
        private static void AskConfirm()
        {
            _step = Step.Confirm;

            var summary = new List<string>();
            if (!string.IsNullOrWhiteSpace(_raceLabel))
                summary.Add(Localization.Language.Pair(Localization.Language.T("Race", "Race"), _raceLabel));
            if (!string.IsNullOrWhiteSpace(_professionLabel))
                summary.Add(Localization.Language.Pair(Localization.Language.T("Métier", "Profession"), _professionLabel));
            if (_day > 0)
                summary.Add(Localization.Language.Pair(Localization.Language.T("Anniversaire", "Birthday"),
                                                       $"{SeasonName(_season)} {_day}"));

            // Le récapitulatif est DIT, pas mis dans la liste : une liste dont la moitié des
            // entrées ne fait rien quand on les valide est une liste qui ment sur ce qu'elle est.
            // La touche de répétition le redonne autant de fois qu'on veut.
            if (summary.Count > 0)
                TolkSpeech.Speak(Localization.Language.T("Vos choix : ", "Your choices: ")
                                 + string.Join(". ", summary) + ".", true);

            var labels = new List<string>
            {
                Localization.Language.T("Commencer la partie", "Start the game"),
                Localization.Language.T("Recommencer les questions", "Start the questions again"),
            };

            OpenOwned(Localization.Language.T("Prêt à commencer", "Ready to start"), labels, chosen =>
            {
                if (chosen == 0)
                {
                    _step = Step.Idle;
                    ListMenu.CloseIfOwner(OwnerTag, false);
                    TolkSpeech.Speak(Localization.Language.T("La partie commence.", "The game is starting."), true);
                    Apply(() => Creator().AddNewCharacter());
                }
                else AskRace();
            });
        }

        // ------------------------------------------------------------------ Interne

        /// <summary>
        /// Applique un choix en passant par le jeu. Si l'écran a disparu entre-temps — le joueur a
        /// pu reculer — on abandonne sans rien casser plutôt que de lever une exception dans la
        /// boucle du mod.
        /// </summary>
        private static void Apply(Action action)
        {
            try
            {
                if (Creator() == null) { Cancel(); return; }
                action();
            }
            catch (Exception e)
            {
                Plugin.Log?.LogWarning("Création de personnage, étape refusée par le jeu : " + e.Message);
                TolkSpeech.Speak(Localization.Language.T("Ce choix n'a pas pu être appliqué.",
                                                          "That choice could not be applied."), true);
            }
        }
    }
}
