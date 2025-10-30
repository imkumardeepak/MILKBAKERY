using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Milk_Bakery.Data;
using Milk_Bakery.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Milk_Bakery.Controllers
{
	[Authentication]
	public class DealerOutstandingController : Controller
	{
		private readonly MilkDbContext _context;

		public DealerOutstandingController(MilkDbContext context)
		{
			_context = context;
		}

		// GET: DealerOutstanding
		public async Task<IActionResult> Index()
		{
			// For now, just return the view. We'll load data via AJAX.
			return View();
		}

		// GET: DealerOutstanding/GetDistributors
		[HttpGet]
		public async Task<IActionResult> GetDistributors()
		{
			try
			{
				var distributors = await _context.Customer_Master.ToListAsync();
				var distributorList = distributors.Select(c => new { id = c.Id, name = c.Name }).ToList();
				return Json(new { success = true, distributors = distributorList });
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = ex.Message });
			}
		}

		// GET: DealerOutstanding/GetDealerOutstandings
		[HttpGet]
		public async Task<IActionResult> GetDealerOutstandings(int distributorId)
		{
			try
			{
				// Get dealers for the selected distributor
				var dealers = await _context.DealerMasters
					.Where(d => d.DistributorId == distributorId)
					.ToListAsync();

				var dealerOutstandings = new List<object>();

				foreach (var dealer in dealers)
				{
					// Get the latest outstanding record for this dealer
					var latestOutstanding = await _context.DealerOutstandings
						.Where(a => a.DealerId == dealer.Id)
						.OrderByDescending(a => a.DeliverDate)
						.FirstOrDefaultAsync();

					if (latestOutstanding != null)
					{
						dealerOutstandings.Add(new
						{
							dealerId = latestOutstanding.DealerId,
							dealerName = dealer.Name,
							deliverDate = latestOutstanding.DeliverDate.ToString("dd/MM/yyyy"),
							invoiceAmount = latestOutstanding.InvoiceAmount,
							outstandingAmount = latestOutstanding.OutstandingAmount,
							receivedAmount = latestOutstanding.ReceivedAmount
						});
					}
					else
					{
						// If no record exists, create a default entry
						dealerOutstandings.Add(new
						{
							dealerId = dealer.Id,
							dealerName = dealer.Name,
							deliverDate = "N/A",
							invoiceAmount = 0m,
							outstandingAmount = 0m,
							receivedAmount = 0m
						});
					}
				}

				return Json(new { success = true, outstandings = dealerOutstandings });
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = ex.Message });
			}
		}

		// POST: DealerOutstanding/SaveReceivedAmount
		[HttpPost]
		public async Task<IActionResult> SaveReceivedAmount(int dealerId, decimal receivedAmount)
		{
			try
			{
				// Get the latest outstanding record for this dealer
				var latestOutstanding = await _context.DealerOutstandings
					.Where(a => a.DealerId == dealerId)
					.OrderByDescending(a => a.DeliverDate)
					.FirstOrDefaultAsync();

				if (latestOutstanding != null)
				{
					latestOutstanding.ReceivedAmount = receivedAmount;
					// Update outstanding amount based on received amount
					latestOutstanding.OutstandingAmount = latestOutstanding.InvoiceAmount - receivedAmount;
					await _context.SaveChangesAsync();

					return Json(new { success = true, message = "Received amount updated successfully." });
				}
				else
				{
					return Json(new { success = false, message = "No outstanding record found for this dealer." });
				}
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = ex.Message });
			}
		}
	}
}