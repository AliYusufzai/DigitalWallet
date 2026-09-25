namespace DigitalWallet.Common.Wrappers;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }

    private ApiResponse() { }

    // success with data
    public static ApiResponse<T> Ok(T data, string message = "Success")
    {
        return new ApiResponse<T>()
        {
            Success = true,
            Data = data,
            Message = message,
        };
    }

    // success with no data — for deletes, updates with no return
    public static ApiResponse<T> Ok(string message = "Success")
    {
        return new ApiResponse<T>() { Success = true, Message = message };
    }

    // failure with single error message
    public static ApiResponse<T> Fail(string error)
    {
        return new ApiResponse<T>()
        {
            Success = false,
            Message = error,
            Errors = new List<string> { error },
        };
    }

    // failure with multiple error messages — for validation errors
    public static ApiResponse<T> Fail(List<string> errors)
    {
        return new ApiResponse<T>()
        {
            Success = false,
            Message = "One or more errors occurred",
            Errors = errors,
        };
    }
}
