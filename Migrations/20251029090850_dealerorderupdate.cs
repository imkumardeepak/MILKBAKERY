using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Milk_Bakery.Migrations
{
    /// <inheritdoc />
    public partial class dealerorderupdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ReceivedAmount",
                table: "DealerOutstandings",
                newName: "PaidAmount");

            migrationBuilder.RenameColumn(
                name: "OutstandingAmount",
                table: "DealerOutstandings",
                newName: "BalanceAmount");

            migrationBuilder.AddColumn<int>(
                name: "DeliverFlag",
                table: "DealerOrders",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliverFlag",
                table: "DealerOrders");

            migrationBuilder.RenameColumn(
                name: "PaidAmount",
                table: "DealerOutstandings",
                newName: "ReceivedAmount");

            migrationBuilder.RenameColumn(
                name: "BalanceAmount",
                table: "DealerOutstandings",
                newName: "OutstandingAmount");
        }
    }
}
