using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Milk_Bakery.Data;
using Milk_Bakery.Models;
using Milk_Bakery.ViewModels;
using AspNetCoreHero.ToastNotification.Abstractions;

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

		// GET: DeliveredQuantity/GetDealersByDistributor
		[HttpGet]
		public async Task<IActionResult> GetDealersByDistributor(int distributorId)
		{
			try
			{
				var today = DateTime.Now.Date;

				// Get all dealers for this distributor
				var dealers = await _context.DealerMasters
					.Where(d => d.DistributorId == distributorId)
					.ToListAsync();

				// Get dealer IDs that have orders today
				var dealerIdsWithOrdersToday = await _context.DealerOrders
					.Where(o => o.DistributorId == distributorId && o.OrderDate == today)
					.Select(o => o.DealerId)
					.Distinct()
					.ToListAsync();

				// Create result with order status, sorting to put non-ordered dealers first
				var dealerResults = dealers
					.Select(d => new
					{
						Id = d.Id,
						Name = d.Name,
						hasOrderedToday = dealerIdsWithOrdersToday.Contains(d.Id)
					})
					.OrderBy(d => d.hasOrderedToday) // False (0) comes before True (1)
					.ToList();

				return Json(new { success = true, dealers = dealerResults });
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = ex.Message });
			}
		}

		// POST: DeliveredQuantity/LoadDealerOrders
		[HttpPost]
		public async Task<IActionResult> LoadDealerOrders(int distributorId, int dealerId)
		{
			try
			{
				var viewModel = new DeliveredQuantityEntryViewModel
				{
					SelectedDistributorId = distributorId,
					AvailableDistributors = await GetAvailableDistributors()
				};

				// Get the specific dealer
				var dealer = await _context.DealerMasters
					.FirstOrDefaultAsync(d => d.Id == dealerId && d.DistributorId == distributorId);

				if (dealer == null)
				{
					return Json(new { success = false, message = "Dealer not found." });
				}

				viewModel.Dealers = new List<DealerMaster> { dealer };

				// Get the most recent dealer order for this dealer (not just today's orders)
				var latestOrder = await _context.DealerOrders
					.Where(dbo => dbo.DealerId == dealer.Id && dbo.DistributorId == distributorId && dbo.ProcessFlag == 1)
					.OrderByDescending(dbo => dbo.OrderDate)
					.FirstOrDefaultAsync();

				// If there's a latest order, get all orders for that date
				List<DealerOrder> orders = new List<DealerOrder>();
				if (latestOrder != null)
				{
					// Check if the order has been processed (ProcessFlag = 1)
					if (latestOrder.ProcessFlag != 1)
					{
						return Json(new { success = false, message = "This order has not been processed yet and cannot have delivered quantities entered." });
					}

					orders = await _context.DealerOrders
						.Where(dbo => dbo.DealerId == dealer.Id && dbo.DistributorId == distributorId && dbo.OrderDate == latestOrder.OrderDate)
						.Include(dbo => dbo.DealerOrderItems)
						.ToListAsync();
				}

				viewModel.DealerOrders[dealer.Id] = orders;

				// Return the partial view for this single dealer
				return PartialView("_DeliveredQuantityPartial", viewModel);
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = ex.Message });
			}
		}

		// POST: DeliveredQuantity/LoadExcelView
		[HttpPost]
		public async Task<IActionResult> LoadExcelView(int distributorId)
		{
			try
			{
				var viewModel = new DeliveredQuantityExcelViewModel
				{
					SelectedDistributorId = distributorId,
					AvailableDistributors = await GetAvailableDistributors()
				};

				// Get dealers for the selected distributor
				viewModel.Dealers = await _context.DealerMasters
					.Where(d => d.DistributorId == distributorId)
					.ToListAsync();

				// Get all orders for these dealers that have been processed but not yet delivered
				var dealerIds = viewModel.Dealers.Select(d => d.Id).ToList();
				var allOrders = await _context.DealerOrders
					.Where(o => dealerIds.Contains(o.DealerId) && o.DistributorId == distributorId && o.ProcessFlag == 1)
					.Include(o => o.DealerOrderItems)
					.ToListAsync();

				// Group orders by dealer and only keep the latest order date's orders for each dealer
				foreach (var dealer in viewModel.Dealers)
				{
					var dealerOrders = allOrders.Where(o => o.DealerId == dealer.Id).ToList();
					if (dealerOrders.Any())
					{
						// Get the latest order date for this dealer
						var latestDate = dealerOrders.Max(o => o.OrderDate);
						// Get all orders for this dealer that are from the latest date
						var latestOrders = dealerOrders.Where(o => o.OrderDate == latestDate).ToList();
						viewModel.DealerOrders[dealer.Id] = latestOrders;
					}
					else
					{
						viewModel.DealerOrders[dealer.Id] = new List<DealerOrder>();
					}
				}

				// Get available materials that are mapped to the customer segment
				// First get the customer/distributor
				var distributor = await _context.Customer_Master
					.FirstOrDefaultAsync(c => c.Id == distributorId);

				List<MaterialMaster> availableMaterials;
				if (distributor != null)
				{
					// Get segments mapped to this customer
					var segmentMappings = await _context.CustomerSegementMap
						.Where(m => m.Customername == distributor.Name)
						.ToListAsync();

					// If segments found, filter materials by those segments
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
					// If no distributor found, load all active materials
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

				return PartialView("_DeliveredQuantityExcelPartial", viewModel);
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = ex.Message });
			}
		}

		// POST: DeliveredQuantity/SaveExcelView
		[HttpPost]
		public async Task<IActionResult> SaveExcelView([FromBody] List<ExcelViewOrderModel> allOrders)
		{
			// Removed validation that prevents saving when there are no changes
			// This allows saving even when all "Items to Add" values are not 0

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

					if (dealerOrder == null || dealerOrder.ProcessFlag != 1)
					{
						continue; // Skip this order if not found or not processed
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
							// Update delivered quantity
							item.DeliverQnty = intQuantities[material.Id];
							_context.DealerOrderItems.Update(item);
						}
					}

					// Update dealer order deliver flag to 1 (delivered)
					dealerOrder.DeliverFlag = 1;
					_context.DealerOrders.Update(dealerOrder);

					// Calculate grand total for this order
					decimal grandTotal = 0;
					foreach (var item in dealerOrder.DealerOrderItems)
					{
						grandTotal += item.DeliverQnty * item.Rate;
					}

					// Save data in DealerOutstanding table
				var dealerOutstanding = await _context.DealerOutstandings
					.FirstOrDefaultAsync(d => d.DealerId == dealerOrder.DealerId && d.DeliverDate == dealerOrder.OrderDate);

				// Get the latest outstanding balance from previous dates
				var previousOutstanding = await _context.DealerOutstandings
					.Where(d => d.DealerId == dealerOrder.DealerId && d.DeliverDate < dealerOrder.OrderDate)
					.OrderByDescending(d => d.DeliverDate)
					.FirstOrDefaultAsync();

				decimal previousBalance = previousOutstanding?.BalanceAmount ?? 0;

				if (dealerOutstanding == null)
				{
					// Create new DealerOutstanding record
					dealerOutstanding = new DealerOutstanding
					{
						DealerId = dealerOrder.DealerId,
						DeliverDate = dealerOrder.OrderDate,
						InvoiceAmount = grandTotal,
						PaidAmount = 0,
						BalanceAmount = grandTotal + previousBalance // Add previous balance
					};
					_context.DealerOutstandings.Add(dealerOutstanding);
				}
				else
				{
					// Update existing DealerOutstanding record
					dealerOutstanding.InvoiceAmount = grandTotal;
					dealerOutstanding.BalanceAmount = grandTotal - dealerOutstanding.PaidAmount + previousBalance; // Add previous balance
					_context.DealerOutstandings.Update(dealerOutstanding);
				}
				}

				await _context.SaveChangesAsync();
				await transaction.CommitAsync();

				_notifyService.Success("All delivered quantities saved successfully.");
				return Json(new { success = true });
			}
			catch (Exception ex)
			{
				await transaction.RollbackAsync();
				_notifyService.Error("An error occurred while saving delivered quantities: " + ex.Message);
				return Json(new { success = false, message = ex.Message });
			}
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

				// Save data in DealerOutstanding table
				var dealerOutstanding = await _context.DealerOutstandings
					.FirstOrDefaultAsync(d => d.DealerId == dealerOrder.DealerId && d.DeliverDate == dealerOrder.OrderDate);

				// Get the latest outstanding balance from previous dates
				var previousOutstanding = await _context.DealerOutstandings
					.Where(d => d.DealerId == dealerOrder.DealerId && d.DeliverDate < dealerOrder.OrderDate)
					.OrderByDescending(d => d.DeliverDate)
					.FirstOrDefaultAsync();

				decimal previousBalance = previousOutstanding?.BalanceAmount ?? 0;

				if (dealerOutstanding == null)
				{
					// Create new DealerOutstanding record
					dealerOutstanding = new DealerOutstanding
					{
						DealerId = dealerOrder.DealerId,
						DeliverDate = dealerOrder.OrderDate,
						InvoiceAmount = grandTotal,
						PaidAmount = 0,
						BalanceAmount = grandTotal + previousBalance // Add previous balance
					};
					_context.DealerOutstandings.Add(dealerOutstanding);
				}
				else
				{
					// Update existing DealerOutstanding record
					dealerOutstanding.InvoiceAmount = grandTotal;
					dealerOutstanding.BalanceAmount = grandTotal - dealerOutstanding.PaidAmount + previousBalance; // Add previous balance
					_context.DealerOutstandings.Update(dealerOutstanding);
				}

				// Update dealer order deliver flag to 1 (delivered)
				dealerOrder.DeliverFlag = 1;
				_context.DealerOrders.Update(dealerOrder);

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