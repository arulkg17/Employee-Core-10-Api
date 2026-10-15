namespace Invoice.Model;

public class ApiTaskRequest
{
    public string ApiName { get; set; } = string.Empty;

    public int DelayInSeconds { get; set; }

    public string Status { get; set; } = "Pass";
}