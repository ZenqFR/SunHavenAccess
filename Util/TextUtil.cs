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
        private static readonly Regex TagRegex = new Regex("<[^>]*>");
        private static readonly Regex MultiSpace = new Regex("[ \t]{2,}");

        public static string Clean(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return raw;
            string s = TailArtifact.Replace(raw, "");
            s = TagRegex.Replace(s, "");
            s = MultiSpace.Replace(s, " ");
            return s.Trim();
        }
    }
}
