using System.Security.Claims;
using Invoice.Model;
using Microsoft.AspNetCore.Mvc;

namespace Invoice.CoreAPI.Controllers;

/// <summary>
/// Shared error handling for the transaction controllers:
///   NotFoundException      -> 404
///   BusinessRuleException  -> 400
///   anything else          -> 500 (logged)
/// </summary>
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>User name from the JWT (ClaimTypes.Name), used for CreatedBy / UpdatedBy.</summary>
    protected string CurrentUser =>
        User?.Identity?.Name
        ?? User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? "system";

    protected async Task<IActionResult> RunAsync(
        Func<Task<IActionResult>> action,
        ILogger logger,
        string errorMessage)
    {
        try
        {
            return await action();
        }
        catch (NotFoundException ex)
        {
            return NotFound(Fail(ex.Message, "404", ex.Message));
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(Fail(ex.Message, "400", ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Message}", errorMessage);

            return StatusCode(500, Fail(errorMessage, "500", ex.Message));
        }
    }

    protected static ApiResponse<string> Fail(string message, string code, string details)
    {
        return new ApiResponse<string>
        {
            Success = false,
            Message = message,
            Error = new ApiError { Code = code, Details = details }
        };
    }

    protected static ApiResponse<string> Done(string message)
    {
        return new ApiResponse<string> { Success = true, Message = message };
    }

    protected static ApiResponse<T> Done<T>(string message, T data)
    {
        return new ApiResponse<T> { Success = true, Message = message, Data = data };
    }
}
