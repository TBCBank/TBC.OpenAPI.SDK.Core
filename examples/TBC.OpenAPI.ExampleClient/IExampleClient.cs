using TBC.OpenAPI.Core;
using TBC.OpenAPI.ExampleClient.Models;

namespace TBC.OpenAPI.ExampleClient
{
    public interface IExampleClient : IOpenApiClient
    {
        Task<SomeObject> GetSomeObjectAsync(CancellationToken cancellationToken = default);
    }
}
