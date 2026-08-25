using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace SunHavenAccess.Installer
{
    /// <summary>
    /// Retrouve l'installation de Sun Haven toute seule, pour éviter à l'utilisateur d'avoir à
    /// naviguer dans l'arborescence Windows au lecteur d'écran — c'est justement l'étape la plus
    /// pénible d'une installation manuelle.
    ///
    /// Steam n'installe pas forcément les jeux dans son dossier principal : depuis des années il
    /// gère des « bibliothèques » sur d'autres disques, listées dans libraryfolders.vdf. Ne
    /// chercher qu'au chemin par défaut échouerait donc pour une bonne partie des joueurs.
    /// </summary>
    public static class GameLocator
    {
        /// <summary>Nom du dossier du jeu dans steamapps/common.</summary>
        private const string GameFolderName = "Sun Haven";

        /// <summary>Fichier qui atteste qu'un dossier EST bien l'installation du jeu.</summary>
        public const string GameExecutable = "Sun Haven.exe";

        /// <summary>
        /// Premier chemin valide trouvé, ou null. L'utilisateur peut toujours corriger ensuite :
        /// la détection est une commodité, jamais une contrainte.
        /// </summary>
        public static string FindGameDirectory()
        {
            foreach (string candidate in CandidateDirectories())
            {
                if (IsGameDirectory(candidate)) return candidate;
            }
            return null;
        }

        /// <summary>Un dossier contient-il réellement le jeu ?</summary>
        public static bool IsGameDirectory(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory)) return false;
            try { return File.Exists(Path.Combine(directory, GameExecutable)); }
            catch { return false; }
        }

        private static IEnumerable<string> CandidateDirectories()
        {
            string steamPath = SteamPath();

            if (!string.IsNullOrEmpty(steamPath))
            {
                yield return Path.Combine(steamPath, "steamapps", "common", GameFolderName);

                foreach (string library in SteamLibraries(steamPath))
                {
                    yield return Path.Combine(library, "steamapps", "common", GameFolderName);
                }
            }

            // Derniers recours : les emplacements par défaut, au cas où le registre serait
            // absent (Steam désinstallé puis réinstallé, profil différent...).
            foreach (string root in new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            })
            {
                if (!string.IsNullOrEmpty(root))
                {
                    yield return Path.Combine(root, "Steam", "steamapps", "common", GameFolderName);
                }
            }
        }

        private static string SteamPath()
        {
            foreach (var (hive, key) in new[]
            {
                (RegistryHive.CurrentUser, @"Software\Valve\Steam"),
                (RegistryHive.LocalMachine, @"SOFTWARE\WOW6432Node\Valve\Steam"),
                (RegistryHive.LocalMachine, @"SOFTWARE\Valve\Steam"),
            })
            {
                try
                {
                    using (RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default))
                    using (RegistryKey steam = baseKey.OpenSubKey(key))
                    {
                        string path = steam?.GetValue("SteamPath") as string
                                      ?? steam?.GetValue("InstallPath") as string;
                        if (!string.IsNullOrWhiteSpace(path)) return path.Replace('/', '\\');
                    }
                }
                catch
                {
                    // Clé absente ou accès refusé : on passe simplement à la suivante.
                }
            }
            return null;
        }

        /// <summary>
        /// Bibliothèques Steam secondaires, déclarées dans libraryfolders.vdf. Le format a changé
        /// au fil des versions de Steam ; plutôt que de parser le VDF formellement, on extrait
        /// toutes les valeurs "path" — robuste aux deux formats historiques et suffisant ici.
        /// </summary>
        private static IEnumerable<string> SteamLibraries(string steamPath)
        {
            string vdf = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");

            string content;
            try
            {
                if (!File.Exists(vdf)) yield break;
                content = File.ReadAllText(vdf);
            }
            catch
            {
                yield break;
            }

            foreach (Match match in Regex.Matches(content, "\"path\"\\s*\"([^\"]+)\"", RegexOptions.IgnoreCase))
            {
                string path = match.Groups[1].Value.Replace(@"\\", @"\");
                if (!string.IsNullOrWhiteSpace(path)) yield return path;
            }
        }
    }
}
