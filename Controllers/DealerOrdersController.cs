using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Milk_Bakery.Data;
using Milk_Bakery.Models;
using Milk_Bakery.ViewModels;
using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using OfficeOpenXml;
using System.IO;
using System.Text;

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
            // Set the license context for EPPlus
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

		// GET: DealerOrders
		public async Task<IActionResult> Index()
		{
			var viewModel = new DealerOrdersViewModel();

			// Get available distributors based on user role
			viewModel.AvailableDistributors = await GetAvailableDistributors();

			return View(viewModel);
		}

        // GET: DealerOrders/DispatchRouteSheet
        public async Task<IActionResult> DispatchRouteSheet()
        {
            return View();
        }

        // GET: DealerOrders/GetAllCustomers
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

        // GET: DealerOrders/GetDispatchData
        [HttpGet]
        public async Task<IActionResult> GetDispatchData(DateTime? date, int? customerId)
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

                var dispatchData = new List<object>();

                foreach (var order in dealerOrders)
                {
                    // Get order items for this order
                    var orderItems = await _context.DealerOrderItems
                        .Where(i => i.DealerOrderId == order.Id)
                        .ToListAsync();

                    foreach (var item in orderItems)
                    {
                        // Get material to get unit information
                        var material = await _context.MaterialMaster
                            .FirstOrDefaultAsync(m => m.Materialname == item.MaterialName);

                        dispatchData.Add(new
                        {
                            routeCode = order.Dealer?.RouteCode ?? "",
                            dealerName = order.Dealer?.Name ?? "",
                            phoneNo = order.Dealer?.PhoneNo ?? "",
                            address = order.Dealer?.Address ?? "",
                            materialName = item.MaterialName,
                            quantity = item.Qty,
                            unit = material?.Unit ?? ""
                        });
                    }
                }

                return Json(new { success = true, data = dispatchData });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: DealerOrders/ExportDispatchToExcel
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

                var dispatchData = new List<object>();

                foreach (var order in dealerOrders)
                {
                    // Get order items for this order
                    var orderItems = await _context.DealerOrderItems
                        .Where(i => i.DealerOrderId == order.Id)
                        .ToListAsync();

                    foreach (var item in orderItems)
                    {
                        // Get material to get unit information
                        var material = await _context.MaterialMaster
                            .FirstOrDefaultAsync(m => m.Materialname == item.MaterialName);

                        dispatchData.Add(new
                        {
                            routeCode = order.Dealer?.RouteCode ?? "",
                            dealerName = order.Dealer?.Name ?? "",
                            phoneNo = order.Dealer?.PhoneNo ?? "",
                            address = order.Dealer?.Address ?? "",
                            materialName = item.MaterialName,
                            quantity = item.Qty,
                            unit = material?.Unit ?? ""
                        });
                    }
                }

                // Create Excel file
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Dispatch Route Sheet");

                    // Add headers
                    worksheet.Cells[1, 1].Value = "Route Code";
                    worksheet.Cells[1, 2].Value = "Dealer Name";
                    worksheet.Cells[1, 3].Value = "Phone No";
                    worksheet.Cells[1, 4].Value = "Address";
                    worksheet.Cells[1, 5].Value = "Material";
                    worksheet.Cells[1, 6].Value = "Quantity";
                    worksheet.Cells[1, 7].Value = "Unit";

                    // Add data
                    for (int i = 0; i < dispatchData.Count; i++)
                    {
                        var item = dispatchData[i];
                        var itemType = item.GetType();
                        var properties = itemType.GetProperties();

                        worksheet.Cells[i + 2, 1].Value = itemType.GetProperty("routeCode")?.GetValue(item)?.ToString() ?? "";
                        worksheet.Cells[i + 2, 2].Value = itemType.GetProperty("dealerName")?.GetValue(item)?.ToString() ?? "";
                        worksheet.Cells[i + 2, 3].Value = itemType.GetProperty("phoneNo")?.GetValue(item)?.ToString() ?? "";
                        worksheet.Cells[i + 2, 4].Value = itemType.GetProperty("address")?.GetValue(item)?.ToString() ?? "";
                        worksheet.Cells[i + 2, 5].Value = itemType.GetProperty("materialName")?.GetValue(item)?.ToString() ?? "";
                        worksheet.Cells[i + 2, 6].Value = itemType.GetProperty("quantity")?.GetValue(item)?.ToString() ?? "";
                        worksheet.Cells[i + 2, 7].Value = itemType.GetProperty("unit")?.GetValue(item)?.ToString() ?? "";
                    }

                    // Auto-fit columns
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

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

		// GET: DealerOrders/GetDealersByDistributor
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
					.Select(d => new { 
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

		// POST: DealerOrders/LoadDealerOrder
		[HttpPost]
		public async Task<IActionResult> LoadDealerOrder(int distributorId, int dealerId)
		{
			try
			{
				// Check if an order already exists for this dealer today
				var today = DateTime.Now.Date;
				var existingOrder = await _context.DealerOrders
					.FirstOrDefaultAsync(d => d.DealerId == dealerId && d.DistributorId == distributorId && d.OrderDate == today);

				// If an order exists for today, we'll load it for viewing/editing
				// If no order exists, we'll check if we can create a new one (based on basic orders)
				// The restriction is still in place for creating new orders on the same day

				var viewModel = new DealerOrdersViewModel
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

				// Get basic orders for this dealer
				var basicOrders = await _context.DealerBasicOrders
					.Where(dbo => dbo.DealerId == dealer.Id)
					.ToListAsync();

				viewModel.DealerBasicOrders[dealer.Id] = basicOrders;

				// Initialize order item quantities
				if (!viewModel.DealerOrderItemQuantities.ContainsKey(dealer.Id))
				{
					viewModel.DealerOrderItemQuantities[dealer.Id] = new Dictionary<int, int>();
				}

				// If there's an existing order for today, populate quantities from that order
				// Otherwise, populate from basic orders
				if (existingOrder != null)
				{
					// Load quantities from existing order
					var existingOrderItems = await _context.DealerOrderItems
						.Where(item => item.DealerOrderId == existingOrder.Id)
						.ToListAsync();
					
					// Create a mapping of material name to ordered quantity
					var materialQuantities = existingOrderItems.ToDictionary(item => item.MaterialName, item => item.Qty);
					
					// Populate quantities from existing order
					foreach (var order in basicOrders)
					{
						if (materialQuantities.ContainsKey(order.MaterialName))
						{
							viewModel.DealerOrderItemQuantities[dealer.Id][order.Id] = materialQuantities[order.MaterialName];
						}
						else
						{
							viewModel.DealerOrderItemQuantities[dealer.Id][order.Id] = 0;
						}
					}
					
					// Add flag to indicate this is an existing order
					ViewBag.IsExistingOrder = true;
					ViewBag.ExistingOrderId = existingOrder.Id;
				}
				else
				{
					// Populate quantities from basic orders (new order)
					foreach (var order in basicOrders)
					{
						viewModel.DealerOrderItemQuantities[dealer.Id][order.Id] = order.Quantity;
					}
					
					// Add flag to indicate this is a new order
					ViewBag.IsExistingOrder = false;
				}

				// Get available materials
				viewModel.AvailableMaterials = await _context.MaterialMaster.ToListAsync();

				// Return the partial view for this single dealer
				return PartialView("_DealerOrdersPartial", viewModel);
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = ex.Message });
			}
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

			// Get the current date for order date
			var today = DateTime.Now.Date;

			// Get basic orders and current order quantities for each dealer
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

				// Check if an order already exists for this dealer today
				var existingOrder = await _context.DealerOrders
					.FirstOrDefaultAsync(d => d.DealerId == dealer.Id && d.DistributorId == distributorId && d.OrderDate == today);

				// If an order exists for today, show the updated quantities from that order
				// Otherwise, show the default quantities from basic orders
				if (existingOrder != null)
				{
					// Load quantities from existing order (these are the updated quantities)
					var existingOrderItems = await _context.DealerOrderItems
						.Where(item => item.DealerOrderId == existingOrder.Id)
						.ToListAsync();
					
					// Create a mapping of material name to ordered quantity
					var materialQuantities = existingOrderItems.ToDictionary(item => item.MaterialName, item => item.Qty);
					
					// Populate quantities from existing order (updated quantities)
					foreach (var order in basicOrders)
					{
						if (materialQuantities.ContainsKey(order.MaterialName))
						{
							viewModel.DealerOrderItemQuantities[dealer.Id][order.Id] = materialQuantities[order.MaterialName];
						}
						else
						{
							viewModel.DealerOrderItemQuantities[dealer.Id][order.Id] = 0;
						}
					}
				}
				else
				{
					// Populate quantities from basic orders (default quantities)
					foreach (var order in basicOrders)
					{
						viewModel.DealerOrderItemQuantities[dealer.Id][order.Id] = order.Quantity;
					}
				}
			}

			// Get available materials
			viewModel.AvailableMaterials = await _context.MaterialMaster.ToListAsync();

			return PartialView("_DealerOrdersPartial", viewModel);
		}

		// POST: DealerOrders/SaveOrders
		[HttpPost]
		public async Task<IActionResult> SaveOrders(int SelectedDistributorId, Dictionary<string, Dictionary<string, string>> DealerOrderItemQuantities, Dictionary<string, string> MaterialDealerPrices)
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

				// Convert string-based dictionary to int-based dictionary for quantities
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

				// Convert string-based dictionary to material name-decimal dictionary for dealer prices
				var dealerPrices = new Dictionary<string, decimal>();
				if (MaterialDealerPrices != null)
				{
					foreach (var priceEntry in MaterialDealerPrices)
					{
						if (int.TryParse(priceEntry.Key, out int materialId) &&
							decimal.TryParse(priceEntry.Value, out decimal price))
						{
							// Get the material name by ID to use as the key
							var material = await _context.MaterialMaster.FindAsync(materialId);
							if (material != null)
							{
								dealerPrices[material.Materialname] = price;
							}
						}
					}
				}

				// Process each dealer's orders (should be just one in this case)
				bool hasSavedOrders = false;
                List<int> savedDealerIds = new List<int>(); // Track which dealers had orders saved
                
				foreach (var kvp in intQuantities)
				{
					var dealerId = kvp.Key;
					var orderItems = kvp.Value;
					
					// Check if this dealer has any order items
					if (orderItems.Count > 0)
					{
						// Check if an order already exists for this dealer today
						var existingOrder = await _context.DealerOrders
							.FirstOrDefaultAsync(d => d.DealerId == dealerId && d.DistributorId == SelectedDistributorId && d.OrderDate == orderDate);

						// If an order exists, we're updating it
						// If no order exists, we're creating a new one (but only if quantities > 0)
						if (existingOrder != null)
						{
							// Update existing order
							var existingOrderItems = await _context.DealerOrderItems
								.Where(item => item.DealerOrderId == existingOrder.Id)
								.ToListAsync();
							
							// Create a mapping of material name to existing items for easy lookup
							var existingItemsByMaterial = existingOrderItems.ToDictionary(item => item.MaterialName, item => item);
							
							// Get basic orders to match with order items
							var basicOrders = await _context.DealerBasicOrders
								.Where(dbo => dbo.DealerId == dealerId)
								.ToListAsync();
							
							// Update or create order items
							foreach (var itemKvp in orderItems)
							{
								var basicOrderId = itemKvp.Key;
								var quantity = itemKvp.Value;
								
								// Find the basic order to get material details
								var basicOrder = basicOrders.FirstOrDefault(bo => bo.Id == basicOrderId);
								if (basicOrder != null)
								{
									// Check if an item already exists for this material
									if (existingItemsByMaterial.TryGetValue(basicOrder.MaterialName, out DealerOrderItem existingItem))
									{
										// Update existing item
										existingItem.Qty = quantity;
										
										// Determine the rate to use - dealer price if provided, otherwise basic order rate
										var rate = basicOrder.Rate;
										if (dealerPrices.ContainsKey(basicOrder.MaterialName))
										{
											rate = dealerPrices[basicOrder.MaterialName];
										}
										existingItem.Rate = rate;
										
										_context.DealerOrderItems.Update(existingItem);
									}
									else if (quantity > 0)
									{
										// Create new item only if quantity > 0
										// Determine the rate to use - dealer price if provided, otherwise basic order rate
										var rate = basicOrder.Rate;
										if (dealerPrices.ContainsKey(basicOrder.MaterialName))
										{
											rate = dealerPrices[basicOrder.MaterialName];
										}
										
										var orderItem = new DealerOrderItem
										{
											DealerOrderId = existingOrder.Id,
											MaterialName = basicOrder.MaterialName,
											ShortCode = basicOrder.ShortCode,
											SapCode = basicOrder.SapCode,
											Qty = quantity,
											Rate = rate,
											DeliverQnty = 0
										};
										
										_context.DealerOrderItems.Add(orderItem);
									}
								}
							}
							
							// Remove items that are no longer in the order (quantity = 0)
							foreach (var existingItem in existingOrderItems)
							{
								// Check if this item's material is in the current order
								var isMaterialInOrder = basicOrders.Any(bo => 
									bo.MaterialName == existingItem.MaterialName && 
									orderItems.ContainsKey(bo.Id) && 
									orderItems[bo.Id] > 0);
								
								if (!isMaterialInOrder)
								{
									// Remove the item
									_context.DealerOrderItems.Remove(existingItem);
								}
							}
							
							hasSavedOrders = true;
                            savedDealerIds.Add(dealerId);
						}
						else
						{
							// Creating a new order - check if we have quantities > 0
							bool hasQuantities = orderItems.Values.Any(q => q > 0);
							
							if (hasQuantities)
							{
								hasSavedOrders = true;
                                savedDealerIds.Add(dealerId);
                                
								// Create DealerOrder
								var dealerOrder = new DealerOrder
								{
									OrderDate = orderDate,
									DistributorId = SelectedDistributorId,
									DealerId = dealerId,
									DistributorCode = GetDistributorCode(SelectedDistributorId),
									ProcessFlag = 0 // Default to not processed
								};

								_context.DealerOrders.Add(dealerOrder);
								await _context.SaveChangesAsync();

								// Process order items based on quantities
								foreach (var itemKvp in orderItems)
								{
									var basicOrderId = itemKvp.Key;
									var quantity = itemKvp.Value;

									// Only create order items for quantities > 0
									if (quantity > 0)
									{
										// Get the basic order to get material details
										var basicOrder = await _context.DealerBasicOrders
											.FirstOrDefaultAsync(dbo => dbo.Id == basicOrderId);

										if (basicOrder != null)
										{
											// Determine the rate to use - dealer price if provided, otherwise basic order rate
											var rate = basicOrder.Rate;
											if (dealerPrices.ContainsKey(basicOrder.MaterialName))
											{
												rate = dealerPrices[basicOrder.MaterialName];
											}

											var orderItem = new DealerOrderItem
											{
												DealerOrderId = dealerOrder.Id,
												MaterialName = basicOrder.MaterialName,
												ShortCode = basicOrder.ShortCode,
												SapCode = basicOrder.SapCode,
												Qty = quantity,
												Rate = rate,
												DeliverQnty = 0
											};

											_context.DealerOrderItems.Add(orderItem);
										}
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
                    // Return the list of saved dealer IDs so the frontend can handle them appropriately
                    return Json(new { success = true, savedDealerIds = savedDealerIds });
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