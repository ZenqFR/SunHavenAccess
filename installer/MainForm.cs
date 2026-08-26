using System;
using System.Drawing;
using System.Windows.Forms;

namespace SunHavenAccess.Installer
{
    /// <summary>
    /// Fenêtre unique de l'installateur. Conçue d'abord pour le lecteur d'écran, puisque c'est
    /// exactement le public du mod :
    ///
    /// - Chaque contrôle porte un `AccessibleName` explicite, et le champ de chemin est associé à
    ///   son étiquette — sans quoi NVDA annoncerait « zone d'édition » sans dire de quoi.
    /// - L'ordre de tabulation suit l'ordre de lecture, du haut vers le bas.
    /// - Aucun message éphémère : tout le compte rendu s'écrit dans une zone de texte relisible à
    ///   volonté. Une barre de progression qui disparaît ne laisse aucune trace pour quelqu'un
    ///   qui n'a pas pu la voir passer.
    /// - Aucune boîte de dialogue modale pour signaler un succès ou une erreur : l'information va
    ///   dans le compte rendu, et le focus y est déplacé pour qu'elle soit lue immédiatement.
    /// </summary>
    public sealed class MainForm : Form
    {
        private readonly TextBox _pathBox;
        private readonly TextBox _log;
        private readonly Button _installButton;
        private readonly Button _uninstallButton;

        public MainForm()
        {
            Text = "Installateur de Sun Haven Access";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(680, 460);
            MinimumSize = new Size(560, 400);
            Font = new Font("Segoe UI", 9.75f);

            var intro = new Label
            {
                Text = "Ce programme installe Sun Haven Access, le mod d'accessibilité pour Sun Haven.\r\n"
                     + "Fermez le jeu avant de continuer.",
                Location = new Point(16, 16),
                Size = new Size(640, 40),
                AccessibleName = "Présentation",
            };

            var pathLabel = new Label
            {
                Text = "&Dossier d'installation de Sun Haven :",
                Location = new Point(16, 68),
                Size = new Size(640, 20),
            };

            _pathBox = new TextBox
            {
                Location = new Point(16, 92),
                Size = new Size(520, 25),
                AccessibleName = "Dossier d'installation de Sun Haven",
                AccessibleDescription = "Chemin complet du dossier contenant Sun Haven.exe",
            };
            // Le bouton doit dire « Installer » ou « Mettre à jour » selon ce que contient
            // RÉELLEMENT le dossier saisi, pas selon celui détecté au démarrage : on suit donc
            // chaque frappe. La vérification se résume à un File.Exists, sans coût notable.
            _pathBox.TextChanged += (s, e) => RefreshActionLabel();

            var browseButton = new Button
            {
                Text = "&Parcourir...",
                Location = new Point(548, 90),
                Size = new Size(108, 28),
                AccessibleName = "Parcourir pour choisir le dossier du jeu",
            };
            browseButton.Click += (s, e) => Browse();

            _installButton = new Button
            {
                Text = "&Installer",
                Location = new Point(16, 132),
                Size = new Size(160, 34),
                AccessibleName = "Installer le mod",
            };
            _installButton.Click += (s, e) => RunInstall();

            _uninstallButton = new Button
            {
                Text = "Désinstalle&r",
                Location = new Point(188, 132),
                Size = new Size(160, 34),
                AccessibleName = "Désinstaller le mod",
            };
            _uninstallButton.Click += (s, e) => RunUninstall();

            var logLabel = new Label
            {
                Text = "Compte rendu :",
                Location = new Point(16, 180),
                Size = new Size(640, 20),
            };

            _log = new TextBox
            {
                Location = new Point(16, 204),
                Size = new Size(640, 220),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                AccessibleName = "Compte rendu de l'installation",
                AccessibleDescription = "Résultat de chaque étape. Relisible avec les flèches.",
                TabStop = true,
            };

            Controls.AddRange(new Control[]
            {
                intro, pathLabel, _pathBox, browseButton,
                _installButton, _uninstallButton, logLabel, _log,
            });

            // Ordre de tabulation = ordre de lecture.
            intro.TabIndex = 0;
            pathLabel.TabIndex = 1;
            _pathBox.TabIndex = 2;
            browseButton.TabIndex = 3;
            _installButton.TabIndex = 4;
            _uninstallButton.TabIndex = 5;
            logLabel.TabIndex = 6;
            _log.TabIndex = 7;

            AcceptButton = _installButton;
            Load += (s, e) => Detect();
        }

