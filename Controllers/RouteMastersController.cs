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
    public class RouteMastersController : Controller
    {
        private readonly MilkDbContext _context;
        public INotyfService _notifyService { get; }

        public RouteMastersController(MilkDbContext context, INotyfService notifyService)
        {
            _context = context;
            _notifyService = notifyService;
        }
        public async Task<IActionResult> Index()
        {
            return _context.RouteMaster != null ?
                          View(await _context.RouteMaster.ToListAsync()) :
                          Problem("Entity set 'MilkDbContext.RouteMaster'  is null.");
        }
        public async Task<IActionResult> AddOrEdit(int id = 0)
        {
            if (id == 0)
                return View();
            else
            {
                if (id == null || _context.RouteMaster == null)
                {
                    return NotFound();
                }

                var RouteMaster = await _context.RouteMaster.FindAsync(id);
                if (RouteMaster == null)
                {
                    return NotFound();
                }
                return View(RouteMaster);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOrEdit(int id, RouteMaster RouteMaster)
        {
            //insert
            if (id == 0)
            {
                if (ModelState.IsValid)
                {
                    _context.Add(RouteMaster);
                    await _context.SaveChangesAsync();
                    _notifyService.Success("Record saved sucessfully");
                    return RedirectToAction(nameof(Index));
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
                    _context.Update(RouteMaster);
                    await _context.SaveChangesAsync();
                    _notifyService.Success("Record Update sucessfully");
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    _notifyService.Error("Modal State Is InValid");
                }
            }

            return View(RouteMaster);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (_context.RouteMaster == null)
            {
                return Problem("Entity set 'MilkDbContext.RouteMaster'  is null.");
            }
            var RouteMaster = await _context.RouteMaster.FindAsync(id);
            if (RouteMaster != null)
            {
                _context.RouteMaster.Remove(RouteMaster);
            }

            await _context.SaveChangesAsync();
            _notifyService.Success("Record Delete sucessfully");
            return RedirectToAction(nameof(Index));
        }

        private bool RouteMasterExists(int id)
        {
            return (_context.RouteMaster?.Any(e => e.Id == id)).GetValueOrDefault();
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
                _notifyService.Error("Please select a CSV file to upload.");
                return View();
            }
            if (!csvFile.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                _notifyService.Error("Only CSV files are allowed.");
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

                var records = new List<RouteMaster>();
                while (csv.Read())
                {
                    var person = csv.GetRecord<RouteMaster>();
                    records.Add(person);
                }
                _context.AddRange(records);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index"); // Redirect to a success page or another view
        }

    }
}
