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
    public class CategoryMastersController : Controller
    {
        private readonly MilkDbContext _context;
        public INotyfService _notifyService { get; }

        public CategoryMastersController(MilkDbContext context, INotyfService notifyService)
        {
            _context = context;
            _notifyService = notifyService;
        }
        public async Task<IActionResult> Index()
        {
            return _context.CategoryMaster != null ?
                          View(await _context.CategoryMaster.ToListAsync()) :
                          Problem("Entity set 'MilkDbContext.CategoryMaster'  is null.");
        }
        public async Task<IActionResult> AddOrEdit(int id = 0)
        {
            if (id == 0)
                return View();
            else
            {
                if (id == null || _context.CategoryMaster == null)
                {
                    return NotFound();
                }

                var CategoryMaster = await _context.CategoryMaster.FindAsync(id);
                if (CategoryMaster == null)
                {
                    return NotFound();
                }
                return View(CategoryMaster);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOrEdit(int id, CategoryMaster CategoryMaster)
        {
            //insert
            if (id == 0)
            {
                if (ModelState.IsValid)
                {
                    _context.Add(CategoryMaster);
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
                    _context.Update(CategoryMaster);
                    await _context.SaveChangesAsync();
                    _notifyService.Success("Record Update sucessfully");
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    _notifyService.Error("Modal State Is InValid");
                }
            }

            return View(CategoryMaster);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (_context.CategoryMaster == null)
            {
                return Problem("Entity set 'MilkDbContext.CategoryMaster'  is null.");
            }
            var CategoryMaster = await _context.CategoryMaster.FindAsync(id);
            if (CategoryMaster != null)
            {
                _context.CategoryMaster.Remove(CategoryMaster);
            }

            await _context.SaveChangesAsync();
            _notifyService.Success("Record Delete sucessfully");
            return RedirectToAction(nameof(Index));
        }

        private bool CategoryMasterExists(int id)
        {
            return (_context.CategoryMaster?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
