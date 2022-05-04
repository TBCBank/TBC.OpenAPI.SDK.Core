using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TBC.OpenAPI.Core.Models;

namespace TBC.OpenAPI.Core.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddOpenApiClient<TClientInterface, TClientImplementation, TOptions>(this IServiceCollection services,
            TOptions options,
            Action<HttpClient>? configureClient = null,
            Func<HttpClientHandler>? configureHttpMessageHandler = null)
        where TClientInterface : class, IOpenApiClient
        where TClientImplementation : class, TClientInterface
        where TOptions : OptionsBase
        {
            var httpClientBuilder = services.AddHttpClient(typeof(TClientImplementation).FullName, client =>
            {
                client.BaseAddress = new Uri(options.BaseUrl);
                if (!string.IsNullOrWhiteSpace(options.ApiKey))
                {
                    client.DefaultRequestHeaders.Add("apikey", options.ApiKey);
                }
                configureClient?.Invoke(client);
            });

            if (configureHttpMessageHandler != null)
            {
                httpClientBuilder.ConfigurePrimaryHttpMessageHandler(configureHttpMessageHandler);
            }

            services.TryAddSingleton(typeof(HttpHelper<>));
            services.TryAddSingleton<TClientInterface, TClientImplementation>();
            services.TryAddSingleton(options);

            return services;
        }
    }
}
