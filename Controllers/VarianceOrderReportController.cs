using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Milk_Bakery.Data;
using Milk_Bakery.Models;
using Milk_Bakery.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering; // Add this for SelectListItem

namespace Milk_Bakery.Controllers
{
	[Authentication]
	public class VarianceOrderReportController : Controller
	{
		private readonly MilkDbContext _context;

		public VarianceOrderReportController(MilkDbContext context)
		{
			_context = context;
		}

		public async Task<IActionResult> Index()
		{
			var viewModel = new VarianceOrderReportViewModel
			{
				FromDate = DateTime.Now,
				ToDate = DateTime.Now,
				AvailableCustomers = await GetAvailableCustomers()
			};

			// Set default customer for Customer role
			var role = HttpContext.Session.GetString("role");
			if (string.Equals(role, "Customer", StringComparison.OrdinalIgnoreCase))
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
		public async Task<IActionResult> GenerateReport(VarianceOrderReportViewModel model)
		{
			var reportItems = await GenerateVarianceReport(model.FromDate, model.ToDate, model.CustomerName, model.ShowOnlyVariance);

			var viewModel = new VarianceOrderReportViewModel
			{
				ReportItems = reportItems,
				FromDate = model.FromDate,
				ToDate = model.ToDate,
				CustomerName = model.CustomerName,
				ShowOnlyVariance = model.ShowOnlyVariance,
				AvailableCustomers = await GetAvailableCustomers()
			};

			ViewBag.Customers = GetCustomer(); // Add customer dropdown data
			return View("Index", viewModel);
		}

		private async Task<List<VarianceReportItem>> GenerateVarianceReport(DateTime? fromDate, DateTime? toDate, string customerName, bool showOnlyVariance)
		{
			var reportItems = new List<VarianceReportItem>();

			// Get purchase orders within date range and customer filter
			var purchaseOrdersQuery = _context.PurchaseOrder.Where(a => a.verifyflag == 1 && a.processflag == 1)
				.Include(po => po.ProductDetails)
				.AsNoTracking()
				.Where(po => po.OrderDate >= fromDate && po.OrderDate <= toDate);

			// Apply customer filter based on role
			var role = HttpContext.Session.GetString("role");
			if (string.Equals(role, "Customer", StringComparison.OrdinalIgnoreCase))
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
						purchaseOrdersQuery = purchaseOrdersQuery.Where(po => po.Customername == customerName);
					}
					else
					{
						purchaseOrdersQuery = purchaseOrdersQuery.Where(po => po.Customername == customer);
					}
				}
			}
			else if (!string.IsNullOrEmpty(customerName))
			{
				// For other roles, use the selected customer
				purchaseOrdersQuery = purchaseOrdersQuery.Where(po => po.Customername == customerName);
			}

			var purchaseOrders = await purchaseOrdersQuery.ToListAsync();

			if (purchaseOrders.Count == 0)
			{
				return reportItems;
			}

			// Get invoices within date range and customer filter
			var invoicesQuery = _context.Invoices
				.Include(i => i.InvoiceMaterials)
				.AsNoTracking()
				.Where(i => i.OrderDate >= fromDate && i.OrderDate <= toDate && i.setflag == 1);

