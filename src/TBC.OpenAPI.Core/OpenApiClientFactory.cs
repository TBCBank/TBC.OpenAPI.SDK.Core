using Microsoft.Extensions.DependencyInjection;
using TBC.OpenAPI.Core.Exceptions;
using TBC.OpenAPI.Core.Extensions;
using TBC.OpenAPI.Core.Models;

namespace TBC.OpenAPI.Core
{
    public class OpenApiClientFactory
    {
        private readonly IServiceProvider _serviceProvider;

        private static OpenApiClientFactory? _instance;
        public static OpenApiClientFactory Instance
        {
            get => _instance ?? throw new InvalidOperationException("OpenApiClientFactory is not built. Please use OpenApiClientFactoryBuilder to build OpenApiClientFactory.");
            internal set => _instance = value;
        }

        internal OpenApiClientFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public TClient GetOpenApiClient<TClient>()
            where TClient : class, IOpenApiClient
        {
            return _serviceProvider.GetRequiredService<TClient>();
        }
    }

    public class OpenApiClientFactoryBuilder
    {
        private readonly IServiceCollection _serviceCollection;
        private IServiceProvider? _serviceProvider;
        private OpenApiClientFactory? _clientFactory;

        public OpenApiClientFactoryBuilder()
        {
            _serviceCollection = new ServiceCollection();
        }

        public OpenApiClientFactoryBuilder AddClient<TClientInterface, TClientImplementation, TOptions>(TOptions options,
            Action<HttpClient>? configureClient = null,
            Func<HttpClientHandler>? configureHttpMessageHandler = null)
            where TClientInterface : class, IOpenApiClient
            where TClientImplementation : class, TClientInterface
            where TOptions : OptionsBase
        {
            _serviceCollection.AddOpenApiClient<TClientInterface, TClientImplementation, TOptions>(options, configureClient, configureHttpMessageHandler);
            return this;
        }

        public OpenApiClientFactory Build()
        {
            _serviceProvider ??= _serviceCollection.BuildServiceProvider();
            _clientFactory ??= new OpenApiClientFactory(_serviceProvider);
            OpenApiClientFactory.Instance = _clientFactory;

            return _clientFactory;
        }
    }
}
