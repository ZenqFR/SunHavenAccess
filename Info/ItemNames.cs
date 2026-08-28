using System;
using System.Collections.Generic;
using System.Reflection;
using PSS;
using Wish;

namespace SunHavenAccess.Info
{
    /// <summary>
    /// Le nom d'un objet à partir de son identifiant — dans la langue du jeu, sans table à tenir.
    ///
    /// LE PROBLÈME QUE ÇA RÈGLE. Nommer une case revenait à reconnaître son composant : un minerai
    /// par ci, une culture par là, et une table de traduction maison pour le reste. Chaque objet
    /// jamais rencontré était un trou, chaque ajout du jeu un oubli à venir, et chaque nom une
    /// traduction à écrire deux fois. Un mod qui doit connaître d'avance ce qu'il décrit ne tiendra
    /// jamais dans un donjon dont la disposition est tirée au sort.
    ///
    /// CE QUI EXISTE DÉJÀ DANS LE JEU. `PSS.Database` associe chaque identifiant à ses données
    /// (`Wish.ItemData`), et `ItemData.UnformattedDisplayName` renvoie
    /// `LocalizeText.TranslateText(keyDisplayName, name)` — c'est-à-dire le nom TRADUIT PAR LE JEU,
    /// en français chez qui joue en français, en anglais chez qui joue en anglais. Aucune liste de
    /// notre côté ne pourra jamais être aussi juste ni aussi complète que celle-là.
    ///
    /// DEUX NIVEAUX, JAMAIS DE TROU.
    ///
    /// 1. Le nom traduit, dès qu'on l'a. `Database.GetData` répond IMMÉDIATEMENT quand l'objet est
    ///    déjà en mémoire — ce qui est le cas de tout ce qui est affiché à l'écran, donc de tout ce
    ///    qu'on survole. Sinon le chargement part en tâche de fond et la réponse arrive au passage
    ///    suivant, une fraction de seconde plus tard : le curseur relit sa case en continu.
    ///
    /// 2. Le nom interne, en attendant. `Database.ids` associe le nom d'origine de CHAQUE objet du
    ///    jeu à son identifiant ; retourné une fois, il donne une table complète, sans manque
    ///    possible. Ce nom-là n'est pas traduit, mais il vaut infiniment mieux que « quelque chose
    ///    bloque votre passage ». Il ne sert que le temps du chargement.
    ///
    /// Rien n'est codé en dur, donc rien ne vieillit : un objet ajouté par une mise à jour du jeu
    /// est nommé correctement le jour même, sans qu'on y touche.
    /// </summary>
    internal static class ItemNames
    {
        /// <summary>Noms traduits déjà obtenus. Un identifiant n'est demandé qu'une fois.</summary>
        private static readonly Dictionary<int, string> _translated = new Dictionary<int, string>();

        /// <summary>Identifiants dont le chargement est en cours — pour ne pas le relancer à chaque image.</summary>
        private static readonly HashSet<int> _pending = new HashSet<int>();

        /// <summary>Table complète identifiant → nom interne, construite une fois par réflexion.</summary>
        private static Dictionary<int, string> _internalNames;

        /// <summary>
        /// Le nom de l'objet portant cet identifiant, ou null si le jeu n'en connaît aucun.
        /// Ne bloque jamais : renvoie ce qu'on a de mieux à cet instant.
        /// </summary>
        internal static string Get(int id)
        {
            if (id <= 0) return null;

            if (_translated.TryGetValue(id, out string known)) return known;

            // Un identifiant inconnu du jeu ferait crier `GetData` dans la console à chaque image.
            // On demande d'abord si la question a un sens.
            if (!IsValid(id)) return null;

            Request(id);

            // La demande a pu aboutir sur-le-champ : `GetData` appelle son rappel sans attendre
            // quand l'objet est déjà chargé, ce qui est le cas courant à l'écran.
            if (_translated.TryGetValue(id, out string justArrived)) return justArrived;

            return InternalName(id);
        }

        private static bool IsValid(int id)
        {
            try { return Database.ValidID(id); }
            catch { return false; }
        }

        private static void Request(int id)
        {
            if (!_pending.Add(id)) return;

            try
            {
                Database.GetData<ItemData>(id, data =>
                {
                    _pending.Remove(id);
                    string name = data?.UnformattedDisplayName;
                    if (!string.IsNullOrWhiteSpace(name)) _translated[id] = name.Trim();
                },
                () => _pending.Remove(id));
            }
            catch (Exception e)
            {
                _pending.Remove(id);
                Plugin.Log?.LogDebug($"Nom introuvable pour l'objet {id} : {e.Message}");
            }
        }

        /// <summary>
        /// Le nom d'origine de l'objet, tel que le jeu le range dans sa table. Sert de réponse
        /// immédiate le temps que le nom traduit arrive.
        /// </summary>
        private static string InternalName(int id)
        {
            BuildInternalNames();
            return _internalNames != null && _internalNames.TryGetValue(id, out string name) ? name : null;
        }

        /// <summary>
        /// Retourne une fois pour toutes la table nom → identifiant que le jeu garde en privé.
        ///
        /// Deux objets peuvent partager un identifiant (variantes) ; on garde le premier nom vu,
        /// arbitrairement, parce qu'aucun n'est plus juste que l'autre et que ce nom ne sert que
        /// d'attente. En cas d'échec on n'insiste pas : la table reste vide et seuls les noms
        /// traduits répondent, ce qui reste correct — simplement un peu plus tardif.
        /// </summary>
        private static void BuildInternalNames()
        {
            if (_internalNames != null) return;
            _internalNames = new Dictionary<int, string>();

            try
            {
                Database instance = Database.Instance;
                if (instance == null)
                {
                    _internalNames = null; // La base n'existe pas encore : réessayer plus tard.
                    return;
                }

                FieldInfo field = typeof(Database).GetField("ids", BindingFlags.NonPublic | BindingFlags.Instance);
                if (field?.GetValue(instance) is not Dictionary<string, int> ids) return;

                foreach (var pair in ids)
                {
                    if (string.IsNullOrWhiteSpace(pair.Key)) continue;
                    if (!_internalNames.ContainsKey(pair.Value)) _internalNames[pair.Value] = pair.Key.Trim();
                }

                Plugin.Log?.LogInfo($"Table des objets du jeu : {_internalNames.Count} identifiants connus.");
            }
            catch (Exception e)
            {
                Plugin.Log?.LogWarning($"Table des objets illisible : {e.Message}");
            }
        }
    }
}
