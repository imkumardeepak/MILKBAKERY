using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Milk_Bakery.Migrations
{
    /// <inheritdoc />
    public partial class updatedealermaster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BasicAmount",
                table: "DealerBasicOrders",
                newName: "Rate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Rate",
                table: "DealerBasicOrders",
                newName: "BasicAmount");
        }
    }
}
