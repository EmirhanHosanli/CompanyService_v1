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

        public CompaniesController(AppDbContext db, IRedisService redis)
        {
            _db = db;
            _redis = redis;
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
            await _redis.RemoveAsync(cacheKey); // Cache invalidasyonu

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

            return NoContent();
        }
    }
}
