using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobScheduler.Api.Migrations
{
    /// <inheritdoc />
    public partial class FixSpelling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Attemps",
                table: "Jobs",
                newName: "Attempts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Attempts",
                table: "Jobs",
                newName: "Attemps");
        }
    }
}
