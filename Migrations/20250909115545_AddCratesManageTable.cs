using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Milk_Bakery.Migrations
{
    /// <inheritdoc />
    public partial class AddCratesManageTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CratesManages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    SegmentCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DispDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Opening = table.Column<int>(type: "int", nullable: false),
                    Outward = table.Column<int>(type: "int", nullable: false),
                    Inward = table.Column<int>(type: "int", nullable: false),
                    Balance = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CratesManages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CratesManages_Customer_Master_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customer_Master",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CratesManages_CustomerId",
                table: "CratesManages",
                column: "CustomerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CratesManages");
        }
    }
}
