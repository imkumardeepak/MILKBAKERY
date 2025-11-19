using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AspNetCoreHero.ToastNotification.Abstractions;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Milk_Bakery.Data;
using Milk_Bakery.Models;

namespace Milk_Bakery.Controllers
{
	[Authentication]
	public class MaterialMastersController : Controller
	{
		private readonly MilkDbContext _context;
		public INotyfService _notifyService { get; }

		public MaterialMastersController(MilkDbContext context, INotyfService notifyService)
		{
			_context = context;
			_notifyService = notifyService;
		}
		public async Task<IActionResult> Index()
		{
			return _context.MaterialMaster != null ?
						  View(await _context.MaterialMaster.OrderBy(a => a.Id).ToListAsync()) :
						  Problem("Entity set 'MilkDbContext.MaterialMaster'  is null.");
		}
		public async Task<IActionResult> AddOrEdit(int id = 0)
		{
			ViewBag.unit = Getunit();
			ViewBag.crates = GetCratesType();
			ViewBag.subcatgory = Getsubcategory();
			ViewBag.Category = GetCategory(); // Add this line to ensure Category dropdown is populated
			ViewBag.gram = Getunit();
			ViewBag.segement = GetSegement();
			if (id == 0)
			{
				MaterialMaster company = new MaterialMaster();
				company.Id = 0;
				return View(company);
			}
			else
			{
				if (id == null || _context.MaterialMaster == null)
				{
					return NotFound();
				}

				var MaterialMaster = await _context.MaterialMaster.FindAsync(id);
				if (MaterialMaster == null)
				{
					return NotFound();
				}
				return View(MaterialMaster);
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> AddOrEdit(int id, MaterialMaster MaterialMaster)
		{
			//insert
			if (id == 0)
			{
				if (ModelState.IsValid)
				{
					var validate = _context.MaterialMaster.Where(a => a.Materialname == MaterialMaster.Materialname).FirstOrDefault();
					if (validate != null)
					{
						_notifyService.Error("Already Added In Database");
					}
					else
					{
						_context.Add(MaterialMaster);
						await _context.SaveChangesAsync();
						_notifyService.Success("Record saved sucessfully");
						return RedirectToAction(nameof(Index));
					}

				}
				else
				{
					_notifyService.Error("Modal State Is InValid");
				}
			}
			else
			{
				//update
				if (ModelState.IsValid)
				{
					_context.Update(MaterialMaster);
					await _context.SaveChangesAsync();
					_notifyService.Success("Record Update sucessfully");
					return RedirectToAction(nameof(Index));
				}
				else
				{
					_notifyService.Error("Modal State Is InValid");
				}
			}
			ViewBag.unit = Getunit();
			ViewBag.Category = GetCategory();
			ViewBag.subcatgory = Getsubcategory();
			ViewBag.crates = GetCratesType();
			ViewBag.gram = Getunit();
			ViewBag.segement = GetSegement();
			return View(MaterialMaster);
		}

		public async Task<IActionResult> Delete(int? id)
		{
			if (_context.MaterialMaster == null)
			{
				return Problem("Entity set 'MilkDbContext.MaterialMaster'  is null.");
			}
			var MaterialMaster = await _context.MaterialMaster.FindAsync(id);
			if (MaterialMaster != null)
			{
				_context.MaterialMaster.Remove(MaterialMaster);
			}

			await _context.SaveChangesAsync();
			_notifyService.Success("Record Delete sucessfully");
			return RedirectToAction(nameof(Index));
		}

		private bool MaterialMasterExists(int id)
		{
			return (_context.MaterialMaster?.Any(e => e.Id == id)).GetValueOrDefault();
		}
		private List<SelectListItem> Getunit()
		{
			var lstProducts = new List<SelectListItem>();

			lstProducts = _context.UnitMaster.AsNoTracking().Select(n =>
			new SelectListItem
			{
				Value = n.UnitName,
				Text = n.UnitName
			}).ToList();

			var defItem = new SelectListItem()
			{
				Value = "",
				Text = "----Select Unit----"
			};

			lstProducts.Insert(0, defItem);

			return lstProducts;
		}
		private List<SelectListItem> Getsubcategory()
		{
			var lstProducts = new List<SelectListItem>();

			lstProducts = _context.Sub_CategoryMaster.AsNoTracking().Select(n =>
			new SelectListItem
			{
				Value = n.SubCategoryName,
				Text = n.SubCategoryName
			}).ToList();

			var defItem = new SelectListItem()
			{
				Value = "",
				Text = "----Select SubCategory----"
			};

			lstProducts.Insert(0, defItem);

			return lstProducts;
		}

		[HttpPost]
		public IActionResult GetSubCategoriesByCategory(string category)
		{
			var subCategories = _context.Sub_CategoryMaster
				.Where(s => s.CategoryName == category)
				.Select(n => new SelectListItem
				{
					Value = n.SubCategoryName,
					Text = n.SubCategoryName
				})
				.ToList();

			return Json(subCategories);
		}

		private List<SelectListItem> GetCategory()
		{
			var lstProducts = new List<SelectListItem>();

			lstProducts = _context.CategoryMaster.AsNoTracking().Select(n =>
			new SelectListItem
			{
				Value = n.CategoryName,
				Text = n.CategoryName
			}).ToList();

			var defItem = new SelectListItem()
			{
				Value = "",
				Text = "----Select Category----"
			};

			lstProducts.Insert(0, defItem);

			return lstProducts;
		}
		private List<SelectListItem> Getgram()
		{
			var lstProducts = new List<SelectListItem>();

			lstProducts = _context.UnitMaster.AsNoTracking().Select(n =>
			new SelectListItem
			{
				Value = n.UnitName,
				Text = n.UnitName
			}).ToList();

			var defItem = new SelectListItem()
			{
				Value = "",
				Text = "----Select Unit----"
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
				Value = n.Cratestype,
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
		private List<SelectListItem> GetSegement()
		{
			var lstProducts = new List<SelectListItem>();

			lstProducts = _context.SegementMaster.AsNoTracking().Select(n =>
			new SelectListItem
			{
				Value = n.SegementName,
				Text = n.SegementName
			}).ToList();

			var defItem = new SelectListItem()
			{
				Value = "",
				Text = "----Select Segement----"
			};

			lstProducts.Insert(0, defItem);

			return lstProducts;
		}
		[HttpPost]
		public IActionResult ActionName(string optionValue)
		{
			var category = _context.Sub_CategoryMaster.Where(a => a.SubCategoryName.Equals(optionValue)).FirstOrDefault();

			string resultData = category.CategoryName; // Replace with your actual data

			return Json(new { data = resultData }); // Return the data to bind to the textbox
		}

		[HttpGet]
		public IActionResult Upload()
		{
			return View();
		}

		[HttpPost]
		public async Task<IActionResult> Upload(IFormFile csvFile)
		{
			if (csvFile == null || csvFile.Length <= 0)
			{
				ModelState.AddModelError("csvFile", "Please select a CSV file to upload.");
				return View();
			}
			if (!csvFile.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
			{
				ModelState.AddModelError("csvFile", "Only CSV files are allowed.");
				return View();
			}
			var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
			{
				HasHeaderRecord = true, // Set this to 'true' if your CSV file has a header row, 'false' if not.
				MissingFieldFound = null
			};
			using (var reader = new StreamReader(csvFile.OpenReadStream()))
			using (var csv = new CsvHelper.CsvReader(reader, csvConfig))
			{
				csv.Read();
				csv.ReadHeader();

				var records = new List<MaterialMaster>();
				while (csv.Read())
				{
					var person = csv.GetRecord<MaterialMaster>();
					var validate = _context.MaterialMaster.Where(a => a.Materialname == person.Materialname).FirstOrDefault();
					if (validate == null)
					{
						records.Add(person);
					}

				}
				_context.AddRange(records);
				await _context.SaveChangesAsync();
			}

			return RedirectToAction("Index"); // Redirect to a success page or another view
		}
	}
}
