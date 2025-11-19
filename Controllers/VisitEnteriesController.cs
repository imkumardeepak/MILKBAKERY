﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AspNetCoreHero.ToastNotification.Abstractions;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Milk_Bakery.Data;
using Milk_Bakery.Models;

namespace Milk_Bakery.Controllers
{
    [Authentication]
    public class VisitEnteriesController : Controller
    {
        private readonly MilkDbContext _context;
        public INotyfService _notifyService { get; }

        public VisitEnteriesController(MilkDbContext context, INotyfService notyfService)
        {
            _context = context;
            _notifyService = notyfService;
        }

        // GET: VisitEnteries
        public async Task<IActionResult> Index()
        {
            return _context.VisitEntery != null ?
                        View(await _context.VisitEntery.ToListAsync()) :
                        Problem("Entity set 'MilkDbContext.VisitEntery'  is null.");
        }

        // GET: VisitEnteries/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.VisitEntery == null)
            {
                return NotFound();
            }
            var visitEntery = await _context.VisitEntery
                .FirstOrDefaultAsync(m => m.Id == id);
            if (visitEntery == null)
            {
                return NotFound();
            }
            return View(visitEntery);
        }

        // GET: VisitEnteries/Create
        public IActionResult Create()
        {
            VisitEntery visit = new VisitEntery();
            visit.Id = 0;
            visit.dateTime = DateTime.Now;
            ViewBag.customer = GetCustomer();
            return View(visit);
        }

        // POST: VisitEnteries/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VisitEntery visitEntery)
        {
            var salesinfo = _context.EmployeeMaster.Where(a => a.PhoneNumber == HttpContext.Session.GetString("UserName")).FirstOrDefault();
            visitEntery.Salesname = salesinfo.FirstName;
            // visitEntery.location = visitEntery.location.Substring(0, visitEntery.location.IndexOf(','));
            string[] parts = visitEntery.location.Split(',');
            // Check if there are at least three parts (two commas)
            if (parts.Length >= 4)
            {
                visitEntery.location = string.Join(",", parts, 0, parts.Length - 4);
            }
            _context.Add(visitEntery);
            await _context.SaveChangesAsync();
            _notifyService.Success("Saved SuccesFully");
            return RedirectToAction("Index", "Home");

        }

        // GET: VisitEnteries/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.VisitEntery == null)
            {
                return NotFound();
            }

            var visitEntery = await _context.VisitEntery.FindAsync(id);
            if (visitEntery == null)
            {
                return NotFound();
            }
            return View(visitEntery);
        }

        // POST: VisitEnteries/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, VisitEntery visitEntery)
        {
            if (id != visitEntery.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(visitEntery);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VisitEnteryExists(visitEntery.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(visitEntery);
        }

        // GET: VisitEnteries/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.VisitEntery == null)
            {
                return NotFound();
            }

            var visitEntery = await _context.VisitEntery
                .FirstOrDefaultAsync(m => m.Id == id);
            if (visitEntery == null)
            {
                return NotFound();
            }

            return View(visitEntery);
        }

        // POST: VisitEnteries/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.VisitEntery == null)
            {
                return Problem("Entity set 'MilkDbContext.VisitEntery'  is null.");
            }
            var visitEntery = await _context.VisitEntery.FindAsync(id);
            if (visitEntery != null)
            {
                _context.VisitEntery.Remove(visitEntery);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool VisitEnteryExists(int id)
        {
            return (_context.VisitEntery?.Any(e => e.Id == id)).GetValueOrDefault();
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
        [HttpPost]
        public IActionResult ActionName(string optionValue)
        {
            var category = _context.Customer_Master.Where(a => a.Name.Contains(optionValue)).FirstOrDefault();


            if (category != null)
            {
                return Json(category);
            }
            else
            {
                return Json(null);
            }


        }

        [HttpPost]
        public JsonResult GetMap()
        {
            var data1 = Map();
            return Json(data1);
        }
        public IEnumerable<Map> Map()
        {
            List<Map> listOfMaps = new List<Map>();

            listOfMaps.Add(new Map
            {
                Name = "Deepak",
                Latitude = 21.117237034709625,
                Longitude = 79.07197189726398,
                Location = "Nagpur"
            });

            return listOfMaps;
        }

    }
    public class Map
    {
        public string Name { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Location { get; set; }
    }
}
