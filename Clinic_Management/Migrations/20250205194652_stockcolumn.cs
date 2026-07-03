using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clinic_Management.Migrations
{
    /// <inheritdoc />
    public partial class stockcolumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StockQuantity",
                table: "MedicalInstruments",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StockQuantity",
                table: "MedicalInstruments");
        }
    }
}
