using Microsoft.EntityFrameworkCore;
using CompanyService.Models;

namespace CompanyService.DATA
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Company> Companies { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Company>().ToTable("FIRMA_BILGILERI");
        }
    }

}
