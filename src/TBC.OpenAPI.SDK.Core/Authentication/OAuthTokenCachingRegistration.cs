using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace TBC.OpenAPI.SDK.Core.Authentication
{
    /// <summary>
    /// Shared registration logic behind the token caching builders. Every cache selection ends up
    /// here, so the service-collection and client-factory flavours of the fluent API stay in sync.
    /// </summary>
    internal static class OAuthTokenCachingRegistration
    {
        /// <summary>
        /// Registers the placeholder <see cref="IOAuthTokenCacheHelper{TClient}"/> used when no cache
        /// has been selected yet. Resolving it throws an actionable error, so a forgotten terminal
        /// call fails loudly instead of silently sending unauthenticated requests.
        /// </summary>
        public static void AddUnconfiguredCache<TClient>(IServiceCollection services)
            where TClient : class, IOpenApiClient
        {
            services.TryAdd(ServiceDescriptor.Singleton<IOAuthTokenCacheHelper<TClient>>(
                _ => throw new InvalidOperationException(NoCacheSelectedMessage<TClient>())));
        }

        /// <summary>
        /// Rejects an interface <typeparamref name="TClient"/>. Token caching attaches its handler to
        /// the named <see cref="HttpClient"/> called <c>typeof(TClient).FullName</c>, and
        /// <c>AddOpenApiClient</c> always configures that client under the implementation type - its
        /// <c>TClientImplementation</c> parameter is constrained to a class. An interface therefore
        /// attaches the handler to a client nothing resolves, and requests silently go out without an
        /// <c>Authorization</c> header.
        /// </summary>
        public static void ValidateClientTypeArgument<TClient>()
            where TClient : class, IOpenApiClient
        {
            if (typeof(TClient).IsInterface)
            {
                throw new InvalidOperationException(InterfaceTypeArgumentMessage<TClient>());
            }
        }

        /// <summary>
        /// Points <typeparamref name="TClient"/> at a private in-memory cache owned by this SDK.
        /// Nothing is registered in the container under <see cref="IDistributedCache"/>.
        /// </summary>
        public static IServiceCollection UseInMemoryCache<TClient>(IServiceCollection services)
            where TClient : class, IOpenApiClient
        {
            return UseCache<TClient>(
                services,
                _ => new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())));
        }

        /// <summary>
        /// Points <typeparamref name="TClient"/> at the <see cref="IDistributedCache"/> registered in
        /// the container, failing loudly when there is none or when it is not actually distributed.
        /// </summary>
        public static IServiceCollection UseRegisteredDistributedCache<TClient>(IServiceCollection services)
            where TClient : class, IOpenApiClient
        {
            return UseCache<TClient>(services, serviceProvider =>
            {
                var cache = serviceProvider.GetService<IDistributedCache>();

                if (cache is null)
                {
                    throw new InvalidOperationException(NoRegisteredCacheMessage<TClient>());
                }

                if (cache is MemoryDistributedCache)
                {
                    throw new InvalidOperationException(NotActuallyDistributedMessage<TClient>());
                }

                return cache;
            });
        }

        /// <summary>
        /// Points <typeparamref name="TClient"/> at the supplied <see cref="IDistributedCache"/> instance.
        /// </summary>
        public static IServiceCollection UseDistributedCache<TClient>(IServiceCollection services, IDistributedCache cache)
            where TClient : class, IOpenApiClient
        {
            return UseCache<TClient>(services, _ => cache);
        }

        /// <summary>
        /// Points <typeparamref name="TClient"/> at an <see cref="IDistributedCache"/> built by the
        /// supplied factory when the container is first asked for a token.
        /// </summary>
        public static IServiceCollection UseDistributedCache<TClient>(
            IServiceCollection services,
            Func<IServiceProvider, IDistributedCache> cacheFactory)
            where TClient : class, IOpenApiClient
        {
            return UseCache<TClient>(services, serviceProvider =>
                cacheFactory(serviceProvider)
                ?? throw new InvalidOperationException(NullFactoryResultMessage<TClient>()));
        }

        private static IServiceCollection UseCache<TClient>(
            IServiceCollection services,
            Func<IServiceProvider, IDistributedCache> cacheFactory)
            where TClient : class, IOpenApiClient
        {
            services.Replace(ServiceDescriptor.Singleton<IOAuthTokenCacheHelper<TClient>>(serviceProvider =>
                new OAuthTokenCacheHelper<TClient>(
                    serviceProvider.GetRequiredService<IOAuthTokenHelper<TClient>>(),
                    cacheFactory(serviceProvider))));

            return services;
        }

        private static string NoCacheSelectedMessage<TClient>()
            where TClient : class, IOpenApiClient
        {
            return $"No OAuth token cache has been selected for client '{ClientName<TClient>()}'. " +
                   $"Follow AddOAuthTokenCaching<{typeof(TClient).Name}>() with exactly one of: " +
                   "UseInMemoryCache() for a per-process cache, " +
                   "UseRegisteredDistributedCache() to use the IDistributedCache registered in the container, " +
                   "or UseDistributedCache(...) to supply one directly.";
        }

        private static string InterfaceTypeArgumentMessage<TClient>()
            where TClient : class, IOpenApiClient
        {
            return $"AddOAuthTokenCaching<{typeof(TClient).Name}>() was called with the interface " +
                   $"'{ClientName<TClient>()}'. Token caching attaches its handler to the named HttpClient of the " +
                   "type argument, and AddOpenApiClient always configures that client under the implementation " +
                   "type, so an interface attaches the handler to an HttpClient nothing resolves and requests go " +
                   "out without an Authorization header. Pass the client implementation type instead - the same " +
                   "type argument the client passes to IHttpHelper<T>.";
        }

        private static string NoRegisteredCacheMessage<TClient>()
            where TClient : class, IOpenApiClient
        {
            return $"UseRegisteredDistributedCache() was selected for client '{ClientName<TClient>()}', " +
                   "but no IDistributedCache is registered in the container. Register one " +
                   "(for example services.AddStackExchangeRedisCache(...) or services.AddDistributedSqlServerCache(...)), " +
                   "or select UseInMemoryCache() / UseDistributedCache(...) instead.";
        }

        private static string NotActuallyDistributedMessage<TClient>()
            where TClient : class, IOpenApiClient
        {
            return $"UseRegisteredDistributedCache() was selected for client '{ClientName<TClient>()}', " +
                   "but the registered IDistributedCache is a MemoryDistributedCache, which is not shared " +
                   "across processes or instances. Every instance of a multi-instance deployment would keep " +
                   "its own token cache and request its own tokens. Register a genuinely distributed cache " +
                   "(for example Redis or SQL Server), or select UseInMemoryCache() to explicitly accept a " +
                   "per-process cache.";
        }

        private static string NullFactoryResultMessage<TClient>()
            where TClient : class, IOpenApiClient
        {
            return $"The IDistributedCache factory supplied to UseDistributedCache(...) for client " +
                   $"'{ClientName<TClient>()}' returned null.";
        }

        private static string ClientName<TClient>()
            where TClient : class, IOpenApiClient
        {
            return typeof(TClient).FullName ?? typeof(TClient).Name;
        }
    }
}
