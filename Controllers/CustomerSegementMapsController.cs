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
    public class CustomerSegementMapsController : Controller
    {
        private readonly MilkDbContext _context;
        public INotyfService _notifyService { get; }

        public CustomerSegementMapsController(MilkDbContext context, INotyfService notifyService)
        {
            _context = context;
            _notifyService = notifyService;
        }
        public async Task<IActionResult> Index()
        {
            return _context.CustomerSegementMap != null ?
                          View(await _context.CustomerSegementMap.ToListAsync()) :
                          Problem("Entity set 'MilkDbContext.CustomerSegementMap'  is null.");
        }
        public async Task<IActionResult> AddOrEdit(int id = 0)
        {
            ViewBag.customer = GetCustomer();
            ViewBag.segement = GetSegement();
            if (id == 0)
                return View();
            else
            {
                if (id == null || _context.CustomerSegementMap == null)
                {
                    return NotFound();
                }

                var CustomerSegementMap = await _context.CustomerSegementMap.FindAsync(id);
                if (CustomerSegementMap != null)
                {
                    var data = _context.Customer_Master.Where(a => a.Name == CustomerSegementMap.Customername).FirstOrDefault();
                    CustomerSegementMap.shortcode = data.shortname;
                }
                if (CustomerSegementMap == null)
                {
                    return NotFound();
                }
                return View(CustomerSegementMap);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOrEdit(int id, CustomerSegementMap CustomerSegementMap)
        {
            //insert
            if (id == 0)
            {
                if (ModelState.IsValid)
                {
                    var validate = _context.CustomerSegementMap.Where(a => a.Customername == CustomerSegementMap.Customername && a.SegementName==CustomerSegementMap.SegementName).FirstOrDefault();
                    if (validate != null)
                    {
                        _notifyService.Error("Already Added In Database");
                    }
                    else
                    {
                        _context.Add(CustomerSegementMap);
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
                    _context.Update(CustomerSegementMap);
                    await _context.SaveChangesAsync();
                    _notifyService.Success("Record Update sucessfully");
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    _notifyService.Error("Modal State Is InValid");
                }
            }
            ViewBag.customer = GetCustomer();
            ViewBag.segement = GetSegement();
            return View(CustomerSegementMap);
           
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (_context.CustomerSegementMap == null)
            {
                return Problem("Entity set 'MilkDbContext.CustomerSegementMap'  is null.");
            }
            var CustomerSegementMap = await _context.CustomerSegementMap.FindAsync(id);
            if (CustomerSegementMap != null)
            {
                _context.CustomerSegementMap.Remove(CustomerSegementMap);
            }

            await _context.SaveChangesAsync();
            _notifyService.Success("Record Delete sucessfully");
            return RedirectToAction(nameof(Index));
        }

        private bool CustomerSegementMapExists(int id)
        {
            return (_context.CustomerSegementMap?.Any(e => e.Id == id)).GetValueOrDefault();
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

        private List<SelectListItem> GetCustomer()
        {
            var lstProducts = new List<SelectListItem>();

            lstProducts = _context.Customer_Master.AsNoTracking().Select(n =>
            new SelectListItem
            {
                Value = n.Name,
                Text = n.shortname+" "+n.Name
            }).OrderBy(a => a.Value).ToList();

            var defItem = new SelectListItem()
            {
                Value = "",
                Text = "----Select Customer----"
            };

            lstProducts.Insert(0, defItem);

            return lstProducts;
        }
        public IActionResult ActionName(string optionValue)
        {
            var category = _context.Customer_Master.Where(a => a.Name == optionValue).FirstOrDefault();

            return Json(category.shortname);
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
                    records.Add(person);
                }
                _context.AddRange(records);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index"); // Redirect to a success page or another view
        }
    }
}
