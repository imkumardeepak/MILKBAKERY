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

					// Pass customer data to view for displaying customer names
					var customers = await _context.Customer_Master.ToListAsync();
					ViewBag.CustomerList = customers;

					return View(dealers);
				}
				else
				{
					// If customer not found, return empty list
					ViewBag.Customers = new List<SelectListItem>();
					ViewBag.SelectedCustomerId = "";
					ViewBag.CustomerList = new List<Customer_Master>();
					return View(new List<DealerMaster>());
				}
			}
			else
			{
				// For other roles (Admin, Sales), show all dealers
				var dealers = await _context.DealerMasters
					.Include(d => d.DealerBasicOrders)
					.ToListAsync();

				// Pass customer data to view for displaying customer names
				var customers = await _context.Customer_Master.ToListAsync();
				ViewBag.CustomerList = customers;

				return View(dealers);
			}
		}

		// GET: DealerMasters/AddOrEdit/5
		public async Task<IActionResult> AddOrEdit(int? id)
		{
			// Create the view model
			var viewModel = new DealerOrderViewModel();

			// Get customers for dropdown using the same logic as RepeatOrderController
			ViewBag.Customers = GetCustomer();

			if (id == 0 || id == null)
			{
				// For new dealers, we'll populate materials when a customer is selected
				// Initially show no materials until customer is selected
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

				// Get materials based on customer segment mapping
				var materials = await GetMaterialsForCustomerAsync(dealerMaster.DistributorId);
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
					CratesCode = m.CratesTypes,
					dealerprice = m.dealerprice
				}).ToList();

				// Populate the existing orders and selected materials
				viewModel.DealerBasicOrders = dealerMaster.DealerBasicOrders.ToList();

				// Populate the quantities and dealer prices
				foreach (var order in dealerMaster.DealerBasicOrders)
				{
					// Find the corresponding material in AvailableMaterials
					var material = viewModel.AvailableMaterials.FirstOrDefault(m => m.Materialname == order.MaterialName);
					if (material != null)
					{
						viewModel.SelectedMaterialIds.Add(material.Id);
						viewModel.MaterialQuantities[material.Id] = order.Quantity;

						// Store the rate from the DealerBasicOrder (this is the saved dealer rate)
						viewModel.MaterialDealerPrices[material.Id] = order.Rate;
					}
				}

				// For materials that don't have existing orders, use the material's default dealer price
				// This ensures that new materials show the default dealer price from MaterialMaster
				foreach (var material in viewModel.AvailableMaterials)
				{
					if (!viewModel.MaterialDealerPrices.ContainsKey(material.Id))
					{
						// Use the default dealer price from MaterialMaster when no specific dealer price exists
						viewModel.MaterialDealerPrices[material.Id] = material.dealerprice;
					}
				}

				viewDataForView(viewModel);
				return View(viewModel);
			}
		}

		// POST: DealerMasters/AddOrEdit/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> AddOrEdit(int id, DealerOrderViewModel viewModel, string submit, Dictionary<int, decimal> MaterialDealerPrices = null)
		{
			// Get customers for dropdown (needed in both GET and POST)
			ViewBag.Customers = GetCustomer();

			// Automatically populate RouteCode based on selected customer
			if (viewModel.DealerMaster.DistributorId > 0)
			{
				var customer = await _context.Customer_Master.FindAsync(viewModel.DealerMaster.DistributorId);
				if (customer != null)
				{
					viewModel.DealerMaster.RouteCode = customer.route;
				}

				// Get materials based on customer segment mapping
				var materials = await GetMaterialsForCustomerAsync(viewModel.DealerMaster.DistributorId);
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
					CratesCode = m.CratesTypes,
					dealerprice = m.dealerprice
				}).ToList();
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

						// Check if the phone number is already mapped to another dealer under the same distributor
						var existingDealerWithPhone = await _context.DealerMasters.FirstOrDefaultAsync(d => d.PhoneNo == viewModel.DealerMaster.PhoneNo && d.DistributorId == viewModel.DealerMaster.DistributorId);
						if (existingDealerWithPhone != null)
						{
							_notifyService.Error("A dealer with the same phone number already exists for the selected customer.");
							ModelState.AddModelError("", "A dealer with the same phone number already exists for the selected customer.");
							return View(viewModel);
						}


						//add in user table
						var user = new User
						{
							name = viewModel.DealerMaster.Name,
							phoneno = viewModel.DealerMaster.PhoneNo,
							ConfirmPassword = "1234",
							Password = "1234",
							Role = "Dealer"
						};
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
										// Get the rate from MaterialDealerPrices, or use default rate of 1
										decimal rate = 1;
										if (MaterialDealerPrices != null && MaterialDealerPrices.ContainsKey(materialId))
										{
											rate = MaterialDealerPrices[materialId];
										}

										var dealerBasicOrder = new DealerBasicOrder
										{
											DealerId = viewModel.DealerMaster.Id,
											MaterialName = material.Materialname,
											SapCode = material.material3partycode,
											ShortCode = material.ShortName,
											Quantity = quantity,
											Rate = rate
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
						// Check if the phone number is already mapped to another dealer under the same distributor (excluding current dealer)
						var existingDealerWithPhone = await _context.DealerMasters.FirstOrDefaultAsync(d => d.PhoneNo == viewModel.DealerMaster.PhoneNo && d.DistributorId == viewModel.DealerMaster.DistributorId && d.Id != id);
						if (existingDealerWithPhone != null)
						{
							_notifyService.Error("A dealer with the same phone number already exists for the selected customer.");
							ModelState.AddModelError("", "A dealer with the same phone number already exists for the selected customer.");
							return View(viewModel);
						}

						// Automatically populate RouteCode based on selected customer
						if (viewModel.DealerMaster.DistributorId > 0)
						{
							var customer = await _context.Customer_Master.FindAsync(viewModel.DealerMaster.DistributorId);
							if (customer != null)
							{
								viewModel.DealerMaster.RouteCode = customer.route;
							}
						}
						//update user table
						var user = await _context.Users.FirstOrDefaultAsync(u => u.phoneno == viewModel.DealerMaster.PhoneNo && u.Role == "Dealer");
						if (user != null)
						{
							user.name = viewModel.DealerMaster.Name;
							user.phoneno = viewModel.DealerMaster.PhoneNo;
							user.Role = "Dealer";
							user.Password = "1234";
							_context.Update(user);
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
										// Get the rate from MaterialDealerPrices, or use material price as fallback
										decimal rate = 1;
										if (MaterialDealerPrices != null && MaterialDealerPrices.ContainsKey(materialId))
										{
											rate = MaterialDealerPrices[materialId];
										}
										else
										{
											// For existing orders, try to preserve the existing rate
											var existingOrder = viewModel.DealerBasicOrders.FirstOrDefault(o => o.MaterialName == material.Materialname);
											if (existingOrder != null && existingOrder.Quantity > 0)
											{
												rate = existingOrder.Rate;
											}
										}

										var dealerBasicOrder = new DealerBasicOrder
										{
											DealerId = viewModel.DealerMaster.Id,
											MaterialName = material.Materialname,
											SapCode = material.material3partycode,
											ShortCode = material.ShortName,
											Quantity = quantity,
											Rate = rate
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

		// GET: DealerMasters/UploadDealers
		public async Task<IActionResult> UploadDealers()
		{
			ViewBag.Customers = GetCustomer();
			return View();
		}

		// POST: DealerMasters/UploadDealers
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> UploadDealers(IFormFile file, int customerId)
		{
			if (file == null || file.Length == 0)
			{
				_notifyService.Error("Please select a file to upload.");
				ViewBag.Customers = GetCustomer();
				return View();
			}

			if (customerId <= 0)
			{
				_notifyService.Error("Please select a customer.");
				ViewBag.Customers = GetCustomer();
				return View();
			}

			try
			{
				var dealers = new List<DealerMaster>();
				using (var stream = new MemoryStream())
				{
					await file.CopyToAsync(stream);
					stream.Position = 0;

					using (var reader = new StreamReader(stream))
					{
						string line;
						bool isFirstLine = true;

						while ((line = await reader.ReadLineAsync()) != null)
						{
							if (isFirstLine)
							{
								isFirstLine = false;
								continue; // Skip header line
							}

							var values = line.Split(',');
							if (values.Length >= 5) // Updated to require only 5 columns
							{
								// Get the distributor ID from the selected customer
								int distributorId = customerId;

								var dealer = new DealerMaster
								{
									Name = values[0].Trim(),
									DistributorId = distributorId,
									ContactPerson = values[1].Trim(),
									Address = values[2].Trim(),
									PhoneNo = values[3].Trim(),
									Email = values[4].Trim()
								};

								dealers.Add(dealer);
							}
						}
					}
				}

				// Save dealers to database
				foreach (var dealer in dealers)
				{
					// Set the route code based on the customer
					var customer = await _context.Customer_Master.FindAsync(dealer.DistributorId);
					if (customer != null)
					{
						dealer.RouteCode = customer.route;
					}

					// Check if dealer already exists for this customer
					var existingDealer = await _context.DealerMasters
						.FirstOrDefaultAsync(d => d.Name == dealer.Name && d.DistributorId == dealer.DistributorId);

					// Check if the phone number is already mapped to another dealer under the same distributor
					var existingDealerWithPhone = await _context.DealerMasters
						.FirstOrDefaultAsync(d => d.PhoneNo == dealer.PhoneNo && d.DistributorId == dealer.DistributorId);

					if (existingDealerWithPhone != null && (existingDealer == null || existingDealer.Id != existingDealerWithPhone.Id))
					{
						_notifyService.Error($"A dealer with phone number {dealer.PhoneNo} already exists for the selected customer.");
						ViewBag.Customers = GetCustomer();
						return View();
					}

					if (existingDealer == null)
					{
						User user = new User();
						user.phoneno = dealer.PhoneNo;
						user.Password = "1234";
						user.Role = "Dealer";
						user.ConfirmPassword = "1234";
						user.name = dealer.Name;

						_context.Users.Add(user);
						_context.DealerMasters.Add(dealer);
					}
					else
					{
						// Update existing dealer
						existingDealer.ContactPerson = dealer.ContactPerson;
						existingDealer.Address = dealer.Address;
						existingDealer.PhoneNo = dealer.PhoneNo;
						existingDealer.Email = dealer.Email;

						// Update route code based on customer
						if (customer != null)
						{
							existingDealer.RouteCode = customer.route;
						}

						// Update user name
						var user = await _context.Users.FirstOrDefaultAsync(u => u.phoneno == existingDealer.PhoneNo && u.Role == "Dealer");
						if (user != null)
						{
							user.name = dealer.Name;
							user.phoneno = dealer.PhoneNo;
							user.Password = "1234";
							_context.Users.Update(user);
						}


						_context.DealerMasters.Update(existingDealer);
					}
				}

				await _context.SaveChangesAsync();
				_notifyService.Success($"Successfully uploaded {dealers.Count} dealers.");
				return RedirectToAction(nameof(Index));
			}
			catch (Exception ex)
			{
				_notifyService.Error("Error uploading dealers: " + ex.Message);
				ViewBag.Customers = GetCustomer();
				return View();
			}
		}

		// GET: DealerMasters/DownloadTemplate
		public IActionResult DownloadTemplate()
		{
			var templateContent = "Name,ContactPerson,Address,PhoneNo,Email\n" +
								 "Dealer Name,Contact Person,123 Main St,1234567890,email@example.com\n";

			var bytes = Encoding.UTF8.GetBytes(templateContent);
			var stream = new MemoryStream(bytes);

			return File(stream, "text/csv", "dealer_template.csv");
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
			// Get customers for dropdown using the same logic as RepeatOrderController
			ViewBag.Customers = GetCustomer();
		}

		// Helper method to get materials based on customer segment mapping
		private async Task<List<MaterialMaster>> GetMaterialsForCustomerAsync(int customerId)
		{
			// Get the customer
			var customer = await _context.Customer_Master.FirstOrDefaultAsync(c => c.Id == customerId);
			if (customer == null)
			{
				// If no customer found, return empty list
				return new List<MaterialMaster>();
			}

			// Get segments mapped to this customer
			var segmentMappings = await _context.CustomerSegementMap
				.Where(m => m.Customername == customer.Name)
				.ToListAsync();

			// If no segments found for this customer, return empty list
			if (!segmentMappings.Any())
			{
				return new List<MaterialMaster>();
			}

			// Get the segment names
			var segmentNames = segmentMappings.Select(m => m.SegementName).ToList();

			// Get materials that belong to these segments
			var materials = await _context.MaterialMaster
				.Where(m => segmentNames.Contains(m.segementname) && !m.Materialname.StartsWith("CRATES") && m.isactive == true)
				.OrderBy(m => m.sequence)
				.ToListAsync();

			return materials;
		}

		// Helper method to get materials based on customer segment mapping (synchronous version)
		private List<MaterialMaster> GetMaterialsForCustomer(int customerId)
		{
			// Get the customer
			var customer = _context.Customer_Master.FirstOrDefault(c => c.Id == customerId);
			if (customer == null)
			{
				// If no customer found, return empty list
				return new List<MaterialMaster>();
			}

			// Get segments mapped to this customer
			var segmentMappings = _context.CustomerSegementMap
				.Where(m => m.Customername == customer.Name)
				.ToList();

			// If no segments found for this customer, return empty list
			if (!segmentMappings.Any())
			{
				return new List<MaterialMaster>();
			}

			// Get the segment names
			var segmentNames = segmentMappings.Select(m => m.SegementName).ToList();

			// Get materials that belong to these segments
			var materials = _context.MaterialMaster
				.Where(m => segmentNames.Contains(m.segementname) && !m.Materialname.StartsWith("CRATES") && m.isactive == true)
				.OrderBy(m => m.sequence)
				.ToList();

			return materials;
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

		// AJAX endpoint to get materials for a customer
		[HttpGet]
		public async Task<IActionResult> GetMaterialsForCustomer(int customerId, int? dealerId = null)
		{
			var materials = await GetMaterialsForCustomerAsync(customerId);
			var materialDisplayModels = materials.Select(m => new MaterialDisplayModel
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
				CratesCode = m.CratesTypes,
				dealerprice = m.dealerprice
			}).ToList();

			// If editing an existing dealer, include existing order information
			if (dealerId.HasValue && dealerId.Value > 0)
			{
				var dealerMaster = await _context.DealerMasters
					.Include(d => d.DealerBasicOrders)
					.FirstOrDefaultAsync(d => d.Id == dealerId.Value);

				if (dealerMaster != null)
				{
					// Create dictionaries for quantities and dealer prices
					var materialQuantities = new Dictionary<int, int>();
					var materialDealerPrices = new Dictionary<int, decimal>();

					// Populate with existing order data
					foreach (var order in dealerMaster.DealerBasicOrders)
					{
						// Find the corresponding material
						var material = materialDisplayModels.FirstOrDefault(m => m.Materialname == order.MaterialName);
						if (material != null)
						{
							materialQuantities[material.Id] = order.Quantity;
							// Use the rate from DealerBasicOrder (saved dealer-specific rate)
							materialDealerPrices[material.Id] = order.Rate;
						}
					}

					// For materials without existing orders, use default dealer price from MaterialMaster
					// This ensures that new materials show the default dealer price
					foreach (var material in materialDisplayModels)
					{
						if (!materialDealerPrices.ContainsKey(material.Id))
						{
							// Use the default dealer price from MaterialMaster when no specific dealer price exists
							materialDealerPrices[material.Id] = material.dealerprice;
						}
					}

					return Json(new
					{
						materials = materialDisplayModels,
						quantities = materialQuantities,
						dealerPrices = materialDealerPrices
					});
				}
			}

			// For new dealers, initialize with default dealer prices from MaterialMaster
			var defaultDealerPrices = materialDisplayModels.ToDictionary(m => m.Id, m => m.dealerprice);

			return Json(new
			{
				materials = materialDisplayModels,
				quantities = new Dictionary<int, int>(),
				dealerPrices = defaultDealerPrices
			});
		}
	}
}