using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Milk_Bakery.Migrations
{
    /// <inheritdoc />
    public partial class gatepass : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "GatePassGenerated",
                table: "Invoices",
                type: "bit",
                nullable: true,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_GatePassGenerated",
                table: "Invoices",
                column: "GatePassGenerated");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Invoices_GatePassGenerated",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "GatePassGenerated",
                table: "Invoices");
        }
    }
}
