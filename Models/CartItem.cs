namespace Milk_Bakery.Models
{
	public class CartItem
	{
		public int MaterialId { get; set; }
		public string MaterialName { get; set; }
		public string ShortName { get; set; }
		public decimal Price { get; set; }
		public int Quantity { get; set; }
		public string ImagePath { get; set; }
		public decimal TotalPrice => Price * Quantity;
	}
}
