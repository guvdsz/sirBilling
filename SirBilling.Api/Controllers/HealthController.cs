using Microsoft.AspNetCore.Mvc;

namespace SirBilling.Api.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        var response = new { healthy = true };

        return Created("", response);
    }
}
