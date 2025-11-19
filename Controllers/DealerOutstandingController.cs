using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Milk_Bakery.Data;
using Milk_Bakery.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

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
            // Return the OutstandingReport view
            return View();
        }

        // GET: DealerOutstanding/GetDistributors
        [HttpGet]
        public async Task<IActionResult> GetDistributors()
        {
            try
            {
                var distributors = await GetAvailableDistributors();
                var distributorList = distributors.Select(c => new { id = c.Id, name = c.Name }).ToList();
                return Json(new { success = true, distributors = distributorList });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves a filtered list of dealer names based on specific order criteria for the current date.
        /// Dealers must have placed an order on the current system date with ProcessFlag = 1 and DeliverFlag = 1.
        /// </summary>
        /// <param name="distributorId">The ID of the distributor to filter dealers by.</param>
        /// <returns>A JSON result containing a success flag and a list of filtered dealer names.</returns>
        [HttpGet]
        public async Task<IActionResult> GetDealerOutstandings(int distributorId)
        {
            try
            {
                var currentDate = DateTime.Today;

                // Get dealers with orders for today that are processed and delivered
                var filteredDealers = await _context.DealerMasters
                    .Where(d => d.DistributorId == distributorId)
                    .Join(
                        _context.DealerOrders,
                        dealer => dealer.Id,
                        order => order.DealerId,
                        (dealer, order) => new { Dealer = dealer, Order = order }
                    )
                    .Where(doj => doj.Order.OrderDate.Date == currentDate && doj.Order.ProcessFlag == 1 && doj.Order.DeliverFlag == 1)
                    .Select(doj => new { id = doj.Dealer.Id, name = doj.Dealer.Name })
                    .Distinct()
                    .ToListAsync();

                // Get outstanding information for these dealers
                var dealerOutstandings = new List<object>();
                
                foreach (var dealer in filteredDealers)
                {
                    // Get the latest outstanding record for this dealer
                    var latestOutstanding = await _context.DealerOutstandings
                        .Where(d => d.DealerId == dealer.id)
                        .OrderByDescending(d => d.DeliverDate)
                        .FirstOrDefaultAsync();

                    if (latestOutstanding != null)
                    {
                        dealerOutstandings.Add(new
                        {
                            dealerId = latestOutstanding.DealerId,
                            dealerName = dealer.name,
                            deliverDate = latestOutstanding.DeliverDate.ToString("dd/MM/yyyy"),
                            invoiceAmount = latestOutstanding.InvoiceAmount,
                            outstandingAmount = latestOutstanding.BalanceAmount,
                            receivedAmount = latestOutstanding.PaidAmount,
                            balanceAmount = latestOutstanding.BalanceAmount
                        });
                    }
                    else
                    {
                        // If no outstanding record exists, create a default entry
                        dealerOutstandings.Add(new
                        {
                            dealerId = dealer.id,
                            dealerName = dealer.name,
                            deliverDate = currentDate.ToString("dd/MM/yyyy"),
                            invoiceAmount = 0m,
                            outstandingAmount = 0m,
                            receivedAmount = 0m,
                            balanceAmount = 0m
                        });
                    }
                }

                return Json(new { success = true, outstandings = dealerOutstandings });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: DealerOutstanding/GetCustomers
        [HttpGet]
        public async Task<IActionResult> GetCustomers()
        {
            try
            {
                var customers = await GetAvailableDistributors(); // Reusing the logic from GetAvailableDistributors
                var customerList = customers.Select(c => new { id = c.Id, name = c.Name }).ToList();
                return Json(new { success = true, customers = customerList });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: DealerOutstanding/GetDealersByCustomer
        [HttpGet]
        public async Task<IActionResult> GetDealersByCustomer(int customerId)
        {
            try
            {
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

        // GET: DealerOutstanding/GenerateOutstandingReport
        [HttpGet]
        public async Task<IActionResult> GenerateOutstandingReport(int dealerId, int customerId, DateTime fromDate, DateTime toDate)
        {
            try
            {
                var dealerOutstandings = new List<object>();

                var query = _context.DealerOutstandings
                    .Include(d => d.Dealer)
                    .Where(d => d.DeliverDate >= fromDate && d.DeliverDate <= toDate);

                if (dealerId != 0)
                {
                    query = query.Where(d => d.DealerId == dealerId);
                }
                else
                {
                    // If dealerId is 0, it means "Select All Dealers" for the selected customer
                    var customerDealers = await _context.DealerMasters
                        .Where(d => d.DistributorId == customerId)
                        .Select(d => d.Id)
                        .ToListAsync();
                    query = query.Where(d => customerDealers.Contains(d.DealerId));
                }

                var outstandings = await query.ToListAsync();

                foreach (var outstanding in outstandings)
                {
                    dealerOutstandings.Add(new
                    {
                        dealerId = outstanding.DealerId,
                        dealerName = outstanding.Dealer.Name,
                        deliverDate = outstanding.DeliverDate.ToString("dd/MM/yyyy"),
                        invoiceAmount = outstanding.InvoiceAmount,
                        paidAmount = outstanding.PaidAmount,
                        balanceAmount = outstanding.BalanceAmount
                    });
                }

                return Json(new { success = true, outstandings = dealerOutstandings });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: DealerOutstanding/SaveReceivedAmount
        [HttpPost]
        public async Task<IActionResult> SaveReceivedAmount(int dealerId, decimal receivedAmount)
        {
            try
            {
                // Get the latest outstanding record for this dealer
                var latestOutstanding = await _context.DealerOutstandings
                    .Where(a => a.DealerId == dealerId)
                    .OrderByDescending(a => a.DeliverDate)
                    .FirstOrDefaultAsync();

                if (latestOutstanding != null)
                {
                    latestOutstanding.PaidAmount = receivedAmount;
                    // Update outstanding amount based on received amount
                    latestOutstanding.BalanceAmount = latestOutstanding.InvoiceAmount + latestOutstanding.BalanceAmount - receivedAmount;
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

        private async Task<List<Customer_Master>> GetAvailableDistributors()
        {
            var role = HttpContext.Session.GetString("role");
            var userName = HttpContext.Session.GetString("UserName");

            if (string.Equals(role, "Customer", StringComparison.OrdinalIgnoreCase))
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
            else if (string.Equals(role, "Sales", StringComparison.OrdinalIgnoreCase))
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

        private string GetDistributorCode(int distributorId)
        {
            var customer = _context.Customer_Master.FirstOrDefault(c => c.Id == distributorId);
            return customer?.shortname ?? "";
        }

    }
}