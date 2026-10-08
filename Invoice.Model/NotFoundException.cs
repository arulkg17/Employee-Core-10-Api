namespace Invoice.Model;

/// <summary>
/// A referenced record does not exist. The API maps this to HTTP 404.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }

    public NotFoundException(string message, Exception innerException)
        : base(message, innerException) { }
}
