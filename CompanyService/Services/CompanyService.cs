using CompanyService.Models;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using CompanyService.DATA;

namespace CompanyService.Services
{
    public interface ICompanyService
    {
        Task<Company?> GetCompanyAsync(int id);
        Task<Company> CreateCompanyAsync(Company company);
        Task<Company?> UpdateCompanyAsync(int id, Company updated);
        Task<bool> DeleteCompanyAsync(int id);
        Task<List<Company>> GetAllCompaniesAsync();
    }

    public class CompanyService : ICompanyService
    {
        private readonly AppDbContext _db;
        private readonly IRedisService _cache;
        private readonly ILogger<CompanyService> _logger;
        private readonly TimeSpan _cacheTTL;

        public CompanyService(
            AppDbContext db,
            IRedisService cache,
            ILogger<CompanyService> logger,
            IOptions<CacheSettings> options)
        {
            _db = db;
            _cache = cache;
            _logger = logger;
            _cacheTTL = TimeSpan.FromMinutes(options.Value.DefaultTTLMinutes);
        }
        public async Task<List<Company>> GetAllCompaniesAsync()
        {
            return await _db.Companies.ToListAsync();
        }


        public async Task<Company?> GetCompanyAsync(int id)
        {
            var key = $"company:{id}";

            var cached = await _cache.GetAsync<Company>(key);
            if (cached != null)
            {
                _logger.LogInformation("Cache hit for key {CacheKey}", key);
                return cached;
            }

            var fromDb = await _db.Companies.FindAsync(id);
            if (fromDb != null)
            {
                await _cache.SetAsync(key, fromDb, _cacheTTL);
            }

            return fromDb;
        }

        public async Task<Company> CreateCompanyAsync(Company company)
        {
           
            await _db.Companies.AddAsync(company);
            await _db.SaveChangesAsync();

            var key = $"company:{company.Id}";
            await _cache.SetAsync(key, company, _cacheTTL);

            return company;
        }

        public async Task<Company?> UpdateCompanyAsync(int id, Company updated)
        {
            var existing = await _db.Companies.FindAsync(id);
            if (existing == null) return null;

            existing.Name = updated.Name;
            existing.Sector = updated.Sector;
            existing.StockName = updated.StockName;
   

            await _db.SaveChangesAsync();

            var key = $"company:{id}";
            await _cache.RemoveAsync(key);

            return existing;
        }


        public async Task<bool> DeleteCompanyAsync(int id)
        {
            var existing = await _db.Companies.FindAsync(id);
            if (existing == null) return false;

            _db.Companies.Remove(existing);
            await _db.SaveChangesAsync();

            var key = $"company:{id}";
            await _cache.RemoveAsync(key);

            return true;
        }
    }
}
