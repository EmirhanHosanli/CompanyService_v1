namespace CompanyService.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using System.Threading.Tasks;

    [ApiController]
    [Route("api/[controller]")]
    public class ExternalCompaniesController : ControllerBase
    {
        private readonly IHttpClientFactory _clientFactory;

        public ExternalCompaniesController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        [HttpGet("random")]
        public async Task<IActionResult> GetRandomCompany()
        {
            var client = _clientFactory.CreateClient();
            var response = await client.GetAsync("https://randomuser.me/api/");
            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, "External API error.");

            var content = await response.Content.ReadAsStringAsync();
            return Content(content, "application/json");
        }
    }
}
