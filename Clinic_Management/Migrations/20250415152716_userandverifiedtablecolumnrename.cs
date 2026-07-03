using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clinic_Management.Migrations
{
    /// <inheritdoc />
    public partial class userandverifiedtablecolumnrename : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "VerifiedUsers",
                newName: "VerifiedAt");

            migrationBuilder.AddColumn<int>(
                name: "Verified",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Verified",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "VerifiedAt",
                table: "VerifiedUsers",
                newName: "CreatedAt");
        }
    }
}
