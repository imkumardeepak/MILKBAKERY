using System.ComponentModel.DataAnnotations;

namespace Milk_Bakery.DTOs
{
	// Base response DTO for all API responses
	public class ApiResponseDto<T>
	{
		public bool Success { get; set; }
		public string Message { get; set; } = string.Empty;
		public T? Data { get; set; }
		public List<string> Errors { get; set; } = new List<string>();
		public DateTime Timestamp { get; set; } = DateTime.UtcNow;
	}

	// Invoice response DTO
	public class InvoiceResponseDto
	{
		public int TotalMaterialCount { get; set; }
		public int TotalQuantityCases { get; set; }
		public int TotalQuantityUnits { get; set; }
	}

	// Invoice material response DTO
	public class InvoiceMaterialResponseDto
	{
		public int MaterialId { get; set; }
		public int InvoiceId { get; set; }
		public string? ProductDescription { get; set; }
		public string? MaterialSapCode { get; set; }
		public string? Batch { get; set; }
		public int UnitPerCase { get; set; }
		public int QuantityCases { get; set; }
		public int QuantityUnits { get; set; }
		public string? UOM { get; set; }
		public int TotalUnits => (QuantityCases * UnitPerCase) + QuantityUnits;
	}

	// Invoice summary response DTO
	public class InvoiceSummaryResponseDto
	{
		public int TotalInvoices { get; set; }
		public decimal TotalAmount { get; set; }
		public int TodayInvoices { get; set; }
		public int MonthlyInvoices { get; set; }
		public decimal AverageInvoiceAmount { get; set; }
		public decimal TodayAmount { get; set; }
		public decimal MonthlyAmount { get; set; }
		public DateTime LastInvoiceDate { get; set; }
		public string? LastInvoiceNo { get; set; }
	}

	// Invoice material summary response DTO
	public class InvoiceMaterialSummaryResponseDto
	{
		public int InvoiceId { get; set; }
		public string? InvoiceNo { get; set; }
		public int TotalMaterials { get; set; }
		public int TotalCases { get; set; }
		public int TotalUnits { get; set; }
		public int TotalCalculatedUnits { get; set; }
		public List<MaterialGroupSummaryDto> MaterialGroups { get; set; } = new List<MaterialGroupSummaryDto>();
	}

	// Material group summary DTO
	public class MaterialGroupSummaryDto
	{
		public string? MaterialSapCode { get; set; }
		public string? ProductDescription { get; set; }
		public int TotalCases { get; set; }
		public int TotalUnits { get; set; }
		public string? UOM { get; set; }
		public List<string> Batches { get; set; } = new List<string>();
	}

	// Paginated response DTO
	public class PaginatedResponseDto<T>
	{
		public List<T> Data { get; set; } = new List<T>();
		public int TotalRecords { get; set; }
		public int PageNumber { get; set; }
		public int PageSize { get; set; }
		public int TotalPages { get; set; }
		public bool HasNextPage { get; set; }
		public bool HasPreviousPage { get; set; }
	}

	// Invoice list item DTO (for list views)
	public class InvoiceListItemDto
	{
		public int InvoiceId { get; set; }
		public string InvoiceNo { get; set; } = string.Empty;
		public DateTime InvoiceDate { get; set; }
		public string? BillToName { get; set; }
		public string? BillToCode { get; set; }
		public string? ShipToName { get; set; }
		public string? ShipToCode { get; set; }
		public decimal TotalAmount { get; set; }
		public int MaterialCount { get; set; }
		public string? VehicleNo { get; set; }
		public string FormattedInvoiceDate => InvoiceDate.ToString("dd/MM/yyyy");
		public string FormattedAmount => $"₹{TotalAmount:N2}";

		public List<InvoiceMaterialResponseDto> InvoiceMaterials { get; set; } = new List<InvoiceMaterialResponseDto>();
	}

	// Search criteria DTO
	public class InvoiceSearchCriteriaDto
	{
		public string? InvoiceNo { get; set; }
		public string? CustomerCode { get; set; }
		public DateTime? FromDate { get; set; }
		public DateTime? ToDate { get; set; }
		public string? CompanyCode { get; set; }
		public string? VehicleNo { get; set; }
		public decimal? MinAmount { get; set; }
		public decimal? MaxAmount { get; set; }
		public int PageNumber { get; set; } = 1;
		public int PageSize { get; set; } = 20;
		public string SortBy { get; set; } = "InvoiceDate";
		public string SortDirection { get; set; } = "desc";
	}

	// Material search criteria DTO
	public class MaterialSearchCriteriaDto
	{
		public string? MaterialCode { get; set; }
		public string? ProductDescription { get; set; }
		public string? Batch { get; set; }
		public int? InvoiceId { get; set; }
		public DateTime? FromDate { get; set; }
		public DateTime? ToDate { get; set; }
		public int PageNumber { get; set; } = 1;
		public int PageSize { get; set; } = 50;
	}

	// Error response DTO
	public class ErrorResponseDto
	{
		public string Message { get; set; } = string.Empty;
		public string? Details { get; set; }
		public int StatusCode { get; set; }
		public DateTime Timestamp { get; set; } = DateTime.UtcNow;
		public string? TraceId { get; set; }
		public List<ValidationErrorDto> ValidationErrors { get; set; } = new List<ValidationErrorDto>();
	}

	// Validation error DTO
	public class ValidationErrorDto
	{
		public string Field { get; set; } = string.Empty;
		public string Message { get; set; } = string.Empty;
		public object? AttemptedValue { get; set; }
	}
}