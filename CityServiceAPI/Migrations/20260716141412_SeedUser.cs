using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CityServiceAPI.Migrations
{
    /// <inheritdoc />
    public partial class SeedUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Email", "FirstName", "LastName", "PasswordHash", "Role" },
                values: new object[] { 1, "test@vatandas.com", "Test", "Vatandaş", "$2a$11$DummyHashDummyHashDummyHashDummyHashDummyHashDumm", "Citizen" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1);
        }
    }
}
