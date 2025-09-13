using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Milk_Bakery.Migrations
{
    /// <inheritdoc />
    public partial class REMOVECOLUMN : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CratesCode",
                table: "MaterialMaster");

            migrationBuilder.DropColumn(
                name: "CratesCode",
                table: "CratesTypes");

            migrationBuilder.AddColumn<string>(
                name: "CratesTypes",
                table: "MaterialMaster",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CratesTypes",
                table: "MaterialMaster");

            migrationBuilder.AddColumn<string>(
                name: "CratesCode",
                table: "MaterialMaster",
                type: "nvarchar(1)",
                maxLength: 1,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CratesCode",
                table: "CratesTypes",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "");
        }
    }
}
