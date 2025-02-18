namespace StoryTimeComicBookApi.Models.Common;

public class ApiResponse<T>
{
    public T? Data { get; set; }
    public ApiError? Error { get; set; }

    // Static factory methods for convenience
    public static ApiResponse<T> Success(T data)
    {
        return new ApiResponse<T>
        {
            Data = data,
            Error = null
        };
    }

    public static ApiResponse<T> Failure(string message, string? code = null, object? details = null)
    {
        return new ApiResponse<T>
        {
            Data = default,
            Error = new ApiError
            {
                Message = message,
                Code = code,
                Details = details
            }
        };
    }
}

public class ApiError
{
    public string Message { get; set; } = string.Empty;
    public string? Code { get; set; }
    public object? Details { get; set; }
} 