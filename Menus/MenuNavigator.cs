using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SunHavenAccess.Speech;
using SunHavenAccess.Util;

namespace SunHavenAccess.Menus
{
    /// <summary>
    /// Beaucoup de menus de Sun Haven (menu principal, sélection de personnage, options...) ne
    /// passent JAMAIS par le système de sélection d'UnityEngine.EventSystems : ils sont pilotés
    /// uniquement à la souris (Button.onClick), sans jamais appeler SetSelectedGameObject.
    /// FocusReader (qui surveille EventSystem.currentSelectedGameObject) ne peut donc rien
    /// annoncer sur ces écrans-là puisque rien n'y est jamais "sélectionné" au sens Unity.
    ///
    /// Ce navigateur construit sa propre liste des éléments interactifs actuellement visibles
    /// (tous les Selectable actifs de la scène) et permet de les parcourir/activer avec des
    /// touches dédiées, entièrement indépendantes du système d'input du jeu. Il complète
    /// FocusReader plutôt que de le remplacer : les écrans qui utilisent bien la sélection
    /// Unity (ex. choix de dialogue) continuent d'être couverts par FocusReader.
    /// </summary>
    public static class MenuNavigator
    {
        private static readonly List<Selectable> _items = new List<Selectable>();
        private static int _index = -1;

        public static void Next() => Move(1);
        public static void Previous() => Move(-1);

        private static void Move(int direction)
        {
            // Bug corrigé : Rescan() remettait _index à -1 avant même qu'on applique la
            // direction, donc "suivant" retombait TOUJOURS sur l'élément 0 et "précédent"
            // TOUJOURS sur l'avant-dernier, quel que soit le nombre de pressions déjà faites —
            // impossible de vraiment parcourir la liste (symptôme : les flèches "ne marchent
            // pas", toujours le même élément annoncé). On retrouve d'abord l'élément
            // actuellement sélectionné DANS la nouvelle liste rescannée pour partir de sa
            // position réelle, pas d'un index remis à zéro.
            Selectable previousSelection = (_index >= 0 && _index < _items.Count) ? _items[_index] : null;
            Rescan();
            if (_items.Count == 0)
            {
                TolkSpeech.Speak("Aucun élément de menu détecté à l'écran.", true);
                return;
            }
            int baseIndex = previousSelection != null ? _items.IndexOf(previousSelection) : -1;
            _index = ((baseIndex + direction) % _items.Count + _items.Count) % _items.Count;
            Announce(_items[_index]);
        }

        /// <summary>
        /// Bug corrigé : sur une pression "à froid" (aucune navigation aux flèches depuis
        /// l'apparition de l'écran actuel), cette méthode sélectionnait silencieusement le
        /// PREMIER élément interactif trouvé sur tout l'écran — y compris des éléments du HUD
        /// permanent (barre d'action, suivi de quête...) toujours présents même en jeu normal,
        /// hors de tout menu. Une pression d'Entrée involontaire pendant les déplacements pouvait
        /// donc, en deux appuis, activer un bouton du HUD au hasard (ex. ouvrir le menu Tab).
        /// Corrigé pour EXIGER une sélection déjà faite aux flèches avant de pouvoir valider —
        /// Entrée seule, même répétée, ne clique donc plus jamais rien à l'aveugle.
        /// </summary>
        public static void Activate()
        {
            if (_index < 0 || _index >= _items.Count)
            {
                TolkSpeech.Speak("Utilisez les flèches pour sélectionner un élément avant de valider.", true);
                return;
            }

            Selectable sel = _items[_index];
            if (sel == null || !sel.gameObject.activeInHierarchy)
            {
                TolkSpeech.Speak("Cet élément n'est plus disponible, nouvelle recherche.", true);
                Rescan();
                return;
            }

            // Bug corrigé : un clic sur un bouton comme "Jouer" déclenche souvent une
            // transition d'écran (nouvel écran de sélection/création de personnage...), qui ne
            // se produit pas forcément à l'instant même. Si le joueur presse Entrée une
            // deuxième fois par réflexe avant d'avoir re-navigué aux flèches sur le NOUVEL
            // écran, cette méthode réutilisait l'ancien `_items[_index]` — un bouton de l'écran
            // PRÉCÉDENT, parfois encore techniquement "actif" le temps de la transition — et le
            // cliquait à l'aveugle, avec des effets imprévisibles (ex. rouvrir "Nouvelle
            // partie" alors qu'on est déjà sur l'écran suivant, d'où des messages du type
            // "cette option n'est pas disponible"). On invalide donc TOUJOURS la sélection
            // après une activation : il faut re-choisir aux flèches avant de valider à nouveau,
            // même sur le même écran.
            _index = -1;

            ActivateObject(sel.gameObject, sel);
        }

