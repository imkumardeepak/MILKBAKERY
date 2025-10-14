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
using OfficeOpenXml;

namespace Milk_Bakery.Controllers
{
	[Authentication]
	public class CratesOutwardManagesController : Controller
	{
		private readonly MilkDbContext _context;
		public INotyfService _notifyService { get; }

		public CratesOutwardManagesController(MilkDbContext context, INotyfService notyf)
		{
			_context = context;
			_notifyService = notyf;
		}

		// GET: CratesOutwardManages
		public IActionResult Index()
		{
			return View();
		}

		// GET: CratesOutwardManages/BulkOutwardEntry
		[HttpGet]
		public async Task<IActionResult> BulkOutwardEntry()
		{
			var viewModel = new BulkOutwardEntryViewModel();

			ViewBag.DivisionId = new SelectList(await _context.SegementMaster.ToListAsync(), "SegementName", "SegementName");
			ViewBag.CratesTypeId = new SelectList(await _context.CratesTypes.ToListAsync(), "Id", "Cratestype");

			return View(viewModel);
		}

		// POST: CratesOutwardManages/BulkOutwardEntry
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> BulkOutwardEntry(BulkOutwardEntryViewModel viewModel)
		{
			if (ModelState.IsValid)
			{
				try
				{
					int recordsProcessed = 0;

					foreach (var customer in viewModel.Customers.Where(a => a.Outward > 0))
					{
						if (customer.Outward > 0)
						{
							// Get the segment mapping for this customer and segment
							var segment = await _context.CustomerSegementMap
								.FirstOrDefaultAsync(s => s.Customername == customer.CustomerName &&
														s.SegementName == viewModel.SegmentCode);

							// Skip if no valid segment mapping found
							if (segment == null)
							{
								continue;
							}

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
								existingRecord.Outward += customer.Outward;
								existingRecord.Balance = existingRecord.Opening + existingRecord.Outward - existingRecord.Inward;
								_context.CratesManages.Update(existingRecord);
								recordsProcessed++;
							}
							else
							{
								// Check if a record for this customer, segment, and date already exists
								var TOPexistingRecord = await _context.CratesManages
								.Where(cm => cm.CustomerId == customer.CustomerId &&
											 cm.SegmentCode == segment.custsegementcode &&
											 cm.CratesTypeId == viewModel.CratesTypeId)
								.OrderByDescending(cm => cm.DispDate)
								.FirstOrDefaultAsync();

								if (TOPexistingRecord != null)
								{

									var cratesManage = new CratesManage
									{
										CustomerId = customer.CustomerId,
										SegmentCode = segment.custsegementcode,
										CratesTypeId = viewModel.CratesTypeId,
										DispDate = viewModel.DispDate,
										Opening = TOPexistingRecord.Balance, // Default opening balance for outward entries
										Outward = customer.Outward,
										Inward = 0, // Default inward for outward entries
										Balance = TOPexistingRecord.Balance + customer.Outward
									};
									_context.Add(cratesManage);
									recordsProcessed++;
								}
								else
								{
									// Create new record when no existing record found
									var cratesManage = new CratesManage
									{
										CustomerId = customer.CustomerId,
										SegmentCode = segment.custsegementcode,
										CratesTypeId = viewModel.CratesTypeId,
										DispDate = viewModel.DispDate,
										Opening = 0, // Default opening balance for outward entries
										Outward = customer.Outward,
										Inward = 0, // Default inward for outward entries
										Balance = customer.Outward // Balance calculation (Opening + Outward - Inward)
									};
									_context.Add(cratesManage);
									recordsProcessed++;
								}
							}
						}
					}

					if (recordsProcessed > 0)
					{
						await _context.SaveChangesAsync();
						_notifyService.Success($"{recordsProcessed} outward entries saved successfully.");
					}
					else
					{
						_notifyService.Warning("No outward entries were processed.");
					}

					return RedirectToAction("Index", "CratesManages"); // Redirect to CratesManages Index
				}
				catch (Exception ex)
				{
					_notifyService.Error("Error saving outward entries: " + ex.Message);
					return RedirectToAction("Index", "CratesManages");
				}
			}

