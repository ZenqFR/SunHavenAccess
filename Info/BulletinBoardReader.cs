using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using I2.Loc;
using UnityEngine;
using Wish;
using SunHavenAccess.Speech;
using SunHavenAccess.Util;

namespace SunHavenAccess.Info
{
    /// <summary>
    /// Les panneaux d'affichage des villes : les tâches quotidiennes qu'on y prend, source
    /// régulière d'argent, d'expérience et de relations.
    ///
    /// Chaque panneau affiche deux post-its renouvelés chaque jour (un seul aux baraquements).
    /// Visuellement on voit tout de suite s'il reste quelque chose à prendre — une icône
    /// surmonte le panneau. Sans la vue, il fallait interagir avec le panneau, ouvrir chaque
    /// post-it et espérer que quelque chose soit lu : rien ne disait ce qui était proposé, ni si
    /// on avait déjà accepté la tâche de la veille.
    ///
    /// Ce lecteur répond aux deux questions du jour : qu'est-ce qui est proposé, et l'ai-je déjà
    /// pris. Il fonctionne dès qu'on est près d'un panneau, sans avoir à l'ouvrir.
    /// </summary>
    public static class BulletinBoardReader
    {
        /// <summary>
        /// Distance de détection, en unités monde. Assez large pour couvrir « je suis devant le
        /// panneau » sans capter celui d'une autre place ; les villes de Sun Haven n'en ont qu'un
        /// chacune, donc la marge est confortable.
        /// </summary>
        private const float Radius = 8f;

        // `BulletinBoard.bulletinBoardType` est privé, alors que c'est lui qui détermine à la fois
        // les tâches du panneau et la clé de sauvegarde disant si on les a acceptées. Résolu une
        // fois, gardé en cache : la lecture peut être demandée en boucle.
        private static FieldInfo _typeField;
        private static bool _typeResolved;

        /// <summary>
        /// Annonce les tâches du panneau le plus proche. Ne demande pas que le panneau soit
        /// ouvert : c'est précisément avant de l'ouvrir qu'on veut savoir s'il vaut le détour.
        /// </summary>
        public static void AnnounceNearest()
        {
            BulletinBoard board = FindNearest();
            if (board == null)
            {
                TolkSpeech.Speak("Aucun panneau d'affichage à proximité.", true);
                return;
            }

            BulletinBoardType? type = TypeOf(board);
            if (type == null)
            {
                TolkSpeech.Speak("Panneau d'affichage trouvé, mais son type est illisible.", true);
                return;
            }

            var parts = new List<string>
            {
                Localization.Language.T($"Panneau d'affichage, {BoardName(type.Value)}.",
                                        $"Bulletin board, {BoardName(type.Value)}.")
            };
            List<string> tasks = DescribeTasks(type.Value);

            if (tasks.Count == 0) parts.Add(Localization.Language.T(
                "Aucune tâche proposée aujourd'hui.", "No task on offer today."));
            else parts.AddRange(tasks);

            TolkSpeech.Speak(string.Join(" ", parts), true);
        }

        // ------------------------------------------------------------------ Tâches

        /// <summary>
        /// Les tâches du jour pour ce panneau, chacune avec son état.
        ///
        /// Les baraquements n'ont qu'une commission, les autres panneaux deux tâches. On lit le
        /// gestionnaire du jeu plutôt que les post-its affichés : les post-its ne sont peuplés
        /// qu'à l'ouverture du panneau, alors que le but ici est justement de savoir sans ouvrir.
        /// </summary>
        private static List<string> DescribeTasks(BulletinBoardType type)
        {
            var result = new List<string>();
            BulletinBoardManager manager = SingletonBehaviour<BulletinBoardManager>.Instance;
            if (manager == null) return result;

            if (type == BulletinBoardType.Barracks)
            {
                Describe(result, Safe(() => manager.GetBarracksTask()), Accepted("BarracksCommission0"), 1);
                return result;
            }

            for (int i = 0; i < 2; i++)
            {
                int index = i;
                QuestAsset quest = Safe(() => manager.GetTask(index, type));
                Describe(result, quest, Accepted($"Accepted{type}Task{index}"), index + 1);
            }
            return result;
        }

        private static void Describe(List<string> into, QuestAsset quest, bool accepted, int number)
        {
            if (quest == null) return;

            string name = TextUtil.Clean(Safe(() => quest.LocalizedQuestName))
                          ?? Localization.Language.T("tâche sans nom", "unnamed task");
            string description = TextUtil.Clean(LocalizedBoardDescription(quest));
            string state = Localization.Language.T(
                accepted ? "déjà acceptée" : "à prendre",
                accepted ? "already accepted" : "up for grabs");

            string line = Localization.Language.T($"Tâche {number} : {name}, {state}.",
                                                  $"Task {number}: {name}, {state}.");
            if (!string.IsNullOrWhiteSpace(description)) line += $" {description}";

            string reward = DescribeRewards(quest);
            if (reward != null) line += " " + Localization.Language.Pair(
                Localization.Language.T("Récompense", "Reward"), reward) + ".";

            into.Add(line);
        }

