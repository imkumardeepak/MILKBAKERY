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
    public class InvoiceMaterialController : ControllerBase
    {
        private readonly MilkDbContext _context;
        private readonly IInvoiceMappingService _mappingService;

        public InvoiceMaterialController(MilkDbContext context, IInvoiceMappingService mappingService)
        {
            _context = context;
            _mappingService = mappingService;
        }

        // GET: api/InvoiceMaterial
        [HttpGet]
        public async Task<ActionResult<ApiResponseDto<List<InvoiceMaterialResponseDto>>>> GetInvoiceMaterials()
        {
            try
            {
                var materials = await _context.InvoiceMaterials
                    .Include(m => m.Invoice)
                    .ToListAsync();

                var materialDtos = materials.Select(_mappingService.MapToResponseDto).ToList();
                var response = _mappingService.CreateSuccessResponse(materialDtos, "Invoice materials retrieved successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                var errorResponse = _mappingService.CreateErrorResponse<List<InvoiceMaterialResponseDto>>(
                    "Failed to retrieve invoice materials", new List<string> { ex.Message });
                return StatusCode(500, errorResponse);
            }
        }

        // GET: api/InvoiceMaterial/5
        [HttpGet("{id}")]
        public async Task<ActionResult<InvoiceMaterialDetail>> GetInvoiceMaterial(int id)
        {
            try
            {
                var material = await _context.InvoiceMaterials
                    .Include(m => m.Invoice)
                    .FirstOrDefaultAsync(m => m.MaterialId == id);

                if (material == null)
                {
                    return NotFound(new { message = $"Invoice material with ID {id} not found" });
                }

                return Ok(material);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        // GET: api/InvoiceMaterial/invoice/{invoiceId}
        [HttpGet("invoice/{invoiceId}")]
        public async Task<ActionResult<IEnumerable<InvoiceMaterialDetail>>> GetMaterialsByInvoice(int invoiceId)
        {
            try
            {
                var materials = await _context.InvoiceMaterials
                    .Where(m => m.InvoiceId == invoiceId)
                    .ToListAsync();

                return Ok(materials);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        // GET: api/InvoiceMaterial/search?materialCode=MAT001
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<InvoiceMaterialDetail>>> SearchMaterials(
            [FromQuery] string? materialCode = null,
            [FromQuery] string? productDescription = null,
            [FromQuery] string? batch = null)
        {
            try
            {
                var query = _context.InvoiceMaterials.Include(m => m.Invoice).AsQueryable();

                if (!string.IsNullOrEmpty(materialCode))
                {
                    query = query.Where(m => m.MaterialSapCode.Contains(materialCode));
                }

                if (!string.IsNullOrEmpty(productDescription))
                {
                    query = query.Where(m => m.ProductDescription.Contains(productDescription));
                }

                if (!string.IsNullOrEmpty(batch))
                {
                    query = query.Where(m => m.Batch != null && m.Batch.Contains(batch));
                }

                var materials = await query.ToListAsync();
                return Ok(materials);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        // POST: api/InvoiceMaterial
        [HttpPost]
        public async Task<ActionResult<ApiResponseDto<InvoiceMaterialResponseDto>>> CreateInvoiceMaterial(CreateInvoiceMaterialRequestDto materialDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var validationErrors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    var validationResponse = _mappingService.CreateErrorResponse<InvoiceMaterialResponseDto>(
                        "Validation failed", validationErrors);
                    return BadRequest(validationResponse);
                }

                // Verify that the invoice exists
                var invoiceExists = await _context.Invoices.AnyAsync(i => i.InvoiceId == materialDto.InvoiceId);
                if (!invoiceExists)
                {
                    var notFoundResponse = _mappingService.CreateErrorResponse<InvoiceMaterialResponseDto>(
                        $"Invoice with ID {materialDto.InvoiceId} does not exist");
                    return BadRequest(notFoundResponse);
                }

                var material = _mappingService.MapToEntity(materialDto);
                _context.InvoiceMaterials.Add(material);
                await _context.SaveChangesAsync();

                var materialResponseDto = _mappingService.MapToResponseDto(material);
                var response = _mappingService.CreateSuccessResponse(materialResponseDto, "Invoice material created successfully");

                return CreatedAtAction(nameof(GetInvoiceMaterial), 
                    new { id = material.MaterialId }, response);
            }
            catch (Exception ex)
            {
                var errorResponse = _mappingService.CreateErrorResponse<InvoiceMaterialResponseDto>(
                    "Failed to create invoice material", new List<string> { ex.Message });
                return StatusCode(500, errorResponse);
            }
        }

        // POST: api/InvoiceMaterial/batch
        [HttpPost("batch")]
        public async Task<ActionResult<IEnumerable<InvoiceMaterialDetail>>> CreateInvoiceMaterialsBatch(
            IEnumerable<InvoiceMaterialDetail> materials)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var materialsList = materials.ToList();
                
                // Verify that all invoices exist
                var invoiceIds = materialsList.Select(m => m.InvoiceId).Distinct();
                var existingInvoiceIds = await _context.Invoices
                    .Where(i => invoiceIds.Contains(i.InvoiceId))
                    .Select(i => i.InvoiceId)
                    .ToListAsync();

                var missingInvoiceIds = invoiceIds.Except(existingInvoiceIds);
                if (missingInvoiceIds.Any())
                {
                    return BadRequest(new { 
                        message = "Some invoices do not exist", 
                        missingInvoiceIds = missingInvoiceIds 
                    });
                }

                _context.InvoiceMaterials.AddRange(materialsList);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetInvoiceMaterials), materialsList);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        // PUT: api/InvoiceMaterial/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateInvoiceMaterial(int id, InvoiceMaterialDetail material)
        {
            try
            {
                if (id != material.MaterialId)
                {
                    return BadRequest(new { message = "Material ID mismatch" });
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var existingMaterial = await _context.InvoiceMaterials.FindAsync(id);
                if (existingMaterial == null)
                {
                    return NotFound(new { message = $"Invoice material with ID {id} not found" });
                }

                // Verify that the invoice exists
                var invoiceExists = await _context.Invoices.AnyAsync(i => i.InvoiceId == material.InvoiceId);
                if (!invoiceExists)
                {
                    return BadRequest(new { message = $"Invoice with ID {material.InvoiceId} does not exist" });
                }

                // Update properties
                existingMaterial.InvoiceId = material.InvoiceId;
                existingMaterial.ProductDescription = material.ProductDescription;
                existingMaterial.MaterialSapCode = material.MaterialSapCode;
                existingMaterial.Batch = material.Batch;
                existingMaterial.UnitPerCase = material.UnitPerCase;
                existingMaterial.QuantityCases = material.QuantityCases;
                existingMaterial.QuantityUnits = material.QuantityUnits;
                existingMaterial.UOM = material.UOM;

                await _context.SaveChangesAsync();
                return Ok(new { message = "Invoice material updated successfully" });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!InvoiceMaterialExists(id))
                {
                    return NotFound(new { message = $"Invoice material with ID {id} not found" });
                }
                throw;
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        // DELETE: api/InvoiceMaterial/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteInvoiceMaterial(int id)
        {
            try
            {
                var material = await _context.InvoiceMaterials.FindAsync(id);
                if (material == null)
                {
                    return NotFound(new { message = $"Invoice material with ID {id} not found" });
                }

                _context.InvoiceMaterials.Remove(material);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Invoice material deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        // DELETE: api/InvoiceMaterial/invoice/{invoiceId}
        [HttpDelete("invoice/{invoiceId}")]
        public async Task<IActionResult> DeleteMaterialsByInvoice(int invoiceId)
        {
            try
            {
                var materials = await _context.InvoiceMaterials
                    .Where(m => m.InvoiceId == invoiceId)
                    .ToListAsync();

                if (!materials.Any())
                {
                    return NotFound(new { message = $"No materials found for invoice ID {invoiceId}" });
                }

                _context.InvoiceMaterials.RemoveRange(materials);
                await _context.SaveChangesAsync();

                return Ok(new { message = $"All materials for invoice {invoiceId} deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        // GET: api/InvoiceMaterial/summary/invoice/{invoiceId}
        [HttpGet("summary/invoice/{invoiceId}")]
        public async Task<ActionResult> GetInvoiceMaterialSummary(int invoiceId)
        {
            try
            {
                var totalMaterials = await _context.InvoiceMaterials
                    .Where(m => m.InvoiceId == invoiceId)
                    .CountAsync();

                var totalCases = await _context.InvoiceMaterials
                    .Where(m => m.InvoiceId == invoiceId)
                    .SumAsync(m => m.QuantityCases);

                var totalUnits = await _context.InvoiceMaterials
                    .Where(m => m.InvoiceId == invoiceId)
                    .SumAsync(m => m.QuantityUnits);

                var summary = new
                {
                    InvoiceId = invoiceId,
                    TotalMaterials = totalMaterials,
                    TotalCases = totalCases,
                    TotalUnits = totalUnits
                };

                return Ok(summary);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        private bool InvoiceMaterialExists(int id)
        {
            return _context.InvoiceMaterials.Any(e => e.MaterialId == id);
        }
    }
}