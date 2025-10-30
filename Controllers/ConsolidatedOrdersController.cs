using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Milk_Bakery.Data;
using Milk_Bakery.Models;
using Milk_Bakery.ViewModels;
using AspNetCoreHero.ToastNotification.Abstractions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using OfficeOpenXml;
using System.IO;

namespace Milk_Bakery.Controllers
{
	[Authentication]
	public class ConsolidatedOrdersController : Controller
	{
		private readonly MilkDbContext _context;
		public INotyfService _notifyService { get; }

		public ConsolidatedOrdersController(MilkDbContext context, INotyfService notifyService)
		{
			_context = context;
			_notifyService = notifyService;
			// Set the license context for EPPlus
			ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
		}

		// GET: ConsolidatedOrders
		public async Task<IActionResult> Index()
		{
			var viewModel = new DealerOrdersViewModel();

			// Get available distributors based on user role
			viewModel.AvailableDistributors = await GetAvailableDistributors();

			return View(viewModel);
		}

		// GET: ConsolidatedOrders/GetAllCustomers
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

		// GET: ConsolidatedOrders/GetConsolidatedOrderData
		[HttpGet]
		public async Task<IActionResult> GetConsolidatedOrderData(DateTime? date, int? customerId)
		{
			try
			{
				var orderDate = date ?? DateTime.Now.Date;

				// Get dealer orders for the specified date
				var dealerOrdersQuery = _context.DealerOrders
					.Include(o => o.Dealer)
					.Where(o => o.OrderDate == orderDate);

				// Filter by customer if specified
				if (customerId.HasValue && customerId.Value > 0)
				{
					dealerOrdersQuery = dealerOrdersQuery.Where(o => o.DistributorId == customerId.Value);
				}

				var dealerOrders = await dealerOrdersQuery.ToListAsync();

				// Group by material and sum quantities across all dealers
				var consolidatedData = new Dictionary<string, int>(); // material -> total quantity
				var materialDetails = new Dictionary<string, object>(); // material -> details (shortCode, sapCode, unit)

				// Collect dealer details for display
				var dealerDetails = new List<object>();

				foreach (var order in dealerOrders)
				{
					// Get order items for this order
					var orderItems = await _context.DealerOrderItems
						.Where(i => i.DealerOrderId == order.Id)
						.ToListAsync();

					// Add dealer details
					var dealerMaterials = new List<object>();
					foreach (var item in orderItems)
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

							// Store material details (will be the same for all entries of this material)
							materialDetails[materialName] = new
							{
								shortCode = item.ShortCode ?? "",
								sapCode = item.SapCode ?? "",
								unit = "PCS" // Default unit, could be enhanced to get from MaterialMaster
							};
						}

						// Add material to dealer details
						dealerMaterials.Add(new
						{
							materialName = materialName,
							quantity = item.Qty
						});
					}

					// Add dealer to dealer details list
					dealerDetails.Add(new
					{
						dealerName = order.Dealer?.Name ?? "Unknown Dealer",
						orderDate = order.OrderDate.ToString("dd/MM/yyyy"),
						materials = dealerMaterials
					});
				}

				// Get conversion data for all materials
				var conversionData = await _context.ConversionTables.ToListAsync();
				var conversionDict = conversionData.ToDictionary(c => c.MaterialName, c => c);

				// Create result data with detailed crate calculations
				var resultData = new List<object>();

				foreach (var kvp in consolidatedData)
				{
					var materialName = kvp.Key;
					var totalQuantity = kvp.Value;

					var materialInfo = materialDetails.ContainsKey(materialName) ?
						materialDetails[materialName] as dynamic :
						new { shortCode = "", sapCode = "", unit = "PCS" };

					// Calculate crates based on conversion data
					var crates = 0;
					var itemsInCrates = 0;
					var leftoverItems = 0;
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
							itemsInCrates = crates * totalQuantityPerUnit;
							leftoverItems = totalQuantity % totalQuantityPerUnit;
						}
					}
					else
					{
						// If no conversion data, treat each item as a separate unit
						crates = totalQuantity;
						itemsInCrates = totalQuantity;
						leftoverItems = 0;
					}

