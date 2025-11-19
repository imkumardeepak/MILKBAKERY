﻿using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Milk_Bakery.Data;
using Milk_Bakery.Models;

namespace Milk_Bakery.Controllers
{
    [Authentication]
    public class OutstandingController : Controller
    {
        private readonly MilkDbContext _context;
        public INotyfService _notifyService { get; }

        public OutstandingController(MilkDbContext context, INotyfService notyfService)
        {
            _context = context;
            _notifyService = notyfService;
        }

        public async Task<IActionResult> Index()
        {
            if (string.Equals(HttpContext.Session.GetString("role"), "Customer", StringComparison.OrdinalIgnoreCase))
            {

                ViewBag.customer = GetCustomer();
                ViewBag.company = GetCompany();
                var customer = _context.Customer_Master.Where(a => a.phoneno == HttpContext.Session.GetString("UserName")).FirstOrDefault();
                return _context.custTransactions != null ?
                    View(await _context.custTransactions.Where(a => a.customername == customer.Name).ToListAsync()) :
                    Problem("Entity set 'MilkDbContext.PurchaseOrder'  is null.");

            }
            else
            {
                ViewBag.customer = GetCustomer();
                ViewBag.company = GetCompany();
                return _context.custTransactions != null ?
                    View(await _context.custTransactions.ToListAsync()) :
                    Problem("Entity set 'MilkDbContext.PurchaseOrder'  is null.");
            }

        }
        private List<SelectListItem> GetCustomer()
        {

            if (string.Equals(HttpContext.Session.GetString("role"), "Sales", StringComparison.OrdinalIgnoreCase))
            {
                var sales = _context.EmployeeMaster.Where(a => a.PhoneNumber == HttpContext.Session.GetString("UserName")).FirstOrDefault();

                var order = _context.EmpToCustMap.Where(a => a.phoneno == sales.PhoneNumber).AsNoTracking().FirstOrDefault();
                List<Cust2EmpMap> poDetails = new List<Cust2EmpMap>();
                if (order != null)
                {
                    poDetails = _context.cust2EmpMaps.Where(d => d.empt2custid == order.id).AsNoTracking().ToList();
                }

                var lstProducts = new List<SelectListItem>();

                lstProducts = poDetails.Select(n =>
                new SelectListItem
                {
                    Value = n.customer,
                    Text = n.customer
                }).ToList();

                var defItem = new SelectListItem()
                {
                    Value = "",
                    Text = "----Select Customer----"
                };

                lstProducts.Insert(0, defItem);

                return lstProducts;
            }
            else if (string.Equals(HttpContext.Session.GetString("role"), "Customer", StringComparison.OrdinalIgnoreCase))
            {
                var sales = _context.Customer_Master.Where(a => a.phoneno == HttpContext.Session.GetString("UserName")).FirstOrDefault();
                var lstProducts = new List<SelectListItem>();
                var lstProducts1 = new List<SelectListItem>();

                var mappedcusr = _context.Cust2CustMap.Where(a => a.phoneno == sales.phoneno).AsNoTracking().FirstOrDefault();

                if (mappedcusr != null)
                {
                    lstProducts1 = _context.mappedcusts.Where(a => a.cust2custId == mappedcusr.id).Select(n =>
                              new SelectListItem
                              {
                                  Value = n.customer,
                                  Text = n.customer
                              }).ToList();
                }


                lstProducts = _context.Customer_Master.Where(a => a.Name == sales.Name).AsNoTracking().Select(n =>
                new SelectListItem
                {
                    Value = n.Name,
                    Text = n.Name
                }).ToList();




                var defItem = new SelectListItem()
                {
                    Value = "",
                    Text = "----Select Customer----"
                };


                lstProducts = lstProducts.Concat(lstProducts1).ToList();
                lstProducts.Insert(0, defItem);


                return lstProducts;
            }
            else
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
                    Text = "----Select Customer----"
                };

                lstProducts.Insert(0, defItem);

                return lstProducts;
            }

        }

        private List<SelectListItem> GetCompany()
        {
            var lstProducts = new List<SelectListItem>();

            lstProducts = _context.CompanyMaster.AsNoTracking().Select(n =>
            new SelectListItem
            {
                Value = n.Name,
                Text = n.Name
            }).ToList();

            var defItem = new SelectListItem()
            {
                Value = "",
                Text = "----Select Company----"
            };

            lstProducts.Insert(0, defItem);

            return lstProducts;
        }

        //public ActionResult fill_form(string selectedValue)
        //{

        //    List<SelectListItem> wbridge = _context.coma.AsNoTracking()
        //           .Where(n => n.Customername == selectedValue).OrderBy(n => n.SegementName)
        //               .Select(n =>
        //               new SelectListItem
        //               {
        //                   Selected = true,
        //                   Value = n.SegementName,
        //                   Text = n.SegementName
        //               }).ToList();

        //    return Json(wbridge);


        //}

        [HttpPost]
        public IActionResult ActionName(string company, string customer, DateTime FromDate, DateTime ToDate)
        {
            var category = _context.custTransactions.Where(a => a.customername == customer && a.cmpname == company && a.edate >= FromDate && a.edate <= ToDate).ToList();

            if (category.Count > 0)
            {
                ViewBag.edate = category.Where(a => a.customername == customer && a.cmpname == company).Max(a => a.lastupdate).ToString("dd-MM-yyyy hh:mm");
                return PartialView("_IndexView", category);
            }
            else
            {
                return View();
            }



        }
    }
}
