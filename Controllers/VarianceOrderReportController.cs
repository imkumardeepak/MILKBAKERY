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
    [Authentication]
    public class VarianceOrderReportController : Controller
    {
        private readonly MilkDbContext _context;

        public VarianceOrderReportController(MilkDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new VarianceOrderReportViewModel
            {
                FromDate = DateTime.Now.AddDays(-30),
                ToDate = DateTime.Now,
                AvailableCustomers = await GetAvailableCustomers()
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> GenerateReport(VarianceOrderReportViewModel model)
        {
            var reportItems = await GenerateVarianceReport(model.FromDate, model.ToDate, model.CustomerName, model.ShowOnlyVariance);
            
            var viewModel = new VarianceOrderReportViewModel
            {
                ReportItems = reportItems,
                FromDate = model.FromDate,
                ToDate = model.ToDate,
                CustomerName = model.CustomerName,
                ShowOnlyVariance = model.ShowOnlyVariance,
                AvailableCustomers = await GetAvailableCustomers()
            };

            return View("Index", viewModel);
        }

        private async Task<List<VarianceReportItem>> GenerateVarianceReport(DateTime? fromDate, DateTime? toDate, string customerName, bool showOnlyVariance)
        {
            var reportItems = new List<VarianceReportItem>();

            // Get purchase orders within date range and customer filter
            var purchaseOrdersQuery = _context.PurchaseOrder
                .Include(po => po.ProductDetails)
                .AsNoTracking()
                .Where(po => po.OrderDate >= fromDate && po.OrderDate <= toDate);

            if (!string.IsNullOrEmpty(customerName))
            {
                purchaseOrdersQuery = purchaseOrdersQuery.Where(po => po.Customername == customerName);
            }

            var purchaseOrders = await purchaseOrdersQuery.ToListAsync();

            // Get invoices within date range and customer filter
            var invoicesQuery = _context.Invoices
                .Include(i => i.InvoiceMaterials)
                .AsNoTracking()
                .Where(i => i.OrderDate >= fromDate && i.OrderDate <= toDate);

            if (!string.IsNullOrEmpty(customerName))
            {
                invoicesQuery = invoicesQuery.Where(i => i.BillToName == customerName);
            }

            var invoices = await invoicesQuery.ToListAsync();

            // Group purchase order data by customer and material
            var orderedData = purchaseOrders
                .SelectMany(po => po.ProductDetails, (po, pd) => new
                {
                    CustomerName = po.Customername,
                    MaterialName = pd.ProductName,
                    MaterialCode = pd.ProductCode,
                    Quantity = pd.qty,
                    Amount = pd.Price
                })
                .GroupBy(x => new { x.CustomerName, x.MaterialName, x.MaterialCode })
                .Select(g => new
                {
                    g.Key.CustomerName,
                    g.Key.MaterialName,
                    g.Key.MaterialCode,
                    Quantity = g.Sum(x => x.Quantity),
                    Amount = g.Sum(x => x.Amount)
                })
                .ToList();

            // Group invoice data by customer and material
            var invoicedData = invoices
                .SelectMany(inv => inv.InvoiceMaterials, (inv, im) => new
                {
                    CustomerName = inv.BillToName,
                    MaterialName = im.ProductDescription,
                    MaterialCode = im.MaterialSapCode,
                    Quantity = im.QuantityCases * im.UnitPerCase + im.QuantityUnits, // Total quantity
                    Amount = (decimal)(im.QuantityCases * im.UnitPerCase + im.QuantityUnits) * 10 // Placeholder rate
                })
                .GroupBy(x => new { x.CustomerName, x.MaterialName, x.MaterialCode })
                .Select(g => new
                {
                    g.Key.CustomerName,
                    g.Key.MaterialName,
                    g.Key.MaterialCode,
                    Quantity = g.Sum(x => x.Quantity),
                    Amount = g.Sum(x => x.Amount)
                })
                .ToList();

            // Combine data to calculate variance
            var allCustomers = orderedData.Select(x => x.CustomerName)
                .Union(invoicedData.Select(x => x.CustomerName))
                .Distinct();

            foreach (var customer in allCustomers)
            {
                var customerOrderedData = orderedData.Where(x => x.CustomerName == customer).ToDictionary(x => x.MaterialCode, x => x);
                var customerInvoicedData = invoicedData.Where(x => x.CustomerName == customer).ToDictionary(x => x.MaterialCode, x => x);

                var allCustomerMaterials = customerOrderedData.Keys.Union(customerInvoicedData.Keys).Distinct();

                foreach (var materialCode in allCustomerMaterials)
                {
                    var orderedQuantity = customerOrderedData.ContainsKey(materialCode) ? customerOrderedData[materialCode].Quantity : 0;
                    var invoicedQuantity = customerInvoicedData.ContainsKey(materialCode) ? customerInvoicedData[materialCode].Quantity : 0;
                    var orderedAmount = customerOrderedData.ContainsKey(materialCode) ? customerOrderedData[materialCode].Amount : 0;
                    var invoicedAmount = customerInvoicedData.ContainsKey(materialCode) ? customerInvoicedData[materialCode].Amount : 0;

                    // Only add items where there's data
                    if (orderedQuantity > 0 || invoicedQuantity > 0)
                    {
                        var materialName = "";
                        if (customerOrderedData.ContainsKey(materialCode))
                            materialName = customerOrderedData[materialCode].MaterialName;
                        else if (customerInvoicedData.ContainsKey(materialCode))
                            materialName = customerInvoicedData[materialCode].MaterialName;

                        var reportItem = new VarianceReportItem
                        {
                            CustomerName = customer,
                            MaterialName = materialName,
                            MaterialCode = materialCode,
                            OrderedQuantity = orderedQuantity,
                            InvoicedQuantity = invoicedQuantity,
                            QuantityVariance = orderedQuantity - invoicedQuantity,
                            OrderedAmount = orderedAmount,
                            InvoicedAmount = invoicedAmount,
                            AmountVariance = orderedAmount - invoicedAmount
                        };

                        // If showOnlyVariance is true, only add items with variance
                        if (!showOnlyVariance || reportItem.HasVariance)
                        {
                            reportItems.Add(reportItem);
                        }
                    }
                }
            }

            return reportItems.OrderBy(r => r.CustomerName).ThenBy(r => r.MaterialName).ToList();
        }

        private async Task<List<string>> GetAvailableCustomers()
        {
            var role = HttpContext.Session.GetString("role");
            var userName = HttpContext.Session.GetString("UserName");

            if (role == "Customer")
            {
                // For customer role, return only the logged-in customer
                var customer = await _context.Customer_Master
                    .Where(c => c.phoneno == userName)
                    .Select(c => c.Name)
                    .FirstOrDefaultAsync();

                return customer != null ? new List<string> { customer } : new List<string>();
            }
            else
            {
                // For admin/sales roles, return all customers
                return await _context.Customer_Master
                    .Select(c => c.Name)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();
            }
        }
    }
}