			// Apply customer filter based on role
			if (string.Equals(role, "Customer", StringComparison.OrdinalIgnoreCase))
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
						invoicesQuery = invoicesQuery.Where(i => i.BillToName == customerName);
					}
					else
					{
						invoicesQuery = invoicesQuery.Where(i => i.BillToName == customer);
					}
				}
			}
			else if (!string.IsNullOrEmpty(customerName))
			{
				// For other roles, use the selected customer
				invoicesQuery = invoicesQuery.Where(i => i.BillToName == customerName);
			}

			var invoices = await invoicesQuery.ToListAsync();

			// Group purchase order data by customer and material
			var orderedData = purchaseOrders
				.SelectMany(po => po.ProductDetails, (po, pd) => new
				{
					CustomerName = po.Customername,
					MaterialName = pd.ProductName,
					// Use ShortName from MaterialMaster instead of ProductCode
					ShortCode = _context.MaterialMaster
						.Where(m => m.material3partycode == pd.ProductCode)
						.Select(m => m.ShortName)
						.FirstOrDefault() ?? pd.ProductCode, // Fallback to ProductCode if not found
					Quantity = pd.qty,
					Amount = pd.Price
				})
				.GroupBy(x => new { x.CustomerName, x.MaterialName, x.ShortCode })
				.Select(g => new
				{
					g.Key.CustomerName,
					g.Key.MaterialName,
					g.Key.ShortCode,
					Quantity = g.Sum(x => x.Quantity),
					Amount = g.Sum(x => x.Amount)
				})
				.ToList();

			// Group invoice data by customer and material
			var invoicedData = invoices
				.SelectMany(inv => inv.InvoiceMaterials, (inv, im) => new
				{
					CustomerName = inv.BillToName,
					MaterialName = im.ProductDescription,
					// Use ShortName from MaterialMaster instead of MaterialSapCode
					ShortCode = _context.MaterialMaster
						.Where(m => m.material3partycode == im.MaterialSapCode)
						.Select(m => m.ShortName)
						.FirstOrDefault() ?? im.MaterialSapCode, // Fallback to MaterialSapCode if not found
					Quantity = im.QuantityCases, // Total quantity
					Amount = (decimal)(im.QuantityUnits)// Calculate amount properly

				})
				.GroupBy(x => new { x.CustomerName, x.MaterialName, x.ShortCode })
				.Select(g => new
				{
					g.Key.CustomerName,
					g.Key.MaterialName,
					g.Key.ShortCode,
					Quantity = g.Sum(x => x.Quantity),
					Amount = g.Sum(x => x.Amount)
				})
				.ToList();

			// Combine data to calculate variance
			var allCustomers = orderedData.Select(x => x.CustomerName)
				.Union(invoicedData.Select(x => x.CustomerName))
				.Distinct();

			foreach (var customer in allCustomers)
			{
				var customerOrderedData = orderedData.Where(x => x.CustomerName == customer).ToDictionary(x => x.ShortCode, x => x);
				var customerInvoicedData = invoicedData.Where(x => x.CustomerName == customer).ToDictionary(x => x.ShortCode, x => x);

				var allCustomerMaterials = customerOrderedData.Keys.Union(customerInvoicedData.Keys).Distinct();

				foreach (var shortCode in allCustomerMaterials)
				{
					var orderedQuantity = customerOrderedData.ContainsKey(shortCode) ? customerOrderedData[shortCode].Quantity : 0;
					var invoicedQuantity = customerInvoicedData.ContainsKey(shortCode) ? customerInvoicedData[shortCode].Quantity : 0;
					var orderedAmount = customerOrderedData.ContainsKey(shortCode) ? customerOrderedData[shortCode].Amount : 0;
					var invoicedAmount = customerInvoicedData.ContainsKey(shortCode) ? customerInvoicedData[shortCode].Amount : 0;

					// Only add items where there's data
					if (orderedQuantity > 0 || invoicedQuantity > 0)
					{
						var materialName = "";
						if (customerOrderedData.ContainsKey(shortCode))
							materialName = customerOrderedData[shortCode].MaterialName;
						else if (customerInvoicedData.ContainsKey(shortCode))
							materialName = customerInvoicedData[shortCode].MaterialName;

						var reportItem = new VarianceReportItem
						{
							CustomerName = customer,
							MaterialName = materialName,
							// Use ShortCode instead of MaterialCode
							ShortCode = shortCode,
							OrderedQuantity = orderedQuantity,
							InvoicedQuantity = invoicedQuantity,
							QuantityVariance = orderedQuantity - invoicedQuantity,
							OrderedAmount = orderedAmount,
							InvoicedAmount = invoicedAmount,
							AmountVariance = orderedAmount - invoicedAmount
						};

						// If showOnlyVariance is true, only add items with variance
						if (!showOnlyVariance || reportItem.HasVariance)
						{
							reportItems.Add(reportItem);
						}
					}
				}
			}

			return reportItems.OrderBy(r => r.CustomerName).ThenBy(r => r.MaterialName).ToList();
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

			if (string.Equals(role, "Customer", StringComparison.OrdinalIgnoreCase))
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
			if (string.Equals(HttpContext.Session.GetString("role"), "Sales", StringComparison.OrdinalIgnoreCase))
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
					Text = "All Customers"
				};

				lstProducts.Insert(0, defItem);

				return lstProducts;
			}
			else if (string.Equals(HttpContext.Session.GetString("role"), "Customer", StringComparison.OrdinalIgnoreCase))
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

				// Add the logged-in customer
				if (loggedInCustomer != null)
				{
					lstProducts.Add(new SelectListItem
					{
						Value = loggedInCustomer.Name, // Use Name instead of Id
						Text = loggedInCustomer.Name
					});
				}

				var defItem = new SelectListItem()
				{
					Value = "",
					Text = "All Customers"
				};

				lstProducts.AddRange(lstProducts1);
				lstProducts.Insert(0, defItem);

				return lstProducts;
			}
			else
			{
				var lstProducts = new List<SelectListItem>();

				lstProducts = _context.Customer_Master.AsNoTracking().Select(n =>
				new SelectListItem
				{
					Value = n.Name, // Use Name instead of Id for consistency
					Text = n.Name
				}).ToList();

				var defItem = new SelectListItem()
				{
					Value = "",
					Text = "All Customers"
				};

				lstProducts.Insert(0, defItem);

				return lstProducts;
			}
		}
	}
}