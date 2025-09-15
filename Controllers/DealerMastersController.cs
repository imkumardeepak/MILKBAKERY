using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Milk_Bakery.Data;
using Milk_Bakery.Models;
using Milk_Bakery.ViewModels; // Added for DealerOrderViewModel
using System.Collections.Generic; // Added for collections
using System.Text; // Added for StringBuilder
using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq; // Added for LINQ operations

namespace Milk_Bakery.Controllers
{
	[Authentication]
	public class DealerMastersController : Controller
	{
		private readonly MilkDbContext _context;
		public INotyfService _notifyService { get; }

		public DealerMastersController(MilkDbContext context, INotyfService notifyService)
		{
			_context = context;
			_notifyService = notifyService;
		}

		// GET: DealerMasters
		public async Task<IActionResult> Index(string selectedCustomerId = null)
		{
			// Check if the user is a customer
			if (HttpContext.Session.GetString("role") == "Customer")
			{
				// Get the logged-in customer
				var loggedInCustomer = await _context.Customer_Master
					.FirstOrDefaultAsync(c => c.phoneno == HttpContext.Session.GetString("UserName"));

				if (loggedInCustomer != null)
				{
					// Get mapped customers for dropdown
					ViewBag.Customers = GetCustomer();

					// Determine which customer's dealers to show
					int customerIdToShow = loggedInCustomer.Id;
					if (!string.IsNullOrEmpty(selectedCustomerId))
					{
						// Validate that the selected customer is either the logged-in customer or a mapped customer
						var customer = await _context.Customer_Master.FindAsync(int.Parse(selectedCustomerId));
						if (customer != null)
						{
							// Check if the selected customer is the logged-in customer or a mapped customer
							bool isAllowed = customer.Id == loggedInCustomer.Id;

							// Check if it's a mapped customer
							if (!isAllowed)
							{
								var mappedcusr = await _context.Cust2CustMap
									.FirstOrDefaultAsync(a => a.phoneno == loggedInCustomer.phoneno);

								if (mappedcusr != null)
								{
									var mappedCustomers = await _context.mappedcusts
										.Where(a => a.cust2custId == mappedcusr.id)
										.ToListAsync();

									isAllowed = mappedCustomers.Any(mc => mc.customer == customer.Name);
								}
							}

							if (isAllowed)
							{
								customerIdToShow = customer.Id;
							}
						}
					}

					// Show only dealers associated with the selected customer
					var dealers = await _context.DealerMasters
						.Include(d => d.DealerBasicOrders)
						.Where(d => d.DistributorId == customerIdToShow)
						.ToListAsync();

					ViewBag.SelectedCustomerId = customerIdToShow.ToString();
					return View(dealers);
				}
				else
				{
					// If customer not found, return empty list
					ViewBag.Customers = new List<SelectListItem>();
					ViewBag.SelectedCustomerId = "";
					return View(new List<DealerMaster>());
				}
			}
			else
			{
				// For other roles (Admin, Sales), show all dealers
				var dealers = await _context.DealerMasters
					.Include(d => d.DealerBasicOrders)
					.ToListAsync();

				return View(dealers);
			}
		}

		// GET: DealerMasters/AddOrEdit/5
		public async Task<IActionResult> AddOrEdit(int? id)
		{
			// Create the view model
			var viewModel = new DealerOrderViewModel();

			// Get all materials for the dropdown/table
			var materials = await _context.MaterialMaster.ToListAsync();
			viewModel.AvailableMaterials = materials.Select(m => new MaterialDisplayModel
			{
				Id = m.Id,
				ShortName = m.ShortName,
				Materialname = m.Materialname,
				Unit = m.Unit,
				Category = m.Category,
				subcategory = m.subcategory,
				sequence = m.sequence,
				segementname = m.segementname,
				material3partycode = m.material3partycode,
				price = m.price,
				isactive = m.isactive,
				CratesCode = m.CratesTypes
			}).ToList();

			// Get customers for dropdown using the same logic as RepeatOrderController
			ViewBag.Customers = GetCustomer();

			if (id == 0 || id == null)
			{
				// Creating new dealer - set default rate of 1 for all materials
				foreach (var material in viewModel.AvailableMaterials)
				{
					viewModel.MaterialRates[material.Id] = 1;
				}

				viewDataForView(viewModel);
				return View(viewModel);
			}
			else
			{
				// Editing existing dealer
				var dealerMaster = await _context.DealerMasters
					.Include(d => d.DealerBasicOrders)
					.FirstOrDefaultAsync(d => d.Id == id);

				if (dealerMaster == null)
				{
					return NotFound();
				}

				viewModel.DealerMaster = dealerMaster;

				// Populate the existing orders and selected materials
				viewModel.DealerBasicOrders = dealerMaster.DealerBasicOrders.ToList();

				// Populate the quantities and rates
				foreach (var order in dealerMaster.DealerBasicOrders)
				{
					// Find the corresponding material in AvailableMaterials
					var material = viewModel.AvailableMaterials.FirstOrDefault(m => m.Materialname == order.MaterialName);
					if (material != null)
					{
						viewModel.SelectedMaterialIds.Add(material.Id);
						viewModel.MaterialQuantities[material.Id] = order.Quantity;

						// Store the rate directly from the DealerBasicOrder
						viewModel.MaterialRates[material.Id] = order.Rate;
					}
				}

				viewDataForView(viewModel);
				return View(viewModel);
			}
		}

