namespace Invoice.Model;

/// <summary>
/// A business rule was violated (invalid status, insufficient stock, duplicate number ...).
/// The API maps this to HTTP 400.
/// </summary>
public class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message) { }

    public BusinessRuleException(string message, Exception innerException)
        : base(message, innerException) { }
}
