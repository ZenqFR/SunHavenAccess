using System;
using System.Collections.Generic;
using UnityEngine;
using SunHavenAccess.Config;
using SunHavenAccess.Localization;
using SunHavenAccess.Speech;

namespace SunHavenAccess.Menus
{
    /// <summary>
    /// Une liste vocale simple, parcourue une entrée à la fois.
    ///
    /// C'est la « base saine » demandée pour les panneaux du jeu qui ne sont pas des grilles :
    /// relations, quêtes, compétences, réglages. Ces écrans sont conçus pour l'œil — des colonnes,
    /// des encarts, des icônes dont la position porte le sens — et les parcourir en suivant leur
    /// disposition oblige à se représenter mentalement une image qu'on ne voit pas. Le contenu,
    /// lui, est presque toujours une simple liste : les personnages qu'on connaît, les quêtes en
    /// cours, les compétences d'un métier.
    ///
    /// On lit donc les DONNÉES du jeu, pas son interface, et on les présente comme une liste :
    /// une entrée à la fois, haut et bas pour se déplacer, rien à se représenter. C'est le modèle
    /// de l'aide (F1), le seul écran du mod validé en jeu sans aucune réserve.
    ///
    /// Un seul menu à la fois : ouvrir celui des quêtes ferme celui des relations. Deux listes
    /// vocales concurrentes se voleraient les flèches, ce qui a déjà cassé la navigation deux fois.
    /// </summary>
    public static class ListMenu
    {
        private static string _title;
        private static List<string> _entries = new List<string>();
        private static int _index;
        private static Action<int> _onActivate;

        /// <summary>
        /// Modifie la valeur de l'entrée choisie, et renvoie son nouveau libellé complet.
        /// Null pour une liste dont les entrées n'ont pas de valeur réglable.
        /// </summary>
        private static Func<int, int, string> _onAdjust;

        /// <summary>
        /// Que faire quand on quitte la liste par le haut (Ctrl+haut). Null pour une liste dont on
        /// ne sort que par Échap — le choix d'une sauvegarde, par exemple, n'a rien au-dessus.
        /// </summary>
        private static Action _onExitUp;

        /// <summary>
        /// Déclare comment quitter la liste par le haut, une fois celle-ci ouverte.
        ///
        /// Posé après l'ouverture plutôt que passé en paramètre : les modules qui construisent ces
        /// listes servent aussi aux touches directes (G, V, Z…), où il n'y a pas d'onglet au-dessus,
        /// et ils n'ont pas à savoir d'où on les a appelés.
        /// </summary>
        public static void SetExitUp(Action onExitUp)
        {
            if (IsOpen) _onExitUp = onExitUp;
        }

        /// <summary>
        /// Revendique la liste ouverte, quand c'est un AUTRE module qui l'a remplie.
        ///
        /// L'aiguillage des onglets ne construit pas les listes lui-même : il appelle les modules
        /// qui savent le faire — compétences, relations, quêtes. Ceux-ci servent aussi aux touches
        /// directes et n'ont pas à savoir d'où on les appelle, donc c'est à l'aiguillage de se
        /// déclarer propriétaire une fois la liste ouverte.
        /// </summary>
        public static void Claim(string owner)
        {
            if (IsOpen) Owner = owner;
        }

        public static bool IsOpen { get; private set; }

        /// <summary>
        /// Ouvre une liste.
        ///
        /// <paramref name="onActivate"/> est appelée avec l'indice choisi quand on valide, ou null
        /// pour une liste sans action.
        ///
        /// <paramref name="onAdjust"/> permet de RÉGLER l'entrée courante : gauche et droite
        /// l'appellent avec -1 ou +1 et annoncent le libellé qu'elle renvoie. Une liste de
        /// réglages se parcourt ainsi comme n'importe quelle autre, la valeur se changeant sur
        /// place sans quitter la ligne — c'est ce qu'on attend d'un panneau d'options.
        /// </summary>
        /// <summary>
        /// Qui a ouvert la liste actuellement affichée. Null si personne ne s'en est déclaré.
        ///
        /// Trois modules pilotent cette même liste — le choix de sauvegarde, l'assistant de
        /// création et l'aiguillage des onglets — et chacun la refermait quand SON écran
        /// disparaissait, sans vérifier qu'elle était bien la sienne. Pendant une transition
        /// d'écran, où deux d'entre eux se croisent, l'un fermait donc la liste de l'autre.
        /// </summary>
        public static string Owner { get; private set; }

