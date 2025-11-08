
using Microsoft.AspNetCore.Mvc;
using Milk_Bakery.Data;
using Milk_Bakery.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Milk_Bakery.ViewComponents
{
    public class MenuViewComponent : ViewComponent
    {
        private readonly MilkDbContext _context;

        public MenuViewComponent(MilkDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var role = HttpContext.Session.GetString("role");
            var menuItems = new List<MenuItem>();

            if (!string.IsNullOrEmpty(role))
            {
                var roleId = await _context.Roles
                                           .Where(r => r.RoleName == role)
                                           .Select(r => r.Id)
                                           .FirstOrDefaultAsync();

                if (roleId != 0)
                {
                    var accessiblePages = await _context.PageAccesses
                                                        .Where(pa => pa.RoleId == roleId && pa.HasAccess)
                                                        .Select(pa => pa.PageName)
                                                        .ToListAsync();

                    menuItems = await _context.MenuItems
                                              .Where(m => accessiblePages.Contains(m.Name))
                                              .ToListAsync();
                }
            }
            else
            {
                // For users who are not logged in or have no specific role
                menuItems = await _context.MenuItems.ToListAsync();
            }

            return View(menuItems);
        }
    }
}