namespace TBC.OpenAPI.Core
{
    public abstract class OptionsBase
    {
#pragma warning disable CA1056 // URI-like properties should not be strings
        public string BaseUrl { get; set; } = null!;
#pragma warning restore CA1056 // URI-like properties should not be strings
        public string ApiKey { get; set; } = null!;
    }
}