        /// <param name="owner">
        /// Étiquette du module qui ouvre. Laissée à null pour une SOUS-liste — le détail d'une
        /// sauvegarde, les compétences d'un métier — qui appartient alors au même module que
        /// celle dont elle découle.
        /// </param>
        public static void Open(string title, List<string> entries,
                                Action<int> onActivate = null,
                                Func<int, int, string> onAdjust = null,
                                Action onExitUp = null,
                                string owner = null,
                                bool announce = true)
        {
            if (entries == null || entries.Count == 0)
            {
                TolkSpeech.Speak($"{SunHavenAccess.Localization.Translator.Translate(title)} : rien à afficher.", true);
                return;
            }

            _title = title;
            _entries = entries;
            _onActivate = onActivate;
            _onAdjust = onAdjust;
            _onExitUp = onExitUp;
            if (owner != null) Owner = owner;
            _index = 0;
            IsOpen = true;

            // Le mode d'emploi s'adapte à ce que la liste sait faire : annoncer « Entrée pour
            // choisir » sur une liste purement consultative ferait chercher une action qui
            // n'existe pas. Cette phrase est composée de trop de morceaux variables pour passer
            // par la traduction par motif : elle est donc écrite dans les deux langues ici.
            // OUVRIR SANS PARLER, quand quelque chose d'autre a déjà la parole.
            //
            // Une bulle de dialogue en est le cas type : le message est en train d'être lu, et une
            // liste qui s'annonce par-dessus le coupe net — exactement ce qui était signalé en jeu.
            // La liste existe alors pour les flèches, pas pour prendre la parole ; qui veut savoir
            // ce qu'elle contient appuie sur une flèche, et l'entend.
            if (!announce) return;

            bool en = SunHavenAccess.Localization.Language.IsEnglish;
            string spokenTitle = SunHavenAccess.Localization.Translator.Translate(title);

            string how = onAdjust != null
                ? (en ? ", left and right to change the value" : ", gauche et droite pour changer la valeur")
                : string.Empty;
            if (onActivate != null) how += en ? ", Enter to choose" : ", Entrée pour choisir";

            TolkSpeech.Speak(en
                ? $"{spokenTitle}, {entries.Count} entr{(entries.Count > 1 ? "ies" : "y")}. " +
                  $"Up and down arrows to browse{how}, Escape to close."
                : $"{spokenTitle}, {entries.Count} entrée{(entries.Count > 1 ? "s" : "")}. " +
                  $"Flèches haut et bas pour parcourir{how}, Échap pour fermer.", true);
            AnnounceCurrent();
        }

        /// <summary>
        /// Ferme la liste. <paramref name="announce"/> à false quand une AUTRE liste suit
        /// immédiatement — un sous-menu de compétences ou de sauvegarde : annoncer « fermé » juste
        /// avant d'ouvrir la suivante ferait entendre une fermeture qui n'a pas lieu d'être.
        /// </summary>
        public static void Close(bool announce = true)
        {
            if (!IsOpen) return;
            IsOpen = false;
            _entries = new List<string>();
            _onActivate = null;
            _onAdjust = null;
            _onExitUp = null;
            Owner = null;
            if (announce) TolkSpeech.Speak($"{SunHavenAccess.Localization.Translator.Translate(_title)} fermé.", true);
        }

        /// <summary>
        /// Ferme la liste UNIQUEMENT si elle appartient à ce module.
        ///
        /// C'est ce qu'un module doit appeler quand son écran disparaît : sans ce test, il fermait
        /// aussi la liste que voisin venait d'ouvrir, les écrans se croisant le temps d'une
        /// transition.
        /// </summary>
        public static void CloseIfOwner(string owner, bool announce = true)
        {
            if (!IsOpen || Owner != owner) return;
            Close(announce);
        }

        /// <summary>
        /// Appelée chaque frame tant que la liste est ouverte ; elle a alors la main exclusive sur
        /// le clavier (voir HotkeyManager), sinon parcourir la liste déclencherait au passage les
        /// actions du jeu liées aux mêmes touches.
        /// </summary>
        public static void Tick()
        {
            if (!IsOpen) return;

            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape)) { Close(); return; }

