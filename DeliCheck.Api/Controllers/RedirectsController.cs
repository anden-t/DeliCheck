using DeliCheck.Schemas.Responses;
using Microsoft.AspNetCore.Mvc;

namespace DeliCheck.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RedirectsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        public RedirectsController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("/qr-redirect")]
        public async Task<IActionResult> QrRedirect()
        {
            return Ok(new QrRedirectResponse(new QrRedirectResponseModel() { RelativeUrl = _configuration["QrRedirect"] ?? "" }));
        }
    }
}
