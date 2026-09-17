
namespace Invoice.Model;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public ApiError? Error { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public int TotalRecords { get; set; }
}
