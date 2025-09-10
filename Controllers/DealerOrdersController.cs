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
	public class DealerOrdersController : Controller
	{
		private readonly MilkDbContext _context;
		public INotyfService _notifyService { get; }

		public DealerOrdersController(MilkDbContext context, INotyfService notifyService)
		{
			_context = context;
			_notifyService = notifyService;
		}

		// GET: DealerOrders
		public async Task<IActionResult> Index()
		{
			var viewModel = new DealerOrdersViewModel();

			// Get available distributors based on user role
			viewModel.AvailableDistributors = await GetAvailableDistributors();

			return View(viewModel);
		}

		// POST: DealerOrders/LoadDealers
		[HttpPost]
		public async Task<IActionResult> LoadDealers(int distributorId)
		{
			var viewModel = new DealerOrdersViewModel();

			// Set selected distributor
			viewModel.SelectedDistributorId = distributorId;
			viewModel.AvailableDistributors = await GetAvailableDistributors();

			// Get dealers for the selected distributor
			viewModel.Dealers = await _context.DealerMasters
				.Where(d => d.DistributorId == distributorId)
				.ToListAsync();

			// Get basic orders for each dealer
			foreach (var dealer in viewModel.Dealers)
			{
				var basicOrders = await _context.DealerBasicOrders
					.Where(dbo => dbo.DealerId == dealer.Id)
					.ToListAsync();

				viewModel.DealerBasicOrders[dealer.Id] = basicOrders;

				// Initialize order item quantities
				if (!viewModel.DealerOrderItemQuantities.ContainsKey(dealer.Id))
				{
					viewModel.DealerOrderItemQuantities[dealer.Id] = new Dictionary<int, int>();
				}

				foreach (var order in basicOrders)
				{
					viewModel.DealerOrderItemQuantities[dealer.Id][order.Id] = order.Quantity;
				}
			}

			// Get available materials
			viewModel.AvailableMaterials = await _context.MaterialMaster.ToListAsync();

			return PartialView("_DealerOrdersPartial", viewModel);
		}

		// POST: DealerOrders/SaveOrders
		[HttpPost]
		public async Task<IActionResult> SaveOrders(int SelectedDistributorId, Dictionary<string, Dictionary<string, string>> DealerOrderItemQuantities)
		{
			try
			{
				// Check if we have any data
				if (DealerOrderItemQuantities == null || !DealerOrderItemQuantities.Any())
				{
					_notifyService.Warning("No order data received.");
					return Json(new { success = true }); // Still return success as no data to save is not an error
				}

				// Get the current date for order date
				var orderDate = DateTime.Now.Date;

				// Convert string-based dictionary to int-based dictionary
				var intQuantities = new Dictionary<int, Dictionary<int, int>>();
				foreach (var dealerEntry in DealerOrderItemQuantities)
				{
					// Extract dealer ID from the key - handle both formats:
					// 1. "DealerOrderItemQuantities[123]" (with brackets)
					// 2. "123" (just the ID)
					var key = dealerEntry.Key;
					int dealerId = 0;
					
					// First try to match the bracketed format
					var dealerIdMatch = System.Text.RegularExpressions.Regex.Match(key, @"\[(\d+)\]");
					if (dealerIdMatch.Success && int.TryParse(dealerIdMatch.Groups[1].Value, out dealerId))
					{
						// Successfully extracted dealer ID from brackets
					}
					else
					{
						// Try to parse the key directly as an integer (non-bracketed format)
						if (!int.TryParse(key, out dealerId))
						{
							continue; // Skip this entry if we can't parse the dealer ID
						}
					}

					var orderItems = new Dictionary<int, int>();
					foreach (var itemEntry in dealerEntry.Value)
					{
						// Extract the basic order ID and quantity from the item entry
						// Handle format: "7" = "10" (basicOrderId = 7, quantity = 10)
						var basicOrderIdStr = itemEntry.Key;
						var quantityStr = itemEntry.Value;
						
						if (int.TryParse(basicOrderIdStr, out int basicOrderId) && 
							int.TryParse(quantityStr, out int quantity))
						{
							// Only add quantities > 0
							if (quantity > 0)
							{
								orderItems[basicOrderId] = quantity;
							}
						}
						else
						{
							// Try alternative parsing for bracketed format
							var itemMatch = System.Text.RegularExpressions.Regex.Match(basicOrderIdStr, @"\[(\d+)\]\[(\d+)\]");
							if (itemMatch.Success && 
								int.TryParse(itemMatch.Groups[1].Value, out int extractedDealerId) && 
								int.TryParse(itemMatch.Groups[2].Value, out basicOrderId) &&
								int.TryParse(quantityStr, out quantity))
							{
								// Only add quantities > 0
								if (quantity > 0)
								{
									orderItems[basicOrderId] = quantity;
								}
							}
							else
							{
								// Try another alternative parsing
								var altItemMatch = System.Text.RegularExpressions.Regex.Match(basicOrderIdStr, @"DealerOrderItemQuantities\[(\d+)\]\[(\d+)\]");
								if (altItemMatch.Success &&
									int.TryParse(altItemMatch.Groups[1].Value, out int altDealerId) &&
									int.TryParse(altItemMatch.Groups[2].Value, out basicOrderId) &&
									int.TryParse(quantityStr, out quantity))
								{
									// Only add quantities > 0
									if (quantity > 0)
									{
										orderItems[basicOrderId] = quantity;
									}
								}
							}
						}
					}
						
					if (orderItems.Count > 0)
					{
						intQuantities[dealerId] = orderItems;
					}
				}

				// Get dealers for the selected distributor
				var dealers = await _context.DealerMasters
					.Where(d => d.DistributorId == SelectedDistributorId)
					.ToListAsync();

				// Process each dealer's orders
				bool hasSavedOrders = false;
				foreach (var dealer in dealers)
				{
					// Check if this dealer has any order items
					if (intQuantities.ContainsKey(dealer.Id) && intQuantities[dealer.Id].Count > 0)
					{
						hasSavedOrders = true;
						// Create DealerOrder
						var dealerOrder = new DealerOrder
						{
							OrderDate = orderDate,
							DistributorId = SelectedDistributorId,
							DealerId = dealer.Id,
							DistributorCode = GetDistributorCode(SelectedDistributorId),
							ProcessFlag = 0 // Default to not processed
						};

						_context.DealerOrders.Add(dealerOrder);
						await _context.SaveChangesAsync();

						// Process order items based on quantities
						if (intQuantities.ContainsKey(dealer.Id))
						{
							foreach (var kvp in intQuantities[dealer.Id])
							{
								var basicOrderId = kvp.Key;
								var quantity = kvp.Value;

								// Only create order items for quantities > 0
								if (quantity > 0)
								{
									// Get the basic order to get material details
									var basicOrder = await _context.DealerBasicOrders
										.FirstOrDefaultAsync(dbo => dbo.Id == basicOrderId);

									if (basicOrder != null)
									{
										var orderItem = new DealerOrderItem
										{
											DealerOrderId = dealerOrder.Id,
											MaterialName = basicOrder.MaterialName,
											ShortCode = basicOrder.ShortCode,
											SapCode = basicOrder.SapCode,
											Qty = quantity,
											Rate = basicOrder.Quantity > 0 ? basicOrder.BasicAmount / basicOrder.Quantity : 0, // Calculate rate
											Price = quantity * (basicOrder.Quantity > 0 ? basicOrder.BasicAmount / basicOrder.Quantity : 0) // Calculate price
										};

										_context.DealerOrderItems.Add(orderItem);
									}
								}
							}
						}
					}
				}

				await _context.SaveChangesAsync();
				
				if (hasSavedOrders)
				{
					_notifyService.Success("Dealer orders saved successfully.");
				}
				else
				{
					_notifyService.Success("No orders with quantities greater than zero were found to save.");
				}
				
				return Json(new { success = true });
			}
			catch (Exception ex)
			{
				_notifyService.Error("An error occurred while saving dealer orders.");
				return Json(new { success = false, message = ex.Message });
			}
		}

		#region Helper Methods

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

		private string GetDistributorCode(int distributorId)
		{
			var customer = _context.Customer_Master.FirstOrDefault(c => c.Id == distributorId);
			return customer?.shortname ?? "";
		}

		#endregion
	}
}