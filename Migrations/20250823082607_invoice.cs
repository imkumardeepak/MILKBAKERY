using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Milk_Bakery.Migrations
{
    /// <inheritdoc />
    public partial class invoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    InvoiceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    InvoiceDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CustomerRefPO = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OrderDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BillToName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BillToCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ShipToName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ShipToCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ShipToRoute = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CompanyCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    VehicleNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.InvoiceId);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceMaterials",
                columns: table => new
                {
                    MaterialId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceId = table.Column<int>(type: "int", nullable: false),
                    ProductDescription = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MaterialSapCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Batch = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UnitPerCase = table.Column<int>(type: "int", nullable: false),
                    QuantityCases = table.Column<int>(type: "int", nullable: false),
                    QuantityUnits = table.Column<int>(type: "int", nullable: false),
                    UOM = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceMaterials", x => x.MaterialId);
                    table.ForeignKey(
                        name: "FK_InvoiceMaterials_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "InvoiceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceMaterials_InvoiceId_MaterialId",
                table: "InvoiceMaterials",
                columns: new[] { "InvoiceId", "MaterialId" });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_InvoiceId_ShipToCode_BillToCode_InvoiceDate_VehicleNo",
                table: "Invoices",
                columns: new[] { "InvoiceId", "ShipToCode", "BillToCode", "InvoiceDate", "VehicleNo" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvoiceMaterials");

            migrationBuilder.DropTable(
                name: "Invoices");
        }
    }
}
