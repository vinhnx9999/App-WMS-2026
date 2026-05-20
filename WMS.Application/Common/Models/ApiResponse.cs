namespace WMS.Application.Common.Models;

// Standard API response wrapper
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }
    public string? TraceId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public static ApiResponse<T> Fail(string msg, List<string>? errors = null) => 
        new() { Success = false, Message = msg, Errors = errors };

    public static ApiResponse<T> Ok(T? data, string? msg = null) =>
        new() { Success = true, Data = data, Message = msg };
}

public class ApiResponse : ApiResponse<object> { }