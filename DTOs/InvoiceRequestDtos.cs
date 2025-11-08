using System.ComponentModel.DataAnnotations;

namespace Milk_Bakery.DTOs
{
	// Invoice create request DTO
	public class CreateInvoiceRequestDto
	{
		[Required(ErrorMessage = "Invoice number is required")]
		[MaxLength(50, ErrorMessage = "Invoice number cannot exceed 50 characters")]
		public string InvoiceNo { get; set; } = string.Empty;

		[Required(ErrorMessage = "Invoice date is required")]
		public DateTime InvoiceDate { get; set; }

		[MaxLength(50, ErrorMessage = "Customer reference PO cannot exceed 50 characters")]
		public string? CustomerRefPO { get; set; }

		[Required(ErrorMessage = "Total amount is required")]
		[MaxLength(50, ErrorMessage = "Total amount cannot exceed 50 characters")]
		public string TotalAmount { get; set; }

		[Required(ErrorMessage = "Order date is required")]
		public DateTime OrderDate { get; set; }

		[Required(ErrorMessage = "Bill to name is required")]
		[MaxLength(200, ErrorMessage = "Bill to name cannot exceed 200 characters")]
		public string BillToName { get; set; } = string.Empty;

		[Required(ErrorMessage = "Bill to code is required")]
		[MaxLength(50, ErrorMessage = "Bill to code cannot exceed 50 characters")]
		public string BillToCode { get; set; } = string.Empty;

		[MaxLength(200, ErrorMessage = "Ship to name cannot exceed 200 characters")]
		public string? ShipToName { get; set; }

		[MaxLength(50, ErrorMessage = "Ship to code cannot exceed 50 characters")]
		public string? ShipToCode { get; set; }

		[MaxLength(100, ErrorMessage = "Ship to route cannot exceed 100 characters")]
		public string? ShipToRoute { get; set; }

		[MaxLength(200, ErrorMessage = "Company name cannot exceed 200 characters")]
		public string? CompanyName { get; set; }

		public int CompanyCode { get; set; }

		[MaxLength(50, ErrorMessage = "Vehicle number cannot exceed 50 characters")]
		public string? VehicleNo { get; set; }

		public List<CreateInvoiceMaterialRequestDto> InvoiceMaterials { get; set; } = new List<CreateInvoiceMaterialRequestDto>();
	}

	// Invoice update request DTO
	public class UpdateInvoiceRequestDto
	{
		[Required(ErrorMessage = "Invoice ID is required")]
		public int InvoiceId { get; set; }

		[Required(ErrorMessage = "Invoice number is required")]
		[MaxLength(50, ErrorMessage = "Invoice number cannot exceed 50 characters")]
		public string InvoiceNo { get; set; } = string.Empty;

		[Required(ErrorMessage = "Invoice date is required")]
		public DateTime InvoiceDate { get; set; }

		[MaxLength(50, ErrorMessage = "Customer reference PO cannot exceed 50 characters")]
		public string? CustomerRefPO { get; set; }

		[Required(ErrorMessage = "Total amount is required")]
		public string TotalAmount { get; set; }

		[Required(ErrorMessage = "Order date is required")]
		public DateTime OrderDate { get; set; }

		[Required(ErrorMessage = "Bill to name is required")]
		[MaxLength(200, ErrorMessage = "Bill to name cannot exceed 200 characters")]
		public string BillToName { get; set; } = string.Empty;

		[Required(ErrorMessage = "Bill to code is required")]
		[MaxLength(50, ErrorMessage = "Bill to code cannot exceed 50 characters")]
		public string BillToCode { get; set; } = string.Empty;

		[MaxLength(200, ErrorMessage = "Ship to name cannot exceed 200 characters")]
		public string? ShipToName { get; set; }

		[MaxLength(50, ErrorMessage = "Ship to code cannot exceed 50 characters")]
		public string? ShipToCode { get; set; }

		[MaxLength(100, ErrorMessage = "Ship to route cannot exceed 100 characters")]
		public string? ShipToRoute { get; set; }

