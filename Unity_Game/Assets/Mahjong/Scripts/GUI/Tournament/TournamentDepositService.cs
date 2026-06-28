using Mkey;
using UnityEngine;

namespace Mkey.Tournament
{
    public readonly struct CoinPack
    {
        public readonly string Id;
        public readonly int Coins;
        public readonly int PriceInr;

        public CoinPack(string id, int coins, int priceInr)
        {
            Id = id;
            Coins = coins;
            PriceInr = priceInr;
        }
    }

    public static class TournamentDepositService
    {
        private static readonly CoinPack[] Packs =
        {
            new CoinPack("coins_100", 100, 10),
            new CoinPack("coins_500", 500, 50),
            new CoinPack("coins_1000", 1000, 100),
            new CoinPack("coins_2500", 2500, 250),
            new CoinPack("coins_5000", 5000, 500),
        };

        public static int PackCount => Packs.Length;

        public static CoinPack GetPack(int index)
        {
            if (Packs.Length == 0)
                return default;
            int i = ((index % Packs.Length) + Packs.Length) % Packs.Length;
            return Packs[i];
        }

        public static string GetPriceLabel(string productId, int fallbackInr)
        {
            if (Purchaser.Instance)
            {
                string storePrice = Purchaser.Instance.GetProductPriceFromStore(productId);
                if (!string.IsNullOrEmpty(storePrice))
                    return storePrice;
            }
            return $"₹{fallbackInr}";
        }

        public static void EnsurePurchaser()
        {
            if (Purchaser.Instance)
                return;

            GameObject go = new GameObject("TournamentPurchaser");
            Object.DontDestroyOnLoad(go);
            Purchaser purchaser = go.AddComponent<Purchaser>();
            purchaser.consumable = new[]
            {
                CreatePack("coins_100", "100 Coins"),
                CreatePack("coins_500", "500 Coins"),
                CreatePack("coins_1000", "1000 Coins"),
                CreatePack("coins_2500", "2500 Coins"),
                CreatePack("coins_5000", "5000 Coins"),
            };
        }

        private static ShopThingData CreatePack(string id, string name)
        {
            var pack = new ShopThingData(null);
            pack.kProductID = id;
            pack.name = name;
            return pack;
        }
    }
}
