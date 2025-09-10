using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Milk_Bakery.Data;
using Milk_Bakery.DTOs;
using Milk_Bakery.Services;
using static Milk_Bakery.Models.InvoiceDetails;

namespace Milk_Bakery.Controllers.Api
{
	[Route("api/[controller]")]
	[ApiController]
	public class InvoiceController : ControllerBase
	{
		private readonly MilkDbContext _context;
		private readonly IInvoiceMappingService _mappingService;

		public InvoiceController(MilkDbContext context, IInvoiceMappingService mappingService)
		{
			_context = context;
			_mappingService = mappingService;
		}

		// GET: api/Invoice
		[HttpGet]
		public async Task<ActionResult<ApiResponseDto<PaginatedResponseDto<InvoiceListItemDto>>>> GetInvoices(
			[FromQuery] int pageNumber = 1,
			[FromQuery] int pageSize = 100)
		{
			try
			{
				var totalRecords = await _context.Invoices.CountAsync();

				var invoices = await _context.Invoices
					.Include(i => i.InvoiceMaterials)
					.OrderByDescending(i => i.InvoiceDate)
					.Skip((pageNumber - 1) * pageSize)
					.Take(pageSize)
					.ToListAsync();

				var invoiceDtos = invoices.Select(_mappingService.MapToListItemDto).ToList();
				var paginatedResponse = _mappingService.CreatePaginatedResponse(invoiceDtos, totalRecords, pageNumber, pageSize);
				var response = _mappingService.CreateSuccessResponse(paginatedResponse, "Invoices retrieved successfully");

				return Ok(response);
			}
			catch (Exception ex)
			{
				var errorResponse = _mappingService.CreateErrorResponse<PaginatedResponseDto<InvoiceListItemDto>>(
					"Failed to retrieve invoices", new List<string> { ex.Message });
				return StatusCode(500, errorResponse);
			}
		}

		// GET: api/Invoice/5
		[HttpGet("{id}")]
		public async Task<ActionResult<ApiResponseDto<InvoiceResponseDto>>> GetInvoice(int id)
		{
			try
			{
				var invoice = await _context.Invoices
					.Include(i => i.InvoiceMaterials)
					.FirstOrDefaultAsync(i => i.InvoiceId == id);

				if (invoice == null)
				{
					var notFoundResponse = _mappingService.CreateErrorResponse<InvoiceResponseDto>(
						$"Invoice with ID {id} not found");
					return NotFound(notFoundResponse);
				}

				var invoiceDto = _mappingService.MapToResponseDto(invoice);
				var response = _mappingService.CreateSuccessResponse(invoiceDto, "Invoice retrieved successfully");
				return Ok(response);
			}
			catch (Exception ex)
			{
				var errorResponse = _mappingService.CreateErrorResponse<InvoiceResponseDto>(
					"Failed to retrieve invoice", new List<string> { ex.Message });
				return StatusCode(500, errorResponse);
			}
		}

