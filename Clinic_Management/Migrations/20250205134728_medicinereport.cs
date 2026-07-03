using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clinic_Management.Migrations
{
    /// <inheritdoc />
    public partial class medicinereport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "MedicalInstruments");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "MedicalInstruments");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "MedicalInstruments");

            migrationBuilder.AddColumn<int>(
                name: "AddedBy",
                table: "MedicalInstruments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "MedicalInstruments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Image",
                table: "MedicalInstruments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Manufacturer",
                table: "MedicalInstruments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ManufacturerEmail",
                table: "MedicalInstruments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ReorderLevel",
                table: "MedicalInstruments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "UnitPrice",
                table: "MedicalInstruments",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.CreateTable(
                name: "MedicineStockReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BatchId = table.Column<int>(type: "int", nullable: false),
                    MedicineId = table.Column<int>(type: "int", nullable: false),
                    TransactionType = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicineStockReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicineStockReports_Batches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "Batches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MedicineStockReports_Medicines_MedicineId",
                        column: x => x.MedicineId,
                        principalTable: "Medicines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MedicalInstruments_AddedBy",
                table: "MedicalInstruments",
                column: "AddedBy");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalInstruments_CategoryId",
                table: "MedicalInstruments",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicineStockReports_BatchId",
                table: "MedicineStockReports",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicineStockReports_MedicineId",
                table: "MedicineStockReports",
                column: "MedicineId");

            migrationBuilder.AddForeignKey(
                name: "FK_MedicalInstruments_Categories_CategoryId",
                table: "MedicalInstruments",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MedicalInstruments_Users_AddedBy",
                table: "MedicalInstruments",
                column: "AddedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MedicalInstruments_Categories_CategoryId",
                table: "MedicalInstruments");

            migrationBuilder.DropForeignKey(
                name: "FK_MedicalInstruments_Users_AddedBy",
                table: "MedicalInstruments");

            migrationBuilder.DropTable(
                name: "MedicineStockReports");

            migrationBuilder.DropIndex(
                name: "IX_MedicalInstruments_AddedBy",
                table: "MedicalInstruments");

            migrationBuilder.DropIndex(
                name: "IX_MedicalInstruments_CategoryId",
                table: "MedicalInstruments");

            migrationBuilder.DropColumn(
                name: "AddedBy",
                table: "MedicalInstruments");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "MedicalInstruments");

            migrationBuilder.DropColumn(
                name: "Image",
                table: "MedicalInstruments");

            migrationBuilder.DropColumn(
                name: "Manufacturer",
                table: "MedicalInstruments");

            migrationBuilder.DropColumn(
                name: "ManufacturerEmail",
                table: "MedicalInstruments");

            migrationBuilder.DropColumn(
                name: "ReorderLevel",
                table: "MedicalInstruments");

            migrationBuilder.DropColumn(
                name: "UnitPrice",
                table: "MedicalInstruments");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "MedicalInstruments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "MedicalInstruments",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "MedicalInstruments",
                type: "datetime2",
                nullable: true);
        }
    }
}
