using System.Collections.Generic;
using UnityEngine;
using SunHavenAccess.Config;
using SunHavenAccess.Localization;
using SunHavenAccess.Speech;

namespace SunHavenAccess.Menus
{
    /// <summary>
    /// Aide du mod, parcourue rubrique par rubrique aux flèches.
    ///
    /// L'aide était auparavant lue d'un seul bloc : plus de quarante touches enchaînées en une
    /// tirade de plusieurs minutes, qu'il fallait écouter en entier pour atteindre la dernière, et
    /// reprendre depuis le début en cas d'inattention. Autant dire inutilisable — un joueur voyant
    /// balaie une liste des yeux et s'arrête où il veut ; ici, chaque rubrique se lit
    /// individuellement et on navigue à son rythme.
    ///
    /// Les rubriques sont regroupées par tâche, pas par ordre alphabétique ni par ordre
    /// d'implémentation : on cherche « comment je me repère », pas « que fait la touche P ».
    /// Elles sont construites à chaque ouverture pour refléter les touches RÉELLEMENT
    /// configurées — une touche réassignée dans le menu des raccourcis doit apparaître ici avec sa
    /// nouvelle valeur, pas avec le défaut.
    ///
    /// Ce menu est en lecture seule. Pour CHANGER une touche, c'est le menu des raccourcis
    /// (Menus/ShortcutsMenu.cs), volontairement distinct : mélanger consultation et modification
    /// exposerait à réassigner une touche par erreur en parcourant l'aide.
    /// </summary>
    public static class HelpMenu
    {
        private static bool _open;
        private static int _index;
        private static List<string> _entries = new List<string>();

        public static bool IsOpen => _open;

        public static void Toggle()
        {
            if (_open)
            {
                Close();
                return;
            }

            _entries = BuildEntries();
            _index = 0;
            _open = true;

            string closeKey = Strings.KeyName(ModConfig.Help.Value);
            TolkSpeech.Speak(
                SunHavenAccess.Localization.Language.IsEnglish
                    ? $"Help, {_entries.Count} topics. Up and down arrows to browse, " +
                      $"Home for the first, End for the last, Escape or {closeKey} to close."
                    : $"Aide, {_entries.Count} rubriques. Flèches haut et bas pour parcourir, " +
                      $"Origine pour la première, Fin pour la dernière, Échap ou {closeKey} pour fermer.",
                true);
            AnnounceCurrent();
        }

        public static void Close()
        {
            if (!_open) return;
            _open = false;
            TolkSpeech.Speak("Aide fermée.", true);
        }

        /// <summary>
        /// Appelée chaque frame tant que l'aide est ouverte. Comme le menu des raccourcis, elle a
        /// alors la main exclusive sur le clavier (voir HotkeyManager) : sans ça, parcourir l'aide
        /// déclencherait au passage les actions du jeu et du mod liées aux mêmes touches.
        /// </summary>
        public static void Tick()
        {
            if (!_open) return;

            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape)) { Close(); return; }