		// GET: api/Invoice/search
		[HttpGet("search")]
		public async Task<ActionResult<ApiResponseDto<PaginatedResponseDto<InvoiceListItemDto>>>> SearchInvoices(
			[FromQuery] InvoiceSearchCriteriaDto searchCriteria)
		{
			try
			{
				var query = _context.Invoices.Include(i => i.InvoiceMaterials).AsQueryable();

				if (!string.IsNullOrEmpty(searchCriteria.InvoiceNo))
				{
					query = query.Where(i => i.InvoiceNo.Contains(searchCriteria.InvoiceNo));
				}

				if (!string.IsNullOrEmpty(searchCriteria.CustomerCode))
				{
					query = query.Where(i => i.BillToCode == searchCriteria.CustomerCode || i.ShipToCode == searchCriteria.CustomerCode);
				}

				if (searchCriteria.FromDate.HasValue)
				{
					query = query.Where(i => i.InvoiceDate >= searchCriteria.FromDate.Value);
				}

				if (searchCriteria.ToDate.HasValue)
				{
					query = query.Where(i => i.InvoiceDate <= searchCriteria.ToDate.Value);
				}

				if (!string.IsNullOrEmpty(searchCriteria.CompanyCode))
				{
					query = query.Where(i => i.CompanyCode == searchCriteria.CompanyCode);
				}

				if (!string.IsNullOrEmpty(searchCriteria.VehicleNo))
				{
					query = query.Where(i => i.VehicleNo == searchCriteria.VehicleNo);
				}

				if (searchCriteria.MinAmount.HasValue)
				{
					query = query.Where(i => i.TotalAmount >= searchCriteria.MinAmount.Value);
				}

				if (searchCriteria.MaxAmount.HasValue)
				{
					query = query.Where(i => i.TotalAmount <= searchCriteria.MaxAmount.Value);
				}

				// Apply sorting
				query = searchCriteria.SortDirection.ToLower() == "asc"
					? query.OrderBy(i => EF.Property<object>(i, searchCriteria.SortBy))
					: query.OrderByDescending(i => EF.Property<object>(i, searchCriteria.SortBy));

				var totalRecords = await query.CountAsync();

				var invoices = await query
					.Skip((searchCriteria.PageNumber - 1) * searchCriteria.PageSize)
					.Take(searchCriteria.PageSize)
					.ToListAsync();

				var invoiceDtos = invoices.Select(_mappingService.MapToListItemDto).ToList();
				var paginatedResponse = _mappingService.CreatePaginatedResponse(invoiceDtos, totalRecords, searchCriteria.PageNumber, searchCriteria.PageSize);
				var response = _mappingService.CreateSuccessResponse(paginatedResponse, "Search completed successfully");

				return Ok(response);
			}
			catch (Exception ex)
			{
				var errorResponse = _mappingService.CreateErrorResponse<PaginatedResponseDto<InvoiceListItemDto>>(
					"Search failed", new List<string> { ex.Message });
				return StatusCode(500, errorResponse);
			}
		}

		// POST: api/Invoice
		[HttpPost]
		public async Task<ActionResult<ApiResponseDto<InvoiceResponseDto>>> CreateInvoice(CreateInvoiceRequestDto createDto)
		{
			try
			{
				if (!ModelState.IsValid)
				{
					var validationErrors = ModelState.Values
						.SelectMany(v => v.Errors)
						.Select(e => e.ErrorMessage)
						.ToList();
					var validationResponse = _mappingService.CreateErrorResponse<InvoiceResponseDto>(
						"Validation failed", validationErrors);
					return BadRequest(validationResponse);
				}

				var existingInvoice = await _context.Invoices.Where(i => i.InvoiceNo == createDto.InvoiceNo).FirstOrDefaultAsync();

				if (existingInvoice != null)
				{
					var errorResponse = _mappingService.CreateErrorResponse<InvoiceResponseDto>(
						"Failed to create invoice", new List<string> { "Invoice number already exists" });
					return BadRequest(errorResponse);
				}

				var existingpo = await _context.Invoices.Where(i => i.CustomerRefPO == createDto.CustomerRefPO).FirstOrDefaultAsync();

				if (existingpo != null)
				{
					var errorResponse = _mappingService.CreateErrorResponse<InvoiceResponseDto>(
						"Failed to create invoice", new List<string> { "PO number already exists" });
					return BadRequest(errorResponse);
				}


				var invoice = _mappingService.MapToEntity(createDto);

				// Set default values
				invoice.InvoiceDate = invoice.InvoiceDate == default ? DateTime.Now : invoice.InvoiceDate;
				invoice.OrderDate = invoice.OrderDate == default ? DateTime.Now : invoice.OrderDate;

				_context.Invoices.Add(invoice);
				await _context.SaveChangesAsync();

				var invoiceDto = _mappingService.MapToResponseDto(invoice);
				var response = _mappingService.CreateSuccessResponse(invoiceDto, "Invoice created successfully");

				return CreatedAtAction(nameof(GetInvoice),
					new { id = invoice.InvoiceId }, response);
			}
			catch (Exception ex)
			{
				var errorResponse = _mappingService.CreateErrorResponse<InvoiceResponseDto>(
					"Failed to create invoice", new List<string> { ex.Message });
				return StatusCode(500, errorResponse);
			}
		}

