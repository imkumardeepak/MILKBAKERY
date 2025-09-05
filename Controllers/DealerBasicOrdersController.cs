using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Milk_Bakery.Data;
using Milk_Bakery.Models;

namespace Milk_Bakery.Controllers
{
    public class DealerBasicOrdersController : Controller
    {
        private readonly MilkDbContext _context;

        public DealerBasicOrdersController(MilkDbContext context)
        {
            _context = context;
        }

        // GET: DealerBasicOrders
        public async Task<IActionResult> Index()
        {
            var orders = await _context.DealerBasicOrders
                .Include(d => d.DealerMaster)
                .ToListAsync();

            var orderListItems = orders.Select(o => new
            {
                Id = o.Id,
                DealerId = o.DealerId,
                DealerName = o.DealerMaster?.Name ?? "Unknown",
                MaterialName = o.MaterialName,
                SapCode = o.SapCode,
                Quantity = o.Quantity,
                BasicAmount = o.BasicAmount,
                TotalAmount = o.Quantity * o.BasicAmount
            }).ToList();

            return View(orderListItems);
        }

        // GET: DealerBasicOrders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var dealerBasicOrder = await _context.DealerBasicOrders
                .Include(d => d.DealerMaster)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (dealerBasicOrder == null)
            {
                return NotFound();
            }

            return View(dealerBasicOrder);
        }

        // GET: DealerBasicOrders/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.Dealers = await _context.DealerMasters
                .Select(d => new { d.Id, d.Name })
                .ToListAsync();

            return View();
        }

        // POST: DealerBasicOrders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("DealerId,MaterialName,SapCode,ShortCode,Quantity,BasicAmount")] DealerBasicOrder dealerBasicOrder)
        {
            if (ModelState.IsValid)
            {
                _context.Add(dealerBasicOrder);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Dealers = await _context.DealerMasters
                .Select(d => new { d.Id, d.Name })
                .ToListAsync();

            return View(dealerBasicOrder);
        }

        // GET: DealerBasicOrders/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var dealerBasicOrder = await _context.DealerBasicOrders.FindAsync(id);
            if (dealerBasicOrder == null)
            {
                return NotFound();
            }

            ViewBag.Dealers = await _context.DealerMasters
                .Select(d => new { d.Id, d.Name })
                .ToListAsync();

            return View(dealerBasicOrder);
        }

        // POST: DealerBasicOrders/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,DealerId,MaterialName,SapCode,ShortCode,Quantity,BasicAmount")] DealerBasicOrder dealerBasicOrder)
        {
            if (id != dealerBasicOrder.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(dealerBasicOrder);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DealerBasicOrderExists(dealerBasicOrder.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Dealers = await _context.DealerMasters
                .Select(d => new { d.Id, d.Name })
                .ToListAsync();

            return View(dealerBasicOrder);
        }

        // GET: DealerBasicOrders/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var dealerBasicOrder = await _context.DealerBasicOrders
                .Include(d => d.DealerMaster)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (dealerBasicOrder == null)
            {
                return NotFound();
            }

            return View(dealerBasicOrder);
        }

        // POST: DealerBasicOrders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var dealerBasicOrder = await _context.DealerBasicOrders.FindAsync(id);
            if (dealerBasicOrder != null)
            {
                _context.DealerBasicOrders.Remove(dealerBasicOrder);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool DealerBasicOrderExists(int id)
        {
            return _context.DealerBasicOrders.Any(e => e.Id == id);
        }
    }
}