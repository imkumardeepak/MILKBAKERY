using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Milk_Bakery.Data;
using Milk_Bakery.Models;
using System.Threading.Tasks;
using AspNetCoreHero.ToastNotification.Abstractions;
using System.Linq;

namespace Milk_Bakery.Controllers
{
    [Authentication]
    public class CratesTypesController : Controller
    {
        private readonly MilkDbContext _context;
        public INotyfService _notifyService { get; }

        public CratesTypesController(MilkDbContext context, INotyfService notyf)
        {
            _context = context;
            _notifyService = notyf;
        }

        // GET: CratesTypes
        public async Task<IActionResult> Index()
        {
            return View(await _context.CratesTypes.ToListAsync());
        }

        // GET: CratesTypes/AddOrEdit
        // GET: CratesTypes/AddOrEdit/5
        public async Task<IActionResult> AddOrEdit(int id = 0)
        {
            // Get distinct segments from SegementMaster
            ViewBag.Divisions = _context.SegementMaster
                .Where(s => !string.IsNullOrEmpty(s.SegementName))
                .Select(s => s.SegementName)
                .Distinct()
                .OrderBy(s => s)
                .ToList();

            if (id == 0)
                return View(new CratesType());
            else
            {
                var crate = await _context.CratesTypes.FindAsync(id);
                if (crate == null)
                {
                    return NotFound();
                }
                return View(crate);
            }
        }

        // POST: CratesTypes/AddOrEdit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOrEdit(int id, CratesType crate)
        {
            // Get distinct segments from SegementMaster
            ViewBag.Divisions = _context.SegementMaster
                .Where(s => !string.IsNullOrEmpty(s.SegementName))
                .Select(s => s.SegementName)
                .Distinct()
                .OrderBy(s => s)
                .ToList();

            if (ModelState.IsValid)
            {
                if (crate.Id == 0)
                {
                    var validate = _context.CratesTypes.Where(a => a.Cratestype == crate.Cratestype).FirstOrDefault();
                    if (validate != null)
                    {
                        ModelState.AddModelError("Name", "Crate type already exists.");
                        return View(crate);
                    }
                    _context.Add(crate);
                    await _context.SaveChangesAsync();
                    _notifyService.Success("Crate type created successfully");
                }
                else
                {
                    _context.Update(crate);
                    await _context.SaveChangesAsync();
                    _notifyService.Success("Crate type updated successfully");
                }
                return RedirectToAction(nameof(Index));
            }
            return View(crate);
        }

        // GET: CratesTypes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var crate = await _context.CratesTypes.FindAsync(id);
            if (crate == null)
            {
                return NotFound();
            }

            _context.CratesTypes.Remove(crate);
            await _context.SaveChangesAsync();
            _notifyService.Success("Crate type deleted successfully");

            return RedirectToAction(nameof(Index));
        }
    }
}