using System.Text.Json.Serialization;

namespace TBC.OpenAPI.Core.Models
{
    /// <summary>
    /// A machine-readable format for specifying errors in HTTP API responses based on https://tools.ietf.org/html/rfc7807.
    /// </summary>
    [JsonConverter(typeof(ProblemDetailsJsonConverter))]
    public class ProblemDetails
    {
        public const string MessageDeserializationErrorCode = "MessageDeserializationError";
        public const string MessageDeserializationErrorTitle = "Unable to deserialize response message";
        public const string HttpRequestSendErrorCode = "HttpRequestSendError";
        public const string HttpRequestSendErrorTitle = "Error occurred while sending HTTP request message";

        /// <summary>
        /// A URI reference [RFC3986] that identifies the problem type
        /// </summary>
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        /// <summary>
        /// A short summary of the problem type
        /// </summary>
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        /// <summary>
        /// The HTTP status code
        /// </summary>
        [JsonPropertyName("status")]
        public int? Status { get; set; }

        /// <summary>
        /// A human-readable explanation specific to this occurrence of the problem
        /// </summary>
        [JsonPropertyName("detail")]
        public string? Detail { get; set; }

        /// <summary>
        /// A URI reference that identifies the specific occurrence of the problem
        /// </summary>
        [JsonPropertyName("instance")]
        public string? Instance { get; set; }

        /// <summary>
        /// Gets the <see cref="IDictionary{TKey, TValue}"/> for extension members.
        /// <para>
        /// Problem type definitions MAY extend the problem details object with additional members. Extension members appear in the same namespace as
        /// other members of a problem type.
        /// </para>
        /// </summary>
        /// <remarks>
        /// The round-tripping behavior for <see cref="Extensions"/> is determined by the implementation of the Input \ Output formatters.
        /// In particular, complex types or collection types may not round-trip to the original type when using the built-in JSON or XML formatters.
        /// </remarks>
        [JsonExtensionData]
        public IDictionary<string, object?> Extensions { get; } = new Dictionary<string, object?>(StringComparer.Ordinal);

        /// <summary>
        /// Error code defined in application
        /// </summary>
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        /// <summary>
        /// Request trace identifier
        /// </summary>
        [JsonPropertyName("traceId")]
        public string? TraceId { get; set; }

        /// <summary>
        /// Validation errors
        /// </summary>
        [JsonPropertyName("errors")]
        public IDictionary<string, string?[]?>? Errors { get; set; }
    }
}
