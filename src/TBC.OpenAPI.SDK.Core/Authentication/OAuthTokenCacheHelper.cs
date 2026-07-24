using Microsoft.Extensions.Caching.Distributed;
using TBC.OpenAPI.SDK.Core.Exceptions;

namespace TBC.OpenAPI.SDK.Core.Authentication
{
    public sealed class OAuthTokenCacheHelper<TClient> : IOAuthTokenCacheHelper<TClient>
        where TClient : class, IOpenApiClient
    {
        private readonly SingleFlightExecutor<OAuthTokenResponse> _singleFlight
            = new SingleFlightExecutor<OAuthTokenResponse>();

        private readonly IOAuthTokenHelper<TClient> _tokenHelper;
        private readonly IDistributedCache _distributedCache;

        public OAuthTokenCacheHelper(
            IOAuthTokenHelper<TClient> tokenHelper,
            IDistributedCache distributedCache)
        {
            _tokenHelper = tokenHelper;
            _distributedCache = distributedCache;
        }

        public async Task<OAuthTokenResponse> GetTokenAsync(string scope, CancellationToken cancellationToken)
        {
            string cacheKey = GetCacheKey(scope);

            var cached = await _distributedCache.GetStringAsync(cacheKey, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(cached))
            {
                return new OAuthTokenResponse { AccessToken = cached };
            }

            return await _singleFlight
                .ExecuteAsync(cacheKey, () => FetchAndCacheTokenAsync(scope, cacheKey), cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task<OAuthTokenResponse> FetchAndCacheTokenAsync(string scope, string cacheKey)
        {
            var token = await _distributedCache.GetStringAsync(cacheKey, CancellationToken.None).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(token))
            {
                return new OAuthTokenResponse { AccessToken = token };
            }

            var newTokenResponse = await _tokenHelper.RequestToken(scope, CancellationToken.None).ConfigureAwait(false);
            if (newTokenResponse is null)
            {
                throw new OpenApiException("Failed to obtain a new OAuth token.");
            }

            var accessToken = newTokenResponse.AccessToken;
            if (string.IsNullOrEmpty(accessToken))
            {
                throw new OpenApiException("Received empty access token.");
            }

            int ttlSec = newTokenResponse.ExpiresIn.HasValue
                ? newTokenResponse.ExpiresIn.Value - OAuthConstants.TokenTimeoutGracePeriodSec
                : 0;
            if (ttlSec <= 0)
            {
                ttlSec = OAuthConstants.MinCacheTtlSec;
            }

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(ttlSec)
            };
            await _distributedCache.SetStringAsync(cacheKey, accessToken!, options, CancellationToken.None).ConfigureAwait(false);

            return newTokenResponse;
        }

        public Task RemoveTokenAsync(string scope, CancellationToken cancellationToken)
        {
            return _distributedCache.RemoveAsync(GetCacheKey(scope), cancellationToken);
        }

        private static string GetCacheKey(string scope)
        {
            return $"{OAuthConstants.CacheKeyPrefix}:{typeof(TClient).FullName}:{scope}";
        }
    }
}
