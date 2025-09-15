using AspNetCoreHero.ToastNotification.Abstractions;
using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Milk_Bakery.Data;
using Milk_Bakery.Models;
using Milk_Bakery.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;
using System.Text;

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

		[HttpGet]
		public IActionResult GetCustomersForSegment(string segmentCode)
		{
			if (string.IsNullOrEmpty(segmentCode))
			{
				return Json(new List<SelectListItem>());
			}

			// Get customer segment mappings for this segment
			var customerMappings = _context.CustomerSegementMap
				.Where(csm => csm.custsegementcode == segmentCode)
				.ToList();

			// Get customer IDs from the mappings
			var customerIds = customerMappings.Select(csm => csm.Customername).ToList();

			// Get customers that match these names
			var customers = _context.Customer_Master
				.Where(cm => customerIds.Contains(cm.Name) && cm.Division == segmentCode)
				.Select(cm => new SelectListItem
				{
					Value = cm.Id.ToString(),
					Text = cm.Name
				}).OrderBy(cm => cm.Text)
				.ToList();

			// Add default option
			customers.Insert(0, new SelectListItem
			{
				Value = "",
				Text = "----Select Customer----"
			});

			return Json(customers);
		}

		[HttpGet]
		public IActionResult GetCrateTypesForDivision(string division)
		{
			if (string.IsNullOrEmpty(division))
			{
				return Json(new List<SelectListItem>());
			}

			var crateTypes = _context.CratesTypes
				.Where(ct => ct.Division == division)
				.Select(ct => new SelectListItem
				{
					Value = ct.Id.ToString(),
					Text = ct.Cratestype
				})
				.ToList();

			// Add default option
			crateTypes.Insert(0, new SelectListItem
			{
				Value = "",
				Text = "----Select Crates Type----"
			});

			return Json(crateTypes);
		}

		[HttpGet]
		public IActionResult GetCrateTypesForSegment(string segment)
		{
			if (string.IsNullOrEmpty(segment))
			{
				return Json(new List<SelectListItem>());
			}

			var crateTypes = _context.CratesTypes
				.Where(ct => ct.Division == segment)
				.Select(ct => new SelectListItem
				{
					Value = ct.Id.ToString(),
					Text = ct.Cratestype
				}).OrderBy(cm => cm.Text)
				.ToList();

			// Add default option
			crateTypes.Insert(0, new SelectListItem
			{
				Value = "",
				Text = "----Select Crates Type----"
			});

			return Json(crateTypes);
		}

		// GET: CratesInwardEntry
		public IActionResult CratesInwardEntry()
		{
			// Get segments
			ViewBag.Segments = GetSegment();

			// No need to pre-load crate types since they will be loaded dynamically based on segment selection
			// ViewBag.CrateTypes is no longer needed

			return View();
		}

		// POST: CratesInwardEntry
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CratesInwardEntry(int distributorId, string segmentCode, int cratesTypeId,
			int[] customerIds, int[] quantities, DateTime entryDate)
		{
			if (ModelState.IsValid)
			{
				// Check if segment is selected
				if (string.IsNullOrEmpty(segmentCode))
				{
					_notifyService.Error("Please select a segment.");
					ViewBag.Segments = GetSegment();
					return View();
				}

				// Check if crate type is selected
				if (cratesTypeId <= 0)
				{
					_notifyService.Error("Please select a crate type.");
					ViewBag.Segments = GetSegment();
					return View();
				}

				// Check if distributor is selected
				if (distributorId <= 0)
				{
					_notifyService.Error("Please select a distributor.");
					ViewBag.Segments = GetSegment();
					return View();
				}

				// Check if any quantities are provided
				if (customerIds == null || quantities == null || customerIds.Length == 0 || quantities.Length == 0)
				{
					_notifyService.Error("Please enter at least one crate quantity.");
					ViewBag.Segments = GetSegment();
					return View();
				}

				// Get the distributor
				var distributor = await _context.Customer_Master.FindAsync(distributorId);
				if (distributor == null)
				{
					_notifyService.Error("Invalid distributor selected.");
					ViewBag.Segments = GetSegment();
					return View();
				}

				int recordsUpdated = 0;

				// Create crate records for each customer
				for (int i = 0; i < customerIds.Length; i++)
				{
					var customerId = customerIds[i];
					var quantity = quantities[i];

					// Only create record if quantity > 0
					if (quantity > 0)
					{
						var topRecord = await _context.CratesManages
							.Where(a => a.CustomerId == customerId && a.SegmentCode == segmentCode && a.CratesTypeId == cratesTypeId)
							.OrderByDescending(a => a.DispDate)
							.FirstOrDefaultAsync();

						if (topRecord != null)
						{
							topRecord.Inward += quantity;
							topRecord.Balance = topRecord.Opening + topRecord.Outward - topRecord.Inward;
							_context.CratesManages.Update(topRecord);
							recordsUpdated++;
						}
						else
						{
							// If no record exists, create a new one with default values
							var cratesManage = new CratesManage
							{
								CustomerId = customerId,
								SegmentCode = segmentCode,
								CratesTypeId = cratesTypeId,
								Opening = 0,
								Outward = 0,
								Inward = quantity,
								Balance = quantity, // Since Opening=0 and Outward=0
								DispDate = DateTime.Today
							};
							_context.CratesManages.Add(cratesManage);
							recordsUpdated++;
						}

					}
				}

				if (recordsUpdated > 0)
				{
					await _context.SaveChangesAsync();
					_notifyService.Success($"Successfully updated {recordsUpdated} crate records.");
				}
				else
				{
					_notifyService.Warning("No crate records were updated.");
				}

				return RedirectToAction(nameof(Index));
			}

			ViewBag.Segments = GetSegment();
			// No need to load crate types here since they're loaded dynamically
			return View();
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
				}).OrderBy(cm => cm.Text)
				.ToList();

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
			}).OrderBy(cm => cm.Text).ToList();

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



		// GET: CratesManages/GetCustomersBySegment
		[HttpGet]
		public IActionResult GetCustomersBySegment(string segmentCode)
		{
			if (string.IsNullOrEmpty(segmentCode))
			{
				return Json(new List<object>());
			}

			// Get customer segment mappings for this segment
			var customerMappings = _context.CustomerSegementMap
				.Where(csm => csm.SegementName == segmentCode)
				.ToList();

			// Get customer IDs from the mappings
			var customerIds = customerMappings.Select(csm => csm.Customername).ToList();

			// Get customers that match these names
			var customers = _context.Customer_Master
				.Where(cm => customerIds.Contains(cm.Name))
				.Select(cm => new
				{
					cm.Id,
					cm.Name
				}).OrderBy(cm => cm.Name)
				.ToList();

			return Json(customers);
		}

		// GET: CratesManages/CreateBulkOpeningEntry																							
		[HttpGet]
		public async Task<IActionResult> CreateBulkOpeningEntry()
		{
			var viewModel = new OpeningBalanceEntryViewModel();

			ViewBag.DivisionId = new SelectList(await _context.SegementMaster.ToListAsync(), "SegementName", "SegementName");
			ViewBag.CratesTypeId = new SelectList(await _context.CratesTypes.ToListAsync(), "Id", "Cratestype");

			return View(viewModel);
		}

		// POST: CratesManages/CreateBulkOpeningEntry
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CreateBulkOpeningEntry(OpeningBalanceEntryViewModel viewModel)
		{
			if (ModelState.IsValid)
			{
				try
				{



					// Validate CratesType
					var cratesType = await _context.CratesTypes.FirstOrDefaultAsync(ct => ct.Id == viewModel.CratesTypeId);
					if (cratesType == null)
					{
						_notifyService.Error("Invalid Crates Type selected.");
						ViewBag.DivisionId = new SelectList(await _context.SegementMaster.ToListAsync(), "Segement_Code", "SegementName", viewModel.SegmentCode);
						ViewBag.CratesTypeId = new SelectList(await _context.CratesTypes.ToListAsync(), "Id", "Cratestype", viewModel.CratesTypeId);
						return View(viewModel);
					}

					int recordsProcessed = 0;

					foreach (var customer in viewModel.Customers.Where(a => a.OpeningBalance > 0))
					{
						var segment = await _context.CustomerSegementMap.FirstOrDefaultAsync(s => s.Customername == customer.CustomerName && s.SegementName == viewModel.SegmentCode);

						if (customer.OpeningBalance > 0)
						{
							// Check if a record for this customer, segment, and crates type already exists
							var existingRecord = await _context.CratesManages
								.Where(cm => cm.CustomerId == customer.CustomerId &&
									 cm.SegmentCode == viewModel.SegmentCode &&
									 cm.CratesTypeId == viewModel.CratesTypeId)
								.OrderByDescending(cm => cm.DispDate)
								.FirstOrDefaultAsync();

							if (existingRecord != null)
							{
								continue;
							}
							else
							{
								// Create new record
								var cratesManage = new CratesManage
								{
									CustomerId = customer.CustomerId,
									SegmentCode = segment.custsegementcode,
									CratesTypeId = viewModel.CratesTypeId,
									DispDate = viewModel.DispDate,
									Opening = customer.OpeningBalance,
									Outward = 0,
									Inward = 0,
									Balance = customer.OpeningBalance
								};
								_context.Add(cratesManage);
								recordsProcessed++;
							}
						}
					}

					if (recordsProcessed > 0)
					{
						await _context.SaveChangesAsync();
						_notifyService.Success($"{recordsProcessed} opening entries saved successfully.");
					}
					else
					{
						_notifyService.Warning("No opening entries were processed.");
					}

					return RedirectToAction(nameof(Index));
				}
				catch (Exception ex)
				{
					_notifyService.Error("Error saving opening entries: " + ex.Message);
					ViewBag.DivisionId = new SelectList(await _context.SegementMaster.ToListAsync(), "Segement_Code", "SegementName", viewModel.SegmentCode);
					ViewBag.CratesTypeId = new SelectList(await _context.CratesTypes.ToListAsync(), "Id", "Cratestype", viewModel.CratesTypeId);
					return View(viewModel);
				}
			}

			ViewBag.DivisionId = new SelectList(await _context.SegementMaster.ToListAsync(), "Segement_Code", "SegementName", viewModel.SegmentCode);
			ViewBag.CratesTypeId = new SelectList(await _context.CratesTypes.ToListAsync(), "Id", "Cratestype", viewModel.CratesTypeId);
			return View(viewModel);
		}

		// GET: CratesManages/GetCustomersByDivision
		[HttpGet]
		public async Task<IActionResult> GetCustomersByDivision(string division)
		{
			var customers = await _context.Customer_Master
				.Where(c => c.Division == division)
				.Select(c => new { id = c.Id, name = c.Name })
				.ToListAsync();

			return Json(customers);
		}

		// GET: CratesManages/UploadBulkOpeningEntry
		[HttpGet]
		public async Task<IActionResult> UploadBulkOpeningEntry()
		{
			var viewModel = new OpeningBalanceEntryViewModel();

			ViewBag.DivisionId = new SelectList(await _context.SegementMaster.ToListAsync(), "SegementName", "SegementName");
			// Initialize with empty list so that crate types are loaded dynamically based on segment selection
			ViewBag.CratesTypeId = new List<SelectListItem> { new SelectListItem { Value = "", Text = "-- Select Crates Type --" } };

			return View(viewModel);
		}

		// POST: CratesManages/UploadBulkOpeningEntry
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> UploadBulkOpeningEntry(OpeningBalanceEntryViewModel viewModel, IFormFile csvFile)
		{
			if (ModelState.IsValid)
			{
				try
				{
					// Validate CratesType
					var cratesType = await _context.CratesTypes.FirstOrDefaultAsync(ct => ct.Id == viewModel.CratesTypeId);
					if (cratesType == null)
					{
						_notifyService.Error("Invalid Crates Type selected.");
						ViewBag.DivisionId = new SelectList(await _context.SegementMaster.ToListAsync(), "SegementName", "SegementName", viewModel.SegmentCode);
						ViewBag.CratesTypeId = new SelectList(await _context.CratesTypes.ToListAsync(), "Id", "Cratestype", viewModel.CratesTypeId);
						return View(viewModel);
					}

					// Process uploaded file
					if (csvFile != null && csvFile.Length > 0)
					{
						var customers = new List<OpeningBalanceCustomerViewModel>();

						// Check file extension to determine parsing method
						var fileExtension = Path.GetExtension(csvFile.FileName).ToLowerInvariant();
						if (fileExtension == ".csv")
						{
							customers = await ParseCsvFile(csvFile, viewModel.SegmentCode);
						}
						else if (fileExtension == ".xlsx" || fileExtension == ".xls")
						{
							customers = await ParseExcelFile(csvFile, viewModel.SegmentCode);
						}
						else
						{
							_notifyService.Error("Invalid file format. Please upload a CSV or Excel file.");
							ViewBag.DivisionId = new SelectList(await _context.SegementMaster.ToListAsync(), "SegementName", "SegementName", viewModel.SegmentCode);
							ViewBag.CratesTypeId = new SelectList(await _context.CratesTypes.ToListAsync(), "Id", "Cratestype", viewModel.CratesTypeId);
							return View(viewModel);
						}

						viewModel.Customers = customers;
					}

					int recordsProcessed = 0;

					foreach (var customer in viewModel.Customers.Where(a => a.OpeningBalance > 0))
					{
						if (customer.OpeningBalance > 0)
						{
							var segment = await _context.CustomerSegementMap.FirstOrDefaultAsync(s => s.Customername == customer.CustomerName && s.SegementName == viewModel.SegmentCode);
							// Check if a record for this customer, segment, crates type, and date already exists
							var existingRecord = await _context.CratesManages
								.Where(cm => cm.CustomerId == customer.CustomerId &&
									 cm.SegmentCode == segment.custsegementcode &&
									 cm.CratesTypeId == viewModel.CratesTypeId)
								.OrderByDescending(cm => cm.DispDate)
								.FirstOrDefaultAsync();

							if (existingRecord != null)
							{
								continue;
							}
							else
							{
								// Create new record
								var cratesManage = new CratesManage
								{
									CustomerId = customer.CustomerId,
									SegmentCode = segment.custsegementcode,
									CratesTypeId = viewModel.CratesTypeId,
									DispDate = viewModel.DispDate,
									Opening = customer.OpeningBalance,
									Outward = 0,
									Inward = 0,
									Balance = customer.OpeningBalance
								};
								_context.Add(cratesManage);
								recordsProcessed++;
							}
						}
					}

					if (recordsProcessed > 0)
					{
						await _context.SaveChangesAsync();
						_notifyService.Success($"{recordsProcessed} opening entries saved successfully.");
					}
					else
					{
						_notifyService.Warning("No opening entries were processed.");
					}

					return RedirectToAction(nameof(Index));
				}
				catch (Exception ex)
				{
					_notifyService.Error("Error saving opening entries: " + ex.Message);
					ViewBag.DivisionId = new SelectList(await _context.SegementMaster.ToListAsync(), "SegementName", "SegementName", viewModel.SegmentCode);
					ViewBag.CratesTypeId = new SelectList(await _context.CratesTypes.ToListAsync(), "Id", "Cratestype", viewModel.CratesTypeId);
					return View(viewModel);
				}
			}

			ViewBag.DivisionId = new SelectList(await _context.SegementMaster.ToListAsync(), "SegementName", "SegementName", viewModel.SegmentCode);
			ViewBag.CratesTypeId = new SelectList(await _context.CratesTypes.ToListAsync(), "Id", "Cratestype", viewModel.CratesTypeId);
			return View(viewModel);
		}

		// Helper method to parse CSV file
		private async Task<List<OpeningBalanceCustomerViewModel>> ParseCsvFile(IFormFile file, string segmentCode)
		{
			var customers = new List<OpeningBalanceCustomerViewModel>();

			using (var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8))
			{
				string line;
				bool isFirstLine = true;

				while ((line = await reader.ReadLineAsync()) != null)
				{
					// Skip header line
					if (isFirstLine)
					{
						isFirstLine = false;
						continue;
					}

					var values = line.Split(',');
					if (values.Length >= 3)
					{
						// Assuming CSV format: CustomerId,CustomerName,OpeningBalance
						if (int.TryParse(values[0].Trim(), out int customerId) &&
							int.TryParse(values[2].Trim(), out int openingBalance))
						{
							// Validate that the customer belongs to the selected segment
							var customerSegment = await _context.CustomerSegementMap
								.FirstOrDefaultAsync(csm => csm.Customername == values[1].Trim() &&
														   csm.SegementName == segmentCode);

							if (customerSegment != null)
							{
								customers.Add(new OpeningBalanceCustomerViewModel
								{
									CustomerId = customerId,
									CustomerName = values[1].Trim(),
									OpeningBalance = openingBalance
								});
							}
						}
					}
				}
			}

			return customers;
		}

		// Helper method to parse Excel file
		private async Task<List<OpeningBalanceCustomerViewModel>> ParseExcelFile(IFormFile file, string segmentCode)
		{
			var customers = new List<OpeningBalanceCustomerViewModel>();

			// For now, we'll treat Excel files the same as CSV files since Excel can be saved as CSV
			// In a more advanced implementation, we could use a library like EPPlus or ClosedXML to parse actual Excel files
			return await ParseCsvFile(file, segmentCode);
		}

		// GET: CratesManages/BulkInwardEntry
		[HttpGet]
		public async Task<IActionResult> BulkInwardEntry()
		{
			var viewModel = new BulkInwardEntryViewModel();

			ViewBag.DivisionId = new SelectList(await _context.SegementMaster.ToListAsync(), "SegementName", "SegementName");
			ViewBag.CratesTypeId = new SelectList(await _context.CratesTypes.ToListAsync(), "Id", "Cratestype");

			return View(viewModel);
		}

		// POST: CratesManages/BulkInwardEntry
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> BulkInwardEntry(BulkInwardEntryViewModel viewModel)
		{
			if (ModelState.IsValid)
			{
				try
				{
					int recordsProcessed = 0;

					foreach (var customer in viewModel.Customers.Where(a => a.Inward > 0))
					{
						if (customer.Inward > 0)
						{
							var segment = await _context.CustomerSegementMap.FirstOrDefaultAsync(s => s.Customername == customer.CustomerName && s.SegementName == viewModel.SegmentCode);
							// Check if a record for this customer, segment, crates type, and date already exists
							var existingRecord = await _context.CratesManages
								.Where(cm => cm.CustomerId == customer.CustomerId &&
											 cm.SegmentCode == segment.custsegementcode &&
											 cm.DispDate == viewModel.DispDate &&
											 cm.CratesTypeId == viewModel.CratesTypeId)
								.OrderByDescending(cm => cm.DispDate)
								.FirstOrDefaultAsync();

							if (existingRecord != null)
							{
								// Update existing record
								existingRecord.Inward += customer.Inward;
								existingRecord.Balance = existingRecord.Opening + existingRecord.Outward - existingRecord.Inward;
								_context.CratesManages.Update(existingRecord);
								recordsProcessed++;
							}
							else
							{
								continue;
							}
						}
					}

					if (recordsProcessed > 0)
					{
						await _context.SaveChangesAsync();
						_notifyService.Success($"{recordsProcessed} inward entries saved successfully.");
					}
					else
					{
						_notifyService.Warning("No inward entries were processed.");
					}

					return RedirectToAction(nameof(Index));
				}
				catch (Exception ex)
				{
					_notifyService.Error("Error saving inward entries: " + ex.Message);
					return RedirectToAction(nameof(Index));
				}
			}

			ViewBag.DivisionId = new SelectList(await _context.SegementMaster.ToListAsync(), "SegementName", "SegementName");
			ViewBag.CratesTypeId = new SelectList(await _context.CratesTypes.ToListAsync(), "Id", "Cratestype");
			return View(viewModel);
		}

		// GET: CratesManages/UploadBulkInwardEntry
		[HttpGet]
		public async Task<IActionResult> UploadBulkInwardEntry()
		{
			var viewModel = new BulkInwardEntryViewModel();

			ViewBag.DivisionId = new SelectList(await _context.SegementMaster.ToListAsync(), "SegementName", "SegementName");
			// Initialize with empty list so that crate types are loaded dynamically based on segment selection
			ViewBag.CratesTypeId = new List<SelectListItem> { new SelectListItem { Value = "", Text = "-- Select Crates Type --" } };

			return View(viewModel);
		}

		// POST: CratesManages/UploadBulkInwardEntry
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> UploadBulkInwardEntry(BulkInwardEntryViewModel viewModel, IFormFile csvFile)
		{
			if (ModelState.IsValid)
			{
				try
				{
					// Validate CratesType
					var cratesType = await _context.CratesTypes.FirstOrDefaultAsync(ct => ct.Id == viewModel.CratesTypeId);
					if (cratesType == null)
					{
						_notifyService.Error("Invalid Crates Type selected.");
						ViewBag.DivisionId = new SelectList(await _context.SegementMaster.ToListAsync(), "SegementName", "SegementName", viewModel.SegmentCode);
						ViewBag.CratesTypeId = new SelectList(await _context.CratesTypes.ToListAsync(), "Id", "Cratestype", viewModel.CratesTypeId);
						return View(viewModel);
					}

					// Process uploaded file
					if (csvFile != null && csvFile.Length > 0)
					{
						var customers = new List<BulkInwardCustomerViewModel>();

						// Check file extension to determine parsing method
						var fileExtension = Path.GetExtension(csvFile.FileName).ToLowerInvariant();
						if (fileExtension == ".csv")
						{
							customers = await ParseInwardCsvFile(csvFile, viewModel.SegmentCode);
						}
						else if (fileExtension == ".xlsx" || fileExtension == ".xls")
						{
							customers = await ParseInwardExcelFile(csvFile, viewModel.SegmentCode);
						}
						else
						{
							_notifyService.Error("Invalid file format. Please upload a CSV or Excel file.");
							ViewBag.DivisionId = new SelectList(await _context.SegementMaster.ToListAsync(), "SegementName", "SegementName", viewModel.SegmentCode);
							ViewBag.CratesTypeId = new SelectList(await _context.CratesTypes.ToListAsync(), "Id", "Cratestype", viewModel.CratesTypeId);
							return View(viewModel);
						}

						viewModel.Customers = customers;
					}

					int recordsProcessed = 0;

					foreach (var customer in viewModel.Customers.Where(a => a.Inward > 0))
					{
						if (customer.Inward > 0)
						{
							var segment = await _context.CustomerSegementMap.FirstOrDefaultAsync(s => s.Customername == customer.CustomerName && s.SegementName == viewModel.SegmentCode);
							// Check if a record for this customer, segment, crates type, and date already exists
							var existingRecord = await _context.CratesManages
								.Where(cm => cm.CustomerId == customer.CustomerId &&
											 cm.SegmentCode == segment.custsegementcode &&
											  cm.DispDate == viewModel.DispDate &&
											 cm.CratesTypeId == viewModel.CratesTypeId)
								.OrderByDescending(cm => cm.DispDate)
								.FirstOrDefaultAsync();

							if (existingRecord != null)
							{
								// Update existing record
								existingRecord.Inward += customer.Inward;
								existingRecord.Balance = existingRecord.Opening + existingRecord.Outward - existingRecord.Inward;
								_context.CratesManages.Update(existingRecord);
								recordsProcessed++;
							}
							else
							{
								continue;
							}
						}
					}

					if (recordsProcessed > 0)
					{
						await _context.SaveChangesAsync();
						_notifyService.Success($"{recordsProcessed} inward entries saved successfully.");
					}
					else
					{
						_notifyService.Warning("No inward entries were processed.");
					}

					return RedirectToAction(nameof(Index));
				}
				catch (Exception ex)
				{
					_notifyService.Error("Error saving inward entries: " + ex.Message);
					ViewBag.DivisionId = new SelectList(await _context.SegementMaster.ToListAsync(), "SegementName", "SegementName", viewModel.SegmentCode);
					ViewBag.CratesTypeId = new SelectList(await _context.CratesTypes.ToListAsync(), "Id", "Cratestype", viewModel.CratesTypeId);
					return View(viewModel);
				}
			}

			ViewBag.DivisionId = new SelectList(await _context.SegementMaster.ToListAsync(), "SegementName", "SegementName", viewModel.SegmentCode);
			ViewBag.CratesTypeId = new SelectList(await _context.CratesTypes.ToListAsync(), "Id", "Cratestype", viewModel.CratesTypeId);
			return View(viewModel);
		}

		// Helper method to parse CSV file for inward entries
		private async Task<List<BulkInwardCustomerViewModel>> ParseInwardCsvFile(IFormFile file, string segmentCode)
		{
			var customers = new List<BulkInwardCustomerViewModel>();

			using (var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8))
			{
				string line;
				bool isFirstLine = true;

				while ((line = await reader.ReadLineAsync()) != null)
				{
					// Skip header line
					if (isFirstLine)
					{
						isFirstLine = false;
						continue;
					}

					var values = line.Split(',');
					if (values.Length >= 3)
					{
						// Assuming CSV format: CustomerId,CustomerName,InwardQuantity
						if (int.TryParse(values[0].Trim(), out int customerId) &&
							int.TryParse(values[2].Trim(), out int inwardQuantity))
						{
							// Validate that the customer belongs to the selected segment
							var customerSegment = await _context.CustomerSegementMap
								.FirstOrDefaultAsync(csm => csm.Customername == values[1].Trim() &&
														   csm.SegementName == segmentCode);

							if (customerSegment != null)
							{
								customers.Add(new BulkInwardCustomerViewModel
								{
									CustomerId = customerId,
									CustomerName = values[1].Trim(),
									Inward = inwardQuantity
								});
							}
						}
					}
				}
			}

			return customers;
		}

		// Helper method to parse Excel file for inward entries
		private async Task<List<BulkInwardCustomerViewModel>> ParseInwardExcelFile(IFormFile file, string segmentCode)
		{
			var customers = new List<BulkInwardCustomerViewModel>();

			// For now, we'll treat Excel files the same as CSV files since Excel can be saved as CSV
			// In a more advanced implementation, we could use a library like EPPlus or ClosedXML to parse actual Excel files
			return await ParseInwardCsvFile(file, segmentCode);
		}

		// GET: CratesManages/GenerateTemplate
		[HttpGet]
		public async Task<IActionResult> GenerateTemplate(string segmentCode)
		{
			if (string.IsNullOrEmpty(segmentCode))
			{
				return BadRequest("Segment code is required");
			}

			// Get customers for the selected segment
			var customers = await GetCustomersBySegmentCode(segmentCode);

			// Create CSV content
			var csvContent = new StringBuilder();
			csvContent.AppendLine("Customer ID,Customer Name,Opening Balance"); // Header row

			foreach (var customer in customers)
			{
				csvContent.AppendLine($"{customer.Id},{customer.Name},");
			}

			// Convert to byte array
			var bytes = Encoding.UTF8.GetBytes(csvContent.ToString());

			// Return as file download
			return File(bytes, "text/csv", $"CratesOpeningBalance_Template_{segmentCode}.csv");
		}

		// GET: CratesManages/GenerateInwardTemplate
		[HttpGet]
		public async Task<IActionResult> GenerateInwardTemplate(string segmentCode)
		{
			if (string.IsNullOrEmpty(segmentCode))
			{
				return BadRequest("Segment code is required");
			}

			// Get customers for the selected segment
			var customers = await GetCustomersBySegmentCode(segmentCode);

			// Create CSV content
			var csvContent = new StringBuilder();
			csvContent.AppendLine("Customer ID,Customer Name,Inward Quantity"); // Header row

			foreach (var customer in customers)
			{
				csvContent.AppendLine($"{customer.Id},{customer.Name},");
			}

			// Convert to byte array
			var bytes = Encoding.UTF8.GetBytes(csvContent.ToString());

			// Return as file download
			return File(bytes, "text/csv", $"CratesInward_Template_{segmentCode}.csv");
		}

		// Helper method to get customers by segment code
		private async Task<List<Customer_Master>> GetCustomersBySegmentCode(string segmentCode)
		{
			// Get customer segment mappings for this segment
			var customerMappings = await _context.CustomerSegementMap
				.Where(csm => csm.SegementName == segmentCode)
				.ToListAsync();

			// Get customer names from the mappings
			var customerNames = customerMappings.Select(csm => csm.Customername).ToList();

			// Get customers that match these names
			var customers = await _context.Customer_Master
				.Where(cm => customerNames.Contains(cm.Name) && cm.Division == segmentCode)
				.OrderBy(cm => cm.Name)
				.ToListAsync();

			return customers;
		}
	}
}