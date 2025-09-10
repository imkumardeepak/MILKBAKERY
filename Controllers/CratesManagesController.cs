using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Milk_Bakery.Data;
using Milk_Bakery.Models;
using Milk_Bakery.ViewModels;

namespace Milk_Bakery.Controllers
{
	[Authentication]
	public class CratesManagesController : Controller
	{
		private readonly MilkDbContext _context;
		public INotyfService _notifyService { get; }

		public CratesManagesController(MilkDbContext context, INotyfService notyf)
		{
			_context = context;
			_notifyService = notyf;
		}

		// GET: CratesManages
		public async Task<IActionResult> Index()
		{
			var milkDbContext = _context.CratesManages
				.Include(c => c.Customer)
				.Include(c => c.CratesType)
				.OrderBy(c => c.CustomerId)
				.ThenByDescending(c => c.DispDate); // Order by date descending to get latest records first

			// Get all crates manages records
			var allCratesManages = await milkDbContext.ToListAsync();

			// Group by CustomerId and select only the first record for each customer
			var cratesManages = allCratesManages
				.GroupBy(c => c.CustomerId)
				.Select(g => g.First())
				.ToList();

			// Pass customer and segment lists for filtering
			ViewBag.CustomerList = GetCustomerList();
			ViewBag.SegmentList = GetSegmentList();

			// Get segment names for each crates manage record
			foreach (var item in cratesManages)
			{
				var segmentName = GetSegmentNameForCustomer(item.CustomerId, item.SegmentCode);
				ViewData["SegmentName_" + item.Id] = segmentName;
			}

			return View(cratesManages);
		}

		// GET: CratesManages/Details/5
		public async Task<IActionResult> Details(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var cratesManage = await _context.CratesManages
				.Include(c => c.Customer)
				.Include(c => c.CratesType)
				.FirstOrDefaultAsync(m => m.Id == id);
			if (cratesManage == null)
			{
				return NotFound();
			}

			return View(cratesManage);
		}

		// GET: CratesManages/Create
		public IActionResult Create()
		{
			ViewBag.customer = GetCustomer();
			ViewBag.cratesType = GetCratesType();
			// No need to populate segments here as they will be loaded dynamically
			return View();
		}

		// POST: CratesManages/Create
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(CratesManage cratesManage)
		{
			if (ModelState.IsValid)
			{
				var cratesManages = await _context.CratesManages
					.Where(c => c.CustomerId == cratesManage.CustomerId && c.SegmentCode == cratesManage.SegmentCode && c.CratesTypeId == cratesManage.CratesTypeId)
					.OrderByDescending(c => c.DispDate)
					.ToListAsync();
				if (cratesManages.Any())
				{
					ViewBag.customer = GetCustomer();
					ViewBag.cratesType = GetCratesType();
					_notifyService.Error("Crates record already exists for this customer, segment and crates type.");
					return View(cratesManage);
				}

				// Calculate balance: Opening + Inward - Outward
				cratesManage.Balance = cratesManage.Opening + cratesManage.Inward - cratesManage.Outward;

				_context.Add(cratesManage);
				await _context.SaveChangesAsync();
				_notifyService.Success("Crates record created successfully");
				return RedirectToAction(nameof(Index));
			}
			ViewBag.customer = GetCustomer();
			ViewBag.cratesType = GetCratesType();
			ViewBag.segment = GetSegment();
			return View(cratesManage);
		}

		// GET: CratesManages/Edit/5
		public async Task<IActionResult> Edit(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var cratesManage = await _context.CratesManages.FindAsync(id);
			if (cratesManage == null)
			{
				return NotFound();
			}
			ViewBag.customer = GetCustomer();
			ViewBag.cratesType = GetCratesType();
			// No need to populate segments here as they will be loaded dynamically
			return View(cratesManage);
		}

		// POST: CratesManages/Edit/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id, CratesManage cratesManage)
		{
			if (id != cratesManage.Id)
			{
				return NotFound();
			}

			if (ModelState.IsValid)
			{
				try
				{
					// Calculate balance: Opening + Inward - Outward
					cratesManage.Balance = cratesManage.Opening + cratesManage.Inward - cratesManage.Outward;

					_context.Update(cratesManage);
					await _context.SaveChangesAsync();
					_notifyService.Success("Crates record updated successfully");
				}
				catch (DbUpdateConcurrencyException)
				{
					if (!CratesManageExists(cratesManage.Id))
					{
						return NotFound();
					}
					else
					{
						throw;
					}
				}
				return RedirectToAction(nameof(Index));
			}
			ViewBag.customer = GetCustomer();
			ViewBag.cratesType = GetCratesType();
			ViewBag.segment = GetSegment();
			return View(cratesManage);
		}

