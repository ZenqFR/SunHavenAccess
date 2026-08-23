using Wish;

namespace SunHavenAccess.Dialogue
{
    /// <summary>
    /// La lecture du texte se fait désormais via Patches/DialogueLinePatch.cs (dès le début de
    /// la ligne, pas à la fin de l'animation). Cette classe ne conserve que l'état "un dialogue
    /// est en cours", utilisé par TileCursor/HoverReader pour ne pas parasiter la lecture.
    /// </summary>
    public static class DialogueReader
    {
        public static bool DialogueOnGoing =>
            DialogueController.Instance != null && DialogueController.Instance.DialogueOnGoing;
    }
}
