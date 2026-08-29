using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using Wish;
using SunHavenAccess.Info;
using SunHavenAccess.Patches;
using SunHavenAccess.Speech;
using SunHavenAccess.Util;

namespace SunHavenAccess.Menus
{
    /// <summary>
    /// La boutique : parcourir, connaître le prix, et savoir ce que ça coûtera VRAIMENT.
    ///
    /// CE QUI MARCHAIT DÉJÀ, ET CE QUI MANQUAIT. L'écran de boutique se parcourait correctement —
    /// c'est ce qui a été dit en jeu. Ce qui manquait, c'est le calcul : le prix affiché est celui
    /// de l'unité, et les boutons du jeu achètent par un, cinq ou vingt. Combien coûtent vingt
    /// graines ? Peut-on se les payer ? À l'œil, on lit le prix, on regarde sa bourse, on tranche
    /// en une seconde. Sans la vue, il faudrait multiplier de tête à chaque article.
    ///
    /// CE QU'ON FAIT. Chaque article s'annonce avec son prix unitaire et son stock. Le valider
    /// ouvre les quantités — et CHAQUE quantité annonce son prix TOTAL avant qu'on la choisisse,
    /// pas après. Acheter n'est jamais une découverte.
    ///
    /// CE QU'ON DIT SANS QU'ON DEMANDE. Ce qu'on possède, et donc combien on peut s'offrir. C'est
    /// la question qu'on se pose devant chaque article, et la seule dont la réponse ne soit écrite
    /// nulle part à portée d'oreille.
    ///
    /// AUCUN SONDAGE : `ShopUI.OpenUI` et `CloseUI` préviennent aux deux instants qui comptent.
    /// </summary>
    internal static class ShopMenu
    {
        private const string OwnerTag = "boutique";

        /// <summary>Instant limite d'attente des articles ; zéro quand on n'attend rien.</summary>
        private static float _waitUntil;

        [HarmonyPatch(typeof(ShopUI), nameof(ShopUI.OpenUI))]
        public static class OpenPatch
        {
            private static void Postfix() =>
                PatchGuard.Run("BoutiqueOuverte", () => _waitUntil = Time.unscaledTime + 2f);
        }

        [HarmonyPatch(typeof(ShopUI), nameof(ShopUI.CloseUI))]
        public static class ClosePatch
        {
            private static void Postfix() =>
                PatchGuard.Run("BoutiqueFermee", () =>
                {
                    _waitUntil = 0f;
                    ListMenu.CloseIfOwner(OwnerTag, false);
                });
        }

        internal static void Tick()
        {
            if (_waitUntil == 0f) return; // rien en attente : coût nul

            if (Time.unscaledTime > _waitUntil)
            {
                _waitUntil = 0f;
                return;
            }

            List<BuyableItem> items = Items();
            if (items.Count == 0) return; // articles pas encore construits, on repasse

            _waitUntil = 0f;
            Open(items);
        }

        private static List<BuyableItem> Items()
        {
            try
            {
                return Object.FindObjectsOfType<BuyableItem>()
                    .Where(b => b != null && b.gameObject.activeInHierarchy)
                    .ToList();
            }
            catch { return new List<BuyableItem>(); }
        }

        private static void Open(List<BuyableItem> items)
        {
            var entries = items.Select(Describe).ToList();

            TolkSpeech.Speak(Localization.Language.T(
                $"Boutique, {items.Count} article{(items.Count > 1 ? "s" : "")}. {Purse()}",
                $"Shop, {items.Count} item{(items.Count > 1 ? "s" : "")}. {Purse()}"), true);

            ListMenu.Open(Localization.Language.T("Boutique", "Shop"), entries,
                chosen =>
                {
                    if (chosen >= 0 && chosen < items.Count) OpenItem(items, items[chosen]);
                },
                owner: OwnerTag,
                announce: false);
        }

