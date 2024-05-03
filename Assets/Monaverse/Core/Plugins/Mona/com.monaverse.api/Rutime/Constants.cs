namespace Monaverse.Api
{
    internal static class Constants
    {
        public const string BaseUrlLocal = "http://localhost:3007";
        public const string BaseUrlDevelopment = "https://api-dev.helios.monaver.se";
        public const string BaseUrlStaging = "https://api-staging.helios.monaver.se";
        public const string BaseUrlProduction = "https://api.helios.monaver.se";
        
        public static class Endpoints
        {
            public const string PostNonce = "v1/wallet-sdk/auth/nonce";
            public const string PostAuthorize = "v1/wallet-sdk/auth/authorize";
            public const string GetWalletCollectibles = "v1/wallet-sdk/wallet/collectibles";
            public static string GetWalletCollectibleById(string id) => $"v1/wallet-sdk/wallet/collectibles/{id}";
        }
    }
}