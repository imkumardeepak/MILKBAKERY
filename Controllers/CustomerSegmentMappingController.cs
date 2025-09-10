using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Milk_Bakery.Data;
using Milk_Bakery.Models;

namespace Milk_Bakery.Controllers
{
    [Authentication]
    public class CustomerSegmentMappingController : Controller
    {
        private readonly MilkDbContext _context;

        public CustomerSegmentMappingController(MilkDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.customer = GetCustomer();
            ViewBag.segment = GetSegment();
            
            // Get all mappings initially
            var mappings = await _context.CustomerSegementMap.ToListAsync();
            return View(mappings);
        }

        [HttpPost]
        public IActionResult GetMappings(string customer, string segment)
        {
            var mappings = _context.CustomerSegementMap.AsQueryable();

            if (!string.IsNullOrEmpty(customer))
            {
                mappings = mappings.Where(m => m.Customername == customer);
            }

            if (!string.IsNullOrEmpty(segment))
            {
                mappings = mappings.Where(m => m.SegementName == segment);
            }

            var result = mappings.ToList();
            return PartialView("_MappingTable", result);
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
                Text = "All Customers"
            };

            lstProducts.Insert(0, defItem);

            return lstProducts;
        }

        private List<SelectListItem> GetSegment()
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
                Text = "All Segments"
            };

            lstProducts.Insert(0, defItem);

            return lstProducts;
        }
    }
}