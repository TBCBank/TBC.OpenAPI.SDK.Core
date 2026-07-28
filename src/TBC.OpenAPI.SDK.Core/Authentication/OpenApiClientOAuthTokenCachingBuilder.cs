using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;

namespace TBC.OpenAPI.SDK.Core.Authentication
{
    /// <summary>
    /// Selects which cache stores OAuth access tokens for <typeparamref name="TClient"/> when clients
    /// are composed through <see cref="OpenApiClientFactoryBuilder"/>.
    /// <para>
    /// Mirrors <see cref="OAuthTokenCachingBuilder{TClient}"/>, but each terminal method returns the
    /// <see cref="OpenApiClientFactoryBuilder"/> so that further clients can be added and
    /// <see cref="OpenApiClientFactoryBuilder.Build"/> can still be chained.
    /// </para>
    /// </summary>
    /// <typeparam name="TClient">The Open API client the token cache applies to.</typeparam>
    public sealed class OpenApiClientOAuthTokenCachingBuilder<TClient>
        where TClient : class, IOpenApiClient
    {
        private readonly IServiceCollection _services;
        private readonly OpenApiClientFactoryBuilder _factoryBuilder;

        internal OpenApiClientOAuthTokenCachingBuilder(IServiceCollection services, OpenApiClientFactoryBuilder factoryBuilder)
        {
            _services = services;
            _factoryBuilder = factoryBuilder;
        }

        /// <summary>
        /// Caches tokens in a private in-memory store owned by this SDK and dedicated to
        /// <typeparamref name="TClient"/>. Nothing is registered under <see cref="IDistributedCache"/>.
        /// <para>
        /// The cache lives inside a single process and is <b>not</b> shared. In a multi-instance
        /// deployment every instance requests and caches its own tokens.
        /// </para>
        /// </summary>
        /// <returns>The client factory builder, so configuration can continue to be chained.</returns>
        public OpenApiClientFactoryBuilder UseInMemoryCache()
        {
            OAuthTokenCachingRegistration.UseInMemoryCache<TClient>(_services);
            return _factoryBuilder;
        }

        /// <summary>
        /// Caches tokens in the <see cref="IDistributedCache"/> registered in the underlying service
        /// collection.
        /// <para>
        /// Throws when no <see cref="IDistributedCache"/> is registered, and also when the registered
        /// one is a <see cref="MemoryDistributedCache"/>, which is not shared across processes. Call
        /// <see cref="UseInMemoryCache"/> to accept a per-process cache explicitly.
        /// </para>
        /// </summary>
        /// <returns>The client factory builder, so configuration can continue to be chained.</returns>
        public OpenApiClientFactoryBuilder UseRegisteredDistributedCache()
        {
            OAuthTokenCachingRegistration.UseRegisteredDistributedCache<TClient>(_services);
            return _factoryBuilder;
        }

        /// <summary>
        /// Caches tokens in the supplied <see cref="IDistributedCache"/> instance.
        /// </summary>
        /// <param name="cache">The cache to store access tokens in.</param>
        /// <returns>The client factory builder, so configuration can continue to be chained.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="cache"/> is <see langword="null"/>.</exception>
        public OpenApiClientFactoryBuilder UseDistributedCache(IDistributedCache cache)
        {
#if NET
            ArgumentNullException.ThrowIfNull(cache);
#else
            if (cache is null)
            {
                throw new ArgumentNullException(nameof(cache));
            }
#endif

            OAuthTokenCachingRegistration.UseDistributedCache<TClient>(_services, cache);
            return _factoryBuilder;
        }

        /// <summary>
        /// Caches tokens in an <see cref="IDistributedCache"/> produced by <paramref name="cacheFactory"/>
        /// when the client is first used.
        /// </summary>
        /// <param name="cacheFactory">Factory that produces the cache to store access tokens in.</param>
        /// <returns>The client factory builder, so configuration can continue to be chained.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="cacheFactory"/> is <see langword="null"/>.</exception>
        public OpenApiClientFactoryBuilder UseDistributedCache(Func<IServiceProvider, IDistributedCache> cacheFactory)
        {
#if NET
            ArgumentNullException.ThrowIfNull(cacheFactory);
#else
            if (cacheFactory is null)
            {
                throw new ArgumentNullException(nameof(cacheFactory));
            }
#endif

            OAuthTokenCachingRegistration.UseDistributedCache<TClient>(_services, cacheFactory);
            return _factoryBuilder;
        }
    }
}
