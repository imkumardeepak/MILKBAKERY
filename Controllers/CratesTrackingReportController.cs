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
    public class CratesTrackingReportController : Controller
    {
        private readonly MilkDbContext _context;

        public CratesTrackingReportController(MilkDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            ViewBag.customer = GetCustomer();
            ViewBag.division = GetDivision();
            return View();
        }

        [HttpPost]
        public IActionResult GetReportData(int? customerId, DateTime? fromDate, DateTime? toDate, string division)
        {
            var query = _context.CratesManages.Include(c => c.Customer).Include(c => c.CratesType).AsQueryable();

            // Apply customer filter
            if (customerId.HasValue && customerId.Value > 0)
            {
                query = query.Where(c => c.CustomerId == customerId.Value);
            }

            // Apply date range filter
            if (fromDate.HasValue)
            {
                query = query.Where(c => c.DispDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(c => c.DispDate <= toDate.Value);
            }

            // Apply division filter
            if (!string.IsNullOrEmpty(division))
            {
                query = query.Where(c => c.Customer.Division == division);
            }

            var reportData = query.ToList();

            // Calculate totals
            var totalOpening = reportData.Sum(c => c.Opening);
            var totalInward = reportData.Sum(c => c.Inward);
            var totalOutward = reportData.Sum(c => c.Outward);
            var totalBalance = reportData.Sum(c => c.Balance);

            ViewBag.TotalOpening = totalOpening;
            ViewBag.TotalInward = totalInward;
            ViewBag.TotalOutward = totalOutward;
            ViewBag.TotalBalance = totalBalance;

            return PartialView("_ReportTable", reportData);
        }

        private List<SelectListItem> GetCustomer()
        {
            var lstProducts = new List<SelectListItem>();

            lstProducts = _context.Customer_Master.AsNoTracking().Select(n =>
            new SelectListItem
            {
                Value = n.Id.ToString(),
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

        private List<SelectListItem> GetDivision()
        {
            var lstProducts = new List<SelectListItem>();

            lstProducts = _context.Customer_Master.AsNoTracking()
                .Where(c => !string.IsNullOrEmpty(c.Division))
                .Select(n => n.Division)
                .Distinct()
                .Select(d => new SelectListItem
                {
                    Value = d,
                    Text = d
                }).ToList();

            var defItem = new SelectListItem()
            {
                Value = "",
                Text = "All Divisions"
            };

            lstProducts.Insert(0, defItem);

            return lstProducts;
        }
    }
}