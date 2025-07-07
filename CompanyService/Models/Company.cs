using System.ComponentModel.DataAnnotations.Schema;

namespace CompanyService.Models
{
    [Table("FIRMA_BILGILERI")]
    public class Company
    {
        [Column("FIRMA_ID")]
        public int Id { get; set; }

        [Column("FIRMA_ADI")]
        public string Name { get; set; } = null!;

        [Column("FIRMA_SEKTOR_BILGISI")]
        public string Sector { get; set; } = null!;

        [Column("HISSE_ADI")]
        public string? StockName { get; set; }
    }
}
