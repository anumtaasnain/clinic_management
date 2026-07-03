using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clinic_Management.Migrations
{
    /// <inheritdoc />
    public partial class seminarupdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRegistrationOpen",
                table: "Seminars");

            migrationBuilder.AddColumn<int>(
                name: "Approve",
                table: "Seminars",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Image",
                table: "Seminars",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Price",
                table: "Seminars",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Registration",
                table: "Seminars",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Approve",
                table: "Seminars");

            migrationBuilder.DropColumn(
                name: "Image",
                table: "Seminars");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "Seminars");

            migrationBuilder.DropColumn(
                name: "Registration",
                table: "Seminars");

            migrationBuilder.AddColumn<bool>(
                name: "IsRegistrationOpen",
                table: "Seminars",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
