using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Milk_Bakery.Data;
using Milk_Bakery.Models;
using Milk_Bakery.ViewModels;
using System.Diagnostics;

namespace Milk_Bakery.Controllers
{
	public class GatePassController : Controller
	{
		private readonly MilkDbContext _context;
		private readonly ILogger<GatePassController> _logger;

		public GatePassController(MilkDbContext context, ILogger<GatePassController> logger)
		{
			_context = context;
			_logger = logger;
		}

		public async Task<IActionResult> Index()
		{
			try
			{
				// Group data by truck number and date, and count customers
				var groupedData = await _context.Invoices.Where(c => c.InvoiceDate.Date <= DateTime.Now.Date && c.InvoiceDate.Date >= DateTime.Now.Date.AddDays(-1))
					.Include(c => c.InvoiceMaterials)
					.GroupBy(c => new { c.ShipToCode, c.InvoiceDate, c.ShipToRoute }) // Group by truck (route) and date
					.Select(g => new GatePassGroupedData
					{
						TruckNumber = g.First().VehicleNo,
						DispatchDate = g.Key.InvoiceDate,
						CustomerCount = g.Count(),
					})
					.OrderBy(g => g.TruckNumber)
					.ToListAsync();

				var viewModel = new GatePassIndexViewModel
				{
					GroupedData = groupedData
				};

				return View(viewModel);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error occurred while fetching gate pass data");
				return View(new GatePassIndexViewModel());
			}
		}

		// GET: GatePass/GetGatePassDataByDate
		[HttpGet]
		public async Task<IActionResult> GetGatePassDataByDate(string date)
		{
			try
			{
				_logger.LogInformation("GetGatePassDataByDate called with date parameter: {Date}", date);
				
				DateTime filterDate;
				if (string.IsNullOrEmpty(date))
				{
					filterDate = DateTime.Now.Date;
					_logger.LogInformation("No date provided, using today's date: {FilterDate}", filterDate);
				}
				else
				{
					_logger.LogInformation("Attempting to parse date: {Date}", date);
					
					// Try multiple date formats
					string[] formats = { "yyyy-MM-dd", "dd/MM/yyyy", "M/d/yyyy", "MM/dd/yyyy" };
					if (!DateTime.TryParseExact(date, formats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out filterDate))
					{
						// Try general parsing as fallback
						if (!DateTime.TryParse(date, out filterDate))
						{
							_logger.LogWarning("Failed to parse date: {Date}. Using today's date instead.", date);
							filterDate = DateTime.Now.Date;
						}
						else
						{
							_logger.LogInformation("Parsed date using general parsing: {FilterDate}", filterDate);
						}
					}
					else
					{
						_logger.LogInformation("Parsed date using exact format: {FilterDate}", filterDate);
					}
				}

				_logger.LogInformation("Filtering invoices for date: {FilterDate}", filterDate);

				// Group data by truck number and date, and count customers
				var groupedData = await _context.Invoices
					.Include(c => c.InvoiceMaterials)
					.Where(c => c.InvoiceDate.Date == filterDate.Date) // Filter by selected date
					.GroupBy(c => new { c.ShipToCode, c.InvoiceDate, c.ShipToRoute }) // Group by truck (route) and date
					.Select(g => new GatePassGroupedData
					{
						TruckNumber = g.First().VehicleNo ?? "Unknown",
						DispatchDate = g.Key.InvoiceDate,
						CustomerCount = g.Count(),
					})
					.OrderBy(g => g.TruckNumber)
					.ToListAsync();

				_logger.LogInformation("Found {Count} gate pass records", groupedData.Count);

				// Convert to JSON-friendly format
				var jsonData = groupedData.Select(g => new
				{
					truckNumber = g.TruckNumber,
					dispatchDate = g.DispatchDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"), // ISO format for JavaScript
					customerCount = g.CustomerCount
				}).ToList();

				return Json(new { success = true, data = jsonData });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error occurred while fetching gate pass data for date {Date}", date);
				return Json(new { success = false, message = ex.Message });
			}
		}

		public async Task<IActionResult> PrintGatePass(string truckNumber, DateTime date)
		{
			try
			{
				// Get invoice data for the specific truck and date
				// Include InvoiceMaterials to get material details
				var invoiceData = await _context.Invoices
					.Include(i => i.InvoiceMaterials)
					.Where(i => i.VehicleNo == truckNumber && i.InvoiceDate.Date == date.Date)
					.ToListAsync();

				// Get all material masters to map crates codes
				var materialMasters = await _context.MaterialMaster.ToListAsync();

				// Create a dictionary for quick lookup of crates codes by material SAP code
				var materialCratesMap = materialMasters
					.Where(m => !string.IsNullOrEmpty(m.CratesTypes))
					.ToDictionary(m => m.material3partycode, m => m.CratesTypes);

				// Create dictionaries to store crate type classifications
				var smallCratesTypes = new Dictionary<string, bool>();
				var largeCratesTypes = new Dictionary<string, bool>();

				// Populate dictionaries based on crate type names
				foreach (var crateType in materialCratesMap.Values.Distinct())
				{
					if (crateType.Contains("Small", StringComparison.OrdinalIgnoreCase))
					{
						smallCratesTypes[crateType] = true;
					}
					else if (crateType.Contains("Large", StringComparison.OrdinalIgnoreCase))
					{
						largeCratesTypes[crateType] = true;
					}
				}

				// Group by customer and sum quantities for small and large crates
				var customerDetails = invoiceData
					.GroupBy(i => new { i.ShipToCode, i.ShipToName })
					.Select(g => new GatePassCustomerDetail
					{
						CustomerName = g.Key.ShipToName,
						SmallCrates = g.SelectMany(i => i.InvoiceMaterials)
									  .Where(m => materialCratesMap.ContainsKey(m.MaterialSapCode) &&
												  smallCratesTypes.ContainsKey(materialCratesMap[m.MaterialSapCode]))
									  .Sum(m => m.QuantityCases),
						LargeCrates = g.SelectMany(i => i.InvoiceMaterials)
									  .Where(m => materialCratesMap.ContainsKey(m.MaterialSapCode) &&
												  largeCratesTypes.ContainsKey(materialCratesMap[m.MaterialSapCode]))
									  .Sum(m => m.QuantityCases)
					})
					.ToList();

				var viewModel = new GatePassViewModel
				{
					TruckNumber = truckNumber,
					DispatchDate = date,
					CustomerCount = customerDetails.Count,
					CustomerDetails = customerDetails
				};

				return View(viewModel);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error occurred while generating gate pass for truck {TruckNumber} on {Date}", truckNumber, date);
				return RedirectToAction(nameof(Index));
			}
		}
	}
}