namespace CompanyService.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using CompanyService.Models;
    using CompanyService.Services;
    using CompanyService.DATA;

    [ApiController]
    [Route("api/[controller]")]
    public class CompaniesController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IRedisService _redis;
        private readonly WebSocketNotifier _notifier;


        public CompaniesController(AppDbContext db, IRedisService redis, WebSocketNotifier notifier)
        {
            _db = db;
            _redis = redis;
            _notifier = notifier;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllCompanies()
        {
            var companies = await _db.Companies.ToListAsync();
            return Ok(companies);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCompany(int id)
        {
            var cacheKey = $"company:{id}";
            var cachedCompany = await _redis.GetAsync<Company>(cacheKey);

            if (cachedCompany != null)
            {
                Console.WriteLine($"✅ Cache hit: {cacheKey}");
                return Ok(cachedCompany);
            }

            Console.WriteLine($"❌ Cache miss: {cacheKey}");
            var company = await _db.Companies.FindAsync(id);
            if (company == null)
                return NotFound(new { message = "Company not found." });

            await _redis.SetAsync(cacheKey, company);
            return Ok(company);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCompany([FromBody] Company company)
        {
            _db.Companies.Add(company);
            await _db.SaveChangesAsync();

            var cacheKey = $"company:{company.Id}";
            await _redis.SetAsync(cacheKey, company);
            _notifier.NotifyAll(System.Text.Json.JsonSerializer.Serialize(new
            {
                type = "company_created",
                id = company.Id,
                name = company.Name,
                sector = company.Sector
            }));

            return CreatedAtAction(nameof(GetCompany), new { id = company.Id }, company);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCompany(int id, [FromBody] Company updated)
        {
            var company = await _db.Companies.FindAsync(id);
            if (company == null) return NotFound(new { message = "Company not found." });

            company.Name = updated.Name;
            company.Sector = updated.Sector;
            company.StockName = updated.StockName;

            await _db.SaveChangesAsync();

            var cacheKey = $"company:{id}";
            await _redis.RemoveAsync(cacheKey); 
            _notifier.NotifyAll(System.Text.Json.JsonSerializer.Serialize(new
            {
                type = "company_updated",
                id = company.Id,
                name = company.Name,
                sector = company.Sector
            }));

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCompany(int id)
        {
            var company = await _db.Companies.FindAsync(id);
            if (company == null) return NotFound(new { message = "Company not found." });

            _db.Companies.Remove(company);
            await _db.SaveChangesAsync();

            var cacheKey = $"company:{id}";
            await _redis.RemoveAsync(cacheKey);
            _notifier.NotifyAll(System.Text.Json.JsonSerializer.Serialize(new
            {
                type = "company_deleted",
                id = id
            }));

            return NoContent();
        }
    }
}
