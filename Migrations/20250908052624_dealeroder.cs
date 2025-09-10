using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Milk_Bakery.Migrations
{
    /// <inheritdoc />
    public partial class dealeroder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DealerOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DistributorId = table.Column<int>(type: "int", nullable: false),
                    DealerId = table.Column<int>(type: "int", nullable: false),
                    DistributorCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProcessFlag = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DealerOrders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DealerOrderItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DealerOrderId = table.Column<int>(type: "int", nullable: false),
                    MaterialName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ShortCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    SapCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Qty = table.Column<int>(type: "int", nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DealerOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DealerOrderItems_DealerOrders_DealerOrderId",
                        column: x => x.DealerOrderId,
                        principalTable: "DealerOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DealerOrderItems_DealerOrderId_SapCode_ShortCode",
                table: "DealerOrderItems",
                columns: new[] { "DealerOrderId", "SapCode", "ShortCode" });

            migrationBuilder.CreateIndex(
                name: "IX_DealerOrders_OrderDate_DealerId_DistributorId_ProcessFlag",
                table: "DealerOrders",
                columns: new[] { "OrderDate", "DealerId", "DistributorId", "ProcessFlag" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DealerOrderItems");

            migrationBuilder.DropTable(
                name: "DealerOrders");
        }
    }
}
