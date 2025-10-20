using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Milk_Bakery.Data;
using Milk_Bakery.Models;
using Milk_Bakery.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;

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
					var quantityVariance = item.Qty - item.DeliverQnty;
					var amountVariance = orderedAmount - deliveredAmount;

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
				// Ensure we have the correct date values
				var fromDate = model.FromDate ?? DateTime.Now.Date;
				var toDate = model.ToDate ?? DateTime.Now.Date;
				
				// Generate report data
				var reportItems = await GenerateDeliveredQuantityReport(fromDate, toDate, model.CustomerName, model.DealerId, model.ShowOnlyVariance);
				
				// Log the number of items for debugging
				System.Diagnostics.Debug.WriteLine($"Exporting {reportItems.Count} items to Excel");

				// Create CSV content
				var csv = new System.Text.StringBuilder();
				csv.AppendLine("Customer,Dealer,Order Date,Order ID,Material,Short Code,Ordered Quantity,Delivered Quantity,Quantity Variance,Unit Price,Ordered Amount,Delivered Amount,Amount Variance");

				foreach (var item in reportItems)
				{
					// Escape any commas in text fields to prevent CSV formatting issues
					var customerName = item.CustomerName?.Replace(",", ";") ?? "";
					var dealerName = item.DealerName?.Replace(",", ";") ?? "";
					var materialName = item.MaterialName?.Replace(",", ";") ?? "";
					var shortCode = item.ShortCode?.Replace(",", ";") ?? "";
					
					csv.AppendLine($"{customerName},{dealerName},{item.OrderDate:yyyy-MM-dd},{item.OrderId},{materialName},{shortCode},{item.OrderedQuantity},{item.DeliveredQuantity},{item.QuantityVariance},{item.UnitPrice:F2},{item.OrderedAmount:F2},{item.DeliveredAmount:F2},{item.AmountVariance:F2}");
				}

				var fileName = $"DeliveredQuantityReport_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
				byte[] fileBytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
				return File(fileBytes, "text/csv", fileName);
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