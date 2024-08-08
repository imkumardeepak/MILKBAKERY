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
    public class DepartmentMastersController : Controller
    {
        private readonly MilkDbContext _context;
        public INotyfService _notifyService { get; }

        public DepartmentMastersController(MilkDbContext context, INotyfService notifyService)
        {
            _context = context;
            _notifyService = notifyService;
        }
        public async Task<IActionResult> Index()
        {
            return _context.DepartmentMaster != null ?
                          View(await _context.DepartmentMaster.ToListAsync()) :
                          Problem("Entity set 'MilkDbContext.DepartmentMaster'  is null.");
        }
        public async Task<IActionResult> AddOrEdit(int id = 0)
        {
            if (id == 0)
                return View();
            else
            {
                if (id == null || _context.DepartmentMaster == null)
                {
                    return NotFound();
                }

                var DepartmentMaster = await _context.DepartmentMaster.FindAsync(id);
                if (DepartmentMaster == null)
                {
                    return NotFound();
                }
                return View(DepartmentMaster);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOrEdit(int id, DepartmentMaster DepartmentMaster)
        {
            //insert
            if (id == 0)
            {
                if (ModelState.IsValid)
                {
                    _context.Add(DepartmentMaster);
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
                    _context.Update(DepartmentMaster);
                    await _context.SaveChangesAsync();
                    _notifyService.Success("Record Update sucessfully");
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    _notifyService.Error("Modal State Is InValid");
                }
            }

            return View(DepartmentMaster);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (_context.DepartmentMaster == null)
            {
                return Problem("Entity set 'MilkDbContext.DepartmentMaster'  is null.");
            }
            var DepartmentMaster = await _context.DepartmentMaster.FindAsync(id);
            if (DepartmentMaster != null)
            {
                _context.DepartmentMaster.Remove(DepartmentMaster);
            }

            await _context.SaveChangesAsync();
            _notifyService.Success("Record Delete sucessfully");
            return RedirectToAction(nameof(Index));
        }

        private bool DepartmentMasterExists(int id)
        {
            return (_context.DepartmentMaster?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
