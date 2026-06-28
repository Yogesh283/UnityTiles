namespace Mkey.Network
{
    public sealed class ApiResult<T>
    {
        public bool Success { get; private set; }
        public T Data { get; private set; }
        public string ErrorMessage { get; private set; }
        public int StatusCode { get; private set; }
        public bool IsServerUnavailable { get; private set; }

        public static ApiResult<T> Ok(T data) =>
            new ApiResult<T> { Success = true, Data = data };

        public static ApiResult<T> Fail(string message, int statusCode = 0, bool serverUnavailable = false) =>
            new ApiResult<T>
            {
                Success = false,
                ErrorMessage = message,
                StatusCode = statusCode,
                IsServerUnavailable = serverUnavailable
            };
    }
}
