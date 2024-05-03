using Monaverse.Api.Logging;
using Monaverse.Api.Modules.Auth;
using Monaverse.Api.Modules.Collectibles;
using Monaverse.Api.MonaHttpClient;
using Monaverse.Api.Options;

namespace Monaverse.Api
{
    internal sealed class MonaApiClientImpl : IMonaApiClient
    {
        private readonly IMonaHttpClient _monaHttpClient;
        public IAuthApiModule Auth { get; private set; }
        public ICollectiblesApiModule Collectibles { get; private set; }
        
        public bool IsAuthenticated => !string.IsNullOrEmpty(_monaHttpClient.AccessToken);

        public MonaApiClientImpl(IMonaApiOptions monaApiOptions)
        {
            //Configure API client
            var monaApiLogger = new UnityMonaApiLogger(monaApiOptions.LogLevel);
            _monaHttpClient = new DefaultHttpClient(monaApiLogger);
            
            //Configure API modules
            Auth = new AuthApiModule(monaApiOptions, _monaHttpClient);
            Collectibles = new CollectiblesApiModule(monaApiOptions, _monaHttpClient);
        }

        public void SetAccessToken(string accessToken)
            => _monaHttpClient.AccessToken = accessToken;
    }
}