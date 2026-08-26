using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;

namespace SunHavenAccess.Installer
{
    /// <summary>
    /// Pose et retire les fichiers du mod. Volontairement séparé de l'interface : la logique
    /// d'installation ne dépend d'aucun contrôle graphique et rapporte sa progression par un
    /// simple callback texte.
    ///
    /// Tout est embarqué dans l'exécutable, sous forme d'une archive unique : un seul fichier à
    /// télécharger, installation possible hors ligne, et rien ne casse le jour où une URL de
    /// téléchargement change. L'archive reproduit exactement l'arborescence à créer dans le
    /// dossier du jeu, donc l'installation se réduit à une extraction — sans reconstruction de
    /// chemins, qui s'était révélée fragile.
    /// </summary>
    public static class ModInstaller
    {
        /// <summary>Sous-dossier du mod dans BepInEx/plugins.</summary>
        private const string PluginFolder = @"BepInEx\plugins\SunHavenAccess";

        /// <summary>Nom logique de l'archive embarquée (voir le csproj).</summary>
        private const string PayloadResource = "payload.zip";

        public delegate void Report(string message);

        public static bool PayloadAvailable() =>
            Assembly.GetExecutingAssembly().GetManifestResourceNames().Contains(PayloadResource);

        /// <summary>
        /// Installe le mod. Chaque étape est rapportée au fur et à mesure : sur un lecteur
        /// d'écran, un compte rendu écrit ligne à ligne est bien plus exploitable qu'une barre de
        /// progression, qui ne laisse aucune trace relisible.
        /// </summary>
        public static bool Install(string gameDirectory, Report report)
        {
            if (!GameLocator.IsGameDirectory(gameDirectory))
            {
                report($"Ce dossier ne contient pas {GameLocator.GameExecutable}. Vérifiez le chemin du jeu.");
                return false;
            }

            if (!PayloadAvailable())
            {
                report("Les fichiers à installer sont absents de cet installateur.");
                report("C'est un problème de construction de l'installateur lui-même, pas de votre installation.");
                return false;
            }

            try
            {
                report("Installation en cours...");
                int written = 0;

                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(PayloadResource))
                using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        // Une entrée sans nom est un dossier : l'extraction créera l'arborescence.
                        if (string.IsNullOrEmpty(entry.Name)) continue;

                        string destination = SafeDestination(gameDirectory, entry.FullName);
                        if (destination == null)
                        {
                            report("Entrée d'archive ignorée car son chemin sort du dossier du jeu : " + entry.FullName);
                            continue;
                        }

                        Directory.CreateDirectory(Path.GetDirectoryName(destination));
                        entry.ExtractToFile(destination, overwrite: true);
                        written++;
                    }
                }

                report($"{written} fichier{(written > 1 ? "s" : "")} installé{(written > 1 ? "s" : "")}.");
                report("");

                // Sans BepInEx, les fichiers du mod sont bien posés mais rien ne les charge, et le
                // jeu se lance dans le silence le plus complet. Annoncer « terminé » dans ce cas
                // enverrait l'utilisateur chercher une panne de synthèse vocale qui n'existe pas.
                if (!BepInExPresent(gameDirectory))
                {
                    report("ATTENTION : BepInEx est absent de ce dossier de jeu.");
                    report("Le mod est bien posé, mais rien ne le chargera tant que BepInEx");
                    report("n'est pas installé : le jeu démarrera sans aucune annonce vocale.");
                    report("");
                    report("Installez BepInEx 5.4.23.5 (x64, Mono) depuis :");
                    report("  https://github.com/BepInEx/BepInEx/releases");
                    report("Décompressez-le dans ce même dossier, puis relancez cet installateur.");
                    return false;
                }

                report("Installation terminée.");
                report("Lancez Sun Haven : un message vocal doit confirmer que le mod est chargé.");
                report("Si vous n'entendez rien, appuyez sur F11 dans le jeu pour vérifier le son.");
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                report("Accès refusé à l'écriture dans le dossier du jeu.");
                report("Fermez Sun Haven s'il est ouvert, puis relancez cet installateur en tant qu'administrateur.");
                return false;
            }
            catch (IOException e)
            {
                report("Erreur d'écriture : " + e.Message);
                report("Vérifiez que Sun Haven est bien fermé, puis réessayez.");
                return false;
            }
            catch (Exception e)
            {
                report("Échec inattendu : " + e.Message);
                return false;
            }
        }

        /// <summary>
        /// Retire le mod. Ne supprime QUE le dossier du mod, jamais BepInEx : d'autres mods
        /// peuvent en dépendre, et supprimer le travail d'autrui serait inacceptable.
        /// </summary>
        public static bool Uninstall(string gameDirectory, Report report)
        {
            if (!GameLocator.IsGameDirectory(gameDirectory))
            {
                report($"Ce dossier ne contient pas {GameLocator.GameExecutable}. Vérifiez le chemin du jeu.");
                return false;
            }

            string pluginDirectory = Path.Combine(gameDirectory, PluginFolder);

            try
            {
                if (!Directory.Exists(pluginDirectory))
                {
                    report("Le mod n'est pas installé dans ce dossier : rien à retirer.");
                    return true;
                }

                Directory.Delete(pluginDirectory, recursive: true);
                report("Le mod a été retiré.");
                report("");
                report("BepInEx a volontairement été conservé : d'autres mods peuvent en dépendre.");
                report("Vos sauvegardes et votre fichier de configuration ne sont pas touchés.");
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                report("Accès refusé. Fermez Sun Haven, puis relancez en tant qu'administrateur.");
                return false;
            }
            catch (Exception e)
            {
                report("Échec de la désinstallation : " + e.Message);
                return false;
            }
        }

        /// <summary>
        /// BepInEx est-il en place dans ce dossier de jeu ?
        ///
        /// On teste le chargeur lui-même (`BepInEx/core/BepInEx.dll`) et le point d'entrée natif
        /// (`winhttp.dll`), et non la simple existence du dossier `BepInEx` : ce dossier est
        /// justement celui que cet installateur vient de créer pour y poser le mod, donc sa
        /// présence ne prouve strictement rien.
        /// </summary>
        public static bool BepInExPresent(string gameDirectory)
        {
            try
            {
                return File.Exists(Path.Combine(gameDirectory, @"BepInEx\core\BepInEx.dll"))
                    && File.Exists(Path.Combine(gameDirectory, "winhttp.dll"));
            }
            catch { return false; }
        }

        /// <summary>Le mod est-il déjà présent dans ce dossier de jeu ?</summary>
        public static bool IsInstalled(string gameDirectory)
        {
            try
            {
                return GameLocator.IsGameDirectory(gameDirectory)
                    && File.Exists(Path.Combine(gameDirectory, PluginFolder, "SunHavenAccess.dll"));
            }
            catch { return false; }
        }

        /// <summary>
        /// Chemin de destination d'une entrée d'archive, ou null si elle tente de sortir du
        /// dossier du jeu. Une archive peut contenir des chemins du type "..\..\windows\system32"
        /// ; même si celle-ci est produite par nos soins, un installateur qui écrit hors de son
        /// périmètre est exactement le genre de faille à ne jamais laisser ouverte.
        /// </summary>
        private static string SafeDestination(string gameDirectory, string entryPath)
        {
            string root = Path.GetFullPath(gameDirectory);
            string candidate = Path.GetFullPath(Path.Combine(root, entryPath));

            string rootWithSeparator = root.EndsWith(Path.DirectorySeparatorChar.ToString())
                ? root
                : root + Path.DirectorySeparatorChar;

            return candidate.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase)
                ? candidate
                : null;
        }
    }
}
