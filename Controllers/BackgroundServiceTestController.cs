using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Milk_Bakery.Data;
using Milk_Bakery.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Milk_Bakery.Controllers
{
    public class BackgroundServiceTestController : Controller
    {
        private readonly MilkDbContext _context;
        private readonly ILogger<BackgroundServiceTestController> _logger;

        public BackgroundServiceTestController(MilkDbContext context, ILogger<BackgroundServiceTestController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: BackgroundServiceTest
        public IActionResult Index()
        {
            return View();
        }

        // POST: BackgroundServiceTest/CreateSampleInvoices
        [HttpPost]
        public async Task<IActionResult> CreateSampleInvoices(int count = 5)
        {
            try
            {
                var random = new Random();
                for (int i = 0; i < count; i++)
                {
                    var invoice = new InvoiceDetails.Invoice
                    {
                        InvoiceNo = $"INV-{DateTime.Now:yyyyMMdd}-{random.Next(1000, 9999)}",
                        InvoiceDate = DateTime.Now.AddDays(-random.Next(1, 30)),
                        CustomerRefPO = $"PO-{random.Next(10000, 99999)}",
                        TotalAmount = random.Next(1000, 10000),
                        OrderDate = DateTime.Now.AddDays(-random.Next(1, 15)),
                        BillToName = $"Customer {random.Next(1, 100)}",
                        BillToCode = $"CUST-{random.Next(100, 999)}",
                        ShipToName = $"Ship To {random.Next(1, 50)}",
                        ShipToCode = $"SHIP-{random.Next(100, 999)}",
                        ShipToRoute = $"Route-{random.Next(1, 20)}",
                        CompanyName = "Haldiram Dairy",
                        CompanyCode = "HD001",
                        VehicleNo = $"DL{random.Next(1, 99)} XX {random.Next(1000, 9999)}",
                        setflag = 0 // This is what our background service will look for
                    };

                    _context.Invoices.Add(invoice);
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"{count} sample invoices created successfully with setflag=0";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating sample invoices");
                TempData["ErrorMessage"] = "Error creating sample invoices: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: BackgroundServiceTest/ViewInvoicesWithSetFlagZero
        public async Task<IActionResult> ViewInvoicesWithSetFlagZero()
        {
            var invoices = await _context.Invoices
                .Where(i => i.setflag == 0)
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();

            ViewBag.InvoiceCount = invoices.Count;
            return View(invoices);
        }
    }
}