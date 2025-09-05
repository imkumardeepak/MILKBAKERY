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
	public class Cust2CustMapController : Controller
	{
		private readonly MilkDbContext _context;
		public INotyfService _notifyService { get; }

		public Cust2CustMapController(MilkDbContext context, INotyfService notyfService)
		{
			_context = context;
			_notifyService = notyfService;
		}

		// GET: Cust2CustMap
		public async Task<IActionResult> Index()
		{
			return _context.Cust2CustMap != null ?
						View(await _context.Cust2CustMap.ToListAsync()) :
						Problem("Entity set 'MilkDbContext.Cust2CustMap'  is null.");
		}

		public async Task<IActionResult> AddOrEdit(int id = 0)
		{
			ViewBag.customer = GetCustomer();
			if (id == 0)
			{
				Cust2CustMap custMap = new Cust2CustMap();
				custMap.Mappedcusts.Add(new Mappedcust() { id = 1 });
				return View(custMap);
			}
			else
			{
				if (id == null || _context.Cust2CustMap == null)
				{
					return NotFound();
				}

				var Cust2CustMap = await _context.Cust2CustMap.Where(a => a.id == id).Include(d => d.Mappedcusts).FirstOrDefaultAsync();
				if (Cust2CustMap.Mappedcusts.Count == 0)
				{
					Cust2CustMap.Mappedcusts.Add(new Mappedcust() { id = 1 });
				}
				if (Cust2CustMap == null)
				{
					return NotFound();
				}
				return View(Cust2CustMap);
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> AddOrEdit(int id, Cust2CustMap Cust2CustMap)
		{
			Cust2CustMap.Mappedcusts.RemoveAll(a => a.phone == null || a.customer == null);

			//insert
			if (id == 0)
			{

				_context.Add(Cust2CustMap);
				await _context.SaveChangesAsync();
				_notifyService.Success("Record saved sucessfully");
				return RedirectToAction(nameof(Index));



			}
			else
			{
				//update
				List<Mappedcust> poDetails = _context.mappedcusts.Where(d => d.cust2custId == Cust2CustMap.id).ToList();
				_context.mappedcusts.RemoveRange(poDetails);
				_context.SaveChanges();
				_context.Update(Cust2CustMap);
				await _context.SaveChangesAsync();
				_notifyService.Success("Record Update sucessfully");
				return RedirectToAction(nameof(Index));

			}

			return View(Cust2CustMap);
		}


		// GET: Cust2CustMap/Delete/5
		public async Task<IActionResult> Delete(int? id)
		{
			if (_context.Cust2CustMap == null)
			{
				return Problem("Entity set 'MilkDbContext.Cust2CustMap'  is null.");
			}
			var cust2CustMap = await _context.Cust2CustMap.FindAsync(id);
			if (cust2CustMap != null)
			{
				_context.Cust2CustMap.Remove(cust2CustMap);
			}

			await _context.SaveChangesAsync();
			return RedirectToAction(nameof(Index));
		}

		// POST: Cust2CustMap/Delete/5


		private bool Cust2CustMapExists(int id)
		{
			return (_context.Cust2CustMap?.Any(e => e.id == id)).GetValueOrDefault();
		}

		public ActionResult fill_form(string selectedValue)
		{

			var wbridge = _context.Customer_Master.Where(n => n.Name == selectedValue).FirstOrDefault();

			return Json(wbridge.phoneno);


		}
		private List<SelectListItem> GetCustomer()
		{
			var lstProducts = new List<SelectListItem>();

			lstProducts = _context.Customer_Master.AsNoTracking().Select(n =>
			new SelectListItem
			{
				Value = n.Name,
				Text = n.Name
			}).ToList();

			var defItem = new SelectListItem()
			{
				Value = "",
				Text = "----Select Customer----"
			};

			lstProducts.Insert(0, defItem);

			return lstProducts;
		}
	}
}