		// PUT: api/Invoice/5
		[HttpPut("{id}")]
		public async Task<ActionResult<ApiResponseDto<string>>> UpdateInvoice(int id, UpdateInvoiceRequestDto updateDto)
		{
			try
			{
				if (id != updateDto.InvoiceId)
				{
					var mismatchResponse = _mappingService.CreateErrorResponse<string>("Invoice ID mismatch");
					return BadRequest(mismatchResponse);
				}

				if (!ModelState.IsValid)
				{
					var validationErrors = ModelState.Values
						.SelectMany(v => v.Errors)
						.Select(e => e.ErrorMessage)
						.ToList();
					var validationResponse = _mappingService.CreateErrorResponse<string>(
						"Validation failed", validationErrors);
					return BadRequest(validationResponse);
				}

				var existingInvoice = await _context.Invoices
					.Include(i => i.InvoiceMaterials)
					.FirstOrDefaultAsync(i => i.InvoiceId == id);

				if (existingInvoice == null)
				{
					var notFoundResponse = _mappingService.CreateErrorResponse<string>(
						$"Invoice with ID {id} not found");
					return NotFound(notFoundResponse);
				}

				// Update invoice properties
				existingInvoice.InvoiceNo = updateDto.InvoiceNo;
				existingInvoice.InvoiceDate = updateDto.InvoiceDate;
				existingInvoice.CustomerRefPO = updateDto.CustomerRefPO;
				existingInvoice.TotalAmount = updateDto.TotalAmount;
				existingInvoice.OrderDate = updateDto.OrderDate;
				existingInvoice.BillToName = updateDto.BillToName;
				existingInvoice.BillToCode = updateDto.BillToCode;
				existingInvoice.ShipToName = updateDto.ShipToName;
				existingInvoice.ShipToCode = updateDto.ShipToCode;
				existingInvoice.ShipToRoute = updateDto.ShipToRoute;
				existingInvoice.CompanyName = updateDto.CompanyName;
				existingInvoice.CompanyCode = updateDto.CompanyCode;
				existingInvoice.VehicleNo = updateDto.VehicleNo;

				// Update invoice materials
				if (updateDto.InvoiceMaterials != null)
				{
					// Remove existing materials
					_context.InvoiceMaterials.RemoveRange(existingInvoice.InvoiceMaterials);

					// Add new materials
					var newMaterials = updateDto.InvoiceMaterials.Select(_mappingService.MapToEntity).ToList();
					foreach (var material in newMaterials)
					{
						material.InvoiceId = id;
						material.MaterialId = 0; // Reset to let EF assign new ID
					}
					_context.InvoiceMaterials.AddRange(newMaterials);
				}

				await _context.SaveChangesAsync();
				var response = _mappingService.CreateSuccessResponse("Invoice updated successfully");
				return Ok(response);
			}
			catch (DbUpdateConcurrencyException)
			{
				if (!InvoiceExists(id))
				{
					var notFoundResponse = _mappingService.CreateErrorResponse<string>(
						$"Invoice with ID {id} not found");
					return NotFound(notFoundResponse);
				}
				throw;
			}
			catch (Exception ex)
			{
				var errorResponse = _mappingService.CreateErrorResponse<string>(
					"Failed to update invoice", new List<string> { ex.Message });
				return StatusCode(500, errorResponse);
			}
		}

