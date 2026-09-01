using Microsoft.AspNetCore.Mvc;
using SirBilling.Api.Controllers;

namespace SirBilling.Api.Tests;

public class HealthControllerTests
{
    [Fact]
    public void Get_ReturnsHealthy()
    {
        var result = new HealthController().Get();

        var ok = Assert.IsType<OkObjectResult>(result);
        var healthy = ok.Value!.GetType().GetProperty("healthy")!.GetValue(ok.Value);

        Assert.Equal(true, healthy);
    }
}
