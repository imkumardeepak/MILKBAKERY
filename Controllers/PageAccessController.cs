using Microsoft.AspNetCore.Mvc;
using Milk_Bakery.Data;
using Milk_Bakery.Models;
using System.Collections.Generic;
using System.Linq;

namespace Milk_Bakery.Controllers
{
    public class PageAccessController : Controller
    {
        private readonly MilkDbContext _context;

        public PageAccessController(MilkDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult GetAccessiblePages(string role)
        {
            var accessiblePages = _context.PageAccesses
                .Where(p => p.Role.RoleName == role && p.HasAccess)
                .Select(p => p.PageName)
                .ToList();

            return Json(accessiblePages);
        }
    }
}