		// DELETE: api/Invoice/5
		[HttpDelete("{id}")]
		public async Task<ActionResult<ApiResponseDto<string>>> DeleteInvoice(int id)
		{
			try
			{
				var invoice = await _context.Invoices
					.Include(i => i.InvoiceMaterials)
					.FirstOrDefaultAsync(i => i.InvoiceId == id);

				if (invoice == null)
				{
					var notFoundResponse = _mappingService.CreateErrorResponse<string>(
						$"Invoice with ID {id} not found");
					return NotFound(notFoundResponse);
				}

				_context.Invoices.Remove(invoice);
				await _context.SaveChangesAsync();

				var response = _mappingService.CreateSuccessResponse("Invoice deleted successfully");
				return Ok(response);
			}
			catch (Exception ex)
			{
				var errorResponse = _mappingService.CreateErrorResponse<string>(
					"Failed to delete invoice", new List<string> { ex.Message });
				return StatusCode(500, errorResponse);
			}
		}

		// GET: api/Invoice/customer/{customerCode}
		[HttpGet("customer/{customerCode}")]
		public async Task<ActionResult<ApiResponseDto<List<InvoiceListItemDto>>>> GetInvoicesByCustomer(string customerCode)
		{
			try
			{
				var invoices = await _context.Invoices
					.Include(i => i.InvoiceMaterials)
					.Where(i => i.BillToCode == customerCode || i.ShipToCode == customerCode)
					.OrderByDescending(i => i.InvoiceDate)
					.ToListAsync();

				var invoiceDtos = invoices.Select(_mappingService.MapToListItemDto).ToList();
				var response = _mappingService.CreateSuccessResponse(invoiceDtos,
					$"Retrieved {invoiceDtos.Count} invoices for customer {customerCode}");

				return Ok(response);
			}
			catch (Exception ex)
			{
				var errorResponse = _mappingService.CreateErrorResponse<List<InvoiceListItemDto>>(
					"Failed to retrieve customer invoices", new List<string> { ex.Message });
				return StatusCode(500, errorResponse);
			}
		}

		// GET: api/Invoice/summary
		[HttpGet("summary")]
		public async Task<ActionResult<ApiResponseDto<InvoiceSummaryResponseDto>>> GetInvoiceSummary()
		{
			try
			{
				var totalInvoices = await _context.Invoices.CountAsync();
				var totalAmount = await _context.Invoices.SumAsync(i => i.TotalAmount);
				var todayInvoices = await _context.Invoices
					.Where(i => i.InvoiceDate.Date == DateTime.Today)
					.CountAsync();
				var monthlyInvoices = await _context.Invoices
					.Where(i => i.InvoiceDate.Month == DateTime.Now.Month &&
							   i.InvoiceDate.Year == DateTime.Now.Year)
					.CountAsync();

				var todayAmount = await _context.Invoices
					.Where(i => i.InvoiceDate.Date == DateTime.Today)
					.SumAsync(i => i.TotalAmount);

				var monthlyAmount = await _context.Invoices
					.Where(i => i.InvoiceDate.Month == DateTime.Now.Month &&
							   i.InvoiceDate.Year == DateTime.Now.Year)
					.SumAsync(i => i.TotalAmount);

				var lastInvoice = await _context.Invoices
					.OrderByDescending(i => i.InvoiceDate)
					.FirstOrDefaultAsync();

				var summary = new InvoiceSummaryResponseDto
				{
					TotalInvoices = totalInvoices,
					TotalAmount = totalAmount,
					TodayInvoices = todayInvoices,
					MonthlyInvoices = monthlyInvoices,
					AverageInvoiceAmount = totalInvoices > 0 ? totalAmount / totalInvoices : 0,
					TodayAmount = todayAmount,
					MonthlyAmount = monthlyAmount,
					LastInvoiceDate = lastInvoice?.InvoiceDate ?? DateTime.MinValue,
					LastInvoiceNo = lastInvoice?.InvoiceNo
				};

				var response = _mappingService.CreateSuccessResponse(summary, "Invoice summary retrieved successfully");
				return Ok(response);
			}
			catch (Exception ex)
			{
				var errorResponse = _mappingService.CreateErrorResponse<InvoiceSummaryResponseDto>(
					"Failed to retrieve invoice summary", new List<string> { ex.Message });
				return StatusCode(500, errorResponse);
			}
		}

		private bool InvoiceExists(int id)
		{
			return _context.Invoices.Any(e => e.InvoiceId == id);
		}
	}
}