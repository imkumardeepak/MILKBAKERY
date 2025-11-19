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

		// GET: OrderReconciliation/DispatchRouteSheet
		public async Task<IActionResult> DispatchRouteSheet()
		{
			return View();
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

		// GET: OrderReconciliation/GetDispatchData
		[HttpGet]
		public async Task<IActionResult> GetDispatchData(DateTime? date, int? customerId)
		{
			try
			{
				if (customerId == null)
				{
					return Json(new { success = false, message = "Please provide either a date or a customer." });
				}

				var dispatchDate = date ?? DateTime.Now.Date;

				// Get dealer orders for the specified date
				var dealerOrdersQuery = _context.DealerOrders
					.Include(o => o.Dealer)
					.Where(o => o.OrderDate == dispatchDate);

				// Filter by customer if specified
				if (customerId.HasValue && customerId.Value > 0)
				{
					dealerOrdersQuery = dealerOrdersQuery.Where(o => o.DistributorId == customerId.Value);
				}

				var dealerOrders = await dealerOrdersQuery.ToListAsync();

				// Get all unique short codes for column headers
				var shortCodes = new List<string>();
				// Changed to store quantity and amount data
				var dealerData = new Dictionary<string, Dictionary<string, int>>(); // dealer -> shortCode -> quantity
				var dealerAmountData = new Dictionary<string, Dictionary<string, decimal>>(); // dealer -> shortCode -> amount
				var dealerRoutes = new Dictionary<string, string>(); // dealer -> route code
				var dealerPhones = new Dictionary<string, string>(); // dealer -> phone
				var dealerBalances = new Dictionary<string, decimal>(); // dealer -> balance amount

				foreach (var order in dealerOrders)
				{
					var dealerKey = order.Dealer?.Name ?? "";
					if (!string.IsNullOrEmpty(dealerKey))
					{
						// Initialize dealer data if not exists
						if (!dealerData.ContainsKey(dealerKey))
						{
							dealerData[dealerKey] = new Dictionary<string, int>();
							dealerAmountData[dealerKey] = new Dictionary<string, decimal>();
							dealerRoutes[dealerKey] = order.Dealer?.RouteCode ?? "";
							dealerPhones[dealerKey] = order.Dealer?.PhoneNo ?? "";

							// Get the latest outstanding balance from previous dates
							var previousOutstanding = await _context.DealerOutstandings
								.Where(d => d.DealerId == order.DealerId && d.DeliverDate < dispatchDate)
								.OrderByDescending(d => d.DeliverDate)
								.FirstOrDefaultAsync();

							decimal previousBalance = previousOutstanding?.BalanceAmount ?? 0;
							dealerBalances[dealerKey] = previousBalance;
						}

						// Get order items for this order
						var orderItems = await _context.DealerOrderItems
							.Where(i => i.DealerOrderId == order.Id)
							.ToListAsync();

						foreach (var item in orderItems)
						{
							var shortCode = item.ShortCode ?? "";
							var amount = item.Qty * item.Rate; // Calculate amount

							// Add to short codes list if not exists
							if (!string.IsNullOrEmpty(shortCode) && !shortCodes.Contains(shortCode))
							{
								shortCodes.Add(shortCode);
							}

							// Add quantity to dealer data
							if (dealerData[dealerKey].ContainsKey(shortCode))
							{
								dealerData[dealerKey][shortCode] += item.Qty;
								dealerAmountData[dealerKey][shortCode] += amount; // Add amount
							}
							else
							{
								dealerData[dealerKey][shortCode] = item.Qty;
								dealerAmountData[dealerKey][shortCode] = amount; // Set amount
							}
						}
					}
				}

				// Sort short codes for consistent column order
				shortCodes.Sort();

				// Create pivot table data
				var pivotData = new List<object>();

				foreach (var kvp in dealerData)
				{
					var dealerName = kvp.Key;
					var materials = kvp.Value;
					var amounts = dealerAmountData[dealerName]; // Get amounts for this dealer

					// Create row data with dealer info
					var rowData = new Dictionary<string, object>
					{
						{ "dealerName", dealerName },
						{ "routeCode", dealerRoutes[dealerName] },
						{ "phoneNo", dealerPhones[dealerName] }
					};

					// Add material quantities and amounts
					foreach (var shortCode in shortCodes)
					{
						rowData[shortCode] = materials.ContainsKey(shortCode) ? materials[shortCode] : 0;
						// Add amount with "Amt_" prefix to distinguish from quantity
						rowData["Amt_" + shortCode] = amounts.ContainsKey(shortCode) ? Math.Round(amounts[shortCode], 2) : 0;
					}

					// Calculate row total (sum of all materials for this dealer)
					var rowTotal = materials.Values.Sum();
					var rowAmountTotal = Math.Round(amounts.Values.Sum(), 2); // Total amount for this dealer
					var dealerBalance = dealerBalances.ContainsKey(dealerName) ? dealerBalances[dealerName] : 0; // Get balance
					var rowTotalWithBalance = rowAmountTotal + dealerBalance; // Total amount + balance

					rowData["total"] = rowTotal;
					rowData["totalAmount"] = rowAmountTotal; // Amount
					rowData["balance"] = dealerBalance; // Old (Balance)
					rowData["totalWithBalance"] = rowTotalWithBalance; // Total (Amount + Balance)

					pivotData.Add(rowData);
				}

				// Calculate column totals (sum of each material across all dealers)
				var columnTotals = new Dictionary<string, int>();
				var columnAmountTotals = new Dictionary<string, decimal>(); // Amount totals
				foreach (var shortCode in shortCodes)
				{
					// Quantity totals
					var total = 0;
					foreach (var dealer in dealerData)
					{
						if (dealer.Value.ContainsKey(shortCode))
						{
							total += dealer.Value[shortCode];
						}
					}
					columnTotals[shortCode] = total;

					// Amount totals
					var amountTotal = 0m;
					foreach (var dealer in dealerAmountData)
					{
						if (dealer.Value.ContainsKey(shortCode))
						{
							amountTotal += dealer.Value[shortCode];
						}
					}
					columnAmountTotals[shortCode] = Math.Round(amountTotal, 2);
				}

				// Add total row with column totals
				var totalRow = new Dictionary<string, object>
				{
					{ "dealerName", "Total" },
					{ "routeCode", "" },
					{ "phoneNo", "" }
				};

				// Add column totals for each short code
				foreach (var shortCode in shortCodes)
				{
					totalRow[shortCode] = columnTotals[shortCode];
					totalRow["Amt_" + shortCode] = columnAmountTotals[shortCode]; // Add amount totals
				}

				// Calculate grand total (sum of all column totals)
				var grandTotal = columnTotals.Values.Sum();
				var grandTotalAmount = Math.Round(columnAmountTotals.Values.Sum(), 2); // Grand total amount
				var totalBalance = dealerBalances.Values.Sum(); // Total balance
				var grandTotalWithBalance = grandTotalAmount + totalBalance; // Grand total amount + balance

				totalRow["total"] = grandTotal;
				totalRow["totalAmount"] = grandTotalAmount; // Amount
				totalRow["balance"] = totalBalance; // Old (Balance)
				totalRow["totalWithBalance"] = grandTotalWithBalance; // Total (Amount + Balance)

				pivotData.Add(totalRow);

				return Json(new
				{
					success = true,
					data = pivotData,
					materials = shortCodes,
					grandTotal = grandTotal,
					grandTotalAmount = grandTotalAmount, // Include grand total amount in response
					totalBalance = totalBalance, // Include total balance in response
					grandTotalWithBalance = grandTotalWithBalance // Include grand total with balance in response
				});
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = ex.Message });
			}
		}

		// GET: OrderReconciliation/ExportDispatchToExcel
		[HttpGet]
		public async Task<IActionResult> ExportDispatchToExcel(DateTime? date, int? customerId)
		{
			try
			{
				var dispatchDate = date ?? DateTime.Now.Date;

				// Get dealer orders for the specified date
				var dealerOrdersQuery = _context.DealerOrders
					.Include(o => o.Dealer)
					.Where(o => o.OrderDate == dispatchDate);

				// Filter by customer if specified
				if (customerId.HasValue && customerId.Value > 0)
				{
					dealerOrdersQuery = dealerOrdersQuery.Where(o => o.DistributorId == customerId.Value);
				}

				var dealerOrders = await dealerOrdersQuery.ToListAsync();

				// Get distributor name for header
				string distributorName = "All Distributors";
				if (customerId.HasValue && customerId.Value > 0)
				{
					var customer = await _context.Customer_Master
						.FirstOrDefaultAsync(c => c.Id == customerId.Value);
					if (customer != null)
					{
						distributorName = customer.Name;
					}
				}

				// Get all unique short codes for column headers based on segment
				var shortCodes = new List<string>();
				var dealerData = new Dictionary<string, Dictionary<string, int>>(); // dealer -> shortCode -> quantity
				var dealerAmountData = new Dictionary<string, Dictionary<string, decimal>>(); // dealer -> shortCode -> amount
				var dealerBalances = new Dictionary<string, decimal>(); // dealer -> balance amount

				// Get available materials based on customer segment
				List<MaterialMaster> availableMaterials = new List<MaterialMaster>();

				// If customerId is provided, get materials based on customer segment
				if (customerId.HasValue && customerId.Value > 0)
				{
					// Get the customer name for the selected distributor ID
					var customer = await _context.Customer_Master
						.FirstOrDefaultAsync(c => c.Id == customerId.Value);

					if (customer != null)
					{
						// Get customer segment mappings for the selected distributor using the customer name
						var segmentMappings = await _context.CustomerSegementMap
							.Where(csm => csm.Customername == customer.Name)
							.ToListAsync();

						if (segmentMappings.Any())
						{
							var segmentNames = segmentMappings.Select(m => m.SegementName).ToList();
							availableMaterials = await _context.MaterialMaster
								.Where(m => segmentNames.Contains(m.segementname) && m.isactive == true && !m.Materialname.StartsWith("CRATES"))
								.OrderBy(m => m.sequence)
								.ToListAsync();
						}
						else
						{
							// If no segments found, load all active materials
							availableMaterials = await _context.MaterialMaster
								.Where(m => m.isactive == true && !m.Materialname.StartsWith("CRATES"))
								.OrderBy(m => m.sequence)
								.ToListAsync();
						}
					}
					else
					{
						// If customer not found, load all active materials
						availableMaterials = await _context.MaterialMaster
							.Where(m => m.isactive == true && !m.Materialname.StartsWith("CRATES"))
							.OrderBy(m => m.sequence)
							.ToListAsync();
					}
				}
				else
				{
					// If no customer specified, load all active materials
					availableMaterials = await _context.MaterialMaster
						.Where(m => m.isactive == true && !m.Materialname.StartsWith("CRATES"))
						.OrderBy(m => m.sequence)
						.ToListAsync();
				}

				// Get all short codes from available materials
				shortCodes = availableMaterials.Select(m => m.ShortName).Distinct().OrderBy(sc => sc).ToList();

				// Initialize dealer data with all short codes set to 0
				foreach (var order in dealerOrders)
				{
					// Use only first name of dealer
					var dealerKey = order.Dealer?.Name?.Split(' ').FirstOrDefault() ?? "";
					if (!string.IsNullOrEmpty(dealerKey))
					{
						// Initialize dealer data if not exists
						if (!dealerData.ContainsKey(dealerKey))
						{
							dealerData[dealerKey] = new Dictionary<string, int>();
							dealerAmountData[dealerKey] = new Dictionary<string, decimal>();
							dealerBalances[dealerKey] = 0; // Initialize balance

							// Initialize all short codes with 0 quantity for this dealer
							foreach (var shortCode in shortCodes)
							{
								dealerData[dealerKey][shortCode] = 0;
								dealerAmountData[dealerKey][shortCode] = 0;
							}
						}

						// Get order items for this order
						var orderItems = await _context.DealerOrderItems
							.Where(i => i.DealerOrderId == order.Id)
							.ToListAsync();

						foreach (var item in orderItems)
						{
							// Find the material to get the correct short code
							var material = availableMaterials.FirstOrDefault(m => m.Materialname == item.MaterialName);
							if (material != null)
							{
								var shortCode = material.ShortName;
								var amount = item.Qty * item.Rate; // Calculate amount

								// Update quantity and amount for this dealer and short code
								dealerData[dealerKey][shortCode] = item.Qty;
								dealerAmountData[dealerKey][shortCode] = amount;
							}
						}
					}
				}

				// Get dealer outstanding balances for the previous date
				foreach (var order in dealerOrders)
				{
					var dealerKey = order.Dealer?.Name?.Split(' ').FirstOrDefault() ?? "";
					if (!string.IsNullOrEmpty(dealerKey))
					{
						// Get the latest outstanding balance from previous dates
						var previousOutstanding = await _context.DealerOutstandings
							.Where(d => d.DealerId == order.DealerId && d.DeliverDate < dispatchDate)
							.OrderByDescending(d => d.DeliverDate)
							.FirstOrDefaultAsync();

						decimal previousBalance = previousOutstanding?.BalanceAmount ?? 0;
						dealerBalances[dealerKey] = previousBalance;
					}
				}

				// Create pivot table data
				var pivotData = new List<Dictionary<string, object>>();

				// Add data for dealers with orders
				foreach (var kvp in dealerData)
				{
					var dealerName = kvp.Key;
					var materials = kvp.Value;
					var amounts = dealerAmountData[dealerName]; // Get amounts for this dealer

					// Create row data with dealer info
					var rowData = new Dictionary<string, object>
					{
						{ "dealerName", dealerName }
					};

					// Add material quantities for all short codes
					foreach (var shortCode in shortCodes)
					{
						rowData[shortCode] = materials.ContainsKey(shortCode) ? materials[shortCode] : 0;
					}

					// Calculate row total (sum of all materials for this dealer)
					var rowTotal = materials.Values.Sum();
					var rowAmountTotal = Math.Round(amounts.Values.Sum(), 2); // Total amount for this dealer
					var dealerBalance = dealerBalances.ContainsKey(dealerName) ? dealerBalances[dealerName] : 0; // Get balance
					var rowTotalWithBalance = rowAmountTotal + dealerBalance; // Total amount + balance

					rowData["total"] = rowTotal;
					rowData["totalAmount"] = rowAmountTotal; // Amount
					rowData["balance"] = dealerBalance; // Old (Balance)
					rowData["totalWithBalance"] = rowTotalWithBalance; // Total (Amount + Balance)

					pivotData.Add(rowData);
				}

				// Calculate column totals (sum of each material across all dealers)
				var columnTotals = new Dictionary<string, int>();
				var columnAmountTotals = new Dictionary<string, decimal>(); // Amount totals

				// Initialize column totals with 0 for all short codes
				foreach (var shortCode in shortCodes)
				{
					columnTotals[shortCode] = 0;
					columnAmountTotals[shortCode] = 0;
				}

				// Sum quantities for each short code across all dealers
				foreach (var dealer in dealerData)
				{
					foreach (var shortCode in shortCodes)
					{
						if (dealer.Value.ContainsKey(shortCode))
						{
							columnTotals[shortCode] += dealer.Value[shortCode];
						}
					}
				}

				// Add total row with column totals
				var totalRow = new Dictionary<string, object>
				{
					{ "dealerName", "Total" }
				};

				// Add column totals for each short code
				foreach (var shortCode in shortCodes)
				{
					totalRow[shortCode] = columnTotals[shortCode];
				}

				// Calculate grand total (sum of all column totals)
				var grandTotal = columnTotals.Values.Sum();
				var grandTotalAmount = Math.Round(dealerAmountData.Values.SelectMany(d => d.Values).Sum(), 2); // Grand total amount
				var totalBalance = dealerBalances.Values.Sum(); // Total balance
				var grandTotalWithBalance = grandTotalAmount + totalBalance; // Grand total amount + balance

				totalRow["total"] = grandTotal;
				totalRow["totalAmount"] = grandTotalAmount; // Amount
				totalRow["balance"] = totalBalance; // Old (Balance)
				totalRow["totalWithBalance"] = grandTotalWithBalance; // Total (Amount + Balance)

				pivotData.Add(totalRow);

				// Create Excel file
				using (var package = new ExcelPackage())
				{
					var worksheet = package.Workbook.Worksheets.Add("Dispatch Route Sheet");

					// Set page orientation to landscape and paper size to A4
					worksheet.PrinterSettings.Orientation = eOrientation.Landscape;
					worksheet.PrinterSettings.PaperSize = ePaperSize.A4;

					// Set to fit all columns on one page
					worksheet.PrinterSettings.FitToPage = true;
					worksheet.PrinterSettings.FitToWidth = 1;
					worksheet.PrinterSettings.FitToHeight = 0;

					// Add header with distributor name and date
					worksheet.Cells[1, 1].Value = $"Distributor: {distributorName}";
					worksheet.Cells[1, 1].Style.Font.Bold = true;
					worksheet.Cells[1, 1].Style.Font.Size = 14;

					worksheet.Cells[2, 1].Value = $"Date: {dispatchDate:dd-MM-yyyy}";
					worksheet.Cells[2, 1].Style.Font.Bold = true;
					worksheet.Cells[2, 1].Style.Font.Size = 12;

					// Add headers for the data table (moved down by 2 rows)
					worksheet.Cells[3, 1].Value = "Dealer";

					// Add short code headers (quantity only for each material)
					int colIndex = 2;
					for (int i = 0; i < shortCodes.Count; i++)
					{
						var shortCode = shortCodes[i];
						worksheet.Cells[3, colIndex].Value = shortCode;
						colIndex++;
					}

					// Add Total column headers
					worksheet.Cells[3, colIndex].Value = "Total Qty";
					worksheet.Cells[3, colIndex + 1].Value = "Amount";
					worksheet.Cells[3, colIndex + 2].Value = "Old"; // Balance
					worksheet.Cells[3, colIndex + 3].Value = "Total"; // Amount + Balance

					// Add data rows (starting from row 4)
					for (int i = 0; i < pivotData.Count; i++)
					{
						var rowData = pivotData[i];
						int row = i + 4; // Excel rows start at 1, header is row 3, so data starts at row 4

						worksheet.Cells[row, 1].Value = rowData["dealerName"]?.ToString() ?? "";

						// Add material quantities only
						colIndex = 2;
						for (int j = 0; j < shortCodes.Count; j++)
						{
							var shortCode = shortCodes[j];
							worksheet.Cells[row, colIndex].Value = rowData.ContainsKey(shortCode) ? rowData[shortCode] : 0;
							colIndex++;
						}

						// Add row totals (quantity, amount, balance, and total with balance)
						worksheet.Cells[row, colIndex].Value = rowData["total"];
						worksheet.Cells[row, colIndex + 1].Value = rowData["totalAmount"];
						worksheet.Cells[row, colIndex + 2].Value = rowData["balance"];
						worksheet.Cells[row, colIndex + 3].Value = rowData["totalWithBalance"];
					}

					// Apply styling to header row of the data table
					using (var range = worksheet.Cells[3, 1, 3, shortCodes.Count + 5])
					{
						range.Style.Font.Bold = true;
						range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
						range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
						range.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
						range.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
						range.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
						range.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
						range.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
						range.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
						range.Style.WrapText = true;
					}

					// Apply borders to all data cells
					using (var range = worksheet.Cells[3, 1, pivotData.Count + 3, shortCodes.Count + 5])
					{
						range.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
						range.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
						range.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
						range.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
						range.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
						range.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
					}

					// Set column widths - fixed width for material columns
					worksheet.Column(1).Width = 10; // Dealer Name column

					// Set fixed width for all material columns
					for (int i = 0; i < shortCodes.Count; i++)
					{
						worksheet.Column(i + 2).Width = 4; // Fixed width of 4 for material columns
					}

					// Set width for total columns
					worksheet.Column(shortCodes.Count + 2).Width = 8; // Total Qty column
					worksheet.Column(shortCodes.Count + 3).Width = 8; // Amount column
					worksheet.Column(shortCodes.Count + 4).Width = 8; // Old (Balance) column
					worksheet.Column(shortCodes.Count + 5).Width = 8; // Total (Amount + Balance) column

					// Convert to bytes
					var fileBytes = package.GetAsByteArray();

					// Return file
					var fileName = $"DispatchRouteSheet_{dispatchDate:yyyyMMdd}.xlsx";
					return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
				}
			}
			catch (Exception ex)
			{
				_notifyService.Error("Error exporting to Excel: " + ex.Message);
				return RedirectToAction("DispatchRouteSheet");
			}
		}

		// POST: OrderReconciliation/LoadExcelView
		[HttpPost]
		public async Task<IActionResult> LoadExcelView(DateTime orderDate, int customerId)
		{
			try
			{
				var viewModel = new OrderReconciliationExcelViewModel
				{
					SelectedDistributorId = customerId,
					AvailableDistributors = await GetAvailableDistributors()
				};

				// Get dealers for the selected distributor
				viewModel.Dealers = await _context.DealerMasters
					.Where(d => d.DistributorId == customerId)
					.ToListAsync();

				// Get all orders for these dealers for the specified date
				var dealerIds = viewModel.Dealers.Select(d => d.Id).ToList();
				var allOrders = await _context.DealerOrders
					.Where(o => dealerIds.Contains(o.DealerId) && o.DistributorId == customerId && o.OrderDate == orderDate)
					.Include(o => o.DealerOrderItems)
					.ToListAsync();

				// Group orders by dealer
				foreach (var dealer in viewModel.Dealers)
				{
					var dealerOrders = allOrders.Where(o => o.DealerId == dealer.Id).ToList();
					if (dealerOrders.Any())
					{
						viewModel.DealerOrders[dealer.Id] = dealerOrders;
					}
				}

				// Get available materials for these dealers
				var availableMaterials = new List<MaterialMaster>();

				// Get the customer name for the selected distributor ID
				var customer = await _context.Customer_Master
					.FirstOrDefaultAsync(c => c.Id == customerId);

				if (customer != null)
				{
					// Get customer segment mappings for the selected distributor using the customer name
					var segmentMappings = await _context.CustomerSegementMap
						.Where(csm => csm.Customername == customer.Name)
						.ToListAsync();

					if (segmentMappings.Any())
					{
						var segmentNames = segmentMappings.Select(m => m.SegementName).ToList();
						availableMaterials = await _context.MaterialMaster
							.Where(m => segmentNames.Contains(m.segementname) && m.isactive == true && !m.Materialname.StartsWith("CRATES"))
							.OrderBy(m => m.sequence)
							.ToListAsync();
					}
					else
					{
						// If no segments found, load all active materials
						availableMaterials = await _context.MaterialMaster
							.Where(m => m.isactive == true && !m.Materialname.StartsWith("CRATES"))
							.OrderBy(m => m.sequence)
							.ToListAsync();
					}
				}
				else
				{
					// If customer not found, load all active materials
					availableMaterials = await _context.MaterialMaster
						.Where(m => m.isactive == true && !m.Materialname.StartsWith("CRATES"))
						.OrderBy(m => m.sequence)
							.ToListAsync();
				}

				// Set available materials in the view model
				viewModel.AvailableMaterials = availableMaterials;

				// Get conversion data for available materials
				var materialNames = availableMaterials.Select(m => m.Materialname).ToList();
				var conversionData = await _context.ConversionTables
					.Where(c => materialNames.Contains(c.MaterialName))
					.ToListAsync();

				// Populate conversion dictionary
				foreach (var conversion in conversionData)
				{
					if (!viewModel.MaterialConversions.ContainsKey(conversion.MaterialName))
					{
						viewModel.MaterialConversions[conversion.MaterialName] = conversion;
					}
				}

				// Get dealer outstanding balances for the previous date
				var dealerBalances = new Dictionary<int, decimal>();
				foreach (var dealer in viewModel.Dealers)
				{
					// Get the latest outstanding balance from previous dates
					var previousOutstanding = await _context.DealerOutstandings
						.Where(d => d.DealerId == dealer.Id && d.DeliverDate < orderDate)
						.OrderByDescending(d => d.DeliverDate)
						.FirstOrDefaultAsync();

					decimal previousBalance = previousOutstanding?.BalanceAmount ?? 0;
					dealerBalances[dealer.Id] = previousBalance;
				}

				// Pass dealer balances to the view
				ViewData["DealerOutstandings"] = dealerBalances;

				return PartialView("_OrderReconciliationExcelPartial", viewModel);
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = ex.Message });
			}
		}

		// POST: OrderReconciliation/SaveExcelView
		[HttpPost]
		public async Task<IActionResult> SaveExcelView([FromBody] List<ExcelViewOrderModel> allOrders)
		{
			using var transaction = await _context.Database.BeginTransactionAsync();
			try
			{
				// Process each dealer order
				foreach (var model in allOrders)
				{
					// Get the dealer order
					var dealerOrder = await _context.DealerOrders
						.Include(o => o.DealerOrderItems)
						.FirstOrDefaultAsync(o => o.Id == model.dealerId);

					if (dealerOrder == null)
					{
						continue; // Skip this order if not found
					}

					// Convert order items to dictionary
					var intQuantities = new Dictionary<int, int>();
					foreach (var item in model.orderItems)
					{
						intQuantities[item.MaterialId] = item.Quantity;
					}

					// Get materials to validate
					var materials = await _context.MaterialMaster
						.Where(m => intQuantities.Keys.Contains(m.Id))
						.ToDictionaryAsync(m => m.Id, m => m);

					// Update order items based on quantities
					foreach (var item in dealerOrder.DealerOrderItems)
					{
						// Find the material for this item
						var material = materials.Values.FirstOrDefault(m => m.Materialname == item.MaterialName);
						if (material != null && intQuantities.ContainsKey(material.Id))
						{
							// Update ordered quantity (not delivered quantity)
							item.Qty = intQuantities[material.Id];
							_context.DealerOrderItems.Update(item);
						}
					}

					// Update dealer order deliver flag to 0 (not yet delivered)
					dealerOrder.DeliverFlag = 0;
					_context.DealerOrders.Update(dealerOrder);
				}

				await _context.SaveChangesAsync();
				await transaction.CommitAsync();

				return Json(new { success = true, message = "Order quantities updated successfully." });
			}
			catch (Exception ex)
			{
				await transaction.RollbackAsync();
				return Json(new { success = false, message = ex.Message });
			}
		}

		// Helper method to get available distributors based on user role
		private async Task<List<Customer_Master>> GetAvailableDistributors()
		{
			var role = HttpContext.Session.GetString("role");
			var userName = HttpContext.Session.GetString("UserName");

			if (string.Equals(role, "Customer", StringComparison.OrdinalIgnoreCase))
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
			else if (string.Equals(role, "Sales", StringComparison.OrdinalIgnoreCase))
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