        /// <summary>
        /// Un article en une phrase : son nom, son prix à l'unité, et son stock quand il est limité.
        /// Le stock ne se dit que s'il compte — « en stock illimité » à chaque ligne serait du bruit.
        /// </summary>
        private static string Describe(BuyableItem item)
        {
            string name = Name(item);
            string price = UnitPrice(item);

            string stock = item.Qty > 0
                ? Localization.Language.T($", {item.Qty} en stock", $", {item.Qty} in stock")
                : string.Empty;

            return string.IsNullOrEmpty(price) ? name + stock : $"{name}, {price}{stock}";
        }

        private static string Name(BuyableItem item)
        {
            try
            {
                int id = item.itemImage?.Item?.ID() ?? 0;
                string named = id != 0 ? ItemNames.Get(id) : null;
                if (!string.IsNullOrWhiteSpace(named)) return named;
            }
            catch { }

            string text = TextUtil.Clean(item.itemNameTMP?.text);
            return string.IsNullOrWhiteSpace(text) ? Localization.Language.T("Article", "Item") : text;
        }

        /// <summary>
        /// Le prix unitaire, dans la monnaie réellement demandée. Une boutique peut vendre contre
        /// des pièces, des billets ou des orbes, et annoncer « pièces » partout ferait croire qu'on
        /// peut payer avec ce qu'on n'a pas.
        /// </summary>
        private static string UnitPrice(BuyableItem item)
        {
            if (item.coinsPrice > 0)
                return Localization.Language.T($"{item.coinsPrice} pièces", $"{item.coinsPrice} coins");
            if (item.ticketsPrice > 0)
                return Localization.Language.T($"{item.ticketsPrice} billets", $"{item.ticketsPrice} tickets");
            if (item.orbsPrice > 0)
                return Localization.Language.T($"{item.orbsPrice} orbes", $"{item.orbsPrice} orbs");

            string text = TextUtil.Clean(item.priceTMP?.text);
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }

        private static int UnitCost(BuyableItem item) =>
            item.coinsPrice > 0 ? item.coinsPrice
            : item.ticketsPrice > 0 ? item.ticketsPrice
            : item.orbsPrice;

        private static string Currency(BuyableItem item) =>
            item.coinsPrice > 0 ? Localization.Language.T("pièces", "coins")
            : item.ticketsPrice > 0 ? Localization.Language.T("billets", "tickets")
            : Localization.Language.T("orbes", "orbs");

        private static string Purse()
        {
            try
            {
                int coins = GameSave.Coins;
                return Localization.Language.T($"Vous avez {coins} pièces.", $"You have {coins} coins.");
            }
            catch { return string.Empty; }
        }

        /// <summary>
        /// Les quantités, chacune avec SON PRIX TOTAL dans son intitulé.
        ///
        /// C'est tout l'objet de cet écran : « acheter 20 » ne dit rien, « acheter 20, 240 pièces »
        /// dit tout. Le calcul se fait avant le choix, jamais après — on ne découvre pas le prix
        /// d'un achat une fois qu'il est fait.
        /// </summary>
        private static void OpenItem(List<BuyableItem> all, BuyableItem item)
        {
            string name = Name(item);
            int unit = UnitCost(item);
            string currency = Currency(item);

            string Line(string label, int amount) =>
                unit > 0
                    ? Localization.Language.T($"{label}, {unit * amount} {currency}", $"{label}, {unit * amount} {currency}")
                    : label;

            var actions = new List<string>
            {
                Line(Localization.Language.T("Acheter 1", "Buy 1"), 1),
                Line(Localization.Language.T("Acheter 5", "Buy 5"), 5),
                Line(Localization.Language.T("Acheter 20", "Buy 20"), 20),
                Localization.Language.T("Acheter une autre quantité", "Buy another amount"),
                Localization.Language.T("Ce que je peux m'offrir", "What I can afford"),
            };

            ListMenu.Open(name, actions,
                chosen =>
                {
                    switch (chosen)
                    {
                        case 0: Buy(item, 1, name); break;
                        case 1: Buy(item, 5, name); break;
                        case 2: Buy(item, 20, name); break;
                        case 3: AskAmount(item, name); break;
                        default:
                            TolkSpeech.Speak(Affordable(item, name), true);
                            OpenItem(all, item);
                            break;
                    }
                },
                onExitUp: () => Open(all),
                owner: OwnerTag);
        }

