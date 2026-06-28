using System;
using System.Collections;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
#if ((UNITY_EDITOR || UNITY_IOS || UNITY_ANDROID) && ADDIAP)
using UnityEngine.Purchasing;
#endif

namespace Mkey.Network
{
    /// <summary>
    /// Verifies Google Play purchases with the backend before confirming them in Unity IAP.
    /// </summary>
    public class PlayStorePurchaseBridge : MonoBehaviour
    {
        private static PlayStorePurchaseBridge _instance;

        public static bool ShouldVerifyOnServer()
        {
            return Application.platform == RuntimePlatform.Android
                && !ApiConfig.Current.UseLocalSimulation
                && NetworkManager.HasInstance
                && NetworkManager.Instance.IsAuthenticated;
        }

#if ((UNITY_EDITOR || UNITY_IOS || UNITY_ANDROID) && ADDIAP)
        public static void HandlePurchase(Purchaser purchaser, PurchaseEventArgs args)
        {
            EnsureExists();
            _instance.StartCoroutine(_instance.VerifyRoutine(purchaser, args));
        }

        private IEnumerator VerifyRoutine(Purchaser purchaser, PurchaseEventArgs args)
        {
            Product product = args.purchasedProduct;
            ShopThingData shopItem = purchaser.GetShopItem(product.definition.id);

            if (!TryParseGooglePurchase(product, out string productId, out string purchaseToken))
            {
                Debug.LogWarning("[PlayStorePurchaseBridge] Failed to parse Google Play receipt.");
                purchaser.FailPendingPurchase(product, shopItem);
                yield break;
            }

            var verifyTask = PaymentService.VerifyGooglePlayAsync(productId, purchaseToken);
            while (!verifyTask.IsCompleted)
                yield return null;

            var result = verifyTask.Result;
            if (!result.Success || result.Data == null)
            {
                Debug.LogWarning("[PlayStorePurchaseBridge] Server verification failed: " + result.ErrorMessage);
                purchaser.FailPendingPurchase(product, shopItem);
                yield break;
            }

            purchaser.CompletePendingPurchase(product, shopItem);

            var syncTask = WalletService.SyncToCoinsHolderAsync();
            while (!syncTask.IsCompleted)
                yield return null;

            Debug.Log("[PlayStorePurchaseBridge] Purchase verified. Coins added: " + result.Data.coinsAdded
                + ", balance: " + result.Data.balance);
        }

        private static bool TryParseGooglePurchase(Product product, out string productId, out string purchaseToken)
        {
            productId = product.definition.id;
            purchaseToken = null;

            if (string.IsNullOrWhiteSpace(product.receipt))
                return false;

            try
            {
                var receipt = JsonConvert.DeserializeObject<UnityStoreReceipt>(product.receipt);
                if (receipt == null || !string.Equals(receipt.Store, "GooglePlay", StringComparison.OrdinalIgnoreCase))
                    return false;

                var payload = JsonConvert.DeserializeObject<GooglePlayPayload>(receipt.Payload);
                if (payload == null || string.IsNullOrWhiteSpace(payload.json))
                    return false;

                var purchase = JObject.Parse(payload.json);
                purchaseToken = purchase.Value<string>("purchaseToken");
                string storeProductId = purchase.Value<string>("productId");
                if (!string.IsNullOrWhiteSpace(storeProductId))
                    productId = NormalizeProductId(storeProductId, productId);

                return !string.IsNullOrWhiteSpace(purchaseToken);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[PlayStorePurchaseBridge] Receipt parse error: " + ex.Message);
                return false;
            }
        }

        private static string NormalizeProductId(string storeProductId, string fallbackId)
        {
            int lastDot = storeProductId.LastIndexOf('.');
            if (lastDot >= 0 && lastDot < storeProductId.Length - 1)
                return storeProductId.Substring(lastDot + 1);
            return fallbackId;
        }
#endif

        private static void EnsureExists()
        {
            if (_instance) return;
            var go = new GameObject(nameof(PlayStorePurchaseBridge));
            _instance = go.AddComponent<PlayStorePurchaseBridge>();
            DontDestroyOnLoad(go);
        }

        [Serializable]
        private class UnityStoreReceipt
        {
            public string Store;
            public string TransactionID;
            public string Payload;
        }

        [Serializable]
        private class GooglePlayPayload
        {
            public string json;
            public string signature;
        }
    }
}