		// GET: CratesManages/Delete/5
		public async Task<IActionResult> Delete(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var cratesManage = await _context.CratesManages
				.Include(c => c.Customer)
				.FirstOrDefaultAsync(m => m.Id == id);
			if (cratesManage == null)
			{
				return NotFound();
			}

			return View(cratesManage);
		}

		// POST: CratesManages/Delete/5
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(int id)
		{
			var cratesManage = await _context.CratesManages.FindAsync(id);
			if (cratesManage != null)
			{
				_context.CratesManages.Remove(cratesManage);
				await _context.SaveChangesAsync();
				_notifyService.Success("Crates record deleted successfully");
			}
			return RedirectToAction(nameof(Index));
		}

		private bool CratesManageExists(int id)
		{
			return _context.CratesManages.Any(e => e.Id == id);
		}

		private List<SelectListItem> GetCustomer()
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

		private List<SelectListItem> GetCratesType()
		{
			var lstProducts = new List<SelectListItem>();

			lstProducts = _context.CratesTypes.AsNoTracking().Select(n =>
			new SelectListItem
			{
				Value = n.Id.ToString(),
				Text = n.Cratestype
			}).ToList();

			var defItem = new SelectListItem()
			{
				Value = "",
				Text = "----Select Crates Type----"
			};

			lstProducts.Insert(0, defItem);

			return lstProducts;
		}

		private List<SelectListItem> GetSegment()
		{
			var lstProducts = new List<SelectListItem>();

			lstProducts = _context.SegementMaster.AsNoTracking().Select(n =>
			new SelectListItem
			{
				Value = n.Segement_Code,
				Text = n.SegementName
			}).ToList();

			var defItem = new SelectListItem()
			{
				Value = "",
				Text = "----Select Segment----"
			};

			lstProducts.Insert(0, defItem);

			return lstProducts;
		}

		private List<SelectListItem> GetCustomerList()
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
				Text = "All Customers"
			};

			lstProducts.Insert(0, defItem);