        private static string Affordable(BuyableItem item, string name)
        {
            int unit = UnitCost(item);
            if (unit <= 0) return Localization.Language.T("Prix inconnu.", "Price unknown.");

            try
            {
                // On ne raisonne que sur les pièces : c'est la seule bourse que le mod sait lire de
                // façon sûre, et dire un chiffre faux serait pire que se taire.
                if (item.coinsPrice <= 0)
                    return Localization.Language.T(
                        $"{name} se paie en {Currency(item)}, {unit} l'unité.",
                        $"{name} is paid in {Currency(item)}, {unit} each.");

                int coins = GameSave.Coins;
                int count = coins / unit;

                return Localization.Language.T(
                    $"{unit} pièces l'unité. Vous avez {coins} pièces, de quoi en prendre {count}.",
                    $"{unit} coins each. You have {coins} coins, enough for {count}.");
            }
            catch { return Localization.Language.T("Prix inconnu.", "Price unknown."); }
        }

        private static void AskAmount(BuyableItem item, string name)
        {
            TextPrompt.Ask(
                Localization.Language.T($"Combien de {name} ?", $"How many {name}?"),
                null,
                typed =>
                {
                    if (!int.TryParse(typed.Trim(), out int amount) || amount <= 0)
                    {
                        TolkSpeech.Speak(Localization.Language.T(
                            "Ce n'est pas un nombre.", "That is not a number."), true);
                        return;
                    }

                    if (amount > 200)
                    {
                        TolkSpeech.Speak(Localization.Language.T(
                            "Deux cents au maximum.", "Two hundred at most."), true);
                        return;
                    }

                    Buy(item, amount, name);
                });
        }

        /// <summary>
        /// Achète en cliquant, autant de fois que demandé. On passe par le bouton du jeu plutôt que
        /// par sa logique interne : c'est exactement ce que ferait quelqu'un qui voit, donc toutes
        /// ses vérifications — l'argent, le stock, la place dans le sac — s'appliquent sans qu'on
        /// ait à les refaire ni à risquer de les contredire.
        /// </summary>
        private static void Buy(BuyableItem item, int amount, string name)
        {
            int unit = UnitCost(item);
            int done = 0;

            try
            {
                for (int i = 0; i < amount; i++)
                {
                    if (item == null || item.buyButton == null || !item.buyButton.interactable) break;
                    item.buyButton.onClick.Invoke();
                    done++;
                }
            }
            catch { }

            if (done == 0)
            {
                TolkSpeech.Speak(Localization.Language.T(
                    $"Impossible d'acheter {name} : argent, stock ou place insuffisants.",
                    $"Cannot buy {name}: not enough money, stock or space."), true);
                return;
            }

            string total = unit > 0
                ? Localization.Language.T($" pour {unit * done} {Currency(item)}", $" for {unit * done} {Currency(item)}")
                : string.Empty;

            // On dit ce qui a RÉELLEMENT été acheté, pas ce qui était demandé : la boutique s'arrête
            // dès qu'une condition manque, et annoncer vingt quand cinq sont passés serait un
            // mensonge qu'on ne découvrirait qu'en comptant son sac.
            TolkSpeech.Speak(done == amount
                ? Localization.Language.T($"{done} {name} acheté{(done > 1 ? "s" : "")}{total}. {Purse()}",
                                          $"{done} {name} bought{total}. {Purse()}")
                : Localization.Language.T($"{done} {name} sur {amount}{total} : la boutique s'est arrêtée là. {Purse()}",
                                          $"{done} {name} out of {amount}{total}: the shop stopped there. {Purse()}"), true);
        }
    }
}
