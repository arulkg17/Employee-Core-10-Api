using System.ComponentModel.DataAnnotations;

namespace IMS.Blazor.Models;

public class LoginRequest
{
    [Required(ErrorMessage = "User name is required")]
    public string UserName { get; set; } = "";

    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; } = "";
}

public class LoginResponse
{
    public string Token { get; set; } = "";
    public string? Expiration { get; set; }
}

/// <summary>One page of a list plus the total number of rows.</summary>
public class PagedResult<T>
{
    public List<T> Data { get; set; } = new();
    public int TotalRecords { get; set; }
}

/// <summary>Which document type a PDF belongs to (the names are the API route names).</summary>
public enum PdfDocType
{
    PurchaseOrder,
    SalesInvoice,
    Receipt
}
