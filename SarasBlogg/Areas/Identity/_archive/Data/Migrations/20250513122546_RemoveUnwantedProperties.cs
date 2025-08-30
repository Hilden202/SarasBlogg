using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SarasBlogg.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnwantedProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContactText",
                table: "AboutMe");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "AboutMe");

            migrationBuilder.DropColumn(
                name: "Instagram",
                table: "AboutMe");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContactText",
                table: "AboutMe",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "AboutMe",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Instagram",
                table: "AboutMe",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
