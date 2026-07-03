using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clinic_Management.Migrations
{
    /// <inheritdoc />
    public partial class updaterelationverifiduser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VerifiedUsers_UserId",
                table: "VerifiedUsers");

            migrationBuilder.CreateIndex(
                name: "IX_VerifiedUsers_UserId",
                table: "VerifiedUsers",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VerifiedUsers_UserId",
                table: "VerifiedUsers");

            migrationBuilder.CreateIndex(
                name: "IX_VerifiedUsers_UserId",
                table: "VerifiedUsers",
                column: "UserId",
                unique: true);
        }
    }
}
