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
using Microsoft.Extensions.Logging;

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

					// Add a flag to indicate that this order already exists
					ViewBag.OrderAlreadyExists = true;
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
					ViewBag.OrderAlreadyExists = false;
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

		// POST: DealerOrders/LoadExcelView
		[HttpPost]
		public async Task<IActionResult> LoadExcelView(int distributorId)
		{
			try
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

				return PartialView("_ExcelViewPartial", viewModel);
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = ex.Message });
			}
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

				// Get all materials to use for dealer prices
				var allMaterials = await _context.MaterialMaster.ToDictionaryAsync(m => m.Materialname, m => m);

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
										// Use dealer price from material master if available
										if (allMaterials.ContainsKey(basicOrder.MaterialName))
										{
											rate = allMaterials[basicOrder.MaterialName].dealerprice;
										}
										else if (dealerPrices.ContainsKey(basicOrder.MaterialName))
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
										// Use dealer price from material master if available
										if (allMaterials.ContainsKey(basicOrder.MaterialName))
										{
											rate = allMaterials[basicOrder.MaterialName].dealerprice;
										}
										else if (dealerPrices.ContainsKey(basicOrder.MaterialName))
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
								// Additional validation to prevent duplicate orders
								// Check if an order already exists for this dealer today (double-check)
								var duplicateOrderCheck = await _context.DealerOrders
									.AnyAsync(d => d.DealerId == dealerId && d.DistributorId == SelectedDistributorId && d.OrderDate == orderDate);

								if (duplicateOrderCheck)
								{
									// An order already exists for this dealer today, cannot create another one
									_notifyService.Error("An order already exists for this dealer today. You can only update the existing order.");
									return Json(new { success = false, message = "An order already exists for this dealer today. You can only update the existing order." });
								}

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
											// Use dealer price from material master if available
											if (allMaterials.ContainsKey(basicOrder.MaterialName))
											{
												rate = allMaterials[basicOrder.MaterialName].dealerprice;
											}
											else if (dealerPrices.ContainsKey(basicOrder.MaterialName))
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

		// POST: DealerOrders/SaveExcelViewOrders

		// POST: DealerOrders/SaveAllExcelViewOrders
		[HttpPost]
		public async Task<IActionResult> SaveAllExcelViewOrders([FromBody] List<ExcelViewOrderModel> allOrders)
		{
			// Validate that all "Items to Add" values are 0
			if (!await AreAllItemsToAddZero(allOrders))
			{
				_notifyService.Error("Cannot save orders. All \"Items to Add\" values must be 0 to complete crates.");
				return Json(new { success = false, message = "Cannot save orders. All \"Items to Add\" values must be 0 to complete crates." });
			}

			using var transaction = await _context.Database.BeginTransactionAsync();
			try
			{
				var distributorId = 0;
				var orderDate = DateTime.Now.Date;

				// Process each dealer order
				foreach (var model in allOrders)
				{
					// Get the dealer
					var dealer = await _context.DealerMasters.FirstOrDefaultAsync(d => d.Id == model.dealerId);
					if (dealer == null)
					{
						continue; // Skip this dealer if not found
					}

					// Get the distributor ID from the dealer
					distributorId = dealer.DistributorId;

					// Check if an order already exists for this dealer today
					var existingOrder = await _context.DealerOrders
						.FirstOrDefaultAsync(d => d.DealerId == model.dealerId && d.DistributorId == distributorId && d.OrderDate == orderDate);

					// Convert order items to dictionary
					var intQuantities = new Dictionary<int, int>();
					foreach (var item in model.orderItems)
					{
						intQuantities[item.MaterialId] = item.Quantity;
					}

					// Get materials to get rates
					var materials = await _context.MaterialMaster
						.Where(m => intQuantities.Keys.Contains(m.Id))
						.ToDictionaryAsync(m => m.Id, m => m);

					if (existingOrder != null)
					{
						// Update existing order
						var existingOrderItems = await _context.DealerOrderItems
							.Where(item => item.DealerOrderId == existingOrder.Id)
							.ToListAsync();

						// Create a mapping of material name to existing items for easy lookup
						var existingItemsByMaterial = existingOrderItems.ToDictionary(item => item.MaterialName, item => item);

						// Update or create order items
						foreach (var kvp in intQuantities)
						{
							var materialId = kvp.Key;
							var quantity = kvp.Value;

							// Find the material
							if (materials.TryGetValue(materialId, out var material))
							{
								// Check if an item already exists for this material
								if (existingItemsByMaterial.TryGetValue(material.Materialname, out DealerOrderItem existingItem))
								{
									// Update existing item
									existingItem.Qty = quantity;
									existingItem.Rate = material.dealerprice; // Use dealer price instead of regular price
									_context.DealerOrderItems.Update(existingItem);
								}
								else if (quantity > 0)
								{
									// Create new item only if quantity > 0
									var orderItem = new DealerOrderItem
									{
										DealerOrderId = existingOrder.Id,
										MaterialName = material.Materialname,
										ShortCode = material.ShortName,
										SapCode = material.material3partycode,
										Qty = quantity,
										Rate = material.dealerprice, // Use dealer price instead of regular price
										DeliverQnty = 0
									};

									_context.DealerOrderItems.Add(orderItem);
								}
							}
						}

						// Remove items that are no longer in the order (quantity = 0)
						foreach (var existingItem in existingOrderItems)
						{
							// Find the material ID for this item
							var material = materials.Values.FirstOrDefault(m => m.Materialname == existingItem.MaterialName);
							if (material != null)
							{
								// Check if this item's material is in the current order
								var isMaterialInOrder = intQuantities.ContainsKey(material.Id) && intQuantities[material.Id] > 0;

								if (!isMaterialInOrder)
								{
									// Remove the item
									_context.DealerOrderItems.Remove(existingItem);
								}
							}
						}
					}
					else
					{
						// Creating a new order - check if we have quantities > 0
						bool hasQuantities = intQuantities.Values.Any(q => q > 0);

						if (hasQuantities)
						{
							// Additional validation to prevent duplicate orders
							// Check if an order already exists for this dealer today (double-check)
							var duplicateOrderCheck = await _context.DealerOrders
								.AnyAsync(d => d.DealerId == model.dealerId && d.DistributorId == distributorId && d.OrderDate == orderDate);

							if (duplicateOrderCheck)
							{
								// An order already exists for this dealer today, cannot create another one
								// We'll skip this dealer but continue with others
								continue;
							}

							// Create DealerOrder
							var dealerOrder = new DealerOrder
							{
								OrderDate = orderDate,
								DistributorId = distributorId,
								DealerId = model.dealerId,
								DistributorCode = GetDistributorCode(distributorId),
								ProcessFlag = 0 // Default to not processed
							};

							_context.DealerOrders.Add(dealerOrder);
							await _context.SaveChangesAsync();

							// Process order items based on quantities
							foreach (var kvp in intQuantities)
							{
								var materialId = kvp.Key;
								var quantity = kvp.Value;

								// Only create order items for quantities > 0
								if (quantity > 0 && materials.TryGetValue(materialId, out var material))
								{
									var orderItem = new DealerOrderItem
									{
										DealerOrderId = dealerOrder.Id,
										MaterialName = material.Materialname,
										ShortCode = material.ShortName,
										SapCode = material.material3partycode,
										Qty = quantity,
										Rate = material.dealerprice, // Use dealer price instead of regular price
										DeliverQnty = 0
									};

									_context.DealerOrderItems.Add(orderItem);
								}
							}
						}
					}
				}

				await _context.SaveChangesAsync();

				// Now create the PurchaseOrder based on the consolidated dealer orders
				// Get the customer/distributor details
				var customer = await _context.Customer_Master.FindAsync(distributorId);
				if (customer != null)
				{
					// Get customer segment
					var customerSegment = await _context.CustomerSegementMap
						 .FirstOrDefaultAsync(csm => csm.Customername == customer.Name);

					if (customerSegment != null)
					{
						var company = await _context.Company_SegementMap.FirstOrDefaultAsync(a => a.Segementname == customer.Division);

						if (company != null)
						{
							// Check if an order already exists for this customer on this date
							var existingPurchaseOrder = await _context.PurchaseOrder
								.FirstOrDefaultAsync(po => po.OrderDate.Date == orderDate && po.CustomerCode == customerSegment.custsegementcode);

							if (existingPurchaseOrder == null)
							{
								// Get dealer orders for the specified date and customer that are not yet processed
								var dealerOrders = await _context.DealerOrders
									.Include(o => o.Dealer)
									.Include(o => o.DealerOrderItems)
									.Where(o => o.OrderDate == orderDate && o.DistributorId == distributorId && o.ProcessFlag == 0)
									.ToListAsync();

								if (dealerOrders.Any())
								{
									// Group by material and sum quantities across all dealers
									var consolidatedData = new Dictionary<string, int>(); // material -> total quantity

									foreach (var order in dealerOrders)
									{
										foreach (var item in order.DealerOrderItems)
										{
											var materialName = item.MaterialName ?? "";

											// Add to consolidated data
											if (consolidatedData.ContainsKey(materialName))
											{
												consolidatedData[materialName] += item.Qty;
											}
											else
											{
												consolidatedData[materialName] = item.Qty;
											}
										}
									}

									// Create a new purchase order
									var purchaseOrder = new PurchaseOrder
									{
										OrderNo = GeneratePurchaseOrderNumber(),
										OrderDate = orderDate,
										Id = _context.PurchaseOrder.Any() ? _context.PurchaseOrder.Max(e => e.Id) + 1 : 1,
										Customername = customer.Name,
										Segementname = customerSegment.SegementName,
										Segementcode = customerSegment.segementcode3party,
										CustomerCode = customerSegment.custsegementcode,
										companycode = company.companycode
									};

									// Get conversion data for all materials
									var conversionData = await _context.ConversionTables.ToListAsync();
									var conversionDict = conversionData.ToDictionary(c => c.MaterialName, c => c);

									// Create product details for each consolidated item
									var productDetails = new List<ProductDetail>();

									foreach (var kvp in consolidatedData)
									{
										var materialName = kvp.Key;
										var totalQuantity = kvp.Value;

										// Get material details
										var material = await _context.MaterialMaster
											.FirstOrDefaultAsync(m => m.Materialname == materialName);

										// Calculate crates based on conversion data
										var crates = 0;
										var unitType = "PCS";
										var unitQuantity = 1;
										var totalQuantityPerUnit = 1;

										if (conversionDict.ContainsKey(materialName))
										{
											var conversion = conversionDict[materialName];
											unitType = conversion.UnitType;
											unitQuantity = conversion.UnitQuantity;
											totalQuantityPerUnit = conversion.TotalQuantity;

											// Calculate crates (total quantity / items per crate)
											if (totalQuantityPerUnit > 0)
											{
												crates = totalQuantity / totalQuantityPerUnit;
												// If there's a remainder, we need an additional crate
												if (totalQuantity % totalQuantityPerUnit > 0)
												{
													crates++;
												}
											}
										}

										if (crates == 0)
										{
											continue; // Skip items with zero crates
										}

										var productDetail = new ProductDetail
										{
											Id = 0,
											PurchaseOrderId = purchaseOrder.Id,
											ProductName = materialName,
											ProductCode = material?.material3partycode ?? "",
											Unit = unitType,
											qty = crates, // Using crates as the quantity to order
											Rate = material?.dealerprice ?? 0, // Use dealer price instead of regular price
											Price = crates * (material?.dealerprice ?? 0) // Use dealer price instead of regular price
										};

										productDetails.Add(productDetail);
									}

									if (productDetails.Any())
									{
										purchaseOrder.ProductDetails = productDetails;

										// Save the purchase order
										_context.PurchaseOrder.Add(purchaseOrder);

										// Update ProcessFlag for all dealer orders that were included in the consolidated order
										foreach (var order in dealerOrders)
										{
											order.ProcessFlag = 1; // Mark as processed
											order.DeliverFlag = 0;
										}
									}
								}
							}
						}
					}
				}

				await _context.SaveChangesAsync();
				await transaction.CommitAsync();

				_notifyService.Success("All dealer orders and purchase order saved successfully.");
				return Json(new { success = true });
			}
			catch (Exception ex)
			{
				await transaction.RollbackAsync();
				_notifyService.Error("An error occurred while saving dealer orders: " + ex.Message);
				return Json(new { success = false, message = ex.Message });
			}
		}

		// Helper method to generate purchase order number
		private string GeneratePurchaseOrderNumber()
		{
			int maxId = _context.PurchaseOrder.Any() ? _context.PurchaseOrder.Max(e => e.Id) + 1 : 1;
			string orderNo = "";

			if (maxId.ToString().Length == 1)
				orderNo = "P0000000" + maxId.ToString();
			else if (maxId.ToString().Length == 2)
				orderNo = "P000000" + maxId.ToString();
			else if (maxId.ToString().Length == 3)
				orderNo = "P00000" + maxId.ToString();
			else if (maxId.ToString().Length == 4)
				orderNo = "P0000" + maxId.ToString();
			else if (maxId.ToString().Length == 5)
				orderNo = "P000" + maxId.ToString();
			else if (maxId.ToString().Length == 6)
				orderNo = "P00" + maxId.ToString();
			else if (maxId.ToString().Length == 7)
				orderNo = "P0" + maxId.ToString();
			else if (maxId.ToString().Length == 8)
				orderNo = "P" + maxId.ToString();

			return orderNo;
		}

		// Helper method to check if all items to add are 0
		private async Task<bool> AreAllItemsToAddZero(List<ExcelViewOrderModel> allOrders)
		{
			// Get all material IDs from the orders
			var materialIds = allOrders.SelectMany(o => o.orderItems.Select(i => i.MaterialId)).Distinct().ToList();

			// Get materials and their conversion data
			var materials = await _context.MaterialMaster
				.Where(m => materialIds.Contains(m.Id))
				.ToDictionaryAsync(m => m.Id, m => m);

			var materialNames = materials.Values.Select(m => m.Materialname).ToList();
			var conversionData = await _context.ConversionTables
				.Where(c => materialNames.Contains(c.MaterialName))
				.ToDictionaryAsync(c => c.MaterialName, c => c);

			// Calculate total quantities per material across all dealers
			var totalQuantities = new Dictionary<int, int>(); // materialId -> total quantity

			foreach (var order in allOrders)
			{
				foreach (var item in order.orderItems)
				{
					if (totalQuantities.ContainsKey(item.MaterialId))
					{
						totalQuantities[item.MaterialId] += item.Quantity;
					}
					else
					{
						totalQuantities[item.MaterialId] = item.Quantity;
					}
				}
			}

			// Check if all items to add are 0
			foreach (var kvp in totalQuantities)
			{
				var materialId = kvp.Key;
				var totalQuantity = kvp.Value;

				if (materials.TryGetValue(materialId, out var material) &&
					conversionData.TryGetValue(material.Materialname, out var conversion))
				{
					var itemsPerCrate = conversion.TotalQuantity;
					if (itemsPerCrate > 0)
					{
						var itemsNeeded = (itemsPerCrate - (totalQuantity % itemsPerCrate)) % itemsPerCrate;
						if (itemsNeeded != 0)
						{
							return false; // Found a material that needs items to complete a crate
						}
					}
				}
			}

			return true; // All items to add are 0
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