using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Milk_Bakery.Data;
using Milk_Bakery.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Milk_Bakery.Controllers
{
    [Authentication]
    public class DealerOutstandingController : Controller
    {
        private readonly MilkDbContext _context;

        public DealerOutstandingController(MilkDbContext context)
        {
            _context = context;
        }

        // GET: DealerOutstanding
        public async Task<IActionResult> Index()
        {
            // For now, just return the view. We'll load data via AJAX.
            return View();
        }

        // GET: DealerOutstanding/OutstandingReport
        public async Task<IActionResult> OutstandingReport()
        {
            return View();
        }

        // GET: DealerOutstanding/GetCustomers
        [HttpGet]
        public async Task<IActionResult> GetCustomers()
        {
            try
            {
                var customers = await GetAvailableDistributors();
                var customerList = customers.Select(c => new { id = c.Id, name = c.Name }).ToList();
                return Json(new { success = true, customers = customerList });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private async Task<List<Customer_Master>> GetAvailableDistributors()
        {
            var role = HttpContext.Session.GetString("role");
            var userName = HttpContext.Session.GetString("UserName");

            if (role == "Customer")
            {
                // For customer role, get the logged-in customer and their mapped customers
                var loggedInCustomer = await _context.Customer_Master
                    .FirstOrDefaultAsync(c => c.phoneno == userName);

                if (loggedInCustomer != null)
                {
                    var customers = new List<Customer_Master> { loggedInCustomer };

                    // Get mapped customers
                    var mappedCustomer = await _context.Cust2CustMap
                        .FirstOrDefaultAsync(c => c.phoneno == userName);

                    if (mappedCustomer != null)
                    {
                        var mappedCusts = await _context.mappedcusts
                            .Where(mc => mc.cust2custId == mappedCustomer.id)
                            .ToListAsync();

                        foreach (var mapped in mappedCusts)
                        {
                            var customer = await _context.Customer_Master
                                .FirstOrDefaultAsync(c => c.Name == mapped.customer);
                            if (customer != null)
                            {
                                customers.Add(customer);
                            }
                        }
                    }

                    return customers;
                }
            }
            else if (role == "Sales")
            {
                // For sales role, get mapped customers
                var empToCustMap = await _context.EmpToCustMap
                    .FirstOrDefaultAsync(e => e.empl == userName);

                if (empToCustMap != null)
                {
                    var mappedCusts = await _context.mappedcusts
                        .Where(mc => mc.cust2custId == empToCustMap.id)
                        .ToListAsync();

                    var customers = new List<Customer_Master>();
                    foreach (var mapped in mappedCusts)
                    {
                        var customer = await _context.Customer_Master
                            .FirstOrDefaultAsync(c => c.Name == mapped.customer);
                        if (customer != null)
                        {
                            customers.Add(customer);
                        }
                    }

                    return customers;
                }
            }
            else
            {
                // For admin role, get all customers
                return await _context.Customer_Master.ToListAsync();
            }

            return new List<Customer_Master>();
        }

        // GET: DealerOutstanding/GetDealersByCustomer
        [HttpGet]
        public async Task<IActionResult> GetDealersByCustomer(int customerId)
        {
            try
            {
                var availableCustomers = await GetAvailableDistributors();
                if (!availableCustomers.Any(c => c.Id == customerId))
                {
                    return Json(new { success = false, message = "Unauthorized access to customer's dealers." });
                }

                var dealers = await _context.DealerMasters
                                            .Where(d => d.DistributorId == customerId)
                                            .Select(d => new { id = d.Id, name = d.Name })
                                            .ToListAsync();
                return Json(new { success = true, dealers = dealers });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: DealerOutstanding/GetDealerOutstandingForToday
        [HttpGet]
        public async Task<IActionResult> GetDealerOutstandingForToday(int dealerId)
        {
            try
            {
                var today = DateTime.Today;
                var outstanding = await _context.DealerOutstandings
                                                .Where(d => d.DealerId == dealerId && d.DeliverDate.Date == today.Date)
                                                .FirstOrDefaultAsync();

                if (outstanding != null)
                {
                    var dealerName = _context.DealerMasters.FirstOrDefault(dm => dm.Id == dealerId)?.Name;
                    return Json(new
                    {
                        success = true,
                        outstanding = new
                        {
                            dealerId = outstanding.DealerId,
                            dealerName = dealerName,
                            deliverDate = outstanding.DeliverDate.ToString("dd/MM/yyyy"),
                            invoiceAmount = outstanding.InvoiceAmount,
                            outstandingAmount = outstanding.BalanceAmount,
                            receivedAmount = outstanding.PaidAmount
                        }
                    });
                }
                else
                {
                    return Json(new { success = true, outstanding = (object)null }); // No outstanding for today
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: DealerOutstanding/GenerateOutstandingReport
        [HttpGet]
        public async Task<IActionResult> GenerateOutstandingReport(int dealerId, int customerId, DateTime fromDate, DateTime toDate)
        {
            try
            {
                // Validate customer access
                var availableCustomers = await GetAvailableDistributors();
                if (!availableCustomers.Any(c => c.Id == customerId))
                {
                    return Json(new { success = false, message = "Unauthorized access to customer data." });
                }

                List<DealerOutstanding> outstandings;

                if (dealerId == 0)
                {
                    // "Select All" option - get all dealers for the selected customer
                    var dealers = await _context.DealerMasters
                        .Where(d => d.DistributorId == customerId)
                        .Select(d => d.Id)
                        .ToListAsync();

                    outstandings = await _context.DealerOutstandings
                        .Where(d => dealers.Contains(d.DealerId) && d.DeliverDate.Date >= fromDate.Date && d.DeliverDate.Date <= toDate.Date)
                        .OrderBy(d => d.DealerId)
                        .ThenBy(d => d.DeliverDate)
                        .ToListAsync();
                }
                else
                {
                    // Specific dealer selected
                    outstandings = await _context.DealerOutstandings
                        .Where(d => d.DealerId == dealerId && d.DeliverDate.Date >= fromDate.Date && d.DeliverDate.Date <= toDate.Date)
                        .OrderBy(d => d.DeliverDate)
                        .ToListAsync();
                }

                var reportData = new List<object>();

                foreach (var outstanding in outstandings)
                {
                    var dealerName = _context.DealerMasters.FirstOrDefault(dm => dm.Id == outstanding.DealerId)?.Name;

                    reportData.Add(new
                    {
                        dealerName = dealerName,
                        deliverDate = outstanding.DeliverDate.ToString("dd/MM/yyyy"),
                        invoiceAmount = outstanding.InvoiceAmount,
                        paidAmount = outstanding.PaidAmount,
                        balanceAmount = outstanding.BalanceAmount
                    });
                }

                return Json(new { success = true, outstandings = reportData });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private int GetCustomerIdFromContext()
        {
            // This is a simplified implementation
            // In a real scenario, you would get this from session or user context
            return 1; // Default to first customer for now
        }

        // POST: DealerOutstanding/SaveReceivedAmount
        [HttpPost]
        public async Task<IActionResult> SaveReceivedAmount(int dealerId, decimal receivedAmount)
        {
            try
            {
                var latestOutstanding = await _context.DealerOutstandings
                                                    .Where(a => a.DealerId == dealerId)
                                                    .OrderByDescending(a => a.DeliverDate)
                                                    .FirstOrDefaultAsync();

                if (latestOutstanding != null)
                {
                    latestOutstanding.PaidAmount = receivedAmount;
                    latestOutstanding.BalanceAmount = latestOutstanding.InvoiceAmount - receivedAmount;
                    await _context.SaveChangesAsync();

                    return Json(new { success = true, message = "Received amount updated successfully." });
                }
                else
                {
                    return Json(new { success = false, message = "No outstanding record found for this dealer." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}