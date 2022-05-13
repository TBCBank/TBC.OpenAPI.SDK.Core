namespace TBC.OpenAPI.SDK.Core.Exceptions
{
    [Serializable]
    public class OpenApiException : Exception
    {
        public OpenApiException()
        {
            
        }

        public OpenApiException(string message) : base(message)
        {
        }

        public OpenApiException(string message, Exception? innerException) : base(message, innerException)
        {
        }

        protected OpenApiException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) : base(serializationInfo, streamingContext)
        {
            
        }
    }
}