			ViewBag.DivisionId = new SelectList(await _context.SegementMaster.ToListAsync(), "SegementName", "SegementName");
			ViewBag.CratesTypeId = new SelectList(await _context.CratesTypes.ToListAsync(), "Id", "Cratestype");
			return View(viewModel);
		}

		// GET: CratesOutwardManages/UploadBulkOutwardEntry
		[HttpGet]
		public async Task<IActionResult> UploadBulkOutwardEntry()
		{
			var viewModel = new BulkOutwardEntryViewModel();

			ViewBag.DivisionId = new SelectList(await _context.SegementMaster.ToListAsync(), "SegementName", "SegementName");
			// Initialize with empty list so that crate types are loaded dynamically based on segment selection
			ViewBag.CratesTypeId = new List<SelectListItem> { new SelectListItem { Value = "", Text = "-- Select Crates Type --" } };

			return View(viewModel);
		}

		// POST: CratesOutwardManages/UploadBulkOutwardEntry
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> UploadBulkOutwardEntry(BulkOutwardEntryViewModel viewModel, IFormFile csvFile)
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
						var customers = new List<BulkOutwardCustomerViewModel>();

						// Check file extension to determine parsing method
						var fileExtension = Path.GetExtension(csvFile.FileName).ToLowerInvariant();
						if (fileExtension == ".csv")
						{
							customers = await ParseOutwardCsvFile(csvFile, viewModel.SegmentCode);
						}
						else if (fileExtension == ".xlsx" || fileExtension == ".xls")
						{
							customers = await ParseOutwardExcelFile(csvFile, viewModel.SegmentCode);
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

					foreach (var customer in viewModel.Customers.Where(a => a.Outward > 0))
					{
						if (customer.Outward > 0)
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
								existingRecord.Outward += customer.Outward; // Update Outward
								existingRecord.Balance = existingRecord.Opening + existingRecord.Outward - existingRecord.Inward;
								_context.CratesManages.Update(existingRecord);
								recordsProcessed++;
							}
							else
							{
								var TOPexistingRecord = await _context.CratesManages
								.Where(cm => cm.CustomerId == customer.CustomerId &&
											 cm.SegmentCode == segment.custsegementcode &&
											 cm.CratesTypeId == viewModel.CratesTypeId)
								.OrderByDescending(cm => cm.DispDate)
								.FirstOrDefaultAsync();

								if (TOPexistingRecord != null)
								{

									var cratesManage = new CratesManage
									{
										CustomerId = customer.CustomerId,
										SegmentCode = segment.custsegementcode,
										CratesTypeId = viewModel.CratesTypeId,
										DispDate = viewModel.DispDate,
										Opening = TOPexistingRecord.Balance, // Default opening balance for outward entries
										Outward = customer.Outward,
										Inward = 0, // Default inward for outward entries
										Balance = TOPexistingRecord.Balance + customer.Outward
									};
									_context.Add(cratesManage);
									recordsProcessed++;
								}
								else
								{
									// Create new record when no existing record found
									var cratesManage = new CratesManage
									{
										CustomerId = customer.CustomerId,
										SegmentCode = segment.custsegementcode,
										CratesTypeId = viewModel.CratesTypeId,
										DispDate = viewModel.DispDate,
										Opening = 0, // Default opening balance for outward entries
										Outward = customer.Outward,
										Inward = 0, // Default inward for outward entries
										Balance = customer.Outward // Balance calculation (Opening + Outward - Inward)
									};
									_context.Add(cratesManage);
									recordsProcessed++;
								}
							}
						}
					}

					if (recordsProcessed > 0)
					{
						await _context.SaveChangesAsync();
						_notifyService.Success($"{recordsProcessed} outward entries saved successfully.");
					}
					else
					{
						_notifyService.Warning("No outward entries were processed.");
					}

					return RedirectToAction("Index", "CratesManages"); // Redirect to CratesManages Index
				}
				catch (Exception ex)
				{
					_notifyService.Error("Error saving outward entries: " + ex.Message);
					ViewBag.DivisionId = new SelectList(await _context.SegementMaster.ToListAsync(), "SegementName", "SegementName", viewModel.SegmentCode);
					ViewBag.CratesTypeId = new SelectList(await _context.CratesTypes.ToListAsync(), "Id", "Cratestype", viewModel.CratesTypeId);
					return View(viewModel);
				}
			}

			ViewBag.DivisionId = new SelectList(await _context.SegementMaster.ToListAsync(), "SegementName", "SegementName", viewModel.SegmentCode);
			ViewBag.CratesTypeId = new SelectList(await _context.CratesTypes.ToListAsync(), "Id", "Cratestype", viewModel.CratesTypeId);
			return View(viewModel);
		}

		// GET: CratesOutwardManages/GenerateOutwardTemplate
		[HttpGet]
		public async Task<IActionResult> GenerateOutwardTemplate(string segmentCode)
		{
			if (string.IsNullOrEmpty(segmentCode))
			{
				return BadRequest("Segment code is required");
			}

			// Get customers for the selected segment
			var customers = await GetCustomersBySegmentCode(segmentCode);

			// Create CSV content
			var csvContent = new StringBuilder();
			csvContent.AppendLine("Customer ID,Customer Name,Short Name,City,Route,Outward Quantity"); // Header row

			foreach (var customer in customers)
			{
				csvContent.AppendLine($"{customer.Id},{customer.Name},{customer.shortname},{customer.city},{customer.route},");
			}

			// Convert to byte array
			var bytes = Encoding.UTF8.GetBytes(csvContent.ToString());

			// Return as file download
			return File(bytes, "text/csv", $"CratesOutward_Template_{segmentCode}.csv");
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

		// Helper method to parse CSV file for outward entries
		private async Task<List<BulkOutwardCustomerViewModel>> ParseOutwardCsvFile(IFormFile file, string segmentCode)
		{
			var customers = new List<BulkOutwardCustomerViewModel>();

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
					if (values.Length >= 6 && !string.IsNullOrEmpty(values[0].Trim()) &&
						!string.IsNullOrEmpty(values[5].Trim()))
					{
						// Assuming CSV format: CustomerId,CustomerName,City,Route,OutwardQuantity
						if (int.TryParse(values[0].Trim(), out int customerId) &&
							int.TryParse(values[5].Trim(), out int outwardQuantity))
						{
							//find customer by id
							var customer = await _context.Customer_Master.FindAsync(customerId);

							if (customer == null)
							{
								continue; // Skip if customer not found
							}

							// Validate that the customer belongs to the selected segment
							var customerSegment = await _context.CustomerSegementMap
								.FirstOrDefaultAsync(csm => csm.Customername == customer.Name &&
														   csm.SegementName == segmentCode);

							if (customerSegment != null)
							{
								customers.Add(new BulkOutwardCustomerViewModel
								{
									CustomerId = customerId,
									CustomerName = customer.Name.Trim(),
									Outward = outwardQuantity
								});
							}
						}
					}
				}
			}

			return customers;
		}

		// Helper method to parse Excel file for outward entries
		private async Task<List<BulkOutwardCustomerViewModel>> ParseOutwardExcelFile(IFormFile file, string segmentCode)
		{
			var customers = new List<BulkOutwardCustomerViewModel>();

			// For now, we'll treat Excel files the same as CSV files since Excel can be saved as CSV
			// In a more advanced implementation, we could use a library like EPPlus or ClosedXML to parse actual Excel files
			return await ParseOutwardCsvFile(file, segmentCode);
		}

		// GET: CratesOutwardManages/GetCratesTypesForSegment
		[HttpGet]
		public IActionResult GetCratesTypesForSegment(string segment)
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
				})
				.OrderBy(ct => ct.Text)
				.ToList();

			// Add default option
			crateTypes.Insert(0, new SelectListItem
			{
				Value = "",
				Text = "----Select Crates Type----"
			});

			return Json(crateTypes);
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
	}
}