		// POST: DealerMasters/AddOrEdit/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> AddOrEdit(int id, DealerOrderViewModel viewModel, string submit, Dictionary<int, decimal> MaterialRates = null)
		{
			// Get customers for dropdown (needed in both GET and POST)
			ViewBag.Customers = GetCustomer();

			viewDataForView(viewModel);

			// Automatically populate RouteCode based on selected customer
			if (viewModel.DealerMaster.DistributorId > 0)
			{
				var customer = await _context.Customer_Master.FindAsync(viewModel.DealerMaster.DistributorId);
				if (customer != null)
				{
					viewModel.DealerMaster.RouteCode = customer.route;
				}
			}

			// Validate that at least one material has quantity > 0
			bool hasValidQuantity = false;
			if (viewModel.MaterialQuantities != null)
			{
				foreach (var kvp in viewModel.MaterialQuantities)
				{
					if (kvp.Value > 0)
					{
						hasValidQuantity = true;
						break;
					}
				}
			}

			if (!hasValidQuantity)
			{
				_notifyService.Error("At least one material must have a quantity greater than 0.");
				ModelState.AddModelError("", "At least one material must have a quantity greater than 0.");
			}

			// Validate that quantities are not less than 0
			if (viewModel.MaterialQuantities != null)
			{
				foreach (var kvp in viewModel.MaterialQuantities)
				{
					if (kvp.Value < 0)
					{
						_notifyService.Error("Quantities cannot be less than 0.");
						ModelState.AddModelError("", "Quantities cannot be less than 0.");
						break;
					}
				}
			}

			if (ModelState.IsValid)
			{
				if (id == 0)
				{
					try
					{
						//find the existing dealer master with same name and distributor
						var existingDealerMaster = await _context.DealerMasters.FirstOrDefaultAsync(d => d.Name == viewModel.DealerMaster.Name && d.DistributorId == viewModel.DealerMaster.DistributorId);
						if (existingDealerMaster != null)
						{
							_notifyService.Error("A dealer with the same name already exists for the selected customer.");
							ModelState.AddModelError("", "A dealer with the same name already exists for the selected customer.");
							return View(viewModel);
						}


						// Add the dealer master
						_context.Add(viewModel.DealerMaster);
						await _context.SaveChangesAsync();

						// Process materials - only save those with quantity > 0
						if (viewModel.MaterialQuantities != null)
						{
							foreach (var kvp in viewModel.MaterialQuantities)
							{
								var materialId = kvp.Key;
								var quantity = kvp.Value;

								// Only process materials with quantity > 0
								if (quantity > 0)
								{
									var material = await _context.MaterialMaster.FindAsync(materialId);
									if (material != null)
									{
										// Get the rate from MaterialRates or use default rate of 1
										decimal rate = 1;
										if (MaterialRates != null && MaterialRates.ContainsKey(materialId))
										{
											rate = MaterialRates[materialId];
										}

										var dealerBasicOrder = new DealerBasicOrder
										{
											DealerId = viewModel.DealerMaster.Id,
											MaterialName = material.Materialname,
											SapCode = material.material3partycode,
											ShortCode = material.ShortName,
											Quantity = quantity,
											Rate = rate  // Changed from BasicAmount to Rate
										};

										_context.Add(dealerBasicOrder);
									}
								}
							}
						}

						await _context.SaveChangesAsync();
						_notifyService.Success("Dealer created successfully.");
						return RedirectToAction(nameof(Index));
					}
					catch (Exception ex)
					{
						// Log the exception or handle it as needed
						_notifyService.Error("An error occurred while saving the dealer. Please try again.");
						ModelState.AddModelError("", "An error occurred while saving the dealer. Please try again. Error: " + ex.Message);
					}
				}
				else
				{
					// Update
					try
					{
						// Automatically populate RouteCode based on selected customer
						if (viewModel.DealerMaster.DistributorId > 0)
						{
							var customer = await _context.Customer_Master.FindAsync(viewModel.DealerMaster.DistributorId);
							if (customer != null)
							{
								viewModel.DealerMaster.RouteCode = customer.route;
							}
						}

						// Update the dealer master
						_context.Update(viewModel.DealerMaster);

						// Remove existing dealer basic orders for this dealer
						var existingOrders = await _context.DealerBasicOrders
							.Where(o => o.DealerId == viewModel.DealerMaster.Id)
							.ToListAsync();

						foreach (var order in existingOrders)
						{
							_context.DealerBasicOrders.Remove(order);
						}

						// Process materials - only save those with quantity > 0
						if (viewModel.MaterialQuantities != null)
						{
							foreach (var kvp in viewModel.MaterialQuantities)
							{
								var materialId = kvp.Key;
								var quantity = kvp.Value;

								// Only process materials with quantity > 0
								if (quantity > 0)
								{
									var material = await _context.MaterialMaster.FindAsync(materialId);
									if (material != null)
									{
										// Get the rate from MaterialRates or use material price as fallback
										decimal rate = 1;
										if (MaterialRates != null && MaterialRates.ContainsKey(materialId))
										{
											rate = MaterialRates[materialId];
										}
										else
										{
											// For existing orders, try to preserve the existing rate
											var existingOrder = viewModel.DealerBasicOrders.FirstOrDefault(o => o.MaterialName == material.Materialname);
											if (existingOrder != null && existingOrder.Quantity > 0)
											{
												rate = existingOrder.Rate;  // Changed from BasicAmount calculation to Rate
											}
										}

										var dealerBasicOrder = new DealerBasicOrder
										{
											DealerId = viewModel.DealerMaster.Id,
											MaterialName = material.Materialname,
											SapCode = material.material3partycode,
											ShortCode = material.ShortName,
											Quantity = quantity,
											Rate = rate  // Changed from BasicAmount to Rate
										};

										_context.Add(dealerBasicOrder);
									}
								}
							}
						}

						await _context.SaveChangesAsync();
						_notifyService.Success("Dealer updated successfully.");
						return RedirectToAction(nameof(Index));
					}
					catch (DbUpdateConcurrencyException)
					{
						if (!DealerMasterExists(viewModel.DealerMaster.Id))
						{
							_notifyService.Error("Dealer not found.");
							return NotFound();
						}
						else
						{
							_notifyService.Error("An error occurred while updating the dealer.");
							throw;
						}
					}
					catch (Exception ex)
					{
						// Log the exception or handle it as needed
						_notifyService.Error("An error occurred while updating the dealer. Please try again.");
						ModelState.AddModelError("", "An error occurred while updating the dealer. Please try again. Error: " + ex.Message);
					}
				}
			}

			// If we get here, something failed; redisplay form
			// We need to repopulate the AvailableMaterials for the view
			var materials = await _context.MaterialMaster.ToListAsync();
			viewModel.AvailableMaterials = materials.Select(m => new MaterialDisplayModel
			{
				Id = m.Id,
				ShortName = m.ShortName,
				Materialname = m.Materialname,
				Unit = m.Unit,
				Category = m.Category,
				subcategory = m.subcategory,
				sequence = m.sequence,
				segementname = m.segementname,
				material3partycode = m.material3partycode,
				price = m.price,
				isactive = m.isactive,
				CratesCode = m.CratesTypes
			}).ToList();

			return View(viewModel);
		}

