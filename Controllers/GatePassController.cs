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
				var groupedData = await _context.Invoices
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
					.Where(m => !string.IsNullOrEmpty(m.CratesCode))
					.ToDictionary(m => m.material3partycode, m => m.CratesCode);

				// Group by customer and sum quantities for small and large crates
				var customerDetails = invoiceData
					.GroupBy(i => new { i.ShipToCode, i.ShipToName })
					.Select(g => new GatePassCustomerDetail
					{
						CustomerName = g.Key.ShipToName,
						SmallCrates = g.SelectMany(i => i.InvoiceMaterials)
									  .Where(m => materialCratesMap.ContainsKey(m.MaterialSapCode) && 
												  materialCratesMap[m.MaterialSapCode] == "S")
									  .Sum(m => m.QuantityCases),
						LargeCrates = g.SelectMany(i => i.InvoiceMaterials)
									  .Where(m => materialCratesMap.ContainsKey(m.MaterialSapCode) && 
												  materialCratesMap[m.MaterialSapCode] == "L")
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