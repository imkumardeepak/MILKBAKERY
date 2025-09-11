using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Milk_Bakery.Migrations
{
	/// <inheritdoc />
	public partial class addcolumnindealerorder : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropColumn(
				name: "Price",
				table: "DealerOrderItems");

			migrationBuilder.AddColumn<int>(
				name: "DeliverQnty",
				table: "DealerOrderItems",
				type: "int",
				nullable: false,
				defaultValue: 0);


		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{


			migrationBuilder.DropColumn(
				name: "DeliverQnty",
				table: "DealerOrderItems");



			migrationBuilder.AddColumn<decimal>(
				name: "Price",
				table: "DealerOrderItems",
				type: "decimal(18,2)",
				nullable: false,
				defaultValue: 0m);
		}
	}
}
