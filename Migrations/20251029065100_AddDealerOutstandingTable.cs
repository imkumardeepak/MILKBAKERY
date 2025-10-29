using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Milk_Bakery.Migrations
{
	/// <inheritdoc />
	public partial class AddDealerOutstandingTable : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{

			migrationBuilder.CreateTable(
				name: "DealerOutstandings",
				columns: table => new
				{
					Id = table.Column<int>(type: "int", nullable: false)
						.Annotation("SqlServer:Identity", "1, 1"),
					DealerId = table.Column<int>(type: "int", nullable: false),
					DeliverDate = table.Column<DateTime>(type: "date", nullable: false),
					InvoiceAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
					OutstandingAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_DealerOutstandings", x => x.Id);
					table.ForeignKey(
						name: "FK_DealerOutstandings_DealerMasters_DealerId",
						column: x => x.DealerId,
						principalTable: "DealerMasters",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateIndex(
				name: "IX_DealerOrders_DealerId",
				table: "DealerOrders",
				column: "DealerId");

			migrationBuilder.CreateIndex(
				name: "IX_DealerOutstandings_DealerId",
				table: "DealerOutstandings",
				column: "DealerId");

			migrationBuilder.AddForeignKey(
				name: "FK_DealerOrders_DealerMasters_DealerId",
				table: "DealerOrders",
				column: "DealerId",
				principalTable: "DealerMasters",
				principalColumn: "Id",
				onDelete: ReferentialAction.Cascade);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropForeignKey(
				name: "FK_DealerOrders_DealerMasters_DealerId",
				table: "DealerOrders");


			migrationBuilder.DropTable(
				name: "DealerOutstandings");

			migrationBuilder.DropIndex(
				name: "IX_DealerOrders_DealerId",
				table: "DealerOrders");
		}
	}
}