            // Haut/bas ET gauche/droite : les deux paires font la même chose, pour ne pas avoir à
            // se souvenir de l'orientation d'une liste qu'on ne voit pas.
            if (UnityEngine.Input.GetKeyDown(KeyCode.UpArrow) || UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow)) Move(-1);
            else if (UnityEngine.Input.GetKeyDown(KeyCode.DownArrow) || UnityEngine.Input.GetKeyDown(KeyCode.RightArrow)) Move(1);
            else if (UnityEngine.Input.GetKeyDown(KeyCode.Home)) JumpTo(0);
            else if (UnityEngine.Input.GetKeyDown(KeyCode.End)) JumpTo(_entries.Count - 1);
            else if (UnityEngine.Input.GetKeyDown(KeyCode.PageUp)) Move(-5);
            else if (UnityEngine.Input.GetKeyDown(KeyCode.PageDown)) Move(5);
            else if (UnityEngine.Input.GetKeyDown(ModConfig.Repeat.Value)) AnnounceCurrent();
        }

        /// <summary>
        /// Déplacement SANS bouclage, avec un bip aux extrémités — même convention que la
        /// navigation de menus du jeu. Une liste qui reboucle silencieusement fait perdre le
        /// compte de l'endroit où l'on est, ce qui est précisément ce qu'on cherche à éviter ici.
        /// </summary>
        private static void Move(int delta)
        {
            int target = Mathf.Clamp(_index + delta, 0, _entries.Count - 1);
            if (target == _index)
            {
                UiSound.EdgeBump();
                return;
            }
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
            if (_entries.Count == 0)
            {
                TolkSpeech.Speak("Aide vide.", true);
                return;
            }
            TolkSpeech.Speak($"{_index + 1} sur {_entries.Count}. {_entries[_index]}", true);
        }

        // ------------------------------------------------------------------ Contenu

        private static string K(BepInEx.Configuration.ConfigEntry<KeyCode> entry) =>
            Strings.KeyName(entry.Value);

        /// <summary>
        /// Mentionne une touche seulement si elle est assignée.
        ///
        /// Certaines actions sont livrées sans touche parce qu'elles font doublon avec un
        /// comportement automatique. Les annoncer quand même donnerait « non assignée décrit la
        /// case que vous regardez » — une phrase qui n'a aucun sens et laisse croire à un défaut.
        /// </summary>
        private static string Optional(BepInEx.Configuration.ConfigEntry<KeyCode> entry, string lead) =>
            entry.Value == KeyCode.None ? string.Empty : lead + Strings.KeyName(entry.Value) + ".";

        /// <summary>
        /// Une rubrique par sujet, formulée comme une réponse à une question qu'on se pose en
        /// jouant. Chaque rubrique doit se suffire à elle-même : on peut arriver dessus
        /// directement, sans avoir entendu les précédentes.
        /// </summary>
        private static List<string> BuildEntries()
        {
            if (!SunHavenAccess.Localization.Language.IsEnglish) return FrenchEntries();

            List<string> english = EnglishEntries();

            // Une rubrique ajoutée d'un seul côté disparaît en silence pour l'autre langue, et
            // rien en jeu ne le signale : on ne s'aperçoit jamais d'une aide qu'on n'entend pas.
            // Le journal, lui, le dira dès le premier lancement.
            int french = FrenchEntries().Count;
            if (english.Count != french)
            {
                Plugin.Log?.LogWarning(
                    $"L'aide compte {french} rubriques en français et {english.Count} en anglais : " +
                    "des rubriques manquent d'un côté ou de l'autre.");
            }

            return english;
        }

        /// <summary>
        /// L'aide est le seul texte du mod trop long et trop tourné pour passer par la traduction
        /// phrase à phrase de Translator : ce sont des paragraphes, pas des annonces. Ils sont donc
        /// écrits deux fois, en regard l'un de l'autre — la seule façon que chaque version se lise
        /// naturellement plutôt que comme une traduction mot à mot.
        ///
        /// Les deux listes doivent rester dans le MÊME ORDRE et de la même longueur : une rubrique
        /// ajoutée d'un côté doit l'être de l'autre, sans quoi un utilisateur anglophone perd
        /// silencieusement une fonctionnalité entière — la plus sûre façon qu'elle n'existe pas.
        /// </summary>
        private static List<string> FrenchEntries() => new List<string>
        {
            "Savoir ce qu'il y a devant vous. La case que vous regardez est décrite automatiquement à chaque pas. " +
            $"{K(ModConfig.ToggleVerbosity)} coupe ou rétablit cette annonce, et {K(ModConfig.Repeat)} redit la dernière à l'arrêt." +
            Optional(ModConfig.DescribeFront, " Une touche dédiée peut aussi la redemander : "),

            $"Savoir où vous êtes. {K(ModConfig.Position)} donne vos coordonnées et la direction que vous regardez. " +
            $"{K(ModConfig.TurnLeft)} et {K(ModConfig.TurnRight)} vous font pivoter sans vous déplacer.",

            $"Répéter. {K(ModConfig.Repeat)} redit la dernière annonce. " +
            $"{K(ModConfig.ReadFullDescription)} relit le détail complet de ce qui vient d'être annoncé de façon abrégée — " +
            "description d'un objet, ou niveaux et or d'une sauvegarde.",

            $"L'heure et votre état. {K(ModConfig.Clock)} donne l'heure, le jour, la saison et la météo. " +
            $"{K(ModConfig.Status)} donne votre santé, votre mana et votre bourse — pièces, et orbes ou tickets si vous en avez.",

            "Les montures. Le sifflet sert à la fois à monter et à descendre : le mod annonce « en selle » ou « à pied » à chaque changement. " +
            "À cheval on va nettement plus vite, mais les outils ne s'utilisent plus. Monter est refusé en intérieur, et le jeu le dit lui-même.",

            "Le mana. Ce n'est pas une réserve de sorts : c'est la jauge que consomment vos outils, et à zéro la pioche, " +
            "la houe et l'arrosoir cessent d'agir. Le mod prévient à la moitié, au quart, presque à sec, puis à l'épuisement — " +
            "sans quoi l'outil s'arrêterait d'un coup sans que rien n'explique pourquoi. Une nuit de sommeil le restaure.",

            $"Explorer sans bouger : le curseur de case libre. {K(ModConfig.FreeCursorToggle)} l'active. " +
            "Les flèches le déplacent alors case par case n'importe où sur la carte, et chaque case est décrite avec sa direction et sa distance. " +
            $"{K(ModConfig.FreeCursorRecenter)} le ramène sur vous.",

            $"Agir à distance avec le curseur libre. Curseur actif : Origine redécrit la case visée, " +
            $"Contrôle plus Origine vous y conduit à pied, et {K(ModConfig.SimulateLeftClick)} y agit sans vous déplacer.",

            "Trouver ce qui vous entoure : le scanner. Contrôle plus Page précédente ou Page suivante change de catégorie, " +
            "parmi personnages, plantations, ressources, bâtiments et portails, animaux et compagnons, ennemis, mobilier et rangement. " +
            "Page précédente et Page suivante parcourent les éléments trouvés.",

            "Se rendre quelque part. Une fois un élément du scanner annoncé, Contrôle plus Origine vous y conduit automatiquement. " +
            "Échap annule le trajet en cours. Le nombre d'éléments trouvés est annoncé à chaque changement de catégorie.",

            $"Les personnages proches. {K(ModConfig.NextNpc)} passe au personnage suivant autour de vous, avec son nom, sa direction et sa distance.",

            "Les objets tombés par terre. Le scanner a une catégorie « Objets au sol » : c'est ce qui tombe " +
            "d'un arbre abattu, d'un rocher brisé, d'une créature vaincue — et ce qui disparaît si personne " +
            "ne le ramasse. Chaque objet est annoncé avec sa quantité, « trois Pierre », de quoi décider si le " +
            "détour vaut la peine. Contrôle plus Origine vous y conduit, et le ramassage se fait tout seul : " +
            "dans Sun Haven, marcher dessus suffit. " +
            "Pour qui voit, un tas d'objets par terre saute aux yeux ; sans la vue, on repart en laissant sa " +
            "récolte derrière soi sans même le savoir.",

            $"Vos points favoris. {K(ModConfig.Favorites)} ouvre la liste : « Ajouter un point ici » le pose " +
            "à l'endroit exact où vous vous tenez, et vous demande son nom — tapez, Entrée pour valider, " +
            "Échap pour annuler, F2 pour réentendre ce que vous avez écrit. " +
            "Valider un point existant permet de s'y rendre, de le renommer ou de le supprimer. " +
            "Le scanner a une catégorie Favoris : vos points y sont trouvés comme n'importe quoi d'autre, " +
            "avec leur distance, et le trajet automatique y mène. " +
            "Le scanner ne trouve que ce que le JEU connaît — un rocher, une porte, un habitant ; il ne sait rien " +
            "de l'endroit où vous plantez vos navets ni du coin où vous pêchez. Ces points-là sont à vous. " +
            "Ils sont conservés d'une partie à l'autre, et ceux des autres zones apparaissent aussi dans la liste, " +
            "avec le nom de leur zone.",

            $"Les quêtes, et comment s'y rendre. {K(ModConfig.AnnounceQuests)} ouvre la liste des quêtes en cours ; " +
            "chacune indique à qui la rendre quand elle le sait. Valider une quête ouvre ce qu'on peut en faire : " +
            "s'y rendre, réécouter la description, connaître la progression. " +
            "« S'y rendre » fait le trajet ENTIER et s'arrête sur le point exact du rendu, pas à l'entrée de la ville — " +
            "le jeu range la carte et les coordonnées avec la quête, c'est ce qui lui sert à poser son marqueur, " +
            "et ce marqueur ne vous servait à rien. " +
            "Une quête sans lieu de rendu — tuer des créatures, récolter — vous le dira : elle se termine en faisant " +
            "ce qu'elle demande. Un lieu où vous n'êtes jamais allé aussi, puisque le plan du monde ne connaît que " +
            "ce que vous avez exploré.",

            $"Se déplacer case par case. {K(ModConfig.StepMovement)} active ce mode : une flèche, un pas, " +
            "et la case où vous arrivez est annoncée. Le jeu, lui, fait glisser le personnage tant qu'on tient " +
            "la touche — on dépasse, on revient, on redépasse. Ce mode existe pour se placer exactement : " +
            "labourer la bonne case, franchir une porte étroite, longer une clôture. " +
            "Une case bloquée refuse le pas et le dit. La même touche revient au déplacement normal. " +
            "Un menu ouvert, ou le curseur de case libre, gardent la priorité sur les flèches.",

            $"La souris. {K(ModConfig.MouseFollowToggle)} VERROUILLE la souris sur la case devant vous : elle y reste, " +
            "et la bouger à la main n'a plus aucun effet tant que vous ne la libérez pas avec la même touche. " +
            "C'est ce qui évite qu'une souris oubliée sur un bouton ne vole le curseur du clavier. " +
            $"{K(ModConfig.SimulateLeftClick)} fait un clic gauche, Contrôle plus {K(ModConfig.SimulateRightClick)} un clic droit. " +
            "En jeu et hors menu, Entrée fait aussi le clic gauche et Contrôle plus Entrée le clic droit.",

            "Les grands écrans, comme l'arbre de compétences. Les flèches seules suffisent : elles parcourent la grille rangée par rangée " +
            "et colonne par colonne, avec un bip aux quatre bords, et annoncent l'intitulé de chaque rangée — Mobilité, Bûcheronnage, " +
            "Collecte, Social. Contrôle plus gauche ou droite ne sert qu'à changer de PANNEAU, quand l'écran en compte plusieurs " +
            "côte à côte, comme les trois colonnes de la création de personnage.",

            "Naviguer dans les menus. Les flèches suivent la disposition visuelle réelle : gauche et droite restent sur la ligne et butent en bout avec un bip, " +
            "haut et bas changent de ligne. Entrée valide, Contrôle plus Entrée fait un clic droit.",

            "Changer de zone dans un menu. Contrôle plus une flèche saute directement d'un panneau à l'autre : " +
            "onglets en haut, équipement à gauche, sac à droite, barre d'action en bas. Contrôle plus haut ramène toujours aux onglets.",

            "Changer d'onglet du menu principal. Contrôle plus Tabulation passe à l'onglet suivant, Contrôle plus Majuscule plus Tabulation au précédent, " +
            "parmi sac à dos, arbre de compétences, relations, quêtes, carte, statistiques et paramètres.",

            "Chaque onglet s'ouvre en liste, À LA DEMANDE. Sauf le sac à dos, qui reste une grille, tout onglet du menu " +
            "présente son contenu en liste : compétences, relations, quêtes, lieux, statistiques, réglages. " +
            "Tabulation ouvre le menu, gauche et droite passent d'un onglet à l'autre, ENTRÉE ouvre la liste de l'onglet où " +
            "vous êtes, haut et bas la parcourent, Contrôle plus haut en ressort, Échap referme. Parcourir n'est pas choisir : " +
            "arriver sur un onglet n'ouvre plus rien tout seul, vous passez donc devant librement. Les touches dédiées à " +
            "chacune de ces listes restent utilisables pour les consulter sans ouvrir le menu.",

            "Sortir d'une liste d'onglet, et y revenir. Une liste ouverte capte tout le clavier : " +
            "CONTRÔLE PLUS HAUT en ressort et ramène à la barre d'onglets, CONTRÔLE PLUS BAS y entre à nouveau. " +
            "C'est le même geste que pour changer de zone partout ailleurs dans les menus. " +
            "Tant que vous êtes ressorti, changer d'onglet ne rouvre plus rien : vous parcourez les onglets tranquillement " +
            "aux flèches, puis vous entrez dans celui que vous voulez.",

            $"Ranger son sac. {K(ModConfig.SortBackpack)} trie le sac. {K(ModConfig.AnnounceContents)} en donne un résumé. " +
            $"{K(ModConfig.StoreInChests)} range dans les coffres proches. Les touches 1 à 0 envoient l'objet sélectionné vers la barre d'action, ou l'en retirent.",

            $"Poser un meuble ou un bâtiment. Gardez l'objet en main et activez le curseur libre pour viser : " +
            $"le mod annonce dès que l'emplacement visé devient valide ou invalide. {K(ModConfig.PlacementStatus)} redit l'état du placement en cours.",

            $"Les animaux de la ferme. {K(ModConfig.HerdStatus)} donne le bilan du troupeau présent : " +
            "combien sont à nourrir, combien à caresser, et combien ont laissé un produit au sol.",

            $"Les paquets à compléter : musée, autel, aquarium. Devant un paquet ouvert, {K(ModConfig.BundleStatus)} dit ce qu'il manque encore, " +
            "objet par objet, avec les quantités déjà déposées. En parcourant les emplacements aux flèches, chacun annonce l'objet qu'il attend " +
            "et où en est le dépôt.",

            $"Changer un sort équipé. {K(ModConfig.EquipSpell)} ouvre le choix de sort d'un emplacement : chaque appui passe à l'emplacement " +
            "suivant, puis referme. Les flèches parcourent les sorts disponibles, Entrée équipe celui annoncé. " +
            "C'est le menu du jeu lui-même qui s'ouvre, donc la liste correspond exactement à ce que vous avez débloqué.",

            $"Les sorts. {K(ModConfig.AnnounceSpells)} annonce les quatre sorts équipés, emplacement par emplacement, et signale celui en cours d'incantation. " +
            "Quand un sort est en recharge ou que le mana manque, le jeu le dit lui-même et le mod le lit.",

            $"La pêche. {K(ModConfig.FishingToneToggle)} active ou coupe le bip continu qui aide à viser pendant le mini-jeu.",

            $"Les panneaux d'affichage des villes. Près d'un panneau, {K(ModConfig.BulletinBoardTasks)} annonce les tâches du jour, " +
            "si vous les avez déjà acceptées, et ce qu'elles rapportent — sans avoir à ouvrir le panneau.",

            $"Vos quêtes et vos relations. {K(ModConfig.AnnounceQuests)} liste vos quêtes actives. " +
            $"{K(ModConfig.AnnounceRelationships)} donne vos relations avec les personnages.",

            $"Votre progression. {K(ModConfig.AnnounceProfessions)} donne vos niveaux de compétence. " +
            $"{K(ModConfig.AnnounceSkillPoints)} ouvre les compétences en LISTE : d'abord les métiers avec leurs points à dépenser, " +
            "puis, une fois un métier choisi, ses compétences une par une — nom, palier, rang, condition et effet. " +
            "Rien à se représenter de la grille affichée. " +
            $"{K(ModConfig.AnnounceFestivals)} liste les festivals de la saison en cours.",

            $"La carte du monde. Carte ouverte, {K(ModConfig.MapPreviousLocation)} ouvre la LISTE de tous les lieux : on la parcourt aux flèches " +
            "et Entrée ouvre celui qu'on a choisi. Trois choix apparaissent alors : s'y rendre, lire la description, " +
            "ou sortir vers une autre zone. " +
            $"{K(ModConfig.MapNextLocation)} garde l'ancien parcours au coup par coup.",

            "Se rendre à un lieu de la carte. « S'y rendre » fait le trajet ENTIER, même si le lieu est dans une autre zone : " +
            "le personnage marche jusqu'à la bonne sortie, la franchit, et repart de l'autre côté, jusqu'à la porte du lieu. " +
            "Le mod apprend le plan du monde en jouant, et le retient d'une partie à l'autre : il ne connaît donc que les zones " +
            "où vous êtes déjà passé. Un lieu jamais visité vous le dira, au lieu de vous envoyer marcher au hasard. " +
            "Deux refus viennent du jeu lui-même et non du mod : une boutique fermée à cette heure, et une zone pas encore débloquée. " +
            "Dans ces cas le trajet s'arrête et vous prévient plutôt que de vous laisser planté devant une porte. " +
            "Échap annule un trajet en cours.",

            "Les écrans en colonnes, comme la création de personnage : catégories à gauche, personnalisation au centre, informations à droite. " +
            "Contrôle plus gauche ou droite saute d'une colonne à l'autre, et annonce laquelle. Les flèches seules parcourent l'écran comme d'habitude.",

            "Choisir une sauvegarde. La liste des parties s'ouvre TOUTE SEULE à l'arrivée sur l'écran — nom et date, de quoi les reconnaître. " +
            "Valider une partie propose Charger, Détails complets — niveaux de métier, or, orbes, tickets — puis Supprimer, " +
            "placée en dernier et nommée avec la partie pour qu'on ne puisse pas effacer la mauvaise par réflexe. " +
            "Un emplacement libre lance directement une nouvelle partie. " +
            $"Échap referme la liste, et {K(ModConfig.SaveList)} la rouvre.",

            "Les autres joueurs. En coopération, le mod annonce qui rejoint la partie et qui la quitte. " +
            "Les autres joueurs apparaissent aussi dans la catégorie Personnages du scanner, distingués des personnages du jeu. " +
            "Le mod n'annonce que VOS actions : les coups reçus, les prises à la pêche et les récoltes de vos partenaires restent silencieux. " +
            "Vos partenaires n'ont rien à installer pour jouer avec vous.",

            "La création de personnage. L'écran a trois colonnes : les catégories à gauche, les choix de la catégorie courante au centre, " +
            "votre personnage, le champ du nom et le bouton Valider à droite. Contrôle plus gauche ou droite change de colonne. " +
            "Un nom et une date d'anniversaire sont obligatoires : le mod annonce ce qui manque encore, et le dit quand tout est prêt. " +
            $"{K(ModConfig.AppearancePrevious)} et {K(ModConfig.AppearanceNext)} parcourent aussi les options d'apparence, Contrôle plus l'une ou l'autre change de catégorie.",

            $"Parler dans le tchat. {K(ModConfig.ChatOpenKey)} ouvre le tchat ou la console du jeu — Entrée ne le fait pas, elle sert à valider dans les menus.",

            $"Vérifier que le son marche. {K(ModConfig.TestTone)} joue un son Windows indépendant de la synthèse vocale : " +
            "si vous l'entendez sans entendre de parole, le problème vient du lecteur d'écran et non du mod.",

            $"Les réglages. {K(ModConfig.SettingsList)} les ouvre en liste : d'abord ceux du mod — bip de bord, navigation directionnelle, mode bref — " +
            "puis ceux du jeu affichés à l'écran. Haut et bas parcourent les options, GAUCHE et DROITE changent la valeur de celle où l'on est : " +
            "une case se coche, un curseur se déplace, une liste déroulante change d'option. Les réglages du mod ne demandaient jusqu'ici " +
            "rien de moins que d'éditer un fichier de configuration à la main.",

            $"Changer une touche. {K(ModConfig.ShortcutsMenuToggle)} ouvre le menu des raccourcis, qui liste chaque action avec sa touche " +
            "et permet de la réassigner sans quitter le jeu. Sur l'écran de saisie, Retour arrière ou Suppression retire l'assignation : " +
            "l'action reste dans la liste, disponible si vous en voulez plus tard, mais n'occupe plus aucune touche.",
        };

        private static List<string> EnglishEntries() => new List<string>
        {
            "Knowing what is in front of you. The tile you are facing is described automatically at every step. " +
            $"{K(ModConfig.ToggleVerbosity)} turns that announcement off and back on, and {K(ModConfig.Repeat)} repeats the last one while standing still." +
            Optional(ModConfig.DescribeFront, " A dedicated key can also ask for it again: "),

            $"Knowing where you are. {K(ModConfig.Position)} gives your coordinates and the direction you are facing. " +
            $"{K(ModConfig.TurnLeft)} and {K(ModConfig.TurnRight)} turn you on the spot without moving you.",

            $"Repeating. {K(ModConfig.Repeat)} says the last announcement again. " +
            $"{K(ModConfig.ReadFullDescription)} reads out the full detail of whatever was just announced in short form — " +
            "an item's description, or a save's levels and gold.",

            $"The time and your condition. {K(ModConfig.Clock)} gives the time, the day, the season and the weather. " +
            $"{K(ModConfig.Status)} gives your health, your mana and your purse — coins, plus orbs or tickets if you have any.",

            "Mounts. The whistle both mounts and dismounts: the mod announces \"mounted\" or \"on foot\" at each change. " +
            "On a mount you move considerably faster, but tools no longer work. Mounting is refused indoors, and the game says so itself.",

            "Mana. It is not a spell reserve: it is the gauge your tools drain, and at zero the pickaxe, " +
            "the hoe and the watering can simply stop working. The mod warns you at half, at a quarter, nearly dry, then empty — " +
            "without which the tool would stop dead with nothing to explain why. A night's sleep restores it.",

            $"Exploring without moving: the free tile cursor. {K(ModConfig.FreeCursorToggle)} turns it on. " +
            "The arrows then move it tile by tile anywhere on the map, and each tile is described with its direction and distance. " +
            $"{K(ModConfig.FreeCursorRecenter)} brings it back to you.",

            $"Acting at a distance with the free cursor. With the cursor active: Home describes the targeted tile again, " +
            $"Control plus Home walks you there, and {K(ModConfig.SimulateLeftClick)} acts on it without moving you.",

            "Finding what is around you: the scanner. Control plus Page Up or Page Down changes category, " +
            "among characters, crops, resources, buildings and portals, animals and pets, enemies, furniture and storage. " +
            "Page Up and Page Down move through what was found.",

            "Going somewhere. Once a scanner result has been announced, Control plus Home walks you there automatically. " +
            "Escape cancels the walk in progress. How many things were found is announced at every change of category.",

            $"Nearby characters. {K(ModConfig.NextNpc)} moves to the next character around you, with their name, direction and distance.",

            $"The mouse. {K(ModConfig.MouseFollowToggle)} makes the mouse always point at the tile in front of you. " +
            $"{K(ModConfig.SimulateLeftClick)} left-clicks, Control plus {K(ModConfig.SimulateRightClick)} right-clicks.",

            "Large screens, such as the skill tree. Plain arrows are enough: they move across the grid row by row " +
            "and column by column, with a beep at each of the four edges, and announce each row's label — Mobility, Woodcutting, " +
            "Gathering, Social. Control plus left or right only ever changes PANEL, on screens that hold several " +
            "side by side, like the three columns of character creation.",

            "Moving around menus. The arrows follow the real visual layout: left and right stay on the row and stop at its end with a beep, " +
            "up and down change row. Enter confirms, Control plus Enter right-clicks.",

            "Jumping between areas of a menu. Control plus an arrow jumps straight from one panel to another: " +
            "tabs at the top, equipment on the left, bag on the right, action bar at the bottom. Control plus up always returns to the tabs.",

            "Changing tab in the main menu. Control plus Tab moves to the next tab, Control plus Shift plus Tab to the previous one, " +
            "among backpack, skill tree, relationships, quests, map, statistics and settings.",

            "Each tab opens as a list, ON DEMAND. Except the backpack, which stays a grid, every menu tab presents its " +
            "contents as a list: skills, relationships, quests, locations, statistics, settings. " +
            "Tab opens the menu, left and right move between tabs, ENTER opens the list of the tab you are on, " +
            "up and down browse it, Control plus up leaves it, and Escape closes. Browsing is not choosing: moving onto a " +
            "tab no longer opens anything by itself, so you can pass over them freely. The keys dedicated to each of these " +
            "lists still work, to consult them without opening the menu.",

            "Leaving a tab's list, and coming back. An open list captures the whole keyboard: " +
            "CONTROL PLUS UP leaves it and returns to the tab bar, CONTROL PLUS DOWN enters it again. " +
            "It's the same gesture used to change area everywhere else in the menus. " +
            "While you're out, changing tab no longer reopens anything: you browse the tabs freely with the arrows, " +
            "then enter the one you want.",

            $"Tidying your bag. {K(ModConfig.SortBackpack)} sorts the bag. {K(ModConfig.AnnounceContents)} gives a summary of it. " +
            $"{K(ModConfig.StoreInChests)} puts things away in nearby chests. The 1 to 0 keys send the selected item to the action bar, or take it off.",

            $"Placing furniture or a building. Keep the item in hand and turn on the free cursor to aim: " +
            $"the mod announces the moment the targeted spot becomes valid or invalid. {K(ModConfig.PlacementStatus)} repeats the state of the placement in progress.",

            $"Farm animals. {K(ModConfig.HerdStatus)} gives the state of the herd present: " +
            "how many need feeding, how many need petting, and how many have left a product on the ground.",

            $"Bundles to complete: museum, altar, aquarium. In front of an open bundle, {K(ModConfig.BundleStatus)} says what is still missing, " +
            "item by item, with the quantities already handed in. Browsing the slots with the arrows, each one announces the item it expects " +
            "and how far along the deposit is.",

            $"Changing an equipped spell. {K(ModConfig.EquipSpell)} opens the spell picker for one slot: each press moves to the next " +
            "slot, then closes. The arrows browse the available spells, Enter equips the announced one. " +
            "It is the game's own menu that opens, so the list matches exactly what you have unlocked.",

            $"Spells. {K(ModConfig.AnnounceSpells)} announces the four equipped spells, slot by slot, and flags the one being cast. " +
            "When a spell is on cooldown or mana is short, the game says so itself and the mod reads it.",

            $"Fishing. {K(ModConfig.FishingToneToggle)} turns the continuous aiming beep during the mini-game on or off.",

            $"Town bulletin boards. Near a board, {K(ModConfig.BulletinBoardTasks)} announces the day's tasks, " +
            "whether you have already accepted them, and what they pay — without having to open the board.",

            $"Your quests and your relationships. {K(ModConfig.AnnounceQuests)} lists your active quests. " +
            $"{K(ModConfig.AnnounceRelationships)} gives your relationships with the characters.",

            $"Your progress. {K(ModConfig.AnnounceProfessions)} gives your skill levels. " +
            $"{K(ModConfig.AnnounceSkillPoints)} opens skills as a LIST: first the professions with their points to spend, " +
            "then, once a profession is chosen, its skills one by one — name, tier, rank, condition and effect. " +
            "Nothing about the displayed grid to picture. " +
            $"{K(ModConfig.AnnounceFestivals)} lists the current season's festivals.",

            $"The world map. With the map open, {K(ModConfig.MapPreviousLocation)} opens the LIST of every location: browse it with the arrows " +
            "and Enter opens the one you chose, with its description. " +
            $"{K(ModConfig.MapNextLocation)} keeps the old one-at-a-time browsing. " +
            "The map cannot start a walk: its locations are interface icons, with no position in the world. " +
            "To travel, use the scanner, which targets real objects.",

            "Column screens, such as character creation: categories on the left, customisation in the centre, information on the right. " +
            "Control plus left or right jumps from one column to the next, and says which. Plain arrows browse the screen as usual.",

            "Choosing a save. The list of games opens BY ITSELF when you arrive on the screen — name and date, enough to recognise them. " +
            "Confirming a game offers Load, Full details — profession levels, gold, orbs, tickets — then Delete, " +
            "placed last and named with the save so you cannot erase the wrong one by reflex. " +
            "An empty slot starts a new game directly. " +
            $"Escape closes the list, and {K(ModConfig.SaveList)} reopens it.",

            "Other players. In co-op, the mod announces who joins the game and who leaves. " +
            "Other players also appear in the scanner's Characters category, kept distinct from the game's own characters. " +
            "The mod announces only YOUR actions: hits taken, catches and harvests by your partners stay silent. " +
            "Your partners have nothing to install to play with you.",

            "Character creation. The screen has three columns: categories on the left, the current category's choices in the centre, " +
            "your character, the name field and the Confirm button on the right. Control plus left or right changes column. " +
            "A name and a birthday are required: the mod announces what is still missing, and says so when everything is ready. " +
            $"{K(ModConfig.AppearancePrevious)} and {K(ModConfig.AppearanceNext)} also browse the appearance options, Control plus either one changes category.",

            $"Talking in chat. {K(ModConfig.ChatOpenKey)} opens the game's chat or console — Enter does not, it confirms in menus.",

            $"Checking that sound works. {K(ModConfig.TestTone)} plays a Windows sound independent of speech: " +
            "if you hear it but hear no speech, the problem is the screen reader and not the mod.",

            $"Settings. {K(ModConfig.SettingsList)} opens them as a list: first the mod's own — edge beep, directional navigation, brief mode — " +
            "then the game's, as shown on screen. Up and down browse the options, LEFT and RIGHT change the value of the one you are on: " +
            "a box ticks, a slider moves, a dropdown changes option. Until now the mod's settings asked for nothing less " +
            "than editing a configuration file by hand.",

            $"Changing a key. {K(ModConfig.ShortcutsMenuToggle)} opens the shortcuts menu, which lists every action with its key " +
            "and lets you reassign it without leaving the game. On the entry screen, Backspace or Delete clears the binding: " +
            "the action stays in the list, available if you want it later, but no longer occupies any key.",
        };
    }
}
