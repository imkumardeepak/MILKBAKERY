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
    public class CompanyMastersController : Controller
    {
        private readonly MilkDbContext _context;
        public INotyfService _notifyService { get; }

        public CompanyMastersController(MilkDbContext context, INotyfService notifyService)
        {
            _context = context;
            _notifyService = notifyService;
        }
        public async Task<IActionResult> Index()
        {
            return _context.CompanyMaster != null ?
                          View(await _context.CompanyMaster.ToListAsync()) :
                          Problem("Entity set 'MilkDbContext.CompanyMaster'  is null.");
        }
        public async Task<IActionResult> AddOrEdit(int id = 0)
        {
            ViewBag.segement = GetSegement();
            if (id == 0)
            {
                CompanyMaster company=new CompanyMaster();
                company.Id = 0;
                return View(company);
            } 
            else
            {
                if (id == null || _context.CompanyMaster == null)
                {
                    return NotFound();
                }

                var CompanyMaster = await _context.CompanyMaster.FindAsync(id);
                if (CompanyMaster == null)
                {
                    return NotFound();
                }
                return View(CompanyMaster);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOrEdit(int id, CompanyMaster CompanyMaster)
        {
            //insert
            if (id == 0)
            {
                if (ModelState.IsValid)
                {
                    var validate = _context.CompanyMaster.Where(a => a.Name == CompanyMaster.Name || a.ShortName==CompanyMaster.ShortName).FirstOrDefault();
                    if (validate != null)
                    {
                        _notifyService.Error("Already Added In Database");
                    }
                    else
                    {
                        _context.Add(CompanyMaster);
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

                    _context.Update(CompanyMaster);
                    await _context.SaveChangesAsync();
                    _notifyService.Success("Record Update sucessfully");
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    _notifyService.Error("Modal State Is InValid");
                }
            }
            ViewBag.segement = GetSegement();
            return View(CompanyMaster);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (_context.CompanyMaster == null)
            {
                return Problem("Entity set 'MilkDbContext.CompanyMaster'  is null.");
            }
            var CompanyMaster = await _context.CompanyMaster.FindAsync(id);
            if (CompanyMaster != null)
            {
                _context.CompanyMaster.Remove(CompanyMaster);
            }

            await _context.SaveChangesAsync();
            _notifyService.Success("Record Delete sucessfully");
            return RedirectToAction(nameof(Index));
        }

        private bool CompanyMasterExists(int id)
        {
            return (_context.CompanyMaster?.Any(e => e.Id == id)).GetValueOrDefault();
        }

		[HttpGet]
		public IActionResult Upload()
		{
			return View();
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
				Text = "----Select Division----"
			};

			lstProducts.Insert(0, defItem);

			return lstProducts;
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

				var records = new List<CompanyMaster>();
				while (csv.Read())
				{
					var person = csv.GetRecord<CompanyMaster>();
					records.Add(person);
                }
				_context.AddRange(records);
				await _context.SaveChangesAsync();
			}

			return RedirectToAction("Index"); // Redirect to a success page or another view
		}
	}
}