        /// <summary>
        /// BUG corrigé (24/08/2026) : le repli générique ne simulait QU'UN clic pointeur
        /// (`pointerClickHandler`). Or `Wish.Slot`/`ArmorSlot` (emplacements d'inventaire et
        /// d'équipement) n'implémentent PAS `IPointerClickHandler` — seulement `ISubmitHandler` —
        /// donc valider sur un emplacement d'inventaire ne faisait tout simplement rien. On
        /// essaie maintenant `submitHandler` EN PREMIER (c'est ce que déclenche la validation
        /// clavier native d'Unity), et on ne retombe sur le clic pointeur que s'il n'y a pas de
        /// gestionnaire de validation.
        /// </summary>
        public static void ActivateObject(GameObject go, Selectable sel = null)
        {
            if (go == null) return;
            sel ??= go.GetComponent<Selectable>();

            if (sel is Toggle toggle)
            {
                toggle.isOn = !toggle.isOn;
                TolkSpeech.Speak(toggle.isOn ? "Coché." : "Décoché.", true);
                return;
            }

            var data = new BaseEventData(EventSystem.current);
            if (ExecuteEvents.Execute(go, data, ExecuteEvents.submitHandler))
            {
                TolkSpeech.Speak("Activé.", true);
                return;
            }

            if (sel is Button button)
            {
                button.onClick.Invoke();
                TolkSpeech.Speak("Activé.", true);
                return;
            }

            ExecuteEvents.Execute(go, new PointerEventData(EventSystem.current),
                ExecuteEvents.pointerClickHandler);
            TolkSpeech.Speak("Activé.", true);
        }

        /// <summary>
        /// Ctrl+Gauche/Droite : ajuste la valeur d'un curseur (Slider) sélectionné — utile pour
        /// les écrans de création de personnage (couleurs, apparence...), où le repli générique
        /// d'Activate() (simuler un simple clic) ne fait rien d'utile sur un Slider. Sans effet
        /// si l'élément actuellement sélectionné n'est pas un Slider.
        /// </summary>
        public static void AdjustSlider(int direction)
        {
            if (_index < 0 || _index >= _items.Count) return;
            if (_items[_index] is not Slider slider) return;
            if (slider == null || !slider.gameObject.activeInHierarchy) return;

            float step = slider.wholeNumbers ? 1f : (slider.maxValue - slider.minValue) / 20f;
            slider.value = Mathf.Clamp(slider.value + step * direction, slider.minValue, slider.maxValue);
            AnnounceCurrent();
        }

        // Le changement d'onglet du menu principal vit maintenant dans ZoneNavigator.SwitchTab :
        // il utilise l'API PUBLIQUE du jeu (PlayerInventory.OpenMajorPanel), qui met elle-même à
        // jour l'index d'onglet et la sélection. L'ancienne implémentation ici cliquait un
        // GameObject trouvé par préfixe de nom et suivait le rang courant dans un compteur
        // maintenu par le mod — un compteur qui dérivait dès que l'onglet changeait autrement
        // (souris, action du jeu), cause la plus probable du comportement "bancale" signalé.

        // ---- Variantes agissant sur la sélection Unity courante (mode navigation
        // directionnelle, où c'est ZoneNavigator qui déplace la sélection, pas la liste interne
        // `_items` de ce navigateur). Même comportement, autre source de vérité.

        private static Slider SelectedSlider()
        {
            GameObject go = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            return go != null ? go.GetComponent<Slider>() : null;
        }

        public static bool SelectedIsSlider() => SelectedSlider() != null;

        public static void AdjustSelectedSlider(int direction)
        {
            Slider slider = SelectedSlider();
            if (slider == null || !slider.gameObject.activeInHierarchy) return;

            float step = slider.wholeNumbers ? 1f : (slider.maxValue - slider.minValue) / 20f;
            slider.value = Mathf.Clamp(slider.value + step * direction, slider.minValue, slider.maxValue);

            string valueText = slider.wholeNumbers
                ? $"{Mathf.RoundToInt(slider.value)} sur {Mathf.RoundToInt(slider.maxValue)}"
                : $"{Mathf.RoundToInt((slider.value - slider.minValue) / Mathf.Max(slider.maxValue - slider.minValue, 0.0001f) * 100f)} pour cent";
            TolkSpeech.Speak(valueText, true);
        }

        /// <summary>Clic droit sur un GameObject donné (variante de SecondaryActivate hors liste interne).</summary>
        public static void SecondaryActivateObject(GameObject go)
        {
            if (go == null) return;
            var rightClick = new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Right
            };
            ExecuteEvents.Execute(go, rightClick, ExecuteEvents.pointerClickHandler);
            TolkSpeech.Speak("Clic droit.", true);
        }

