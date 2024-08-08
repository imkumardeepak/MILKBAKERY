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
    public class SegementMastersController : Controller
    {
        private readonly MilkDbContext _context;
        public INotyfService _notifyService { get; }

        public SegementMastersController(MilkDbContext context, INotyfService notifyService)
        {
            _context = context;
            _notifyService = notifyService;
        }
        public async Task<IActionResult> Index()
        {
            return _context.SegementMaster != null ?
                          View(await _context.SegementMaster.ToListAsync()) :
                          Problem("Entity set 'MilkDbContext.SegementMaster'  is null.");
        }
        public async Task<IActionResult> AddOrEdit(int id = 0)
        {
            if (id == 0)
                return View();
            else
            {
                if (id == null || _context.SegementMaster == null)
                {
                    return NotFound();
                }

                var SegementMaster = await _context.SegementMaster.FindAsync(id);
                if (SegementMaster == null)
                {
                    return NotFound();
                }
                return View(SegementMaster);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOrEdit(int id, SegementMaster SegementMaster)
        {
            //insert
            if (id == 0)
            {
                if (ModelState.IsValid)
                {
                    var validate = _context.SegementMaster.Where(a => a.SegementName == SegementMaster.SegementName).FirstOrDefault();
                    if (validate != null)
                    {
                        _notifyService.Error("Already Added In Database");
                    }
                    else
                    {
                        _context.Add(SegementMaster);
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
                    _context.Update(SegementMaster);
                    await _context.SaveChangesAsync();
                    _notifyService.Success("Record Update sucessfully");
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    _notifyService.Error("Modal State Is InValid");
                }
            }

            return View(SegementMaster);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (_context.SegementMaster == null)
            {
                return Problem("Entity set 'MilkDbContext.SegementMaster'  is null.");
            }
            var SegementMaster = await _context.SegementMaster.FindAsync(id);
            if (SegementMaster != null)
            {
                _context.SegementMaster.Remove(SegementMaster);
            }

            await _context.SaveChangesAsync();
            _notifyService.Success("Record Delete sucessfully");
            return RedirectToAction(nameof(Index));
        }

        private bool SegementMasterExists(int id)
        {
            return (_context.SegementMaster?.Any(e => e.id == id)).GetValueOrDefault();
        }
    }
}
