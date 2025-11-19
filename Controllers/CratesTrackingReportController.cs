using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Milk_Bakery.Data;
using Milk_Bakery.Models;

namespace Milk_Bakery.Controllers
{
	[Authentication]
	public class CratesTrackingReportController : Controller
	{
		private readonly MilkDbContext _context;

		public CratesTrackingReportController(MilkDbContext context)
		{
			_context = context;
		}

		public IActionResult Index()
		{
			ViewBag.customer = GetCustomer();
			ViewBag.division = GetDivision();
			return View();
		}

		[HttpPost]
		public IActionResult GetReportData(int? customerId, DateTime? fromDate, DateTime? toDate, string division)
		{
			var query = _context.CratesManages.Include(c => c.Customer).Include(c => c.CratesType).AsQueryable();

			// Apply customer filter - only filter by customer if a specific customer is selected (customerId > 0)
			if (customerId.HasValue && customerId.Value > 0)
			{
				query = query.Where(c => c.CustomerId == customerId.Value);
			}

			// Apply date range filter
			if (fromDate.HasValue && toDate.HasValue)
			{
				query = query.Where(c => c.DispDate >= fromDate.Value && c.DispDate <= toDate.Value);
			}

			// Apply division filter
			if (!string.IsNullOrEmpty(division))
			{
				query = query.Where(c => c.Customer.Division == division);
			}

			var reportData = query.ToList();

			// Calculate totals
			var totalOpening = reportData.Sum(c => c.Opening);
			var totalInward = reportData.Sum(c => c.Inward);
			var totalOutward = reportData.Sum(c => c.Outward);
			var totalBalance = reportData.Sum(c => c.Balance);

			ViewBag.TotalOpening = totalOpening;
			ViewBag.TotalInward = totalInward;
			ViewBag.TotalOutward = totalOutward;
			ViewBag.TotalBalance = totalBalance;

			return PartialView("_ReportTable", reportData);
		}

		private List<SelectListItem> GetCustomer()
		{
			var role = HttpContext.Session.GetString("role");
			var userName = HttpContext.Session.GetString("UserName");

			// Add "All Customers" option at the beginning
			var customers = new List<SelectListItem>();

			if (string.Equals(role, "Customer", StringComparison.OrdinalIgnoreCase))
			{
				// For customer role, get the logged-in customer and their mapped customers
				var loggedInCustomer = _context.Customer_Master
					.AsNoTracking()
					.FirstOrDefault(c => c.phoneno == userName);

				if (loggedInCustomer != null)
				{
					customers.Add(new SelectListItem
					{
						Value = loggedInCustomer.Id.ToString(),
						Text = loggedInCustomer.Name
					});

					// Get mapped customers
					var mappedCustomer = _context.Cust2CustMap
						.AsNoTracking()
						.FirstOrDefault(c => c.phoneno == userName);

					if (mappedCustomer != null)
					{
						var mappedCusts = _context.mappedcusts
							.AsNoTracking()
							.Where(mc => mc.cust2custId == mappedCustomer.id)
							.ToList();

						foreach (var mapped in mappedCusts)
						{
							var customer = _context.Customer_Master
								.AsNoTracking()
								.FirstOrDefault(c => c.Name == mapped.customer);
							if (customer != null)
							{
								customers.Add(new SelectListItem
								{
									Value = customer.Id.ToString(),
									Text = customer.Name
								});
							}
						}
					}
				}
			}
			else if (string.Equals(role, "Sales", StringComparison.OrdinalIgnoreCase))
			{
				// For sales role, get mapped customers
				var empToCustMap = _context.EmpToCustMap
					.AsNoTracking()
					.FirstOrDefault(e => e.empl == userName);

				if (empToCustMap != null)
				{
					var mappedCusts = _context.mappedcusts
						.AsNoTracking()
						.Where(mc => mc.cust2custId == empToCustMap.id)
						.ToList();

					foreach (var mapped in mappedCusts)
					{
						var customer = _context.Customer_Master
							.AsNoTracking()
							.FirstOrDefault(c => c.Name == mapped.customer);
						if (customer != null)
						{
							customers.Add(new SelectListItem
							{
								Value = customer.Id.ToString(),
								Text = customer.Name
							});
						}
					}
				}
			}
			else
			{
				// For admin role, get all customers
				var allCustomers = _context.Customer_Master
					.AsNoTracking()
					.Select(n => new SelectListItem
					{
						Value = n.Id.ToString(),
						Text = n.Name
					}).ToList();

				allCustomers.Insert(0, new SelectListItem
				{
					Value = "0",
					Text = "All Customers"
				});

				customers.AddRange(allCustomers);
			}

			return customers.ToList();
		}

		private List<SelectListItem> GetDivision()
		{
			var lstProducts = new List<SelectListItem>();

			lstProducts = _context.Customer_Master.AsNoTracking()
				.Where(c => !string.IsNullOrEmpty(c.Division))
				.Select(n => n.Division)
				.Distinct()
				.Select(d => new SelectListItem
				{
					Value = d,
					Text = d
				}).ToList();

			var defItem = new SelectListItem()
			{
				Value = "",
				Text = "All Divisions"
			};

			lstProducts.Insert(0, defItem);

			return lstProducts;
		}
	}
}