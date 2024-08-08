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
    [Authentication]
    public class GradeMastersController : Controller
    {
        private readonly MilkDbContext _context;
        public INotyfService _notifyService { get; }

        public GradeMastersController(MilkDbContext context, INotyfService notifyService)
        {
            _context = context;
            _notifyService = notifyService;
        }
        public async Task<IActionResult> Index()
        {
            return _context.GradeMaster != null ?
                          View(await _context.GradeMaster.ToListAsync()) :
                          Problem("Entity set 'MilkDbContext.GradeMaster'  is null.");
        }
        public async Task<IActionResult> AddOrEdit(int id = 0)
        {
            if (id == 0)
                return View();
            else
            {
                if (id == null || _context.GradeMaster == null)
                {
                    return NotFound();
                }

                var GradeMaster = await _context.GradeMaster.FindAsync(id);
                if (GradeMaster == null)
                {
                    return NotFound();
                }
                return View(GradeMaster);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOrEdit(int id, GradeMaster GradeMaster)
        {
            //insert
            if (id == 0)
            {
                if (ModelState.IsValid)
                {
                    _context.Add(GradeMaster);
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
                    _context.Update(GradeMaster);
                    await _context.SaveChangesAsync();
                    _notifyService.Success("Record Update sucessfully");
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    _notifyService.Error("Modal State Is InValid");
                }
            }

            return View(GradeMaster);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (_context.GradeMaster == null)
            {
                return Problem("Entity set 'MilkDbContext.GradeMaster'  is null.");
            }
            var GradeMaster = await _context.GradeMaster.FindAsync(id);
            if (GradeMaster != null)
            {
                _context.GradeMaster.Remove(GradeMaster);
            }

            await _context.SaveChangesAsync();
            _notifyService.Success("Record Delete sucessfully");
            return RedirectToAction(nameof(Index));
        }

        private bool GradeMasterExists(int id)
        {
            return (_context.GradeMaster?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
