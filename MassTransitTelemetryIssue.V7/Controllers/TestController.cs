using MassTransit;
using Microsoft.AspNetCore.Mvc;

namespace MassTransitTelemetryIssue.V7.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestController : ControllerBase
    {
        private readonly ILogger<TestController> _logger;

        public TestController(ILogger<TestController> logger)
        {
            _logger = logger;
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> SubmitOrder([FromServices] IRequestClient<SubmitOrder> requestClient)
        {
            var response = await requestClient.GetResponse<OrderAccepted>(new
            {
                OrderId = NewId.NextGuid()
            });

            return Ok(response.Message);
        }
    }
}