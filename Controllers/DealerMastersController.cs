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

			return View(dealers);
		}

		// GET: DealerMasters/AddOrEdit/5
		public async Task<IActionResult> AddOrEdit(int? id)
		{
			// Get customers for dropdown
			ViewBag.Customers = await _context.Customer_Master
				.Select(c => new { c.Id, c.Name })
				.ToListAsync();

			if (id == 0 || id == null)
			{
				// Creating new dealer
				return View(new DealerMaster());
			}
			else
			{
				// Editing existing dealer
				var dealerMaster = await _context.DealerMasters.FindAsync(id);
				if (dealerMaster == null)
				{
					return NotFound();
				}
				return View(dealerMaster);
			}
		}

		// POST: DealerMasters/AddOrEdit/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> AddOrEdit(int id, DealerMaster dealerMaster)
		{
			// Insert
			if (id == 0)
			{
				if (ModelState.IsValid)
				{
					_context.Add(dealerMaster);
					await _context.SaveChangesAsync();
					return RedirectToAction(nameof(Index));
				}
				else
				{
					// Get customers for dropdown in case of validation error
					ViewBag.Customers = await _context.Customer_Master
						.Select(c => new { c.Id, c.Name })
						.ToListAsync();
					return View(dealerMaster);
				}
			}
			else
			{
				// Update
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
				else
				{
					// Get customers for dropdown in case of validation error
					ViewBag.Customers = await _context.Customer_Master
						.Select(c => new { c.Id, c.Name })
						.ToListAsync();
					return View(dealerMaster);
				}
			}
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
				return RedirectToAction(nameof(Index));
			}

			// If validation fails, redirect back to the dealer list page
			return RedirectToAction(nameof(Index));
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
				return RedirectToAction(nameof(Index));
			}

			// If validation fails, redirect back to the dealer list page
			return RedirectToAction(nameof(Index));
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
				return RedirectToAction(nameof(Index));
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