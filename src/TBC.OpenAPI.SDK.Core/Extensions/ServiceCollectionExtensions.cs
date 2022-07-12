using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Text;

namespace TBC.OpenAPI.SDK.Core.Extensions
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
               
                if (typeof(BasicAuthOptions).IsAssignableFrom(typeof(TOptions)))
                {
                    
                    var opt = options as BasicAuthOptions;

                    string encoded = System.Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1")
                               .GetBytes(opt.ApiKey + ":" + opt.ClientSecret));
                    client.DefaultRequestHeaders.Add("Authorization", "Basic " + encoded);
                }
                else if (typeof(OptionsBase).IsAssignableFrom(typeof(TOptions)))
                {
                    client.DefaultRequestHeaders.Add("apikey", options.ApiKey);
                }
                client.BaseAddress = new Uri(options.BaseUrl);
                configureClient?.Invoke(client);
            });

            if (configureHttpMessageHandler != null)
            {
                httpClientBuilder.ConfigurePrimaryHttpMessageHandler(configureHttpMessageHandler);
            }

            services.TryAddSingleton(typeof(IHttpHelper<>), typeof(HttpHelper<>));
            services.TryAddSingleton<TClientInterface, TClientImplementation>();
            services.TryAddSingleton(options);

            return services;
        }
    }
}