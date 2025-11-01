using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Milk_Bakery.Data;
using Milk_Bakery.Models;
using Milk_Bakery.ViewModels;
using AspNetCoreHero.ToastNotification.Abstractions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using OfficeOpenXml;
using System.IO;

namespace Milk_Bakery.Controllers
{
	[Authentication]
	public class OrderReconciliationController : Controller
	{
		private readonly MilkDbContext _context;
		public INotyfService _notifyService { get; }

		public OrderReconciliationController(MilkDbContext context, INotyfService notifyService)
		{
			_context = context;
			_notifyService = notifyService;
			// Set the license context for EPPlus
			ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
		}

		// GET: OrderReconciliation
		public async Task<IActionResult> Index()
		{
			var viewModel = new DealerOrdersViewModel();

			// Get available distributors based on user role
			viewModel.AvailableDistributors = await GetAvailableDistributors();

			return View(viewModel);
		}

		// GET: OrderReconciliation/GetAllCustomers
		[HttpGet]
		public async Task<IActionResult> GetAllCustomers()
		{
			try
			{
				var customers = await GetAvailableDistributors();
				var customerList = customers.Select(c => new { id = c.Id, name = c.Name }).ToList();
				return Json(new { success = true, customers = customerList });
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = ex.Message });
			}
		}

		// GET: OrderReconciliation/GetReconciliationData
		[HttpGet]
		public async Task<IActionResult> GetReconciliationData(DateTime? date, int? customerId)
		{
			try
			{
				var orderDate = date ?? DateTime.Now.Date;

				// Get dealer orders for the specified date
				var dealerOrdersQuery = _context.DealerOrders
					.Include(o => o.Dealer)
					.Where(o => o.OrderDate == orderDate);

				// Filter by customer if specified
				if (customerId.HasValue && customerId.Value > 0)
				{
					dealerOrdersQuery = dealerOrdersQuery.Where(o => o.DistributorId == customerId.Value);
				}

				var dealerOrders = await dealerOrdersQuery.ToListAsync();

				// Group by material and sum quantities across all dealers
				var consolidatedData = new Dictionary<string, int>(); // material -> total quantity
				var materialDetails = new Dictionary<string, object>(); // material -> details (shortCode, sapCode, unit)

				// Collect dealer details for display
				var dealerDetails = new List<object>();

				foreach (var order in dealerOrders)
				{
					// Get order items for this order
					var orderItems = await _context.DealerOrderItems
						.Where(i => i.DealerOrderId == order.Id)
						.ToListAsync();

					// Add dealer details
					var dealerMaterials = new List<object>();
					foreach (var item in orderItems)
					{
						var materialName = item.MaterialName ?? "";

						// Add to consolidated data
						if (consolidatedData.ContainsKey(materialName))
						{
							consolidatedData[materialName] += item.Qty;
						}
						else
						{
							consolidatedData[materialName] = item.Qty;

							// Store material details (will be the same for all entries of this material)
							materialDetails[materialName] = new
							{
								shortCode = item.ShortCode ?? "",
								sapCode = item.SapCode ?? "",
								unit = "PCS" // Default unit, could be enhanced to get from MaterialMaster
							};
						}

						// Add material to dealer details
						dealerMaterials.Add(new
						{
							materialName = materialName,
							quantity = item.Qty
						});
					}

					// Add dealer to dealer details list
					dealerDetails.Add(new
					{
						dealerName = order.Dealer?.Name ?? "Unknown Dealer",
						orderDate = order.OrderDate.ToString("dd/MM/yyyy"),
						materials = dealerMaterials
					});
				}

				// Get conversion data for all materials
				var conversionData = await _context.ConversionTables.ToListAsync();
				var conversionDict = conversionData.ToDictionary(c => c.MaterialName, c => c);

				// Create result data with detailed crate calculations
				var resultData = new List<object>();

				foreach (var kvp in consolidatedData)
				{
					var materialName = kvp.Key;
					var totalQuantity = kvp.Value;

					var materialInfo = materialDetails.ContainsKey(materialName) ?
						materialDetails[materialName] as dynamic :
						new { shortCode = "", sapCode = "", unit = "PCS" };

					// Calculate crates based on conversion data
					var crates = 0;
					var itemsInCrates = 0;
					var leftoverItems = 0;
					var unitType = "PCS";
					var unitQuantity = 1;
					var totalQuantityPerUnit = 1;

					if (conversionDict.ContainsKey(materialName))
					{
						var conversion = conversionDict[materialName];
						unitType = conversion.UnitType;
						unitQuantity = conversion.UnitQuantity;
						totalQuantityPerUnit = conversion.TotalQuantity;

						// Calculate crates (total quantity / items per crate)
						if (totalQuantityPerUnit > 0)
						{
							crates = totalQuantity / totalQuantityPerUnit;
							itemsInCrates = crates * totalQuantityPerUnit;
							leftoverItems = totalQuantity % totalQuantityPerUnit;
						}
					}
					else
					{
						// If no conversion data, treat each item as a separate unit
						crates = totalQuantity;
						itemsInCrates = totalQuantity;
						leftoverItems = 0;
					}

					resultData.Add(new
					{
						materialName = materialName,
						shortCode = materialInfo.shortCode,
						sapCode = materialInfo.sapCode,
						quantity = totalQuantity,
						crates = crates,
						itemsInCrates = itemsInCrates,
						leftoverItems = leftoverItems,
						unitType = unitType,
						unitQuantity = unitQuantity,
						totalQuantityPerUnit = totalQuantityPerUnit
					});
				}

				// Sort by material name for consistent ordering
				resultData = resultData.OrderBy(item => ((dynamic)item).materialName).ToList();

				// Calculate grand totals
				var grandTotal = consolidatedData.Values.Sum();
				var totalCrates = resultData.Sum(item => (int)((dynamic)item).crates);
				var totalItemsPerCrate = resultData.Sum(item => (int)((dynamic)item).totalQuantityPerUnit);
				var totalItemsInCrates = resultData.Sum(item => (int)((dynamic)item).itemsInCrates);
				var totalLeftoverItems = resultData.Sum(item => (int)((dynamic)item).leftoverItems);

				return Json(new
				{
					success = true,
					data = resultData,
					dealerDetails = dealerDetails,
					grandTotal = grandTotal,
					totalCrates = totalCrates,
					totalItemsPerCrate = totalItemsPerCrate,
					totalItemsInCrates = totalItemsInCrates,
					totalLeftoverItems = totalLeftoverItems,
					orderDate = orderDate.ToString("dd/MM/yyyy")
				});
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = ex.Message });
			}
		}

		// POST: OrderReconciliation/SaveReconciliationData
        [HttpPost]
        public async Task<IActionResult> SaveReconciliationData([FromBody] ReconciliationSaveModel model)
        {
            try
            {
                var orderDate = DateTime.Parse(model.Date);
                
                // Process each adjustment
                foreach (var adjustment in model.Adjustments)
                {
                    // Get all dealer orders for this material on the specified date and customer
                    var dealerOrders = await _context.DealerOrders
                        .Include(o => o.DealerOrderItems)
                        .Where(o => o.OrderDate == orderDate && o.DistributorId == model.CustomerId)
                        .ToListAsync();
                    
                    // Update each dealer order item for this material
                    foreach (var order in dealerOrders)
                    {
                        var orderItem = order.DealerOrderItems
                            .FirstOrDefault(i => i.ShortCode == adjustment.ShortCode);
                        
                        if (orderItem != null)
                        {
                            // Update the quantity based on received crates
                            // New quantity = received crates * items per crate
                            orderItem.Qty = adjustment.ReceivedCrates * adjustment.ItemsPerCrate;
                            orderItem.DeliverQnty = adjustment.ReceivedCrates * adjustment.ItemsPerCrate;
                            
                            _context.DealerOrderItems.Update(orderItem);
                        }
                    }
                }
                
                // Save all changes
                await _context.SaveChangesAsync();
                
                return Json(new { success = true, message = "Reconciliation data saved successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

		// GET: OrderReconciliation/ExportToExcel
		[HttpGet]
		public async Task<IActionResult> ExportToExcel(DateTime? date, int? customerId)
		{
			try
			{
				var orderDate = date ?? DateTime.Now.Date;

				// Get dealer orders for the specified date
				var dealerOrdersQuery = _context.DealerOrders
					.Include(o => o.Dealer)
					.Where(o => o.OrderDate == orderDate);

				// Filter by customer if specified
				if (customerId.HasValue && customerId.Value > 0)
				{
					dealerOrdersQuery = dealerOrdersQuery.Where(o => o.DistributorId == customerId.Value);
				}

				var dealerOrders = await dealerOrdersQuery.ToListAsync();

				// Group by material and sum quantities across all dealers
				var consolidatedData = new Dictionary<string, int>(); // material -> total quantity
				var materialDetails = new Dictionary<string, object>(); // material -> details (shortCode, sapCode, unit)

				foreach (var order in dealerOrders)
				{
					// Get order items for this order
					var orderItems = await _context.DealerOrderItems
						.Where(i => i.DealerOrderId == order.Id)
						.ToListAsync();

					foreach (var item in orderItems)
					{
						var materialName = item.MaterialName ?? "";

						// Add to consolidated data
						if (consolidatedData.ContainsKey(materialName))
						{
							consolidatedData[materialName] += item.Qty;
						}
						else
						{
							consolidatedData[materialName] = item.Qty;

							// Store material details (will be the same for all entries of this material)
							materialDetails[materialName] = new
							{
								shortCode = item.ShortCode ?? "",
								sapCode = item.SapCode ?? "",
								unit = "PCS" // Default unit, could be enhanced to get from MaterialMaster
							};
						}
					}
				}

				// Get conversion data for all materials
				var conversionData = await _context.ConversionTables.ToListAsync();
				var conversionDict = conversionData.ToDictionary(c => c.MaterialName, c => c);

				// Create result data with detailed crate calculations
				var resultData = new List<dynamic>();

				foreach (var kvp in consolidatedData)
				{
					var materialName = kvp.Key;
					var totalQuantity = kvp.Value;

					var materialInfo = materialDetails.ContainsKey(materialName) ?
						materialDetails[materialName] as dynamic :
						new { shortCode = "", sapCode = "", unit = "PCS" };

					// Calculate crates based on conversion data
					var crates = 0;
					var itemsInCrates = 0;
					var leftoverItems = 0;
					var unitType = "PCS";
					var unitQuantity = 1;
					var totalQuantityPerUnit = 1;

					if (conversionDict.ContainsKey(materialName))
					{
						var conversion = conversionDict[materialName];
						unitType = conversion.UnitType;
						unitQuantity = conversion.UnitQuantity;
						totalQuantityPerUnit = conversion.TotalQuantity;

						// Calculate crates (total quantity / items per crate)
						if (totalQuantityPerUnit > 0)
						{
							crates = totalQuantity / totalQuantityPerUnit;
							itemsInCrates = crates * totalQuantityPerUnit;
							leftoverItems = totalQuantity % totalQuantityPerUnit;
						}
					}
					else
					{
						// If no conversion data, treat each item as a separate unit
						crates = totalQuantity;
						itemsInCrates = totalQuantity;
						leftoverItems = 0;
					}

					resultData.Add(new
					{
						materialName = materialName,
						shortCode = materialInfo.shortCode,
						sapCode = materialInfo.sapCode,
						quantity = totalQuantity,
						crates = crates,
						itemsInCrates = itemsInCrates,
						leftoverItems = leftoverItems,
						unitType = unitType,
						unitQuantity = unitQuantity,
						totalQuantityPerUnit = totalQuantityPerUnit
					});
				}

				// Sort by material name for consistent ordering
				resultData = resultData.OrderBy(item => item.materialName).ToList();

				// Calculate grand totals
				var grandTotal = consolidatedData.Values.Sum();
				var totalCrates = resultData.Sum(item => (int)item.crates);
				var totalItemsInCrates = resultData.Sum(item => (int)item.itemsInCrates);
				var totalLeftoverItems = resultData.Sum(item => (int)item.leftoverItems);

				// Create Excel file
				using (var package = new ExcelPackage())
				{
					var worksheet = package.Workbook.Worksheets.Add("Order Reconciliation");

					// Add headers
					worksheet.Cells[1, 1].Value = "Material Name";
					worksheet.Cells[1, 2].Value = "Short Code";
					worksheet.Cells[1, 3].Value = "SAP Code";
					worksheet.Cells[1, 4].Value = "Ordered Quantity";
					worksheet.Cells[1, 5].Value = "Unit Type";
					worksheet.Cells[1, 6].Value = "Items per Crate";
					worksheet.Cells[1, 7].Value = "Total Crates";
					worksheet.Cells[1, 8].Value = "Items in Crates";
					worksheet.Cells[1, 9].Value = "Actual Received Crates";
					worksheet.Cells[1, 10].Value = "Variance";

					// Add data rows
					for (int i = 0; i < resultData.Count; i++)
					{
						var rowData = resultData[i];

						worksheet.Cells[i + 2, 1].Value = rowData.materialName;
						worksheet.Cells[i + 2, 2].Value = rowData.shortCode;
						worksheet.Cells[i + 2, 3].Value = rowData.sapCode;
						worksheet.Cells[i + 2, 4].Value = rowData.quantity;
						worksheet.Cells[i + 2, 5].Value = rowData.unitType;
						worksheet.Cells[i + 2, 6].Value = rowData.totalQuantityPerUnit;
						worksheet.Cells[i + 2, 7].Value = rowData.crates;
						worksheet.Cells[i + 2, 8].Value = rowData.itemsInCrates;
						worksheet.Cells[i + 2, 9].Value = rowData.crates; // Default to ordered crates
						worksheet.Cells[i + 2, 10].Value = 0; // Default variance is 0

						// Add some basic styling
						worksheet.Cells[i + 2, 7].Style.Font.Bold = true;
						worksheet.Cells[i + 2, 9].Style.Font.Bold = true;
					}

					// Add total row
					var totalRow = resultData.Count + 2;
					worksheet.Cells[totalRow, 1].Value = "Grand Total";
					worksheet.Cells[totalRow, 4].Value = grandTotal;
					worksheet.Cells[totalRow, 6].Value = "";
					worksheet.Cells[totalRow, 7].Value = totalCrates;
					worksheet.Cells[totalRow, 8].Value = totalItemsInCrates;
					worksheet.Cells[totalRow, 9].Value = totalCrates; // Default to total crates
					worksheet.Cells[totalRow, 10].Value = 0; // Default variance is 0

					// Format the total row
					worksheet.Cells[totalRow, 1, totalRow, 10].Style.Font.Bold = true;
					worksheet.Cells[totalRow, 4].Style.Numberformat.Format = "#,##0";
					worksheet.Cells[totalRow, 7].Style.Numberformat.Format = "#,##0";
					worksheet.Cells[totalRow, 8].Style.Numberformat.Format = "#,##0";
					worksheet.Cells[totalRow, 9].Style.Numberformat.Format = "#,##0";
					worksheet.Cells[totalRow, 10].Style.Numberformat.Format = "#,##0";

					// Auto-fit columns
					worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

					// Convert to bytes
					var fileBytes = package.GetAsByteArray();

					// Return file
					var fileName = $"OrderReconciliation_{orderDate:yyyyMMdd}.xlsx";
					return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
				}
			}
			catch (Exception ex)
			{
				_notifyService.Error("Error exporting to Excel: " + ex.Message);
				return RedirectToAction("Index");
			}
		}

		// Helper method to get available distributors based on user role
		private async Task<List<Customer_Master>> GetAvailableDistributors()
		{
			var role = HttpContext.Session.GetString("role");
			var userName = HttpContext.Session.GetString("UserName");

			if (role == "Customer")
			{
				// For customer role, get the logged-in customer and their mapped customers
				var loggedInCustomer = await _context.Customer_Master
					.FirstOrDefaultAsync(c => c.phoneno == userName);

				if (loggedInCustomer != null)
				{
					var customers = new List<Customer_Master> { loggedInCustomer };

					// Get mapped customers
					var mappedCustomer = await _context.Cust2CustMap
						.FirstOrDefaultAsync(c => c.phoneno == userName);

					if (mappedCustomer != null)
					{
						var mappedCusts = await _context.mappedcusts
							.Where(mc => mc.cust2custId == mappedCustomer.id)
							.ToListAsync();

						foreach (var mapped in mappedCusts)
						{
							var customer = await _context.Customer_Master
								.FirstOrDefaultAsync(c => c.Name == mapped.customer);
							if (customer != null)
							{
								customers.Add(customer);
							}
						}
					}

					return customers;
				}
			}
			else if (role == "Sales")
			{
				// For sales role, get mapped customers
				var empToCustMap = await _context.EmpToCustMap
					.FirstOrDefaultAsync(e => e.empl == userName);

				if (empToCustMap != null)
				{
					var mappedCusts = await _context.mappedcusts
						.Where(mc => mc.cust2custId == empToCustMap.id)
						.ToListAsync();

					var customers = new List<Customer_Master>();
					foreach (var mapped in mappedCusts)
					{
						var customer = await _context.Customer_Master
							.FirstOrDefaultAsync(c => c.Name == mapped.customer);
						if (customer != null)
						{
							customers.Add(customer);
						}
					}

					return customers;
				}
			}
			else
			{
				// For admin role, get all customers
				return await _context.Customer_Master.ToListAsync();
			}

			return new List<Customer_Master>();
		}
	}
}