using Wish;
using SunHavenAccess.Speech;

namespace SunHavenAccess.Info
{
    /// <summary>
    /// Annonce l'objet/outil pris en main à chaque changement de case de la barre d'action
    /// (touches 1 à 0, molette...). S'abonne à `Wish.Player.onSetUseItem`
    /// (`UnityAction&lt;ushort&gt;`, un champ d'INSTANCE, pas statique — d'où l'abonnement fait
    /// une fois que Player.Instance existe, comme pour les autres évènements du jeu). Limite
    /// connue : `Player.SetUseItem` sort AVANT d'invoquer cet évènement quand on passe sur un
    /// emplacement VIDE (`if (item == 0) return;`, vu en décompilation) — l'évènement ne se
    /// déclenche donc que pour un objet réel, pas pour "main vide", ce qui correspond à la
    /// demande ("dire ce que je viens de prendre en main").
    /// </summary>
    public static class HandItemAnnouncer
    {
        private static bool _subscribed;

        public static void Tick()
        {
            if (_subscribed) return;
            Player player = Player.Instance;
            if (player == null) return;
            player.onSetUseItem += OnSetUseItem;
            _subscribed = true;
        }

        private static void OnSetUseItem(ushort itemId)
        {
            Player player = Player.Instance;
            if (player == null) return;

            ItemData data = player.ItemData;
            if (data == null) return;

            string name = data.UnformattedDisplayName;
            int amount = 0;
            int index = player.ItemIndex;
            var items = player.Inventory?.Items;
            if (items != null && index >= 0 && index < items.Count)
            {
                amount = items[index].amount;
            }

            string text = amount > 1 ? $"{amount}, {name}" : name;
            TolkSpeech.Speak(text, interrupt: true);
        }
    }
}
