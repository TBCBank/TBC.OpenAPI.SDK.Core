using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;

namespace TBC.OpenAPI.SDK.Core.Authentication
{
    /// <summary>
    /// Selects which cache stores OAuth access tokens for <typeparamref name="TClient"/>.
    /// <para>
    /// The choice is deliberately explicit: this SDK never registers a cache backend on your behalf
    /// and never falls back to one, so it cannot quietly take over your application's caching
    /// topology. Exactly one of the <c>Use…</c> methods must be called; until one is, resolving the
    /// client throws an error that names the available options.
    /// </para>
    /// </summary>
    /// <typeparam name="TClient">The Open API client the token cache applies to.</typeparam>
    public sealed class OAuthTokenCachingBuilder<TClient>
        where TClient : class, IOpenApiClient
    {
        internal OAuthTokenCachingBuilder(IServiceCollection services)
        {
            Services = services;
        }

        /// <summary>
        /// The service collection being configured.
        /// </summary>
        public IServiceCollection Services { get; }

        /// <summary>
        /// Caches tokens in a private in-memory store owned by this SDK and dedicated to
        /// <typeparamref name="TClient"/>. Nothing is registered in the container under
        /// <see cref="IDistributedCache"/>, so your application's caching topology is untouched.
        /// <para>
        /// The cache lives inside a single process and is <b>not</b> shared. In a multi-instance
        /// deployment every instance requests and caches its own tokens. Prefer
        /// <see cref="UseRegisteredDistributedCache"/> or <see cref="UseDistributedCache(IDistributedCache)"/>
        /// when running more than one instance.
        /// </para>
        /// </summary>
        /// <returns>The service collection, so registration can continue to be chained.</returns>
        public IServiceCollection UseInMemoryCache()
        {
            return OAuthTokenCachingRegistration.UseInMemoryCache<TClient>(Services);
        }

        /// <summary>
        /// Caches tokens in the <see cref="IDistributedCache"/> registered in the container. The
        /// cache is resolved when the client is first used, so it may be registered before or after
        /// this call.
        /// <para>
        /// Throws when no <see cref="IDistributedCache"/> is registered, and also when the registered
        /// one is a <see cref="MemoryDistributedCache"/> — that implementation is not shared across
        /// processes, so accepting it here would silently give a multi-instance deployment a
        /// per-instance token cache. Call <see cref="UseInMemoryCache"/> if that is what you want.
        /// </para>
        /// </summary>
        /// <returns>The service collection, so registration can continue to be chained.</returns>
        public IServiceCollection UseRegisteredDistributedCache()
        {
            return OAuthTokenCachingRegistration.UseRegisteredDistributedCache<TClient>(Services);
        }

        /// <summary>
        /// Caches tokens in the supplied <see cref="IDistributedCache"/> instance, bypassing the
        /// container entirely.
        /// </summary>
        /// <param name="cache">The cache to store access tokens in.</param>
        /// <returns>The service collection, so registration can continue to be chained.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="cache"/> is <see langword="null"/>.</exception>
        public IServiceCollection UseDistributedCache(IDistributedCache cache)
        {
#if NET
            ArgumentNullException.ThrowIfNull(cache);
#else
            if (cache is null)
            {
                throw new ArgumentNullException(nameof(cache));
            }
#endif

            return OAuthTokenCachingRegistration.UseDistributedCache<TClient>(Services, cache);
        }

        /// <summary>
        /// Caches tokens in an <see cref="IDistributedCache"/> produced by <paramref name="cacheFactory"/>.
        /// The factory runs once, when the client is first used, which allows the cache to be built
        /// from other services in the container.
        /// </summary>
        /// <param name="cacheFactory">Factory that produces the cache to store access tokens in.</param>
        /// <returns>The service collection, so registration can continue to be chained.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="cacheFactory"/> is <see langword="null"/>.</exception>
        public IServiceCollection UseDistributedCache(Func<IServiceProvider, IDistributedCache> cacheFactory)
        {
#if NET
            ArgumentNullException.ThrowIfNull(cacheFactory);
#else
            if (cacheFactory is null)
            {
                throw new ArgumentNullException(nameof(cacheFactory));
            }
#endif

            return OAuthTokenCachingRegistration.UseDistributedCache<TClient>(Services, cacheFactory);
        }
    }
}
