namespace SunHavenAccess.Menus
{
    /// <summary>
    /// Un menu vocal du mod est-il ouvert ?
    ///
    /// Quand une liste, l'aide ou le menu des raccourcis a la parole, il l'a SEUL : tout ce qui
    /// annonce de lui-même — la case devant soi, l'élément survolé, l'infobulle — doit se taire.
    /// Sans quoi chaque déplacement dans la liste se termine par une phrase sans rapport, mise en
    /// file derrière l'entrée qu'on voulait entendre.
    ///
    /// La règle est ici, en un seul endroit, plutôt que recopiée dans chaque module. Elle l'était
    /// déjà à trois endroits, et le quatrième a été oublié : le curseur de case continuait de
    /// décrire le terrain par-dessus la liste des relations. Une règle recopiée est une règle
    /// qu'on finit par oublier quelque part.
    /// </summary>
    public static class VoiceMenus
    {
        public static bool AnyOpen => ListMenu.IsOpen || HelpMenu.IsOpen || ShortcutsMenu.IsOpen;
    }
}
