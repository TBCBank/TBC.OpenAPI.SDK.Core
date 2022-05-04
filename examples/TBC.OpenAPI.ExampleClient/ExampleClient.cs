using TBC.OpenAPI.Core;
using TBC.OpenAPI.Core.Exceptions;
using TBC.OpenAPI.ExampleClient.Models;

namespace TBC.OpenAPI.ExampleClient
{
    public class ExampleClient : IExampleClient
    {
        private readonly HttpHelper<ExampleClient> _http;

        public ExampleClient(HttpHelper<ExampleClient> http)
        {
            _http = http;
        }

        public async Task<SomeObject> GetSomeObjectAsync(CancellationToken cancellationToken = default)
        {
            var result = await _http.GetJsonAsync<SomeObject>("/", cancellationToken).ConfigureAwait(false);

            if (!result.IsSuccess)
                throw new OpenApiException(result.Problem?.Title ?? "Unexpected error occurred", result.Exception);

            return result.Data!;
        }
    }
}