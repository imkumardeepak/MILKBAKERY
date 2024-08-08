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

namespace Milk_Bakery.Controllers
{
    public class PlantMastersController : Controller
    {
        private readonly MilkDbContext _context;
        public INotyfService _notifyService { get; }

        public PlantMastersController(MilkDbContext context, INotyfService notifyService)
        {
            _context = context;
            _notifyService = notifyService;
        }
        public async Task<IActionResult> Index()
        {
            return _context.PlantMaster != null ?
                          View(await _context.PlantMaster.ToListAsync()) :
                          Problem("Entity set 'MilkDbContext.PlantMaster'  is null.");
        }
        public async Task<IActionResult> AddOrEdit(int id = 0)
        {
            if (id == 0)
                return View();
            else
            {
                if (id == null || _context.PlantMaster == null)
                {
                    return NotFound();
                }

                var PlantMaster = await _context.PlantMaster.FindAsync(id);
                if (PlantMaster == null)
                {
                    return NotFound();
                }
                return View(PlantMaster);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOrEdit(int id, PlantMaster PlantMaster)
        {
            //insert
            if (id == 0)
            {
                if (ModelState.IsValid)
                {
                    _context.Add(PlantMaster);
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
                    _context.Update(PlantMaster);
                    await _context.SaveChangesAsync();
                    _notifyService.Success("Record Update sucessfully");
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    _notifyService.Error("Modal State Is InValid");
                }
            }

            return View(PlantMaster);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (_context.PlantMaster == null)
            {
                return Problem("Entity set 'MilkDbContext.PlantMaster'  is null.");
            }
            var PlantMaster = await _context.PlantMaster.FindAsync(id);
            if (PlantMaster != null)
            {
                _context.PlantMaster.Remove(PlantMaster);
            }

            await _context.SaveChangesAsync();
            _notifyService.Success("Record Delete sucessfully");
            return RedirectToAction(nameof(Index));
        }

        private bool PlantMasterExists(int id)
        {
            return (_context.PlantMaster?.Any(e => e.id == id)).GetValueOrDefault();
        }
    }
}