        /// <summary>
        /// Le texte du post-it. `QuestAsset` n'expose un accesseur localisé que pour le nom, pas
        /// pour cette description : on assemble donc la traduction de la même façon que le jeu,
        /// clé d'abord, texte brut en repli.
        /// </summary>
        private static string LocalizedBoardDescription(QuestAsset quest)
        {
            try
            {
                string text = LocalizeText.TranslateText(quest.keyBulletinBoardDescription, quest.bulletinBoardDescription);
                if (!string.IsNullOrWhiteSpace(text)) return text;
            }
            catch { }

            try { return quest.bulletinBoardDescription; }
            catch { return null; }
        }

        /// <summary>
        /// Les récompenses garanties, listées brièvement. Les récompenses au choix sont passées
        /// sous silence ici : elles se choisissent au rendu, pas au moment de décider si la tâche
        /// vaut le déplacement.
        /// </summary>
        private static string DescribeRewards(QuestAsset quest)
        {
            try
            {
                List<ItemInfo> rewards = quest.guaranteeRewards;
                if (rewards == null || rewards.Count == 0) return null;

                var named = rewards
                    .Where(r => r?.item != null)
                    .Select(r => r.amount > 1
                        ? $"{r.amount} {r.item.UnformattedDisplayName}"
                        : r.item.UnformattedDisplayName)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();

                return named.Count == 0 ? null : string.Join(", ", named);
            }
            catch { return null; }
        }

        private static bool Accepted(string progressKey)
        {
            try { return SingletonBehaviour<GameSave>.Instance.GetProgressBoolCharacter(progressKey); }
            catch { return false; }
        }

        // ------------------------------------------------------------------ Repérage

        /// <summary>
        /// Le panneau le plus proche dans la scène active. Comme partout ailleurs dans le mod, on
        /// filtre sur `ScenePortalManager.ActiveSceneName` : Sun Haven charge chaque carte en
        /// scène Unity additive, donc les panneaux des villes voisines restent en mémoire et
        /// seraient sinon candidats.
        /// </summary>
        private static BulletinBoard FindNearest()
        {
            Player player = Player.Instance;
            if (player == null) return null;

            Vector3 from = player.transform.position;

            return UnityEngine.Object.FindObjectsOfType<BulletinBoard>()
                .Where(b => b != null && b.gameObject.activeInHierarchy && InActiveScene(b))
                .Select(b => new { Board = b, Distance = TileGeometry.TileDistance(from, b.transform.position) })
                .Where(x => x.Distance <= Radius)
                .OrderBy(x => x.Distance)
                .Select(x => x.Board)
                .FirstOrDefault();
        }

        private static bool InActiveScene(BulletinBoard board)
        {
            try { return board.gameObject.scene.name == ScenePortalManager.ActiveSceneName; }
            catch { return true; }
        }

        private static BulletinBoardType? TypeOf(BulletinBoard board)
        {
            if (!_typeResolved)
            {
                _typeResolved = true;
                _typeField = typeof(BulletinBoard).GetField("bulletinBoardType",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (_typeField == null)
                    Plugin.Log?.LogWarning("BulletinBoard.bulletinBoardType introuvable : les panneaux d'affichage ne seront pas lus.");
            }

            if (_typeField == null) return null;
            try { return (BulletinBoardType)_typeField.GetValue(board); }
            catch { return null; }
        }

        /// <summary>
        /// Nom de ville lisible. Le nom d'énumération est en anglais et collé
        /// (« GreatCity ») : le lire tel quel serait incompréhensible à l'oral.
        /// </summary>
        private static string BoardName(BulletinBoardType type)
        {
            switch (type)
            {
                case BulletinBoardType.SunHaven:   return "Sun Haven";
                case BulletinBoardType.Withergate: return "Withergate";
                case BulletinBoardType.Nelvari:    return "Nelvari";
                case BulletinBoardType.Brinestone: return "Brinestone";
                case BulletinBoardType.GreatCity:  return Localization.Language.T("la Grande Cité", "the Great City");
                case BulletinBoardType.Barracks:   return Localization.Language.T("les baraquements", "the barracks");
                default:                           return type.ToString();
            }
        }

        private static T Safe<T>(Func<T> read) where T : class
        {
            try { return read(); }
            catch { return null; }
        }
    }
}
