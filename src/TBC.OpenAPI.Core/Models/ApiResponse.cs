namespace TBC.OpenAPI.Core.Models
{
    public class ApiResponseBase
    {
        public bool IsSuccess { get; set; }
        public ProblemDetails? Problem { get; set; }
        public Dictionary<string, IEnumerable<string>>? Headers { get; set; }
        public Exception? Exception { get; set; }
    }

    public class ApiResponse<TData> : ApiResponseBase
    {
        public TData? Data { get; set; }
    }
}
