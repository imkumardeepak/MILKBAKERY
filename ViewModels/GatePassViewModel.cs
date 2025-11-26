using System.ComponentModel.DataAnnotations;

namespace Milk_Bakery.ViewModels
{
	public class GatePassViewModel
	{
		public string TruckNumber { get; set; }
		public DateTime DispatchDate { get; set; }
		public int CustomerCount { get; set; }
		public string Route { get; set; }
		public List<GatePassCustomerDetail> CustomerDetails { get; set; } = new List<GatePassCustomerDetail>();
	}

	public class GatePassCustomerDetail
	{
		public string CustomerName { get; set; }
		public int Cartons { get; set; }
		public int Crates { get; set; }
		public int Numbers { get; set; }
	}

	public class GatePassIndexViewModel
	{
		public List<GatePassGroupedData> GroupedData { get; set; } = new List<GatePassGroupedData>();
	}

	public class GatePassGroupedData
	{
		public string TruckNumber { get; set; }
		public DateTime DispatchDate { get; set; }
		public int CustomerCount { get; set; }
	}
}