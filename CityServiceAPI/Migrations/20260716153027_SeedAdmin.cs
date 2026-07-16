using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CityServiceAPI.Migrations
{
    /// <inheritdoc />
    public partial class SeedAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Email", "FirstName", "LastName", "PasswordHash", "Role" },
                values: new object[] { 2, "admin@belediye.gov.tr", "Sistem", "Yöneticisi", "$2a$11$r47/VCQ1fjeXe7/Y9jjLJuEIV37KVQc0Sfd1Y8GpH1wX4ZDpjb46C", "Staff" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2);
        }
    }
}