            // Ctrl+haut quitte la liste par le haut, sans la fermer « pour de bon ».
            //
            // Une liste d'onglet capte tout le clavier tant qu'elle est ouverte : sans cette
            // sortie, arriver sur l'arbre de compétences enfermait dans sa liste, la barre
            // d'onglets devenant inatteignable. On réemploie le geste que le mod utilise déjà
            // partout ailleurs — Ctrl+haut ramène aux onglets, Ctrl+bas redescend dans le contenu
            // — plutôt que d'inventer une touche de plus à retenir.
            if (_onExitUp != null
                && UnityEngine.Input.GetKeyDown(KeyCode.UpArrow)
                && (UnityEngine.Input.GetKey(KeyCode.LeftControl) || UnityEngine.Input.GetKey(KeyCode.RightControl)))
            {
                Action exit = _onExitUp;
                Close(announce: false); // la barre d'onglets s'annonce juste après : ne pas la couvrir
                exit();
                return;
            }

            // Sur une liste de réglages, gauche et droite changent la VALEUR de la ligne courante :
            // c'est la convention de tout panneau d'options, et le sens attendu. Ailleurs, elles
            // doublent haut et bas — sans voir la liste, on n'a pas à se souvenir de son
            // orientation.
            if (_onAdjust != null)
            {
                if (UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow)) { Adjust(-1); return; }
                if (UnityEngine.Input.GetKeyDown(KeyCode.RightArrow)) { Adjust(1); return; }
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.UpArrow)
                || (_onAdjust == null && UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow))) Move(-1);
            else if (UnityEngine.Input.GetKeyDown(KeyCode.DownArrow)
                || (_onAdjust == null && UnityEngine.Input.GetKeyDown(KeyCode.RightArrow))) Move(1);
            else if (UnityEngine.Input.GetKeyDown(KeyCode.Home)) JumpTo(0);
            else if (UnityEngine.Input.GetKeyDown(KeyCode.End)) JumpTo(_entries.Count - 1);
            else if (UnityEngine.Input.GetKeyDown(KeyCode.PageUp)) Move(-5);
            else if (UnityEngine.Input.GetKeyDown(KeyCode.PageDown)) Move(5);
            else if (Pressed(ModConfig.Repeat)) AnnounceCurrent();
            else if (_onActivate != null && Pressed(ModConfig.MenuActivate)) Activate();
        }

        private static bool Pressed(BepInEx.Configuration.ConfigEntry<KeyCode> entry) =>
            entry != null && entry.Value != KeyCode.None && UnityEngine.Input.GetKeyDown(entry.Value);

        /// <summary>
        /// Change la valeur de la ligne courante et annonce le résultat.
        ///
        /// Le libellé est remplacé par celui que renvoie le réglage : la liste reste donc juste
        /// même après plusieurs changements, et rouvrir le menu n'affiche jamais une valeur
        /// périmée. Un réglage qui refuse de bouger — bout de course d'un curseur — se signale par
        /// le bip de bord, comme partout ailleurs.
        /// </summary>
        private static void Adjust(int direction)
        {
            if (_onAdjust == null || _index < 0 || _index >= _entries.Count) return;

            string updated = _onAdjust(_index, direction);
            if (string.IsNullOrWhiteSpace(updated)) { UiSound.EdgeBump(); return; }

            _entries[_index] = updated;
            TolkSpeech.Speak(updated, true);
        }

        private static void Activate()
        {
            if (_index < 0 || _index >= _entries.Count) return;
            Action<int> callback = _onActivate;
            int chosen = _index;
            // La liste se ferme AVANT l'action : celle-ci peut ouvrir un autre menu, et deux listes
            // ouvertes en même temps se disputeraient les flèches. Sans annonce : ce qui suit
            // parle immédiatement.
            Close(announce: false);
            callback?.Invoke(chosen);
        }

        /// <summary>
        /// Déplacement SANS bouclage, avec un bip aux extrémités. Une liste qui reboucle en
        /// silence fait perdre le compte de l'endroit où l'on est — précisément ce qu'on cherche à
        /// éviter quand on ne la voit pas.
        /// </summary>
        private static void Move(int delta)
        {
            int target = Mathf.Clamp(_index + delta, 0, _entries.Count - 1);
            if (target == _index) { UiSound.EdgeBump(); return; }
            _index = target;
            AnnounceCurrent();
        }

        private static void JumpTo(int target)
        {
            if (_entries.Count == 0) return;
            _index = Mathf.Clamp(target, 0, _entries.Count - 1);
            AnnounceCurrent();
        }

        private static void AnnounceCurrent()
        {
            if (_entries.Count == 0) { TolkSpeech.Speak("Liste vide.", true); return; }
            TolkSpeech.Speak($"{_index + 1} sur {_entries.Count}. {_entries[_index]}", true);
        }
    }
}
