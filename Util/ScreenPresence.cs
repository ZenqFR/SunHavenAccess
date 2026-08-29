using UnityEngine;
using Sirenix.OdinInspector;
using Wish;

namespace SunHavenAccess.Util
{
    /// <summary>
    /// « Cet écran est-il là ? », posé sans ruiner le jeu.
    ///
    /// LE PIÈGE, mesuré et non supposé. `SingletonBehaviour&lt;T&gt;.Instance` a l'air d'être une
    /// simple lecture de champ. Elle l'est — tant que l'objet existe. Dès qu'il est absent, la
    /// propriété relance `FindObjectOfType&lt;T&gt;()`, c'est-à-dire un balayage de TOUTE la scène,
    /// et elle le refait à chaque appel puisqu'elle ne trouve jamais rien à retenir.
    ///
    /// Trois modules du mod surveillaient ainsi l'apparition d'un écran, à chaque image. Hors de
    /// cet écran — donc presque toujours — c'était trois balayages complets par image. Le
    /// chronomètre intégré a désigné le coupable sans ambiguïté : vingt-trois millisecondes par
    /// image pour un seul de ces modules, alors qu'une image entière en dure seize. Le jeu tombait
    /// à vingt-cinq images par seconde, et cela se sentait jusque dans le déplacement de la souris.
    ///
    /// LA RÈGLE ICI. Tant qu'on tient l'objet, on répond instantanément — c'est le cas quand
    /// l'écran est ouvert, et c'est là que la réactivité compte. Quand il est absent, on ne
    /// redemande que quelques fois par seconde : un écran qui s'ouvre est annoncé un quart de
    /// seconde plus tard, ce que personne ne peut percevoir, et le reste du temps le mod ne coûte
    /// plus rien à qui ne s'en sert pas.
    /// </summary>
    internal static class ScreenPresence<T> where T : SerializedMonoBehaviour
    {
        /// <summary>
        /// Un quart de seconde : imperceptible à l'ouverture d'un écran, et divise par une
        /// quinzaine le nombre de balayages à soixante images par seconde.
        /// </summary>
        private const float ProbeInterval = 0.25f;

        private static T _held;
        private static float _nextProbe;

        /// <summary>
        /// L'objet s'il est présent, null sinon. Ne coûte un balayage qu'au rythme ci-dessus.
        /// </summary>
        internal static T Instance
        {
            get
            {
                // `!= null` sur un objet Unity répond aussi « non » à un objet détruit : un écran
                // qu'on vient de fermer ne reste donc pas retenu à tort.
                if (_held != null) return _held;

                if (Time.unscaledTime < _nextProbe) return null;
                _nextProbe = Time.unscaledTime + ProbeInterval;

                _held = SingletonBehaviour<T>.Instance;
                return _held;
            }
        }

        /// <summary>L'objet, seulement s'il est réellement affiché.</summary>
        internal static T Active
        {
            get
            {
                T instance = Instance;
                return instance != null && instance.isActiveAndEnabled ? instance : null;
            }
        }
    }
}
