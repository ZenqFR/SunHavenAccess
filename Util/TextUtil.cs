using System.Text.RegularExpressions;

namespace SunHavenAccess.Util
{
    /// <summary>
    /// Nettoyage de texte pour la synthèse vocale : retire les balises de rich text
    /// (TextMeshPro) et les artefacts internes du moteur de dialogue (effet machine à écrire).
    /// </summary>
    public static class TextUtil
    {
        // L'effet "machine à écrire" de DialogueController ajoute toujours, même une fois
        // le texte entièrement affiché, un résidu du type <alpha=#00 id="a">... en fin de
        // chaîne. On le retire avant tout, avant le nettoyage générique des balises.
        private static readonly Regex TailArtifact = new Regex("<alpha=#00.*", RegexOptions.Singleline);

        // Les prix (boutiques, infobulles de vente) utilisent une icône de monnaie à la place
        // d'un mot ("<sprite="gold_icon" index=0>", "ticket_icon", "orb_icon" — vu en
        // décompilant Wish.NormalItem.GetToolTip et Wish.Shop) : le nettoyage générique des
        // balises les aurait fait disparaître SANS RIEN À LA PLACE, perdant l'info "c'est en
        // pièces d'or / tickets / orbes" — cruciale pour comprendre un prix. On les convertit
        // donc d'abord en mot, avant le nettoyage générique.
        private static readonly Regex GoldSprite = new Regex("<sprite=\"?gold_icon\"?[^>]*>");
        private static readonly Regex TicketSprite = new Regex("<sprite=\"?ticket_icon\"?[^>]*>");
        private static readonly Regex OrbSprite = new Regex("<sprite=\"?orb_icon\"?[^>]*>");

        private static readonly Regex TagRegex = new Regex("<[^>]*>");
        private static readonly Regex MultiSpace = new Regex("[ \t]{2,}");

        public static string Clean(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return raw;
            string s = TailArtifact.Replace(raw, "");
            s = GoldSprite.Replace(s, " pièces d'or");
            s = TicketSprite.Replace(s, " tickets");
            s = OrbSprite.Replace(s, " orbes");
            s = TagRegex.Replace(s, "");
            s = MultiSpace.Replace(s, " ");
            return s.Trim();
        }
    }
}
