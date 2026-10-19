using System.Security.Claims;
using Invoice.CoreAPI.Controllers;
using Invoice.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Invoice.CoreAPI.Test;

public class ApiControllerBaseTests
{
    private sealed class TestController : ApiControllerBase
    {
        public ApiResponse<string> ExposeFail(string message, string code, string details) => Fail(message, code, details);
        public ApiResponse<string> ExposeDone(string message) => Done(message);
        public ApiResponse<int> ExposeDoneWithData(string message, int value) => Done(message, value);
        public string ExposeCurrentUser() => CurrentUser;
    }

    [Fact]
    public void FailAndDoneProduceExpectedResponses()
    {
        var ctl = new TestController();
        var fail = ctl.ExposeFail("bad", "400", "d");
        Assert.False(fail.Success);
        Assert.Equal("bad", fail.Message);
        Assert.NotNull(fail.Error);

        var ok = ctl.ExposeDone("ok");
        Assert.True(ok.Success);
        Assert.Equal("ok", ok.Message);

        var ok2 = ctl.ExposeDoneWithData("ok", 5);
        Assert.True(ok2.Success);
        Assert.Equal(5, ok2.Data);
    }

    [Fact]
    public void CurrentUser_UsesClaimsIdentityName()
    {
        var ctl = new TestController();
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "alice") }, "test"));
        ctl.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };

        Assert.Equal("alice", ctl.ExposeCurrentUser());
    }
}