		[MaxLength(200, ErrorMessage = "Company name cannot exceed 200 characters")]
		public string? CompanyName { get; set; }

		[MaxLength(50, ErrorMessage = "Company code cannot exceed 50 characters")]
		public string? CompanyCode { get; set; }

		[MaxLength(50, ErrorMessage = "Vehicle number cannot exceed 50 characters")]
		public string? VehicleNo { get; set; }

		public List<CreateInvoiceMaterialRequestDto> InvoiceMaterials { get; set; } = new List<CreateInvoiceMaterialRequestDto>();
	}

	// Invoice material create request DTO
	public class CreateInvoiceMaterialRequestDto
	{
		public int? MaterialId { get; set; } // Optional for updates

		[Required(ErrorMessage = "Invoice ID is required")]
		public int InvoiceId { get; set; } = 0;

		[Required(ErrorMessage = "Product description is required")]
		[MaxLength(200, ErrorMessage = "Product description cannot exceed 200 characters")]
		public string ProductDescription { get; set; } = string.Empty;

		[Required(ErrorMessage = "Material SAP code is required")]
		[MaxLength(50, ErrorMessage = "Material SAP code cannot exceed 50 characters")]
		public string MaterialSapCode { get; set; } = string.Empty;

		[MaxLength(50, ErrorMessage = "Batch cannot exceed 50 characters")]
		public string? Batch { get; set; }

		[Required(ErrorMessage = "Unit per case is required")]
		public string UnitPerCase { get; set; }

		[Required(ErrorMessage = "Quantity cases is required")]
		public string QuantityCases { get; set; }

		[Required(ErrorMessage = "Quantity units is required")]
		public string QuantityUnits { get; set; }

		[Required(ErrorMessage = "UOM is required")]
		[MaxLength(20, ErrorMessage = "UOM cannot exceed 20 characters")]
		public string UOM { get; set; } = string.Empty;
	}

	// Invoice material update request DTO
	public class UpdateInvoiceMaterialRequestDto
	{
		[Required(ErrorMessage = "Material ID is required")]
		public int MaterialId { get; set; }

		[Required(ErrorMessage = "Invoice ID is required")]
		public int InvoiceId { get; set; }

		[Required(ErrorMessage = "Product description is required")]
		[MaxLength(200, ErrorMessage = "Product description cannot exceed 200 characters")]
		public string ProductDescription { get; set; } = string.Empty;

		[Required(ErrorMessage = "Material SAP code is required")]
		[MaxLength(50, ErrorMessage = "Material SAP code cannot exceed 50 characters")]
		public string MaterialSapCode { get; set; } = string.Empty;

		[MaxLength(50, ErrorMessage = "Batch cannot exceed 50 characters")]
		public string? Batch { get; set; }

		[Required(ErrorMessage = "Unit per case is required")]
		[Range(1, int.MaxValue, ErrorMessage = "Unit per case must be greater than 0")]
		public int UnitPerCase { get; set; }

		[Required(ErrorMessage = "Quantity cases is required")]
		[Range(0, int.MaxValue, ErrorMessage = "Quantity cases cannot be negative")]
		public int QuantityCases { get; set; }

		[Required(ErrorMessage = "Quantity units is required")]
		[Range(0, int.MaxValue, ErrorMessage = "Quantity units cannot be negative")]
		public int QuantityUnits { get; set; }

		[Required(ErrorMessage = "UOM is required")]
		[MaxLength(20, ErrorMessage = "UOM cannot exceed 20 characters")]
		public string UOM { get; set; } = string.Empty;
	}

	// Bulk material create request DTO
	public class BulkCreateMaterialRequestDto
	{
		[Required(ErrorMessage = "Invoice ID is required")]
		public int InvoiceId { get; set; }

		[Required(ErrorMessage = "Materials list is required")]
		[MinLength(1, ErrorMessage = "At least one material is required")]
		public List<CreateInvoiceMaterialRequestDto> Materials { get; set; } = new List<CreateInvoiceMaterialRequestDto>();
	}
}