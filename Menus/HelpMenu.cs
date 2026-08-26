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

            TolkSpeech.Speak(
                $"Aide, {_entries.Count} rubriques. Flèches haut et bas pour parcourir, " +
                $"Origine pour la première, Fin pour la dernière, Échap ou {Strings.KeyName(ModConfig.Help.Value)} pour fermer.",
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
        /// Une rubrique par sujet, formulée comme une réponse à une question qu'on se pose en
        /// jouant. Chaque rubrique doit se suffire à elle-même : on peut arriver dessus
        /// directement, sans avoir entendu les précédentes.
        /// </summary>
        private static List<string> BuildEntries() => new List<string>
        {
            $"Savoir ce qu'il y a devant vous. {K(ModConfig.DescribeFront)} décrit la case que vous regardez. " +
            $"En marchant, elle est décrite automatiquement à chaque pas ; {K(ModConfig.ToggleVerbosity)} coupe ou rétablit cette annonce.",

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
            "Échap annule le trajet en cours. Fin donne le nombre d'éléments trouvés dans la catégorie.",

            $"Les personnages proches. {K(ModConfig.NextNpc)} passe au personnage suivant autour de vous, avec son nom, sa direction et sa distance.",

            $"La souris. {K(ModConfig.MouseFollowToggle)} active la souris qui pointe toujours la case devant vous. " +
            $"{K(ModConfig.SimulateLeftClick)} fait un clic gauche, Contrôle plus {K(ModConfig.SimulateRightClick)} un clic droit.",

            "Les grands écrans (création de personnage, arbre de compétences, relations). Ils sont vus comme un empilement de bandes — " +
            "barre d'onglets, barre de sous-onglets, grille de contenu, bandeau d'informations. Contrôle plus haut ou bas change de bande, " +
            "Contrôle plus gauche ou droite change de colonne dans la bande. Deux Contrôle plus bas depuis les onglets amènent donc " +
            "dans la barre de métiers, puis dans la grille. Les flèches seules restent dans la case courante, avec un bip aux quatre bords.",

            "Naviguer dans les menus. Les flèches suivent la disposition visuelle réelle : gauche et droite restent sur la ligne et butent en bout avec un bip, " +
            "haut et bas changent de ligne. Entrée valide, Contrôle plus Entrée fait un clic droit.",

            "Changer de zone dans un menu. Contrôle plus une flèche saute directement d'un panneau à l'autre : " +
            "onglets en haut, équipement à gauche, sac à droite, barre d'action en bas. Contrôle plus haut ramène toujours aux onglets.",

            "Changer d'onglet du menu principal. Contrôle plus Tabulation passe à l'onglet suivant, Contrôle plus Majuscule plus Tabulation au précédent, " +
            "parmi sac à dos, arbre de compétences, relations, quêtes, carte, statistiques et paramètres.",

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
            $"{K(ModConfig.AnnounceSkillPoints)} donne les points disponibles dans chaque arbre. " +
            $"{K(ModConfig.AnnounceFestivals)} liste les festivals de la saison en cours.",

            $"La carte du monde. Carte ouverte, {K(ModConfig.MapPreviousLocation)} et {K(ModConfig.MapNextLocation)} parcourent les lieux.",

            "Les écrans en colonnes, comme la création de personnage : catégories à gauche, personnalisation au centre, informations à droite. " +
            "Contrôle plus gauche ou droite saute d'une colonne à l'autre, et annonce laquelle. Les flèches seules parcourent l'écran comme d'habitude.",

            $"La création de personnage. {K(ModConfig.AppearancePrevious)} et {K(ModConfig.AppearanceNext)} parcourent les options d'apparence ; " +
            "Contrôle plus l'une ou l'autre change de catégorie.",

            $"Parler dans le tchat. {K(ModConfig.ChatOpenKey)} ouvre le tchat ou la console du jeu — Entrée ne le fait pas, elle sert à valider dans les menus.",

            $"Vérifier que le son marche. {K(ModConfig.TestTone)} joue un son Windows indépendant de la synthèse vocale : " +
            "si vous l'entendez sans entendre de parole, le problème vient du lecteur d'écran et non du mod.",

            $"Changer une touche. {K(ModConfig.ShortcutsMenuToggle)} ouvre le menu des raccourcis, " +
            "qui liste chaque action avec sa touche et permet de la réassigner sans quitter le jeu.",
        };
    }
}
