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
	public class EmpToCustMapsController : Controller
	{
		private readonly MilkDbContext _context;
		public INotyfService _notifyService { get; }

		public EmpToCustMapsController(MilkDbContext context, INotyfService notyfService)
		{
			_context = context;
			_notifyService = notyfService;
		}

		// GET: EmpToCustMap
		public async Task<IActionResult> Index()
		{
			return _context.EmpToCustMap != null ?
						View(await _context.EmpToCustMap.ToListAsync()) :
						Problem("Entity set 'MilkDbContext.EmpToCustMap'  is null.");
		}

		public async Task<IActionResult> AddOrEdit(int id = 0)
		{
			ViewBag.customer = GetCustomer();
			ViewBag.employee = getempl();
			if (id == 0)
			{
				EmpToCustMap custMap = new EmpToCustMap();
				custMap.Cust2EmpMaps.Add(new Cust2EmpMap() { id = 1 });
				return View(custMap);
			}
			else
			{
				if (id == null || _context.EmpToCustMap == null)
				{
					return NotFound();
				}

				var EmpToCustMap = await _context.EmpToCustMap.Where(a => a.id == id).Include(d => d.Cust2EmpMaps).FirstOrDefaultAsync();

				if (EmpToCustMap == null)
				{
					return NotFound();
				}
				return View(EmpToCustMap);
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> AddOrEdit(int id, EmpToCustMap EmpToCustMap)
		{
			EmpToCustMap.Cust2EmpMaps.RemoveAll(a => a.phone == null || a.customer == null || a.IsDeleted == true);

			//insert
			if (id == 0)
			{

				_context.Add(EmpToCustMap);
				await _context.SaveChangesAsync();
				_notifyService.Success("Record saved sucessfully");
				return RedirectToAction(nameof(Index));


			}
			else
			{
				//update

				List<Cust2EmpMap> poDetails = _context.cust2EmpMaps.Where(d => d.empt2custid == EmpToCustMap.id).ToList();
				_context.cust2EmpMaps.RemoveRange(poDetails);
				_context.SaveChanges();
				_context.Update(EmpToCustMap);
				await _context.SaveChangesAsync();
				_notifyService.Success("Record Update sucessfully");
				return RedirectToAction(nameof(Index));

			}

			return View(EmpToCustMap);
		}


		// GET: EmpToCustMap/Delete/5
		public async Task<IActionResult> Delete(int? id)
		{
			if (_context.EmpToCustMap == null)
			{
				return Problem("Entity set 'MilkDbContext.EmpToCustMap'  is null.");
			}
			var EmpToCustMap = await _context.EmpToCustMap.FindAsync(id);
			if (EmpToCustMap != null)
			{
				_context.EmpToCustMap.Remove(EmpToCustMap);
			}

			await _context.SaveChangesAsync();
			_notifyService.Success("Record Delete sucessfully");
			return RedirectToAction(nameof(Index));
		}

		// POST: EmpToCustMap/Delete/5


		private bool EmpToCustMapExists(int id)
		{
			return (_context.EmpToCustMap?.Any(e => e.id == id)).GetValueOrDefault();
		}

		public ActionResult fill_form(string selectedValue)
		{

			var wbridge = _context.EmployeeMaster.Where(n => n.FirstName == selectedValue).FirstOrDefault();

			return Json(wbridge.PhoneNumber);


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
		private List<SelectListItem> getempl()
		{
			var lstProducts = new List<SelectListItem>();

			lstProducts = _context.EmployeeMaster.AsNoTracking().Select(n =>
			new SelectListItem
			{
				Value = n.FirstName,
				Text = n.FirstName
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
