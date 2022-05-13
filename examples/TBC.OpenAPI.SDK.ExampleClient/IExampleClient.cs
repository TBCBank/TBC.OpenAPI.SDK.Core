using TBC.OpenAPI.SDK.Core;
using TBC.OpenAPI.SDK.ExampleClient.Models;

namespace TBC.OpenAPI.SDK.ExampleClient
{
    public interface IExampleClient : IOpenApiClient
    {
        Task<SomeObject> GetSomeObjectAsync(CancellationToken cancellationToken = default);
    }
}
