using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Milk_Bakery.Data;
using Milk_Bakery.Models;

namespace Milk_Bakery.Controllers
{
    public class DealerMastersController : Controller
    {
        private readonly MilkDbContext _context;

        public DealerMastersController(MilkDbContext context)
        {
            _context = context;
        }

        // GET: DealerMasters
        public async Task<IActionResult> Index()
        {
            var dealers = await _context.DealerMasters
                .Include(d => d.DealerBasicOrders)
                .ToListAsync();

            var dealerListItems = dealers.Select(d => new
            {
                Id = d.Id,
                DistributorId = d.DistributorId,
                Name = d.Name,
                RouteCode = d.RouteCode,
                City = d.City,
                PhoneNo = d.PhoneNo,
                Email = d.Email,
                OrderCount = d.DealerBasicOrders.Count
            }).ToList();

            return View(dealerListItems);
        }

        // GET: DealerMasters/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var dealerMaster = await _context.DealerMasters
                .Include(d => d.DealerBasicOrders)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (dealerMaster == null)
            {
                return NotFound();
            }

            // Get materials for dropdown
            ViewBag.Materials = await _context.MaterialMaster
                .Select(m => new { m.Id, m.Materialname, m.material3partycode, m.ShortName })
                .ToListAsync();

            return View(dealerMaster);
        }

        // GET: DealerMasters/Create
        public async Task<IActionResult> Create()
        {
            // Get customers for dropdown
            ViewBag.Customers = await _context.Customer_Master
                .Select(c => new { c.Id, c.Name })
                .ToListAsync();
            return View();
        }

        // POST: DealerMasters/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("DistributorId,Name,RouteCode,Address,City,PhoneNo,Email")] DealerMaster dealerMaster)
        {

            if (ModelState.IsValid)
            {
                _context.Add(dealerMaster);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            
            // Get customers for dropdown in case of validation error
            ViewBag.Customers = await _context.Customer_Master
                .Select(c => new { c.Id, c.Name })
                .ToListAsync();
            return View(dealerMaster);
        }

        // GET: DealerMasters/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var dealerMaster = await _context.DealerMasters
                .Include(d => d.DealerBasicOrders)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (dealerMaster == null)
            {
                return NotFound();
            }
            
            // Get customers for dropdown
            ViewBag.Customers = await _context.Customer_Master
                .Select(c => new { c.Id, c.Name })
                .ToListAsync();
            return View(dealerMaster);
        }

        // POST: DealerMasters/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,DistributorId,Name,RouteCode,Address,City,PhoneNo,Email")] DealerMaster dealerMaster)
        {
            if (id != dealerMaster.Id)
            {
                return NotFound();
            }

            // Custom email validation
            if (!string.IsNullOrEmpty(dealerMaster.Email) && !dealerMaster.IsValidEmail())
            {
                ModelState.AddModelError("Email", "Invalid Email Address");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(dealerMaster);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DealerMasterExists(dealerMaster.Id))
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
            
            // Get customers for dropdown in case of validation error
            ViewBag.Customers = await _context.Customer_Master
                .Select(c => new { c.Id, c.Name })
                .ToListAsync();
            return View(dealerMaster);
        }

        // GET: DealerMasters/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var dealerMaster = await _context.DealerMasters
                .Include(d => d.DealerBasicOrders)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (dealerMaster == null)
            {
                return NotFound();
            }

            return View(dealerMaster);
        }

        // POST: DealerMasters/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var dealerMaster = await _context.DealerMasters
                .Include(d => d.DealerBasicOrders)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (dealerMaster != null)
            {
                // Remove associated orders first
                _context.DealerBasicOrders.RemoveRange(dealerMaster.DealerBasicOrders);
                _context.DealerMasters.Remove(dealerMaster);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // POST: Add Dealer Basic Order
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddBasicOrder([Bind("DealerId,MaterialName,SapCode,ShortCode,Quantity,BasicAmount")] DealerBasicOrder dealerBasicOrder)
        {
            if (ModelState.IsValid)
            {
                _context.Add(dealerBasicOrder);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Details), new { id = dealerBasicOrder.DealerId });
            }

            // If validation fails, redirect back to the dealer details page
            return RedirectToAction(nameof(Details), new { id = dealerBasicOrder.DealerId });
        }

        // POST: Edit Dealer Basic Order
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBasicOrder([Bind("Id,DealerId,MaterialName,SapCode,ShortCode,Quantity,BasicAmount")] DealerBasicOrder dealerBasicOrder)
        {
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
                return RedirectToAction(nameof(Details), new { id = dealerBasicOrder.DealerId });
            }

            // If validation fails, redirect back to the dealer details page
            return RedirectToAction(nameof(Details), new { id = dealerBasicOrder.DealerId });
        }

        // POST: Delete Dealer Basic Order
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBasicOrder(int id)
        {
            var dealerBasicOrder = await _context.DealerBasicOrders.FindAsync(id);
            if (dealerBasicOrder != null)
            {
                int dealerId = dealerBasicOrder.DealerId;
                _context.DealerBasicOrders.Remove(dealerBasicOrder);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Details), new { id = dealerId });
            }

            // If order not found, redirect back to the dealer list
            return RedirectToAction(nameof(Index));
        }

        private bool DealerMasterExists(int id)
        {
            return _context.DealerMasters.Any(e => e.Id == id);
        }

        private bool DealerBasicOrderExists(int id)
        {
            return _context.DealerBasicOrders.Any(e => e.Id == id);
        }
    }
}