		// GET: DealerMasters/Delete/5
		public async Task<IActionResult> Delete(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var dealerMaster = await _context.DealerMasters
				.Include(d => d.DealerBasicOrders)
				.FirstOrDefaultAsync(m => m.Id == id);

			if (dealerMaster == null)
			{
				return NotFound();
			}

			return View(dealerMaster);
		}

		// POST: DealerMasters/Delete/5
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(int id)
		{
			var dealerMaster = await _context.DealerMasters
				.Include(d => d.DealerBasicOrders)
				.FirstOrDefaultAsync(m => m.Id == id);

			if (dealerMaster == null)
			{
				return NotFound();
			}

			try
			{
				// Remove all associated dealer basic orders first (due to foreign key constraint)
				_context.DealerBasicOrders.RemoveRange(dealerMaster.DealerBasicOrders);

				// Then remove the dealer master
				_context.DealerMasters.Remove(dealerMaster);

				await _context.SaveChangesAsync();
				_notifyService.Success("Dealer deleted successfully.");
				return RedirectToAction(nameof(Index));
			}
			catch (Exception ex)
			{
				// Log the exception
				_notifyService.Error("An error occurred while deleting the dealer. Please try again.");
				ModelState.AddModelError("", "An error occurred while deleting the dealer. Please try again. Error: " + ex.Message);
				return View(dealerMaster);
			}
		}

