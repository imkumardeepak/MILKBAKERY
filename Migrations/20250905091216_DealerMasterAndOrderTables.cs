using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Milk_Bakery.Migrations
{
    /// <inheritdoc />
    public partial class DealerMasterAndOrderTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DealerMasters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DistributorId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RouteCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PhoneNo = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DealerMasters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DealerBasicOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DealerId = table.Column<int>(type: "int", nullable: false),
                    MaterialName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SapCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ShortCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    BasicAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DealerBasicOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DealerBasicOrders_DealerMasters_DealerId",
                        column: x => x.DealerId,
                        principalTable: "DealerMasters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DealerBasicOrders_DealerId",
                table: "DealerBasicOrders",
                column: "DealerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DealerBasicOrders");

            migrationBuilder.DropTable(
                name: "DealerMasters");
        }
    }
}
