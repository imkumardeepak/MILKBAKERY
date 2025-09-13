using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Milk_Bakery.Migrations
{
    /// <inheritdoc />
    public partial class AddDivisionToCratesType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Division",
                table: "CratesTypes",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Division",
                table: "CratesTypes");
        }
    }
}
