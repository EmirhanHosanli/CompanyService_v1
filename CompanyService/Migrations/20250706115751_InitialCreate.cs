using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompanyService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FIRMA_BILGILERI",
                columns: table => new
                {
                    FIRMA_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FIRMA_ADI = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FIRMA_SEKTOR_BILGISI = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HISSE_ADI = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FIRMA_BILGILERI", x => x.FIRMA_ID);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FIRMA_BILGILERI");
        }
    }
}
