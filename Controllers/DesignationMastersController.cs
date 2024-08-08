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
    public class DesignationMastersController : Controller
    {
        private readonly MilkDbContext _context;
        public INotyfService _notifyService { get; }

        public DesignationMastersController(MilkDbContext context, INotyfService notifyService)
        {
            _context = context;
            _notifyService = notifyService;
        }
        public async Task<IActionResult> Index()
        {
            return _context.DesignationMaster != null ?
                          View(await _context.DesignationMaster.ToListAsync()) :
                          Problem("Entity set 'MilkDbContext.DesignationMaster'  is null.");
        }
        public async Task<IActionResult> AddOrEdit(int id = 0)
        {
            if (id == 0)
                return View();
            else
            {
                if (id == null || _context.DesignationMaster == null)
                {
                    return NotFound();
                }

                var DesignationMaster = await _context.DesignationMaster.FindAsync(id);
                if (DesignationMaster == null)
                {
                    return NotFound();
                }
                return View(DesignationMaster);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOrEdit(int id, DesignationMaster DesignationMaster)
        {
            //insert
            if (id == 0)
            {
                if (ModelState.IsValid)
                {
                    _context.Add(DesignationMaster);
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
                    _context.Update(DesignationMaster);
                    await _context.SaveChangesAsync();
                    _notifyService.Success("Record Update sucessfully");
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    _notifyService.Error("Modal State Is InValid");
                }
            }

            return View(DesignationMaster);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (_context.DesignationMaster == null)
            {
                return Problem("Entity set 'MilkDbContext.DesignationMaster'  is null.");
            }
            var DesignationMaster = await _context.DesignationMaster.FindAsync(id);
            if (DesignationMaster != null)
            {
                _context.DesignationMaster.Remove(DesignationMaster);
            }

            await _context.SaveChangesAsync();
            _notifyService.Success("Record Delete sucessfully");
            return RedirectToAction(nameof(Index));
        }

        private bool DesignationMasterExists(int id)
        {
            return (_context.DesignationMaster?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
