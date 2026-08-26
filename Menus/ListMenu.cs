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
        public static void Open(string title, List<string> entries,
                                Action<int> onActivate = null,
                                Func<int, int, string> onAdjust = null)
        {
            if (entries == null || entries.Count == 0)
            {
                TolkSpeech.Speak($"{title} : rien à afficher.", true);
                return;
            }

            _title = title;
            _entries = entries;
            _onActivate = onActivate;
            _onAdjust = onAdjust;
            _index = 0;
            IsOpen = true;

            string how = onAdjust != null ? ", gauche et droite pour changer la valeur" : string.Empty;
            if (onActivate != null) how += ", Entrée pour choisir";

            TolkSpeech.Speak(
                $"{title}, {entries.Count} entrée{(entries.Count > 1 ? "s" : "")}. " +
                $"Flèches haut et bas pour parcourir{how}, Échap pour fermer.", true);
            AnnounceCurrent();
        }

        public static void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            _entries = new List<string>();
            _onActivate = null;
            _onAdjust = null;
            TolkSpeech.Speak($"{_title} fermé.", true);
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
            // ouvertes en même temps se disputeraient les flèches.
            Close();
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
