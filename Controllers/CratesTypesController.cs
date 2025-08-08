using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Milk_Bakery.Data;
using Milk_Bakery.Models;
using System.Threading.Tasks;

public class CratesTypesController : Controller
{
	private readonly MilkDbContext _context;

	public CratesTypesController(MilkDbContext context)
	{
		_context = context;
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
	public async Task<IActionResult> AddOrEdit(int id,CratesType crate)
	{
		if (ModelState.IsValid)
		{
			if (crate.Id == 0)
			{
				var validate = _context.CratesTypes.Where(a => a.CratesCode == crate.CratesCode || a.Cratestype == crate.Cratestype).FirstOrDefault();
				if (validate != null)
				{
					ModelState.AddModelError("Name", "Crate type already exists.");
					return View(crate);
				}
				_context.Add(crate);
			}
			else
			{
				_context.Update(crate);
			}
			await _context.SaveChangesAsync();
			return RedirectToAction(nameof(Index));
		}
		return View(crate);
	}

	// GET: CratesTypes/Delete/5
	public async Task<IActionResult> Delete(int? id)
	{
		var crate = await _context.CratesTypes.FindAsync(id);
		if (crate != null)
		{
			_context.CratesTypes.Remove(crate);
			await _context.SaveChangesAsync();
		}
		return RedirectToAction(nameof(Index));
	}

	
}