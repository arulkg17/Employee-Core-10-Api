using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Invoice.CoreAPI.Test;

public static class ControllerTestHelpers
{
    /// <summary>Gives the controller a logged-in user so CurrentUser / CreatedBy / UpdatedBy can be asserted.</summary>
    public static void SetUser(ControllerBase controller, string userName)
    {
        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Name, userName) },
            "TestAuth");

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }
}
