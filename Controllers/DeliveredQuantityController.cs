using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Milk_Bakery.Data;
using Milk_Bakery.Models;
using Milk_Bakery.ViewModels;
using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace Milk_Bakery.Controllers
{
	[Authentication]
	public class DeliveredQuantityController : Controller
	{
		private readonly MilkDbContext _context;
		public INotyfService _notifyService { get; }

		public DeliveredQuantityController(MilkDbContext context, INotyfService notifyService)
		{
			_context = context;
			_notifyService = notifyService;
		}

		// GET: DeliveredQuantity
		public async Task<IActionResult> Index()
		{
			var viewModel = new DeliveredQuantityEntryViewModel();

			// Get available distributors based on user role
			viewModel.AvailableDistributors = await GetAvailableDistributors();

			return View(viewModel);
		}

		// POST: DeliveredQuantity/LoadDealers
		[HttpPost]
		public async Task<IActionResult> LoadDealers(int distributorId)
		{
			var viewModel = new DeliveredQuantityEntryViewModel();

			// Set selected distributor
			viewModel.SelectedDistributorId = distributorId;
			viewModel.AvailableDistributors = await GetAvailableDistributors();

			// Get dealers for the selected distributor
			viewModel.Dealers = await _context.DealerMasters
				.Where(d => d.DistributorId == distributorId)
				.ToListAsync();

			// Get dealer orders for each dealer
			foreach (var dealer in viewModel.Dealers)
			{
				var orders = await _context.DealerOrders
					.Where(dbo => dbo.DealerId == dealer.Id && dbo.DistributorId == distributorId && dbo.OrderDate == DateTime.Now.Date)
					.Include(dbo => dbo.DealerOrderItems)
					.ToListAsync();

				viewModel.DealerOrders[dealer.Id] = orders;
			}

			return PartialView("_DeliveredQuantityPartial", viewModel);
		}

		// POST: DeliveredQuantity/Save
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Save([FromBody] DeliveredQuantitySaveModel model)
		{
			try
			{
				// Log the received model for debugging
				//System.Diagnostics.Debug.WriteLine($"Received model: OrderId={model?.OrderId}, ItemsCount={model?.Items?.Count ?? 0}");

				// Validate model
				if (model == null)
				{
					return Json(new { success = false, message = "Invalid request data." });
				}

				// Validate delivered quantities - cannot be negative
				foreach (var item in model.Items)
				{
					if (item.DeliveredQuantity < 0)
					{
						return Json(new { success = false, message = "Delivered quantity cannot be negative." });
					}
				}

				var dealerOrder = await _context.DealerOrders
					.Include(o => o.DealerOrderItems)
					.FirstOrDefaultAsync(o => o.Id == model.OrderId);

				if (dealerOrder == null)
				{
					return Json(new { success = false, message = "Order not found." });
				}



				// Update delivered quantities
				decimal grandTotal = 0;
				var updatedItems = new List<DeliveredQuantityItemViewModel>();

				foreach (var item in dealerOrder.DealerOrderItems)
				{
					var modelItem = model.Items.FirstOrDefault(i => i.ItemId == item.Id);
					if (modelItem != null)
					{
						item.DeliverQnty = modelItem.DeliveredQuantity;
						_context.DealerOrderItems.Update(item);

						// Calculate updated values for response
						var orderedAmount = item.Qty * item.Rate;
						var deliveredAmount = item.DeliverQnty * item.Rate;
						var amountVariance = orderedAmount - deliveredAmount;

						var updatedItem = new DeliveredQuantityItemViewModel
						{
							ItemId = item.Id,
							ItemName = item.MaterialName,
							ShortCode = item.ShortCode,
							OrderedQuantity = item.Qty,
							DeliveredQuantity = item.DeliverQnty,
							UnitPrice = item.Rate,
							TotalAmount = deliveredAmount,
							Variance = item.Qty - item.DeliverQnty,
							AmountVariance = amountVariance
						};

						updatedItems.Add(updatedItem);
						grandTotal += updatedItem.TotalAmount;
					}
				}

				await _context.SaveChangesAsync();

				_notifyService.Success("Delivered quantities updated successfully.");
				return Json(new
				{
					success = true,
					items = updatedItems,
					grandTotal = grandTotal
				});
			}
			catch (Exception ex)
			{
				_notifyService.Error("An error occurred while updating delivered quantities.");
				return Json(new { success = false, message = ex.Message });
			}
		}

		#region Helper Methods

		private async Task<string> GetDistributorName(int distributorId)
		{
			var customer = await _context.Customer_Master
				.FirstOrDefaultAsync(c => c.Id == distributorId);
			return customer?.Name ?? "Unknown Distributor";
		}

		private async Task<string> GetDealerName(int dealerId)
		{
			var dealer = await _context.DealerMasters
				.FirstOrDefaultAsync(d => d.Id == dealerId);
			return dealer?.Name ?? "Unknown Dealer";
		}

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

		#endregion
	}

	public class DeliveredQuantitySaveModel
	{
		public int OrderId { get; set; }
		public List<DeliveredQuantitySaveItem> Items { get; set; } = new List<DeliveredQuantitySaveItem>();
	}

	public class DeliveredQuantitySaveItem
	{
		public int ItemId { get; set; }
		public int DeliveredQuantity { get; set; }
	}
}