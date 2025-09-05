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
	public class Company_SegementMapController : Controller
	{
		private readonly MilkDbContext _context;
		public INotyfService _notifyService { get; }

		public Company_SegementMapController(MilkDbContext context, INotyfService notifyService)
		{
			_context = context;
			_notifyService = notifyService;
		}
		public async Task<IActionResult> Index()
		{
			return _context.Company_SegementMap != null ?
						  View(await _context.Company_SegementMap.ToListAsync()) :
						  Problem("Entity set 'MilkDbContext.Company_SegementMap'  is null.");
		}
		public async Task<IActionResult> AddOrEdit(int id = 0)
		{
			ViewBag.company = GetCompany();
			ViewBag.segement = GetSegement();
			if (id == 0)
				return View();
			else
			{
				if (id == null || _context.Company_SegementMap == null)
				{
					return NotFound();
				}

				var Company_SegementMap = await _context.Company_SegementMap.FindAsync(id);
				if (Company_SegementMap == null)
				{
					return NotFound();
				}
				return View(Company_SegementMap);
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> AddOrEdit(int id, Company_SegementMap Company_SegementMap)
		{
			//insert
			if (id == 0)
			{
				if (ModelState.IsValid)
				{
					var validate = _context.Company_SegementMap.Where(a => a.Companyname == Company_SegementMap.Companyname && a.Segementname == Company_SegementMap.Segementname).FirstOrDefault();
					if (validate != null)
					{
						_notifyService.Error("Already Added In Database");
					}
					else
					{
						_context.Add(Company_SegementMap);
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
					_context.Update(Company_SegementMap);
					await _context.SaveChangesAsync();
					_notifyService.Success("Record Update sucessfully");
					return RedirectToAction(nameof(Index));
				}
				else
				{
					_notifyService.Error("Modal State Is InValid");
				}
			}
			ViewBag.company = GetCompany();
			ViewBag.segement = GetSegement();
			return View(Company_SegementMap);
		}

		public async Task<IActionResult> Delete(int? id)
		{
			if (_context.Company_SegementMap == null)
			{
				return Problem("Entity set 'MilkDbContext.Company_SegementMap'  is null.");
			}
			var Company_SegementMap = await _context.Company_SegementMap.FindAsync(id);
			if (Company_SegementMap != null)
			{
				_context.Company_SegementMap.Remove(Company_SegementMap);
			}

			await _context.SaveChangesAsync();
			_notifyService.Success("Record Delete sucessfully");
			return RedirectToAction(nameof(Index));
		}
		private List<SelectListItem> GetCompany()
		{
			var lstProducts = new List<SelectListItem>();

			lstProducts = _context.CompanyMaster.AsNoTracking().Select(n =>
			new SelectListItem
			{
				Value = n.Name,
				Text = n.Name
			}).ToList();

			var defItem = new SelectListItem()
			{
				Value = "",
				Text = "----Select Company----"
			};

			lstProducts.Insert(0, defItem);

			return lstProducts;
		}

		private List<SelectListItem> GetSegement()
		{
			var lstProducts = new List<SelectListItem>();

			lstProducts = _context.SegementMaster.AsNoTracking().Select(n =>
			new SelectListItem
			{
				Value = n.SegementName,
				Text = n.SegementName
			}).ToList();

			var defItem = new SelectListItem()
			{
				Value = "",
				Text = "----Select Segement----"
			};

			lstProducts.Insert(0, defItem);

			return lstProducts;
		}

		private bool Company_SegementMapExists(int id)
		{
			return (_context.Company_SegementMap?.Any(e => e.Id == id)).GetValueOrDefault();
		}
		public IActionResult ActionName(string optionValue)
		{
			var category = _context.CompanyMaster.Where(a => a.Name == optionValue).FirstOrDefault();

			return Json(category.ShortName);
		}
		public IActionResult ActionName1(string optionValue)
		{
			var category = _context.SegementMaster.Where(a => a.SegementName == optionValue).FirstOrDefault();

			return Json(category.Segement_Code);
		}
	}
}
