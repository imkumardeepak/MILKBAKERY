using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Milk_Bakery.Data;
using Milk_Bakery.Models;
using Milk_Bakery.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Milk_Bakery.Controllers
{
    public class RoleMasterController : Controller
    {
        private readonly MilkDbContext _context;

        public RoleMasterController(MilkDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var roles = await _context.Roles.ToListAsync();
            return View(roles);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("RoleName")] Role role)
        {
            if (ModelState.IsValid)
            {
                _context.Add(role);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(role);
        }

        public async Task<IActionResult> Manage(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            var menuItems = await _context.MenuItems.ToListAsync();
            var rolePageAccesses = await _context.PageAccesses
                .Where(pa => pa.RoleId == id)
                .ToListAsync();

            var allMenuItems = menuItems.Select(mi => new MenuItemViewModel
            {
                MenuItemId = mi.Id,
                MenuItemName = mi.Name,
                HasAccess = rolePageAccesses.Any(pa => pa.PageName == mi.Name)
            }).ToList();

            var menuItemsDict = allMenuItems.ToDictionary(m => m.MenuItemId);
            var rootMenuItems = new List<MenuItemViewModel>();

            foreach (var item in allMenuItems)
            {
                var menuItem = menuItems.First(mi => mi.Id == item.MenuItemId);
                if (menuItem.ParentId.HasValue && menuItemsDict.ContainsKey(menuItem.ParentId.Value))
                {
                    var parent = menuItemsDict[menuItem.ParentId.Value];
                    if (parent.Children == null)
                    {
                        parent.Children = new List<MenuItemViewModel>();
                    }
                    parent.Children.Add(item);
                }
                else
                {
                    rootMenuItems.Add(item);
                }
            }

            var model = new RoleViewModel
            {
                RoleId = role.Id,
                RoleName = role.RoleName,
                MenuItems = rootMenuItems
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Manage(RoleViewModel model)
        {
            var role = await _context.Roles.FindAsync(model.RoleId);
            if (role == null)
            {
                return NotFound();
            }

            var existingAccesses = await _context.PageAccesses
                .Where(pa => pa.RoleId == model.RoleId)
                .ToListAsync();

            _context.PageAccesses.RemoveRange(existingAccesses);

            var newAccesses = new List<PageAccess>();
            AddPageAccesses(newAccesses, model, model.MenuItems);
            await _context.PageAccesses.AddRangeAsync(newAccesses);


            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private void AddPageAccesses(List<PageAccess> accesses, RoleViewModel model, List<MenuItemViewModel> menuItems)
        {
            if (menuItems == null) return;
            foreach (var item in menuItems)
            {
                if (item.HasAccess)
                {
                    if (!accesses.Any(a => a.PageName == item.MenuItemName && a.RoleId == model.RoleId))
                    {
                        accesses.Add(new PageAccess
                        {
                            RoleId = model.RoleId,
                            PageName = item.MenuItemName,
                            HasAccess = true
                        });
                    }
                }
                if (item.Children != null && item.Children.Any())
                {
                    AddPageAccesses(accesses, model, item.Children);
                }
            }
        }
    }
}