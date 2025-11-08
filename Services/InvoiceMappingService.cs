using Milk_Bakery.DTOs;
using static Milk_Bakery.Models.InvoiceDetails;

namespace Milk_Bakery.Services
{
	public interface IInvoiceMappingService
	{
		InvoiceResponseDto MapToResponseDto(Invoice invoice);
		InvoiceListItemDto MapToListItemDto(Invoice invoice);
		InvoiceMaterialResponseDto MapToResponseDto(InvoiceMaterialDetail material);
		Invoice MapToEntity(CreateInvoiceRequestDto dto);
		Invoice MapToEntity(UpdateInvoiceRequestDto dto);
		InvoiceMaterialDetail MapToEntity(CreateInvoiceMaterialRequestDto dto);
		InvoiceMaterialDetail MapToEntity(UpdateInvoiceMaterialRequestDto dto);
		ApiResponseDto<T> CreateSuccessResponse<T>(T data, string message = "Success");
		ApiResponseDto<T> CreateErrorResponse<T>(string message, List<string>? errors = null);
		PaginatedResponseDto<T> CreatePaginatedResponse<T>(List<T> data, int totalRecords, int pageNumber, int pageSize);
	}

	public class InvoiceMappingService : IInvoiceMappingService
	{
		public InvoiceResponseDto MapToResponseDto(Invoice invoice)
		{
			return new InvoiceResponseDto
			{
				TotalMaterialCount = invoice.InvoiceMaterials?.Count ?? 0,
				TotalQuantityCases = invoice.InvoiceMaterials?.Sum(m => m.QuantityCases) ?? 0,
				TotalQuantityUnits = invoice.InvoiceMaterials?.Sum(m => m.QuantityUnits) ?? 0
			};
		}

		public InvoiceListItemDto MapToListItemDto(Invoice invoice)
		{
			return new InvoiceListItemDto
			{
				InvoiceId = invoice.InvoiceId,
				InvoiceNo = invoice.InvoiceNo,
				InvoiceDate = invoice.InvoiceDate,
				BillToName = invoice.BillToName,
				BillToCode = invoice.BillToCode,
				ShipToName = invoice.ShipToName,
				ShipToCode = invoice.ShipToCode,
				TotalAmount = invoice.TotalAmount,
				MaterialCount = invoice.InvoiceMaterials?.Count ?? 0,
				VehicleNo = invoice.VehicleNo,
				InvoiceMaterials = invoice.InvoiceMaterials?.Select(MapToResponseDto).ToList() ?? new List<InvoiceMaterialResponseDto>()
			};
		}

		public InvoiceMaterialResponseDto MapToResponseDto(InvoiceMaterialDetail material)
		{
			return new InvoiceMaterialResponseDto
			{
				MaterialId = material.MaterialId,
				InvoiceId = material.InvoiceId,
				ProductDescription = material.ProductDescription,
				MaterialSapCode = material.MaterialSapCode,
				Batch = material.Batch,
				UnitPerCase = material.UnitPerCase,
				QuantityCases = material.QuantityCases,
				QuantityUnits = material.QuantityUnits,
				UOM = material.UOM
			};
		}

		public Invoice MapToEntity(CreateInvoiceRequestDto dto)
		{
			return new Invoice
			{
				InvoiceNo = dto.InvoiceNo,
				InvoiceDate = dto.InvoiceDate,
				CustomerRefPO = dto.CustomerRefPO,
				TotalAmount = Convert.ToDecimal(dto.TotalAmount),
				OrderDate = dto.OrderDate,
				BillToName = dto.BillToName,
				BillToCode = dto.BillToCode,
				ShipToName = dto.ShipToName,
				ShipToCode = dto.ShipToCode,
				ShipToRoute = dto.ShipToRoute,
				CompanyName = dto.CompanyName,
				CompanyCode = dto.CompanyCode.ToString(),
				VehicleNo = dto.VehicleNo,
				InvoiceMaterials = dto.InvoiceMaterials?.Select(MapToEntity).ToList() ?? new List<InvoiceMaterialDetail>()
			};
		}

		public Invoice MapToEntity(UpdateInvoiceRequestDto dto)
		{
			return new Invoice
			{
				InvoiceId = dto.InvoiceId,
				InvoiceNo = dto.InvoiceNo,
				InvoiceDate = dto.InvoiceDate,
				CustomerRefPO = dto.CustomerRefPO,
				TotalAmount = Convert.ToDecimal(dto.TotalAmount),
				OrderDate = dto.OrderDate,
				BillToName = dto.BillToName,
				BillToCode = dto.BillToCode,
				ShipToName = dto.ShipToName,
				ShipToCode = dto.ShipToCode,
				ShipToRoute = dto.ShipToRoute,
				CompanyName = dto.CompanyName,
				CompanyCode = dto.CompanyCode,
				VehicleNo = dto.VehicleNo,
				InvoiceMaterials = dto.InvoiceMaterials?.Select(MapToEntity).ToList() ?? new List<InvoiceMaterialDetail>()
			};
		}

		public InvoiceMaterialDetail MapToEntity(CreateInvoiceMaterialRequestDto dto)
		{
			return new InvoiceMaterialDetail
			{
				InvoiceId = dto.InvoiceId,
				ProductDescription = dto.ProductDescription,
				MaterialSapCode = dto.MaterialSapCode,
				Batch = dto.Batch,
				UnitPerCase = Convert.ToInt32(dto.UnitPerCase),
				QuantityCases = Convert.ToInt32(dto.QuantityCases),
				QuantityUnits = Convert.ToInt32(dto.QuantityUnits),
				UOM = dto.UOM
			};
		}

		public InvoiceMaterialDetail MapToEntity(UpdateInvoiceMaterialRequestDto dto)
		{
			return new InvoiceMaterialDetail
			{
				MaterialId = dto.MaterialId,
				InvoiceId = dto.InvoiceId,
				ProductDescription = dto.ProductDescription,
				MaterialSapCode = dto.MaterialSapCode,
				Batch = dto.Batch,
				UnitPerCase = dto.UnitPerCase,
				QuantityCases = dto.QuantityCases,
				QuantityUnits = dto.QuantityUnits,
				UOM = dto.UOM
			};
		}

		public ApiResponseDto<T> CreateSuccessResponse<T>(T data, string message = "Success")
		{
			return new ApiResponseDto<T>
			{
				Success = true,
				Message = message,
				Data = data,
				Timestamp = DateTime.UtcNow
			};
		}

		public ApiResponseDto<T> CreateErrorResponse<T>(string message, List<string>? errors = null)
		{
			return new ApiResponseDto<T>
			{
				Success = false,
				Message = message,
				Errors = errors ?? new List<string>(),
				Timestamp = DateTime.UtcNow
			};
		}

		public PaginatedResponseDto<T> CreatePaginatedResponse<T>(List<T> data, int totalRecords, int pageNumber, int pageSize)
		{
			var totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

			return new PaginatedResponseDto<T>
			{
				Data = data,
				TotalRecords = totalRecords,
				PageNumber = pageNumber,
				PageSize = pageSize,
				TotalPages = totalPages,
				HasNextPage = pageNumber < totalPages,
				HasPreviousPage = pageNumber > 1
			};
		}
	}
}