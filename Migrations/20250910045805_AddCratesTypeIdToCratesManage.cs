using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Milk_Bakery.Migrations
{
    /// <inheritdoc />
    public partial class AddCratesTypeIdToCratesManage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CratesTypeId",
                table: "CratesManages",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CratesManages_CratesTypeId",
                table: "CratesManages",
                column: "CratesTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_CratesManages_CratesTypes_CratesTypeId",
                table: "CratesManages",
                column: "CratesTypeId",
                principalTable: "CratesTypes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CratesManages_CratesTypes_CratesTypeId",
                table: "CratesManages");

            migrationBuilder.DropIndex(
                name: "IX_CratesManages_CratesTypeId",
                table: "CratesManages");

            migrationBuilder.DropColumn(
                name: "CratesTypeId",
                table: "CratesManages");
        }
    }
}