					resultData.Add(new
					{
						materialName = materialName,
						shortCode = materialInfo.shortCode,
						sapCode = materialInfo.sapCode,
						quantity = totalQuantity,
						crates = crates,
						itemsInCrates = itemsInCrates,
						leftoverItems = leftoverItems,
						unitType = unitType,
						unitQuantity = unitQuantity,
						totalQuantityPerUnit = totalQuantityPerUnit
					});
				}

				// Sort by material name for consistent ordering
				resultData = resultData.OrderBy(item => ((dynamic)item).materialName).ToList();

				// Calculate grand totals
				var grandTotal = consolidatedData.Values.Sum();
				var totalCrates = resultData.Sum(item => (int)((dynamic)item).crates);
				var totalItemsPerCrate = resultData.Sum(item => (int)((dynamic)item).totalQuantityPerUnit);
				var totalItemsInCrates = resultData.Sum(item => (int)((dynamic)item).itemsInCrates);
				var totalLeftoverItems = resultData.Sum(item => (int)((dynamic)item).leftoverItems);

				return Json(new
				{
					success = true,
					data = resultData,
					dealerDetails = dealerDetails,
					grandTotal = grandTotal,
					totalCrates = totalCrates,
					totalItemsPerCrate = totalItemsPerCrate,
					totalItemsInCrates = totalItemsInCrates,
					totalLeftoverItems = totalLeftoverItems,
					orderDate = orderDate.ToString("dd/MM/yyyy")
				});
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = ex.Message });
			}
		}

		// GET: ConsolidatedOrders/ExportConsolidatedToExcel
		[HttpGet]
		public async Task<IActionResult> ExportConsolidatedToExcel(DateTime? date, int? customerId)
		{
			try
			{
				var orderDate = date ?? DateTime.Now.Date;

				// Get dealer orders for the specified date
				var dealerOrdersQuery = _context.DealerOrders
					.Include(o => o.Dealer)
					.Where(o => o.OrderDate == orderDate);

				// Filter by customer if specified
				if (customerId.HasValue && customerId.Value > 0)
				{
					dealerOrdersQuery = dealerOrdersQuery.Where(o => o.DistributorId == customerId.Value);
				}

				var dealerOrders = await dealerOrdersQuery.ToListAsync();

				// Group by material and sum quantities across all dealers
				var consolidatedData = new Dictionary<string, int>(); // material -> total quantity
				var materialDetails = new Dictionary<string, object>(); // material -> details (shortCode, sapCode, unit)

				foreach (var order in dealerOrders)
				{
					// Get order items for this order
					var orderItems = await _context.DealerOrderItems
						.Where(i => i.DealerOrderId == order.Id)
						.ToListAsync();

					foreach (var item in orderItems)
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

							// Store material details (will be the same for all entries of this material)
							materialDetails[materialName] = new
							{
								shortCode = item.ShortCode ?? "",
								sapCode = item.SapCode ?? "",
								unit = "PCS" // Default unit, could be enhanced to get from MaterialMaster
							};
						}
					}
				}

				// Get conversion data for all materials
				var conversionData = await _context.ConversionTables.ToListAsync();
				var conversionDict = conversionData.ToDictionary(c => c.MaterialName, c => c);

				// Create result data with detailed crate calculations
				var resultData = new List<dynamic>();

				foreach (var kvp in consolidatedData)
				{
					var materialName = kvp.Key;
					var totalQuantity = kvp.Value;

					var materialInfo = materialDetails.ContainsKey(materialName) ?
						materialDetails[materialName] as dynamic :
						new { shortCode = "", sapCode = "", unit = "PCS" };

					// Calculate crates based on conversion data
					var crates = 0;
					var itemsInCrates = 0;
					var leftoverItems = 0;
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
							itemsInCrates = crates * totalQuantityPerUnit;
							leftoverItems = totalQuantity % totalQuantityPerUnit;
						}
					}
					else
					{
						// If no conversion data, treat each item as a separate unit
						crates = totalQuantity;
						itemsInCrates = totalQuantity;
						leftoverItems = 0;
					}

					resultData.Add(new
					{
						materialName = materialName,
						shortCode = materialInfo.shortCode,
						sapCode = materialInfo.sapCode,
						quantity = totalQuantity,
						crates = crates,
						itemsInCrates = itemsInCrates,
						leftoverItems = leftoverItems,
						unitType = unitType,
						unitQuantity = unitQuantity,
						totalQuantityPerUnit = totalQuantityPerUnit
					});
				}

				// Sort by material name for consistent ordering
				resultData = resultData.OrderBy(item => item.materialName).ToList();

				// Calculate grand totals
				var grandTotal = consolidatedData.Values.Sum();
				var totalCrates = resultData.Sum(item => (int)item.crates);
				var totalItemsInCrates = resultData.Sum(item => (int)item.itemsInCrates);
				var totalLeftoverItems = resultData.Sum(item => (int)item.leftoverItems);

				// Create Excel file
				using (var package = new ExcelPackage())
				{
					var worksheet = package.Workbook.Worksheets.Add("Consolidated Order");

					// Add headers
					worksheet.Cells[1, 1].Value = "Material Name";
					worksheet.Cells[1, 2].Value = "Short Code";
					worksheet.Cells[1, 3].Value = "SAP Code";
					worksheet.Cells[1, 4].Value = "Quantity (PCS)";
					worksheet.Cells[1, 5].Value = "Unit Type";
					worksheet.Cells[1, 6].Value = "Items per Crate";
					worksheet.Cells[1, 7].Value = "In Crates";
					worksheet.Cells[1, 8].Value = "Leftover Items";

					// Add data rows
					for (int i = 0; i < resultData.Count; i++)
					{
						var rowData = resultData[i];

						worksheet.Cells[i + 2, 1].Value = rowData.materialName;
						worksheet.Cells[i + 2, 2].Value = rowData.shortCode;
						worksheet.Cells[i + 2, 3].Value = rowData.sapCode;
						worksheet.Cells[i + 2, 4].Value = rowData.quantity;
						worksheet.Cells[i + 2, 5].Value = rowData.unitType;
						worksheet.Cells[i + 2, 6].Value = rowData.totalQuantityPerUnit;
						worksheet.Cells[i + 2, 7].Value = rowData.itemsInCrates;
						worksheet.Cells[i + 2, 8].Value = rowData.leftoverItems;
					}

					// Add total row
					var totalRow = resultData.Count + 2;
					worksheet.Cells[totalRow, 1].Value = "Grand Total";
					worksheet.Cells[totalRow, 4].Value = grandTotal;
					worksheet.Cells[totalRow, 6].Value = "";
					worksheet.Cells[totalRow, 7].Value = totalItemsInCrates;
					worksheet.Cells[totalRow, 8].Value = totalLeftoverItems;

					// Format the total row
					worksheet.Cells[totalRow, 1, totalRow, 8].Style.Font.Bold = true;
					worksheet.Cells[totalRow, 4].Style.Numberformat.Format = "#,##0";
					worksheet.Cells[totalRow, 7].Style.Numberformat.Format = "#,##0";
					worksheet.Cells[totalRow, 8].Style.Numberformat.Format = "#,##0";

					// Auto-fit columns
					worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

					// Convert to bytes
					var fileBytes = package.GetAsByteArray();

					// Return file
					var fileName = $"ConsolidatedOrder_{orderDate:yyyyMMdd}.xlsx";
					return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
				}
			}
			catch (Exception ex)
			{
				_notifyService.Error("Error exporting to Excel: " + ex.Message);
				return RedirectToAction("Index");
			}
		}

		// GET: ConsolidatedOrders/GetDealersByDistributor
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

		// GET: ConsolidatedOrders/GetDealerOrdersForMaterial
		[HttpGet]
		public async Task<IActionResult> GetDealerOrdersForMaterial(string materialName, int customerId, DateTime? date)
		{
			try
			{
				var orderDate = date ?? DateTime.Now.Date;

				// Get dealer orders for the specified date and customer
				var dealerOrders = await _context.DealerOrders
					.Include(o => o.Dealer)
					.Where(o => o.OrderDate == orderDate && o.DistributorId == customerId)
					.ToListAsync();

				// Get conversion data for the material
				var conversionData = await _context.ConversionTables
					.FirstOrDefaultAsync(c => c.MaterialName == materialName);
				
				var totalQuantityPerUnit = conversionData?.TotalQuantity ?? 1;

				var dealerOrderItems = new List<object>();

				foreach (var order in dealerOrders)
				{
					// Get order items for this order that match the material name
					var orderItems = await _context.DealerOrderItems
						.Where(i => i.DealerOrderId == order.Id && i.MaterialName == materialName)
						.ToListAsync();

					foreach (var item in orderItems)
					{
						// Get the corresponding basic order to get the basic order ID
						var basicOrder = await _context.DealerBasicOrders
							.FirstOrDefaultAsync(bo => bo.DealerId == order.DealerId && bo.MaterialName == item.MaterialName);

						dealerOrderItems.Add(new
						{
							dealerId = order.DealerId,
							dealerName = order.Dealer?.Name ?? "Unknown Dealer",
							basicOrderId = basicOrder?.Id ?? 0,
							materialName = item.MaterialName,
							shortCode = item.ShortCode, // Add shortCode to the response
							quantity = item.Qty,
							totalQuantityPerUnit = totalQuantityPerUnit // Add totalQuantityPerUnit to the response
						});
					}
				}

				return Json(new { success = true, dealerOrders = dealerOrderItems });
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = ex.Message });
			}
		}

		// POST: ConsolidatedOrders/AdjustDealerOrderQuantities
		[HttpPost]
		public async Task<IActionResult> AdjustDealerOrderQuantities([FromBody] AdjustQuantitiesModel model)
		{
			try
			{
				// Group adjustments by dealer ID
				var dealerAdjustments = model.Adjustments
					.GroupBy(a => a.DealerId)
					.ToDictionary(g => g.Key, g => g.ToList());

				foreach (var dealerGroup in dealerAdjustments)
				{
					var dealerId = dealerGroup.Key;
					var adjustments = dealerGroup.Value;

					// Get the dealer order for today
					var today = DateTime.Now.Date;
					var dealerOrder = await _context.DealerOrders.Include(o => o.DealerOrderItems)
						.FirstOrDefaultAsync(o => o.DealerId == dealerId && o.OrderDate == today);

					if (dealerOrder != null)
					{
						// Process each adjustment for this dealer
						foreach (var adjustment in adjustments)
						{
							// Get the dealer order item by matching the ShortName with MaterialName
							var dealerOrderItem = await _context.DealerOrderItems
								.FirstOrDefaultAsync(i => i.DealerOrderId == dealerOrder.Id && i.ShortCode == adjustment.ShortName);

							if (dealerOrderItem != null)
							{
								// Adjust the quantity
								dealerOrderItem.Qty += adjustment.Adjustment;

								// Ensure quantity doesn't go below 0
								if (dealerOrderItem.Qty < 0)
								{
									dealerOrderItem.Qty = 0;
								}

								_context.DealerOrderItems.Update(dealerOrderItem);
							}
							else
							{
								// If the item doesn't exist, create a new one based on the basic order
								var basicOrder = await _context.DealerBasicOrders
									.FirstOrDefaultAsync(bo => bo.DealerId == dealerId && bo.ShortCode == adjustment.ShortName);

								if (basicOrder != null)
								{
									var newOrderItem = new DealerOrderItem
									{
										DealerOrderId = dealerOrder.Id,
										MaterialName = basicOrder.MaterialName,
										ShortCode = basicOrder.ShortCode,
										SapCode = basicOrder.SapCode,
										Qty = basicOrder.Quantity + adjustment.Adjustment,
										Rate = basicOrder.Rate,
										DeliverQnty = 0
									};

									// Ensure quantity doesn't go below 0
									if (newOrderItem.Qty < 0)
									{
										newOrderItem.Qty = 0;
									}

									_context.DealerOrderItems.Add(newOrderItem);
								}
							}
						}
					}
					else
					{
						// If no order exists for today, create one
						var distributorId = adjustments.FirstOrDefault()?.DealerOrderId ?? 0;
						var distributor = await _context.Customer_Master.FirstOrDefaultAsync(c => c.Id == distributorId);

						var newOrder = new DealerOrder
						{
							OrderDate = today,
							DistributorId = distributorId,
							DealerId = dealerId,
							DistributorCode = distributor?.shortname ?? "",
							ProcessFlag = 0
						};

						_context.DealerOrders.Add(newOrder);
						await _context.SaveChangesAsync();

						// Process each adjustment for this dealer
						foreach (var adjustment in adjustments)
						{
							// Get the basic order by matching ShortName
							var basicOrder = await _context.DealerBasicOrders
								.FirstOrDefaultAsync(bo => bo.DealerId == dealerId && bo.ShortCode == adjustment.ShortName);

							if (basicOrder != null)
							{
								var newOrderItem = new DealerOrderItem
								{
									DealerOrderId = newOrder.Id,
									MaterialName = basicOrder.MaterialName,
									ShortCode = basicOrder.ShortCode,
									SapCode = basicOrder.SapCode,
									Qty = basicOrder.Quantity + adjustment.Adjustment,
									Rate = basicOrder.Rate,
									DeliverQnty = 0
								};

								// Ensure quantity doesn't go below 0
								if (newOrderItem.Qty < 0)
								{
									newOrderItem.Qty = 0;
								}

								_context.DealerOrderItems.Add(newOrderItem);
							}
						}
					}
				}

				await _context.SaveChangesAsync();

				_notifyService.Success("Dealer order quantities updated successfully.");
				return Json(new { success = true });
			}
			catch (Exception ex)
			{
				_notifyService.Error("An error occurred while updating dealer order quantities.");
				return Json(new { success = false, message = ex.Message });
			}
		}

		// POST: ConsolidatedOrders/SaveConsolidatedOrder
		[HttpPost]
		public async Task<IActionResult> SaveConsolidatedOrder(DateTime date, int customerId)
		{
			try
			{
				// Check if date in less the current date
				if (date < DateTime.Now.Date)
				{
					return Json(new { success = false, message = "Cannot save orders for past dates." });
				}

				// Get the customer/distributor details
				var customer = await _context.Customer_Master.FindAsync(customerId);
				if (customer == null)
				{
					return Json(new { success = false, message = "Customer not found." });
				}

				//GET CUSTOMER SEGEMNET
				var customerSegment = await _context.CustomerSegementMap
					 .FirstOrDefaultAsync(csm => csm.Customername == customer.Name);

				if (customerSegment == null)
				{
					return Json(new { success = false, message = "Customer Segment not found." });
				}

				var compaany = await _context.Company_SegementMap.FirstOrDefaultAsync(a => a.Segementname == customer.Division);

				if (compaany == null)
				{
					return Json(new { success = false, message = "Company not found." });
				}

				// Check if an order already exists for this customer on this date
				var existingOrder = await _context.PurchaseOrder
					.FirstOrDefaultAsync(po => po.OrderDate.Date == date.Date && po.CustomerCode == customerSegment.custsegementcode);

				if (existingOrder != null)
				{
					return Json(new { success = false, message = "An order has already been placed for this customer on this date." });
				}

				// Get dealer orders for the specified date and customer
				var dealerOrders = await _context.DealerOrders
					.Include(o => o.Dealer)
					.Include(o => o.DealerOrderItems)
					.Where(o => o.OrderDate == date && o.DistributorId == customerId)
					.ToListAsync();

				if (!dealerOrders.Any())
				{
					return Json(new { success = false, message = "No dealer orders found for this date and customer." });
				}

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
					OrderDate = date,
					Id = _context.PurchaseOrder.Any() ? _context.PurchaseOrder.Max(e => e.Id) + 1 : 1,
					Customername = customer.Name,
					Segementname = customerSegment.SegementName,
					Segementcode = customerSegment.segementcode3party,
					CustomerCode = customerSegment.custsegementcode,
					companycode = compaany.companycode
				};
				// Get conversion data for all materials
				var conversionData = await _context.ConversionTables.ToListAsync();
				var conversionDict = conversionData.ToDictionary(c => c.MaterialName, c => c);

				// Create product details for each consolidated item
				var productDetails = new List<ProductDetail>();
				int itemId = 1;

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
						Rate = material?.price ?? 0,
						Price = crates * material?.price ?? 0
					};

					productDetails.Add(productDetail);
				}

				purchaseOrder.ProductDetails = productDetails;

				// Save the purchase order
				_context.PurchaseOrder.Add(purchaseOrder);
				await _context.SaveChangesAsync();

				// Update ProcessFlag for all dealer orders that were included in the consolidated order
				foreach (var order in dealerOrders)
				{
					order.ProcessFlag = 1; // Mark as processed
				}
				await _context.SaveChangesAsync();

				_notifyService.Success("Consolidated order saved successfully.");
				return Json(new { success = true, message = "Consolidated order saved successfully." });
			}
			catch (Exception ex)
			{
				_notifyService.Error("An error occurred while saving the consolidated order.");
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

		// Model for quantity adjustments
		public class AdjustQuantitiesModel
		{
			public List<QuantityAdjustment> Adjustments { get; set; }
		}

		public class QuantityAdjustment
		{
			public int DealerId { get; set; }
			public int DealerOrderId { get; set; }

			public string ShortName { get; set; }

			public int Adjustment { get; set; }
		}
	}
}