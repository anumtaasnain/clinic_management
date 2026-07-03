using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clinic_Management.Migrations
{
    /// <inheritdoc />
    public partial class medical : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Carts_Medicines_MedicineId",
                table: "Carts");

            migrationBuilder.RenameColumn(
                name: "MedicineId",
                table: "Carts",
                newName: "ProductId");

            migrationBuilder.RenameIndex(
                name: "IX_Carts_MedicineId",
                table: "Carts",
                newName: "IX_Carts_ProductId");

            migrationBuilder.AddColumn<string>(
                name: "ProductType",
                table: "Carts",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "MedicalInstruments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalInstruments", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Carts_MedicalInstruments_ProductId",
                table: "Carts",
                column: "ProductId",
                principalTable: "MedicalInstruments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Carts_Medicines_ProductId",
                table: "Carts",
                column: "ProductId",
                principalTable: "Medicines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Carts_MedicalInstruments_ProductId",
                table: "Carts");

            migrationBuilder.DropForeignKey(
                name: "FK_Carts_Medicines_ProductId",
                table: "Carts");

            migrationBuilder.DropTable(
                name: "MedicalInstruments");

            migrationBuilder.DropColumn(
                name: "ProductType",
                table: "Carts");

            migrationBuilder.RenameColumn(
                name: "ProductId",
                table: "Carts",
                newName: "MedicineId");

            migrationBuilder.RenameIndex(
                name: "IX_Carts_ProductId",
                table: "Carts",
                newName: "IX_Carts_MedicineId");

            migrationBuilder.AddForeignKey(
                name: "FK_Carts_Medicines_MedicineId",
                table: "Carts",
                column: "MedicineId",
                principalTable: "Medicines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
