using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Milk_Bakery.Data;
using Milk_Bakery.Models;
using Milk_Bakery.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using OfficeOpenXml;
using System.Drawing; // Added for Color support

namespace Milk_Bakery.Controllers
{
	[Authentication]
	public class DeliveredQuantityReportController : Controller
	{
		private readonly MilkDbContext _context;

		public DeliveredQuantityReportController(MilkDbContext context)
		{
			_context = context;
		}

		public async Task<IActionResult> Index()
		{
			var viewModel = new DeliveredQuantityReportViewModel
			{
				FromDate = DateTime.Now.Date, // Default to current date
				ToDate = DateTime.Now.Date,   // Default to current date
				AvailableCustomers = await GetAvailableCustomers()
			};

			// Set default customer for Customer role
			var role = HttpContext.Session.GetString("role");
			if (role == "Customer")
			{
				var userName = HttpContext.Session.GetString("UserName");
				var customer = await _context.Customer_Master
					.Where(c => c.phoneno == userName)
					.Select(c => c.Name)
					.FirstOrDefaultAsync();

				if (!string.IsNullOrEmpty(customer))
				{
					viewModel.CustomerName = customer;
				}
			}

			ViewBag.Customers = GetCustomer(); // Add customer dropdown data
			return View(viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> GenerateReport(DeliveredQuantityReportViewModel model)
		{
			var reportItems = await GenerateDeliveredQuantityReport(model.FromDate, model.ToDate, model.CustomerName, model.DealerId, model.ShowOnlyVariance);

			var viewModel = new DeliveredQuantityReportViewModel
			{
				ReportItems = reportItems,
				FromDate = model.FromDate,
				ToDate = model.ToDate,
				CustomerName = model.CustomerName,
				DealerId = model.DealerId,
				ShowOnlyVariance = model.ShowOnlyVariance,
				AvailableCustomers = await GetAvailableCustomers()
			};

			// Populate available dealers based on selected customer
			if (!string.IsNullOrEmpty(model.CustomerName))
			{
				var customer = await _context.Customer_Master.FirstOrDefaultAsync(c => c.Name == model.CustomerName);
				if (customer != null)
				{
					viewModel.AvailableDealers = await _context.DealerMasters
						.Where(d => d.DistributorId == customer.Id)
						.OrderBy(d => d.Name)
						.ToListAsync();
				}
			}

			ViewBag.Customers = GetCustomer(); // Add customer dropdown data
			return View("Index", viewModel);
		}

		private async Task<List<DeliveredQuantityReportItem>> GenerateDeliveredQuantityReport(DateTime? fromDate, DateTime? toDate, string customerName, int? dealerId, bool showOnlyVariance)
		{
			var reportItems = new List<DeliveredQuantityReportItem>();

			// Get dealer orders within date range and customer filter
			var dealerOrdersQuery = _context.DealerOrders
				.Include(d => d.DealerOrderItems)
				.AsNoTracking()
				.Where(d => d.OrderDate >= fromDate && d.OrderDate <= toDate);

			// Apply dealer filter if specified
			if (dealerId.HasValue && dealerId.Value > 0)
			{
				dealerOrdersQuery = dealerOrdersQuery.Where(d => d.DealerId == dealerId.Value);
			}

			// Apply customer filter based on role
			var role = HttpContext.Session.GetString("role");
			if (role == "Customer")
			{
				// For customer role, get the logged-in customer
				var userName = HttpContext.Session.GetString("UserName");
				var customer = await _context.Customer_Master
					.Where(c => c.phoneno == userName)
					.Select(c => c.Name)
					.FirstOrDefaultAsync();

				if (!string.IsNullOrEmpty(customer))
				{
					// Check if the selected customer is allowed for this user
					bool isAllowed = await IsCustomerAllowedForUser(customer, customerName);
					if (isAllowed && !string.IsNullOrEmpty(customerName))
					{
						var customerEntity = await _context.Customer_Master.FirstOrDefaultAsync(c => c.Name == customerName);
						if (customerEntity != null)
						{
							dealerOrdersQuery = dealerOrdersQuery.Where(d => d.DistributorId == customerEntity.Id);
						}
					}
					else
					{
						var customerEntity = await _context.Customer_Master.FirstOrDefaultAsync(c => c.Name == customer);
						if (customerEntity != null)
						{
							dealerOrdersQuery = dealerOrdersQuery.Where(d => d.DistributorId == customerEntity.Id);
						}
					}
				}
			}
			else if (!string.IsNullOrEmpty(customerName))
			{
				// For other roles, use the selected customer
				var customerEntity = await _context.Customer_Master.FirstOrDefaultAsync(c => c.Name == customerName);
				if (customerEntity != null)
				{
					dealerOrdersQuery = dealerOrdersQuery.Where(d => d.DistributorId == customerEntity.Id);
				}
			}

			var dealerOrders = await dealerOrdersQuery.ToListAsync();

			// Get all distributor and dealer data in a single query for better performance
			var distributorIds = dealerOrders.Select(o => o.DistributorId).Distinct().ToList();
			var dealerIds = dealerOrders.Select(o => o.DealerId).Distinct().ToList();

			var distributors = await _context.Customer_Master
				.Where(c => distributorIds.Contains(c.Id))
				.ToDictionaryAsync(c => c.Id, c => c.Name);

			var dealers = await _context.DealerMasters
				.Where(d => dealerIds.Contains(d.Id))
				.ToDictionaryAsync(d => d.Id, d => d.Name);

			// Process each dealer order and its items
			foreach (var order in dealerOrders)
			{
				foreach (var item in order.DealerOrderItems)
				{
					// Skip items with no ordered quantity
					if (item.Qty <= 0)
						continue;

					var orderedAmount = item.Qty * item.Rate;
					var deliveredAmount = item.DeliverQnty * item.Rate;
					// Changed to Delivered - Ordered to show positive values when delivered exceeds ordered
					var quantityVariance = item.DeliverQnty - item.Qty;
					// Changed to Delivered - Ordered for amount as well
					var amountVariance = deliveredAmount - orderedAmount;

					var reportItem = new DeliveredQuantityReportItem
					{
						CustomerName = distributors.ContainsKey(order.DistributorId) ? distributors[order.DistributorId] : "Unknown Customer",
						DealerName = dealers.ContainsKey(order.DealerId) ? dealers[order.DealerId] : "Unknown Dealer",
						OrderDate = order.OrderDate,
						OrderId = order.Id,
						MaterialName = item.MaterialName,
						ShortCode = item.ShortCode,
						OrderedQuantity = item.Qty,
						DeliveredQuantity = item.DeliverQnty,
						QuantityVariance = quantityVariance,
						UnitPrice = item.Rate,
						OrderedAmount = orderedAmount,
						DeliveredAmount = deliveredAmount,
						AmountVariance = amountVariance,
					};

					// If showOnlyVariance is true, only add items with variance
					if (!showOnlyVariance || reportItem.HasVariance)
					{
						reportItems.Add(reportItem);
					}
				}
			}

			return reportItems.OrderBy(r => r.CustomerName).ThenBy(r => r.DealerName).ThenBy(r => r.OrderDate).ToList();
		}

		// Export to Excel
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ExportToExcel(DeliveredQuantityReportViewModel model)
		{
			try
			{
				// Set the license context for EPPlus
				ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

				// Ensure we have the correct date values
				var fromDate = model.FromDate ?? DateTime.Now.Date;
				var toDate = model.ToDate ?? DateTime.Now.Date;

				// Generate report data
				var reportItems = await GenerateDeliveredQuantityReport(fromDate, toDate, model.CustomerName, model.DealerId, model.ShowOnlyVariance);

				// Log the number of items for debugging
				System.Diagnostics.Debug.WriteLine($"Exporting {reportItems.Count} items to Excel");

				// Create Excel package
				using (var package = new ExcelPackage())
				{
					var worksheet = package.Workbook.Worksheets.Add("Delivered Quantity Report");

					// Headers
					worksheet.Cells[1, 1].Value = "Customer";
					worksheet.Cells[1, 2].Value = "Dealer";
					worksheet.Cells[1, 3].Value = "Order Date";
					worksheet.Cells[1, 4].Value = "Order ID";
					worksheet.Cells[1, 5].Value = "Material";
					worksheet.Cells[1, 6].Value = "Short Code";
					worksheet.Cells[1, 7].Value = "Ordered Quantity";
					worksheet.Cells[1, 8].Value = "Delivered Quantity";
					worksheet.Cells[1, 9].Value = "Quantity Variance";
					worksheet.Cells[1, 10].Value = "Unit Price";
					worksheet.Cells[1, 11].Value = "Ordered Amount";
					worksheet.Cells[1, 12].Value = "Delivered Amount";
					worksheet.Cells[1, 13].Value = "Amount Variance";

					// Format headers
					using (var range = worksheet.Cells[1, 1, 1, 13])
					{
						range.Style.Font.Bold = true;
						range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
						range.Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
						range.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
					}

					// Add data
					for (int i = 0; i < reportItems.Count; i++)
					{
						var item = reportItems[i];
						var row = i + 2; // Start from row 2 (after header)

						// Calculate variance as Delivered - Ordered (to show positive when delivered exceeds ordered)
						var quantityVariance = item.DeliveredQuantity - item.OrderedQuantity;
						var amountVariance = item.DeliveredAmount - item.OrderedAmount;

						worksheet.Cells[row, 1].Value = item.CustomerName;
						worksheet.Cells[row, 2].Value = item.DealerName;
						worksheet.Cells[row, 3].Value = item.OrderDate;
						worksheet.Cells[row, 3].Style.Numberformat.Format = "yyyy-mm-dd";
						worksheet.Cells[row, 4].Value = item.OrderId;
						worksheet.Cells[row, 5].Value = item.MaterialName;
						worksheet.Cells[row, 6].Value = item.ShortCode;
						worksheet.Cells[row, 7].Value = item.OrderedQuantity;
						worksheet.Cells[row, 8].Value = item.DeliveredQuantity;
						worksheet.Cells[row, 9].Value = quantityVariance;
						worksheet.Cells[row, 10].Value = item.UnitPrice;
						worksheet.Cells[row, 10].Style.Numberformat.Format = "0.00";
						worksheet.Cells[row, 11].Value = item.OrderedAmount;
						worksheet.Cells[row, 11].Style.Numberformat.Format = "0.00";
						worksheet.Cells[row, 12].Value = item.DeliveredAmount;
						worksheet.Cells[row, 12].Style.Numberformat.Format = "0.00";
						worksheet.Cells[row, 13].Value = amountVariance;
						worksheet.Cells[row, 13].Style.Numberformat.Format = "0.00";

						// Apply borders to all data cells
						for (int col = 1; col <= 13; col++)
						{
							worksheet.Cells[row, col].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
						}

						// Color coding for quantity variance
						if (quantityVariance == 0)
						{
							worksheet.Cells[row, 9].Style.Font.Color.SetColor(Color.Green);
						}
						else if (quantityVariance > 0)
						{
							worksheet.Cells[row, 9].Style.Font.Color.SetColor(Color.Red);
						}
						else
						{
							worksheet.Cells[row, 9].Style.Font.Color.SetColor(Color.Orange);
						}

						// Color coding for amount variance
						if (amountVariance == 0)
						{
							worksheet.Cells[row, 13].Style.Font.Color.SetColor(Color.Green);
						}
						else if (amountVariance > 0)
						{
							worksheet.Cells[row, 13].Style.Font.Color.SetColor(Color.Red);
						}
						else
						{
							worksheet.Cells[row, 13].Style.Font.Color.SetColor(Color.Orange);
						}
					}

					// Auto-fit columns for better appearance
					worksheet.Cells.AutoFitColumns();

					// Set the content type and file name
					var fileName = $"DeliveredQuantityReport_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
					var fileBytes = package.GetAsByteArray();
					return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
				}
			}
			catch (Exception ex)
			{
				// Log the exception for debugging
				System.Diagnostics.Debug.WriteLine($"Error in ExportToExcel: {ex.Message}");
				System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

				// Return an error response
				return BadRequest("An error occurred while generating the export file.");
			}
		}

		// Export to Excel with pivot table format similar to OrderReconciliation
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ExportToExcelPivot(DeliveredQuantityReportViewModel model)
		{
			try
			{
				// Set the license context for EPPlus
				ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

				// Ensure we have the correct date values
				var fromDate = model.FromDate ?? DateTime.Now.Date;
				var toDate = model.ToDate ?? DateTime.Now.Date;

				// Generate report data
				var reportItems = await GenerateDeliveredQuantityReport(fromDate, toDate, model.CustomerName, model.DealerId, model.ShowOnlyVariance);

				// Group data by dealer and material for pivot table format
				var dealerData = new Dictionary<string, Dictionary<string, int>>(); // dealer -> shortCode -> delivered quantity
				var dealerAmountData = new Dictionary<string, Dictionary<string, decimal>>(); // dealer -> shortCode -> delivered amount
				var dealerBalances = new Dictionary<string, decimal>(); // dealer -> balance amount
				var shortCodes = new List<string>();

				// Process report items to build pivot data
				foreach (var item in reportItems)
				{
					// Use only first name of dealer as per Excel export specification
					var dealerKey = item.DealerName?.Split(' ').FirstOrDefault() ?? "";
					if (!string.IsNullOrEmpty(dealerKey))
					{
						// Initialize dealer data if not exists
						if (!dealerData.ContainsKey(dealerKey))
						{
							dealerData[dealerKey] = new Dictionary<string, int>();
							dealerAmountData[dealerKey] = new Dictionary<string, decimal>();

							// Get the latest outstanding balance from previous dates
							// First, we need to get the dealer ID from the dealer name
							var dealer = await _context.DealerMasters
								.FirstOrDefaultAsync(d => d.Name == item.DealerName);
							
							if (dealer != null)
							{
								var previousOutstanding = await _context.DealerOutstandings
									.Where(d => d.DealerId == dealer.Id && d.DeliverDate < fromDate)
									.OrderByDescending(d => d.DeliverDate)
									.FirstOrDefaultAsync();

								decimal previousBalance = previousOutstanding?.BalanceAmount ?? 0;
								dealerBalances[dealerKey] = previousBalance;
							}
							else
							{
								dealerBalances[dealerKey] = 0;
							}
						}

						var shortCode = item.ShortCode ?? "";
						// Calculate amount based on delivered quantity and unit price
						var deliveredAmount = item.DeliveredQuantity * item.UnitPrice;

						// Add to short codes list if not exists
						if (!string.IsNullOrEmpty(shortCode) && !shortCodes.Contains(shortCode))
						{
							shortCodes.Add(shortCode);
						}

						// Add delivered quantity to dealer data
						if (dealerData[dealerKey].ContainsKey(shortCode))
						{
							dealerData[dealerKey][shortCode] += item.DeliveredQuantity;
							dealerAmountData[dealerKey][shortCode] += deliveredAmount;
						}
						else
						{
							dealerData[dealerKey][shortCode] = item.DeliveredQuantity;
							dealerAmountData[dealerKey][shortCode] = deliveredAmount;
						}
					}
				}

				// Sort short codes for consistent column order
				shortCodes.Sort();

				// Create pivot table data
				var pivotData = new List<Dictionary<string, object>>();

				// Add data for dealers
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

				// Get customer name for header
				string customerName = "All Customers";
				if (!string.IsNullOrEmpty(model.CustomerName))
				{
					customerName = model.CustomerName;
				}

				// Create Excel file
				using (var package = new ExcelPackage())
				{
					var worksheet = package.Workbook.Worksheets.Add("Delivered Quantity Report");

					// Set page orientation to landscape and paper size to A4 (Excel Export Specification #4)
					worksheet.PrinterSettings.Orientation = eOrientation.Landscape;
					worksheet.PrinterSettings.PaperSize = ePaperSize.A4;

					// Set to fit all columns on one page (Excel Export Specification #9)
					worksheet.PrinterSettings.FitToPage = true;
					worksheet.PrinterSettings.FitToWidth = 1;
					worksheet.PrinterSettings.FitToHeight = 0;

					// Add header with customer name and date range (Excel Export Specification #10)
					worksheet.Cells[1, 1].Value = $"Customer: {customerName}";
					worksheet.Cells[1, 1].Style.Font.Bold = true;
					worksheet.Cells[1, 1].Style.Font.Size = 14;

					worksheet.Cells[2, 1].Value = $"Date Range: {fromDate:dd-MM-yyyy} to {toDate:dd-MM-yyyy}";
					worksheet.Cells[2, 1].Style.Font.Bold = true;
					worksheet.Cells[2, 1].Style.Font.Size = 12;

					// Add headers for the data table (moved down by 2 rows)
					worksheet.Cells[3, 1].Value = "Dealer";

					// Add short code headers (quantity only for each material) (Excel Export Specification #10)
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

					// Apply styling to header row of the data table (Excel Export Specification #5)
					using (var range = worksheet.Cells[3, 1, 3, shortCodes.Count + 5])
					{
						range.Style.Font.Bold = true;
						range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
						range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
						range.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
						range.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
						range.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
						range.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
						range.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
						range.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
						range.Style.WrapText = true; // Excel Export Specification #7
					}

					// Apply borders to all data cells (Excel Export Specification #1)
					using (var range = worksheet.Cells[3, 1, pivotData.Count + 3, shortCodes.Count + 5])
					{
						range.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
						range.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
						range.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
						range.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
						range.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center; // Excel Export Specification #8
						range.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center; // Excel Export Specification #8
					}

					// Set column widths - fixed width for material columns (Excel Export Specification #6)
					worksheet.Column(1).Width = 10; // Dealer Name column

					// Set fixed width for all material columns (Excel Export Specification #6)
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

					// Return file (Excel Export Specification #4)
					var fileName = $"DeliveredQuantityReport_Pivot_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
					return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
				}
			}
			catch (Exception ex)
			{
				// Log the exception for debugging
				System.Diagnostics.Debug.WriteLine($"Error in ExportToExcelPivot: {ex.Message}");
				System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

				// Return an error response
				return BadRequest("An error occurred while generating the export file.");
			}
		}

		// Helper method to check if a customer is allowed for the current user
		private async Task<bool> IsCustomerAllowedForUser(string loggedInCustomerName, string selectedCustomerName)
		{
			// If no customer selected, use logged in customer
			if (string.IsNullOrEmpty(selectedCustomerName))
				return true;

			// If selected customer is the logged in customer
			if (selectedCustomerName == loggedInCustomerName)
				return true;

			// Check if selected customer is in the mapped customers
			var loggedInCustomer = await _context.Customer_Master
				.FirstOrDefaultAsync(c => c.Name == loggedInCustomerName);

			if (loggedInCustomer == null)
				return false;

			var mappedcusr = await _context.Cust2CustMap
				.FirstOrDefaultAsync(a => a.phoneno == loggedInCustomer.phoneno);

			if (mappedcusr != null)
			{
				var mappedCustomers = await _context.mappedcusts
					.Where(a => a.cust2custId == mappedcusr.id)
					.ToListAsync();

				return mappedCustomers.Any(mc => mc.customer == selectedCustomerName);
			}

			return false;
		}

		private async Task<List<string>> GetAvailableCustomers()
		{
			var role = HttpContext.Session.GetString("role");
			var userName = HttpContext.Session.GetString("UserName");

			if (role == "Customer")
			{
				// For customer role, return only the logged-in customer and mapped customers
				var loggedInCustomer = await _context.Customer_Master
					.Where(c => c.phoneno == userName)
					.FirstOrDefaultAsync();

				if (loggedInCustomer != null)
				{
					var customers = new List<string> { loggedInCustomer.Name };

					// Get mapped customers
					var mappedcusr = await _context.Cust2CustMap
						.FirstOrDefaultAsync(a => a.phoneno == loggedInCustomer.phoneno);

					if (mappedcusr != null)
					{
						var mappedCustomers = await _context.mappedcusts
							.Where(a => a.cust2custId == mappedcusr.id)
							.ToListAsync();

						foreach (var mappedCust in mappedCustomers)
						{
							customers.Add(mappedCust.customer);
						}
					}

					return customers.OrderBy(c => c).ToList();
				}

				return new List<string>();
			}
			else
			{
				// For admin/sales roles, return all customers
				return await _context.Customer_Master
					.Select(c => c.Name)
					.Distinct()
					.OrderBy(c => c)
					.ToListAsync();
			}
		}

		// Method to get customer dropdown similar to DealerMastersController
		private List<SelectListItem> GetCustomer()
		{
			if (HttpContext.Session.GetString("role") == "Sales")
			{
				var sales = _context.EmployeeMaster.Where(a => a.PhoneNumber == HttpContext.Session.GetString("UserName")).FirstOrDefault();

				var order = _context.EmpToCustMap.Where(a => a.phoneno == sales.PhoneNumber).AsNoTracking().FirstOrDefault();
				List<Cust2EmpMap> poDetails = new List<Cust2EmpMap>();
				if (order != null)
				{
					poDetails = _context.cust2EmpMaps.Where(d => d.empt2custid == order.id).AsNoTracking().ToList();
				}

				var lstProducts = new List<SelectListItem>();

				// Get customer IDs and names for the mapped customers
				foreach (var custEmpMap in poDetails)
				{
					var customer = _context.Customer_Master.FirstOrDefault(c => c.Name == custEmpMap.customer);
					if (customer != null)
					{
						lstProducts.Add(new SelectListItem
						{
							Value = customer.Name, // Use Name instead of Id for consistency with existing code
							Text = customer.Name
						});
					}
				}

				var defItem = new SelectListItem()
				{
					Value = "",
					Text = "-- Select Customer --"
				};

				lstProducts.Insert(0, defItem);

				return lstProducts;
			}
			else if (HttpContext.Session.GetString("role") == "Customer")
			{
				var loggedInCustomer = _context.Customer_Master.Where(a => a.phoneno == HttpContext.Session.GetString("UserName")).FirstOrDefault();
				var lstProducts = new List<SelectListItem>();
				var lstProducts1 = new List<SelectListItem>();

				var mappedcusr = _context.Cust2CustMap.Where(a => a.phoneno == loggedInCustomer.phoneno).AsNoTracking().FirstOrDefault();

				// Get mapped customers
				if (mappedcusr != null)
				{
					var mappedCustomers = _context.mappedcusts.Where(a => a.cust2custId == mappedcusr.id).ToList();
					foreach (var mappedCust in mappedCustomers)
					{
						var customer = _context.Customer_Master.FirstOrDefault(c => c.Name == mappedCust.customer);
						if (customer != null)
						{
							lstProducts1.Add(new SelectListItem
							{
								Value = customer.Name, // Use Name instead of Id
								Text = customer.Name
							});
						}
					}
				}

				lstProducts1.Insert(0, new SelectListItem()
				{
					Value = loggedInCustomer.Name,
					Text = loggedInCustomer.Name
				});

				return lstProducts1;
			}
			else
			{
				var lstProducts = new List<SelectListItem>();
				var defItem = new SelectListItem()
				{
					Value = "",
					Text = "-- Select Customer --"
				};
				lstProducts.Add(defItem);

				var customers = _context.Customer_Master.OrderBy(a => a.Name).ToList();
				foreach (var customer in customers)
				{
					lstProducts.Add(new SelectListItem()
					{
						Value = customer.Name,
						Text = customer.Name
					});
				}
				return lstProducts;
			}
		}

		// AJAX method to get dealers by customer
		[HttpGet]
		public async Task<IActionResult> GetDealersByCustomer(string customerName)
		{
			try
			{
				if (string.IsNullOrEmpty(customerName))
				{
					return Json(new { success = true, dealers = new List<DealerMaster>() });
				}

				var customer = await _context.Customer_Master.FirstOrDefaultAsync(c => c.Name == customerName);
				if (customer == null)
				{
					return Json(new { success = true, dealers = new List<DealerMaster>() });
				}

				var dealers = await _context.DealerMasters
					.Where(d => d.DistributorId == customer.Id)
					.OrderBy(d => d.Name)
					.Select(d => new { Id = d.Id, Name = d.Name })
					.ToListAsync();

				// Add "All Dealers" option
				var allDealersOption = new { Id = 0, Name = "All Dealers" };
				dealers.Insert(0, allDealersOption);

				return Json(new { success = true, dealers = dealers });
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = ex.Message });
			}
		}
	}
}