			return lstProducts;
		}

		private List<SelectListItem> GetSegmentList()
		{
			var lstProducts = new List<SelectListItem>();

			lstProducts = _context.SegementMaster.AsNoTracking().Select(n =>
			new SelectListItem
			{
				Value = n.Segement_Code,
				Text = n.SegementName
			}).ToList();

			var defItem = new SelectListItem()
			{
				Value = "",
				Text = "All Segments"
			};

			lstProducts.Insert(0, defItem);

			return lstProducts;
		}

		[HttpGet]
		public IActionResult GetSegmentsForCustomer(int customerId)
		{
			var segments = GetSegmentsForCustomerById(customerId);
			return Json(segments);
		}

		// GET: CratesInwardEntry
		public IActionResult CratesInwardEntry()
		{
			// Get distributors (customers with account type 'Distributor' or similar)
			ViewBag.Distributors = GetDistributors();

			// Get divisions
			ViewBag.Divisions = GetDivisions();

			// Get crate types
			ViewBag.CrateTypes = GetCrateTypes();

			return View();
		}

		// POST: CratesInwardEntry
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CratesInwardEntry(int distributorId, string segmentCode, Dictionary<int, int> crateQuantities, DateTime entryDate)
		{
			if (ModelState.IsValid)
			{
				// Get the distributor
				var distributor = await _context.Customer_Master.FindAsync(distributorId);
				if (distributor == null)
				{
					_notifyService.Error("Invalid distributor selected.");
					ViewBag.Distributors = GetDistributors();
					ViewBag.CrateTypes = GetCrateTypes();
					return View();
				}

				// Create crate records for each crate type
				foreach (var kvp in crateQuantities)
				{
					var crateTypeId = kvp.Key;
					var quantity = kvp.Value;

					// Only create record if quantity > 0
					if (quantity > 0)
					{
						var cratesManage = new CratesManage
						{
							CustomerId = distributorId,
							SegmentCode = segmentCode, // Use the actual segment code from customer-segment mapping
							DispDate = entryDate,
							Opening = 0,
							Outward = 0,
							Inward = quantity,
							Balance = quantity,
							CratesTypeId = crateTypeId
						};
						_context.CratesManages.Add(cratesManage);
					}
				}

				await _context.SaveChangesAsync();
				_notifyService.Success("Crates inward entry recorded successfully.");
				return RedirectToAction(nameof(Index));
			}

			ViewBag.Distributors = GetDistributors();
			ViewBag.CrateTypes = GetCrateTypes();
			return View();
		}

		// GET: CratesManages/Overview
		public async Task<IActionResult> Overview()
		{
			try
			{
				var viewModel = new ViewModels.CratesOverviewViewModel();

				// Get all customers
				var customers = await _context.Customer_Master.ToListAsync();

				// Get all crate types
				var crateTypes = await _context.CratesTypes.ToListAsync();

				// For each customer and each crate type, create an entry
				foreach (var customer in customers)
				{
					// Get segments for this customer
					var segments = GetSegmentsForCustomerById(customer.Id);

					foreach (var segment in segments)
					{
						foreach (var crateType in crateTypes)
						{
							viewModel.CratesEntries.Add(new ViewModels.CratesEntryViewModel
							{
								CustomerId = customer.Id,
								CustomerName = customer.Name ?? string.Empty,
								SegmentCode = segment.Value ?? string.Empty,
								SegmentName = segment.Text ?? string.Empty,
								CrateTypeId = crateType.Id,
								CrateTypeName = crateType.Cratestype ?? string.Empty,
								Opening = 0,
								Outward = 0,
								Inward = 0,
								Balance = 0
							});
						}
					}
				}

				return View(viewModel);
			}
			catch (Exception ex)
			{
				_notifyService.Error("An error occurred while loading the overview page: " + ex.Message);
				return RedirectToAction(nameof(Index));
			}
		}

		// POST: CratesManages/SaveOverview
		[HttpPost]
		public async Task<IActionResult> SaveOverview(CratesOverviewViewModel model)
		{
			try
			{
				if (model == null || model.CratesEntries == null)
				{
					_notifyService.Error("No data received. Please try again.");
					return RedirectToAction(nameof(Overview));
				}

				// Log the number of entries received
				Console.WriteLine($"Received {model.CratesEntries.Count} entries");

				int savedCount = 0;

				// Process each entry
				foreach (var entry in model.CratesEntries)
				{
					// Only process entries with opening values > 0
					if (entry.Opening > 0)
					{
						var cratesManage = new CratesManage
						{
							CustomerId = entry.CustomerId,
							SegmentCode = entry.SegmentCode,
							DispDate = DateTime.Today,
							Opening = entry.Opening,
							Outward = entry.Outward,
							Inward = entry.Inward,
							Balance = entry.Opening + entry.Inward - entry.Outward,
							CratesTypeId = entry.CrateTypeId
						};

						_context.CratesManages.Add(cratesManage);
						savedCount++;
					}
				}

				await _context.SaveChangesAsync();
				_notifyService.Success($"Successfully saved {savedCount} records.");
				return RedirectToAction(nameof(Index));
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Exception in SaveOverview: {ex.Message}");
				Console.WriteLine($"Stack Trace: {ex.StackTrace}");
				_notifyService.Error("An error occurred while processing the form: " + ex.Message);
				return RedirectToAction(nameof(Overview));
			}
		}

		private List<SelectListItem> GetDistributors()
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
				Text = "----Select Distributor----"
			};

			lstProducts.Insert(0, defItem);

			return lstProducts;
		}

		private List<SelectListItem> GetDivisions()
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
				Text = "----Select Division----"
			};

			lstProducts.Insert(0, defItem);

			return lstProducts;
		}

		private List<SelectListItem> GetCrateTypes()
		{
			var lstProducts = new List<SelectListItem>();

			lstProducts = _context.CratesTypes.AsNoTracking().Select(n =>
			new SelectListItem
			{
				Value = n.Id.ToString(),
				Text = n.Cratestype
			}).ToList();

			return lstProducts;
		}

		private List<SelectListItem> GetSegmentsForCustomerById(int customerId)
		{
			var lstProducts = new List<SelectListItem>();

			// Get the customer
			var customer = _context.Customer_Master.FirstOrDefault(c => c.Id == customerId);
			if (customer == null)
			{
				return lstProducts;
			}

			// Get segments mapped to this customer
			var mappings = _context.CustomerSegementMap
				.Where(m => m.Customername == customer.Name)
				.ToList();

			lstProducts = mappings.Select(n => new SelectListItem
			{
				Value = n.custsegementcode,
				Text = n.SegementName
			}).ToList();

			return lstProducts;
		}

		private string GetSegmentNameForCustomer(int customerId, string segmentCode)
		{
			// Get the customer name
			var customer = _context.Customer_Master.FirstOrDefault(c => c.Id == customerId);
			if (customer == null) return "N/A";

			// Get the segment name based on customer-segment mapping
			var mapping = _context.CustomerSegementMap
				.FirstOrDefault(m => m.Customername == customer.Name && m.custsegementcode == segmentCode);

			return mapping?.SegementName ?? "N/A";
		}
	}
}