        private void Detect()
        {
            string found = GameLocator.FindGameDirectory();

            if (found != null)
            {
                _pathBox.Text = found;
                Log("Sun Haven a été trouvé automatiquement :");
                Log("  " + found);
                Log(ModInstaller.IsInstalled(found)
                    ? "Le mod est déjà installé ici : le bouton propose de le mettre à jour."
                    : "Le mod n'est pas encore installé ici.");
            }
            else
            {
                Log("Sun Haven n'a pas été trouvé automatiquement.");
                Log("Indiquez le dossier contenant " + GameLocator.GameExecutable + ",");
                Log("soit en le tapant ci-dessus, soit avec le bouton Parcourir.");
            }

            if (!ModInstaller.PayloadAvailable())
            {
                Log("");
                Log("ATTENTION : cet installateur ne contient aucun fichier à installer.");
                Log("Il s'agit d'un problème de construction de l'installateur lui-même.");
            }

            Log("");
            // Le cas « jeu non trouvé » laisse le champ vide, donc sans TextChanged : on aligne
            // explicitement le bouton plutôt que de compter sur cet événement.
            RefreshActionLabel();
            _pathBox.Focus();
        }

        private void Browse()
        {
            using (var dialog = new FolderBrowserDialog
            {
                Description = "Choisissez le dossier contenant " + GameLocator.GameExecutable,
                ShowNewFolderButton = false,
            })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                _pathBox.Text = dialog.SelectedPath;
                Log(GameLocator.IsGameDirectory(dialog.SelectedPath)
                    ? "Dossier valide : le jeu a bien été trouvé."
                    : "Attention : " + GameLocator.GameExecutable + " est introuvable dans ce dossier.");
            }
        }

        /// <summary>
        /// Fait dire au bouton principal ce qu'il va réellement faire : « Installer » sur un
        /// dossier vierge, « Mettre à jour » là où le mod est déjà posé.
        ///
        /// Le geste est le même dans les deux cas, mais pas ce qu'il signifie pour la personne
        /// qui appuie — et au lecteur d'écran, le libellé du bouton est la seule chose qui le dit.
        /// « Installer » sur une installation existante laisserait croire à un doublon, ou à une
        /// action sans effet.
        /// </summary>
        private void RefreshActionLabel()
        {
            bool installed = ModInstaller.IsInstalled(_pathBox.Text.Trim());

            // Le raccourci change avec le libellé (Alt+I / Alt+J) : garder Alt+I sur un bouton
            // affichant « Mettre à jour » exposerait une lettre absente du texte lu, que rien ne
            // permettrait de deviner.
            _installButton.Text = installed ? "Mettre à &jour" : "&Installer";
            _installButton.AccessibleName = installed
                ? "Mettre à jour le mod"
                : "Installer le mod";
            _installButton.AccessibleDescription = installed
                ? "Remplace les fichiers du mod déjà présents par ceux de cette version."
                : "Installe BepInEx et le mod dans le dossier du jeu.";
        }

        private void RunInstall() => Run(() => ModInstaller.Install(_pathBox.Text.Trim(), Log));

        private void RunUninstall() => Run(() => ModInstaller.Uninstall(_pathBox.Text.Trim(), Log));

        /// <summary>
        /// Exécute une opération en désactivant les boutons le temps du traitement, puis remet le
        /// focus sur le compte rendu : c'est là que se trouve le résultat, donc c'est là que le
        /// lecteur d'écran doit se retrouver.
        /// </summary>
        private void Run(Func<bool> operation)
        {
            _installButton.Enabled = false;
            _uninstallButton.Enabled = false;
            try
            {
                Log("");
                operation();
            }
            finally
            {
                _installButton.Enabled = true;
                _uninstallButton.Enabled = true;
                // L'opération vient de changer ce que contient le dossier : le bouton doit suivre.
                // Après une désinstallation il redevient « Installer », après une pose « Mettre à
                // jour » — sans quoi il décrirait l'état d'avant.
                RefreshActionLabel();
                _log.Focus();
                _log.SelectionStart = _log.TextLength;
            }
        }

        private void Log(string message)
        {
            _log.AppendText(message + Environment.NewLine);
        }

        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