        /// <summary>Action secondaire (Ctrl+Entrée) : équivalent d'un clic droit sur l'élément annoncé.</summary>
        public static void SecondaryActivate()
        {
            if (_index < 0 || _index >= _items.Count)
            {
                TolkSpeech.Speak("Rien à activer.", true);
                return;
            }

            Selectable sel = _items[_index];
            if (sel == null || !sel.gameObject.activeInHierarchy)
            {
                TolkSpeech.Speak("Cet élément n'est plus disponible, nouvelle recherche.", true);
                Rescan();
                return;
            }

            // Même garde-fou que Activate() : on invalide la sélection avant de cliquer, pour
            // ne jamais réutiliser une référence d'un écran qui vient de changer.
            _index = -1;

            PointerEventData rightClick = new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Right
            };
            ExecuteEvents.Execute(sel.gameObject, rightClick, ExecuteEvents.pointerClickHandler);
            TolkSpeech.Speak("Clic droit.", true);
        }

        public static void AnnounceCurrent()
        {
            if (_index < 0 || _index >= _items.Count)
            {
                Move(1);
                return;
            }
            Announce(_items[_index]);
        }

        /// <summary>
        /// Tous les Selectable réellement navigables à l'écran, triés haut -> bas puis gauche ->
        /// droite. Partagé avec ZoneNavigator (zone « générique ») pour que les deux systèmes
        /// s'accordent exactement sur ce qui compte comme élément atteignable.
        /// </summary>
        public static IEnumerable<Selectable> VisibleSelectables()
        {
            List<Selectable> all = Object.FindObjectsOfType<Selectable>()
                .Where(s => s != null && s.interactable && s.gameObject.activeInHierarchy && IsVisible(s))
                .Where(s => !IsExternalLink(s))
                .ToList();

            // Le test « réellement à l'écran » sert à écarter les panneaux des autres onglets, que
            // le jeu garde actifs hors champ. Mais il repose sur une conversion qui dépend du type
            // de canevas, et rien ne garantit qu'elle soit juste sur tous les écrans.
            //
            // On l'applique donc comme un FILTRE, jamais comme un verdict : s'il ne laisse rien,
            // c'est lui qui a tort, et on garde la liste complète. Un écran devenu totalement
            // inatteignable au clavier est infiniment pire qu'un écran où quelques éléments de
            // trop sont proposés — c'est exactement la régression rapportée sur le menu principal.
            List<Selectable> onScreen = all.Where(IsOnScreen).ToList();
            List<Selectable> chosen = onScreen.Count > 0 ? onScreen : all;

            // Sur le menu principal, on se limite au panneau que le JEU considère ouvert.
            //
            // Rapporté en jeu : la première flèche annonçait « Pixelsprout Studios », et il fallait
            // remonter plusieurs fois pour trouver « Jouer ». Le logo du studio est un élément
            // cliquable posé PLUS HAUT que les boutons du menu, mais hors de celui-ci : trié par
            // position, il passait donc devant. Aucune règle géométrique ne pouvait le distinguer
            // des vrais boutons — c'est l'appartenance au panneau qui le fait, et le jeu la connaît.
            List<Selectable> inPanel = WithinActiveMainMenuPanel(chosen);
            if (inPanel.Count > 0) chosen = inPanel;

            return chosen
                .OrderByDescending(s => s.transform.position.y)
                .ThenBy(s => s.transform.position.x);
        }

        /// <summary>
        /// Un bouton qui ouvre un site web plutôt qu'un écran du jeu.
        ///
        /// Le logo du studio et les liens vers les réseaux sont posés PLUS HAUT que les boutons du
        /// menu. Triés par position, ils passaient devant : la première flèche annonçait
        /// « Pixelsprout Studios » et il fallait remonter plusieurs fois pour atteindre « Jouer ».
        /// Les restreindre au panneau de menu n'a rien changé — ils y sont bel et bien.
        ///
        /// Ce qui les distingue vraiment, c'est ce qu'ils FONT : le jeu leur attache un
        /// `Wish.OpenURL`, dont le seul rôle est d'ouvrir une adresse dans le navigateur. Aucun
        /// n'a sa place dans la navigation d'un menu — les écarter, c'est retirer du parcours ce
        /// qui n'y avait rien à faire, pas masquer une fonctionnalité du jeu.
        /// </summary>
        private static bool IsExternalLink(Selectable s)
        {
            try { return s.GetComponentInParent<Wish.OpenURL>() != null; }
            catch { return false; }
        }

        /// <summary>
        /// Les éléments appartenant au panneau de menu principal actuellement ouvert, ou une liste
        /// vide hors du menu principal — auquel cas l'appelant garde sa sélection d'origine.
        ///
        /// `MainMenuController.EnableMenu` n'active qu'un seul de ces objets à la fois : celui qui
        /// est actif EST l'écran courant. On lit donc l'état du jeu plutôt que de deviner d'après
        /// des positions à l'écran, où rien ne sépare un bouton de menu d'un logo cliquable.
        /// </summary>
        private static List<Selectable> WithinActiveMainMenuPanel(List<Selectable> candidates)
        {
            try
            {
                Wish.MainMenuController menu = Wish.MainMenuController.Instance;
                if (menu == null) return new List<Selectable>();

                GameObject[] panels =
                {
                    menu.homeMenu, menu.singlePlayerMenu, menu.newCharacterMenu, menu.loadCharacterMenu,
                    menu.multiplayerMenu, menu.connectMenu, menu.modeMenu, menu.optionsMenu,
                    menu.creditsMenu, menu.patchNotes, menu.lobbySettingsMenu, menu.lobbyMenu, menu.dlcShop,
                };

                GameObject active = panels.FirstOrDefault(p => p != null && p.activeInHierarchy);
                if (active == null) return new List<Selectable>();

                Transform root = active.transform;
                return candidates.Where(s => s.transform.IsChildOf(root)).ToList();
            }
            catch { return new List<Selectable>(); }
        }

        private static void Rescan()
        {
            _items.Clear();
            _items.AddRange(VisibleSelectables());
            _index = -1;
        }

        private static bool IsVisible(Selectable s)
        {
            CanvasGroup group = s.GetComponentInParent<CanvasGroup>();
            return group == null || group.alpha > 0.01f;
        }

        /// <summary>
        /// L'élément est-il réellement dans l'écran ?
        ///
        /// Le seul test de transparence ne suffisait pas : Sun Haven garde les panneaux des autres
        /// onglets actifs et opaques, simplement rangés hors champ. Ils restaient donc candidats à
        /// la navigation, ce qui produisait le défaut rapporté en jeu — Ctrl+bas depuis la barre
        /// d'onglets atterrissait dans l'arbre de compétences quel que soit l'onglet ouvert, parce
        /// que ses nœuds étaient les premiers de la liste. Ils faussaient du même coup le
        /// regroupement en bandes et en colonnes, calculé sur des positions invisibles à l'écran.
        ///
        /// La marge tolère les éléments à cheval sur un bord plutôt que de les écarter : mieux
        /// vaut un élément de trop qu'un élément atteignable rendu inatteignable.
        /// </summary>
        internal static bool IsOnScreen(Component s)
        {
            try
            {
                Canvas canvas = s.GetComponentInParent<Canvas>();
                Camera camera = null;
                if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                {
                    // Un canevas en mode caméra sans caméra assignée retomberait sur une
                    // conversion « écran » appliquée à des coordonnées monde : le résultat serait
                    // minuscule et TOUS les éléments seraient déclarés hors champ.
                    camera = canvas.worldCamera ?? Camera.main;
                    if (camera == null) return true;
                }

                Vector2 screen = RectTransformUtility.WorldToScreenPoint(camera, s.transform.position);
                const float margin = 64f;

                return screen.x >= -margin && screen.x <= Screen.width + margin
                    && screen.y >= -margin && screen.y <= Screen.height + margin;
            }
            catch
            {
                // Hiérarchie inattendue : on préfère garder l'élément. L'écarter par excès de
                // prudence le rendrait définitivement inatteignable au clavier.
                return true;
            }
        }

        private static void Announce(Selectable sel)
        {
            string text = UiTextExtractor.ExtractAll(sel.gameObject);
            string suffix = "";
            if (sel is Toggle t)
                suffix = Localization.Language.T(t.isOn ? ", coché" : ", non coché",
                                                 t.isOn ? ", ticked" : ", not ticked");
            else if (sel is Slider s)
            {
                string valueText = s.wholeNumbers
                    ? Localization.Language.T(
                        $"{Mathf.RoundToInt(s.value)} sur {Mathf.RoundToInt(s.maxValue)}",
                        $"{Mathf.RoundToInt(s.value)} of {Mathf.RoundToInt(s.maxValue)}")
                    : Localization.Language.T(
                        $"{Percent(s)} pour cent",
                        $"{Percent(s)} per cent");
                suffix = Localization.Language.T(
                    $", curseur, valeur {valueText} (Contrôle plus gauche/droite pour ajuster)",
                    $", slider, value {valueText} (Control plus left or right to adjust)");
            }

            TolkSpeech.Speak(Localization.Language.T(
                $"{text}{suffix}. Élément {_index + 1} sur {_items.Count}.",
                $"{text}{suffix}. Item {_index + 1} of {_items.Count}."), true);
        }

        private static int Percent(Slider s) =>
            Mathf.RoundToInt((s.value - s.minValue) / Mathf.Max(s.maxValue - s.minValue, 0.0001f) * 100f);
    }
}