		// POST: Add Dealer Basic Order
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> AddBasicOrder([Bind("DealerId,MaterialName,SapCode,ShortCode,Quantity,Rate")] DealerBasicOrder dealerBasicOrder)
		{
			if (ModelState.IsValid)
			{
				_context.Add(dealerBasicOrder);
				await _context.SaveChangesAsync();
				_notifyService.Success("Dealer basic order added successfully.");
				return RedirectToAction(nameof(Index));
			}

			// If validation fails, redirect back to the dealer list page
			_notifyService.Error("Failed to add dealer basic order.");
			return RedirectToAction(nameof(Index));
		}

		// POST: Edit Dealer Basic Order
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> EditBasicOrder([Bind("Id,DealerId,MaterialName,SapCode,ShortCode,Quantity,Rate")] DealerBasicOrder dealerBasicOrder)
		{
			if (ModelState.IsValid)
			{
				try
				{
					_context.Update(dealerBasicOrder);
					await _context.SaveChangesAsync();
					_notifyService.Success("Dealer basic order updated successfully.");
				}
				catch (DbUpdateConcurrencyException)
				{
					if (!DealerBasicOrderExists(dealerBasicOrder.Id))
					{
						_notifyService.Error("Dealer basic order not found.");
						return NotFound();
					}
					else
					{
						_notifyService.Error("An error occurred while updating the dealer basic order.");
						throw;
					}
				}
				return RedirectToAction(nameof(Index));
			}

			// If validation fails, redirect back to the dealer list page
			_notifyService.Error("Failed to update dealer basic order.");
			return RedirectToAction(nameof(Index));
		}

		// POST: Delete Dealer Basic Order
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteBasicOrder(int id)
		{
			var dealerBasicOrder = await _context.DealerBasicOrders.FindAsync(id);
			if (dealerBasicOrder != null)
			{
				int dealerId = dealerBasicOrder.DealerId;
				_context.DealerBasicOrders.Remove(dealerBasicOrder);
				await _context.SaveChangesAsync();
				_notifyService.Success("Dealer basic order deleted successfully.");
				return RedirectToAction(nameof(Index));
			}

			// If order not found, redirect back to the dealer list
			_notifyService.Error("Dealer basic order not found.");
			return RedirectToAction(nameof(Index));
		}

		private bool DealerMasterExists(int id)
		{
			return _context.DealerMasters.Any(e => e.Id == id);
		}

		private bool DealerBasicOrderExists(int id)
		{
			return _context.DealerBasicOrders.Any(e => e.Id == id);
		}

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
							Value = customer.Id.ToString(),
							Text = customer.Name
						});
					}
				}

				var defItem = new SelectListItem()
				{
					Value = "",
					Text = "----Select Customer----"
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
								Value = customer.Id.ToString(),
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
						Value = loggedInCustomer.Id.ToString(),
						Text = loggedInCustomer.Name
					});
				}

				var defItem = new SelectListItem()
				{
					Value = "",
					Text = "----Select Customer----"
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
					Value = n.Id.ToString(),
					Text = n.Name
				}).ToList();

				var defItem = new SelectListItem()
				{
					Value = "",
					Text = "----Select Customer----"
				};

				lstProducts.Insert(0, defItem);

				return lstProducts;
			}

		}

		private void viewDataForView(DealerOrderViewModel viewModel)
		{
			// Get all materials for the dropdown/table
			if (viewModel.AvailableMaterials == null || viewModel.AvailableMaterials.Count == 0)
			{
				var materials = _context.MaterialMaster.ToList();
				viewModel.AvailableMaterials = materials.Select(m => new MaterialDisplayModel
				{
					Id = m.Id,
					ShortName = m.ShortName,
					Materialname = m.Materialname,
					Unit = m.Unit,
					Category = m.Category,
					subcategory = m.subcategory,
					sequence = m.sequence,
					segementname = m.segementname,
					material3partycode = m.material3partycode,
					price = m.price,
					isactive = m.isactive,
					CratesCode = m.CratesTypes
				}).ToList();
			}

			// Get customers for dropdown using the same logic as RepeatOrderController
			ViewBag.Customers = GetCustomer();
		}

		// AJAX endpoint to get route code for a customer
		[HttpGet]
		public async Task<IActionResult> GetRouteCode(int customerId)
		{
			var customer = await _context.Customer_Master.FindAsync(customerId);
			if (customer != null)
			{
				return Json(new { routeCode = customer.route });
			}
			return Json(new { routeCode = "" });
		}
	}
}