﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Milk_Bakery.Data;
using Milk_Bakery.Models;

namespace Milk_Bakery.Controllers
{
    [Authentication]
    public class RepeatOrderController : Controller
    {
        private readonly MilkDbContext _context;
        public INotyfService _notifyService { get; }

        public RepeatOrderController(MilkDbContext context, INotyfService notyf)
        {
            _context = context;
            _notifyService = notyf;
        }

        // GET: RepeatOrder
        public async Task<IActionResult> Index()
        {
            ViewBag.customer = GetCustomer();
            return View();
        }

        // GET: RepeatOrder/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.PurchaseOrder == null)
            {
                return NotFound();
            }

            var purchaseOrder = await _context.PurchaseOrder
                .FirstOrDefaultAsync(m => m.Id == id);
            if (purchaseOrder == null)
            {
                return NotFound();
            }

            return View(purchaseOrder);
        }

        // GET: RepeatOrder/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: RepeatOrder/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PurchaseOrder purchaseOrder)
        {
            if (HttpContext.Session.GetString("role") == "Customer")
            {
                var data = _context.PurchaseOrder.Where(item => item.OrderDate.Date == DateTime.Now.Date && item.Customername == purchaseOrder.Customername).ToList();

                if (data.Count > 0)
                {
                    _notifyService.Warning("You have already placed an order today.");
                    return RedirectToAction("Index", "Home");
                }
                purchaseOrder.Id = _context.PurchaseOrder.Any() ? _context.PurchaseOrder.Max(e => e.Id) + 1 : 1;
                purchaseOrder.ProductDetails.RemoveAll(a => a.qty == 0);
                purchaseOrder.OrderNo = getmaxorderno();
                var compaany = await _context.Company_SegementMap.Where(a => a.Segementname == purchaseOrder.Segementname).FirstOrDefaultAsync();
                purchaseOrder.OrderDate = DateTime.Now;
                purchaseOrder.companycode = compaany.companycode;

                _context.Add(purchaseOrder);
                await _context.SaveChangesAsync();
                _notifyService.Success("Order Has Been Successfully Placed No " + purchaseOrder.OrderNo + "");
                return RedirectToAction("Index", "Home");

            }
            else
            {
                var data = _context.PurchaseOrder.Where(item => item.OrderDate.Date == DateTime.Now.Date && item.Customername == purchaseOrder.Customername).ToList();

                if (data.Count > 0)
                {
                    _notifyService.Warning("You have already placed an order today.");
                    return RedirectToAction("Index", "Home");
                }
                purchaseOrder.Id = _context.PurchaseOrder.Any() ? _context.PurchaseOrder.Max(e => e.Id) + 1 : 1;
                purchaseOrder.ProductDetails.RemoveAll(a => a.qty == 0);
                purchaseOrder.OrderNo = getmaxorderno();
                var compaany = await _context.Company_SegementMap.Where(a => a.Segementname == purchaseOrder.Segementname).FirstOrDefaultAsync();
                purchaseOrder.OrderDate = DateTime.Now;
                purchaseOrder.companycode = compaany.companycode;
                if (ModelState.IsValid)
                {
                    _context.Add(purchaseOrder);
                    await _context.SaveChangesAsync();
                    _notifyService.Success("Order Has Been Successfully Placed No " + purchaseOrder.OrderNo + "");
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    _notifyService.Error("Data Not Saved");
                }
            }

            return View(purchaseOrder);
        }

        // GET: RepeatOrder/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.PurchaseOrder == null)
            {
                return NotFound();
            }
            //ViewBag.Product = GetProducts();
            ViewBag.segemnt = GetSegement();

            var purchaseOrder = await _context.PurchaseOrder.Where(a => a.Id == id).Include(d => d.ProductDetails).FirstOrDefaultAsync();
            var product = _context.ProductDetails.Where(a => a.PurchaseOrderId == purchaseOrder.Id).AsEnumerable();
            var material = await _context.MaterialMaster.Where(a => !product.Any(p => p.ProductName == a.Materialname) && !a.material3partycode.Contains("UTN") && a.segementname == purchaseOrder.Segementname && a.isactive==true).ToListAsync();

            int i = 0;
            foreach (var mat in material)
            {
                i++;
                purchaseOrder.ProductDetails.Add(new ProductDetail() { Id = i, ProductName = mat.Materialname, ProductCode = mat.material3partycode, Unit = mat.Unit, Rate = mat.price });
            }
            if (purchaseOrder == null)
            {
                return NotFound();
            }
            return View(purchaseOrder);
        }

        // POST: RepeatOrder/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PurchaseOrder purchaseOrder)
        {
            if (id != purchaseOrder.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(purchaseOrder);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PurchaseOrderExists(purchaseOrder.Id))
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
            return View(purchaseOrder);
        }

        // GET: RepeatOrder/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.PurchaseOrder == null)
            {
                return NotFound();
            }

            var purchaseOrder = await _context.PurchaseOrder
                .FirstOrDefaultAsync(m => m.Id == id);
            if (purchaseOrder == null)
            {
                return NotFound();
            }

            return View(purchaseOrder);
        }

        // POST: RepeatOrder/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.PurchaseOrder == null)
            {
                return Problem("Entity set 'MilkDbContext.PurchaseOrder'  is null.");
            }
            var purchaseOrder = await _context.PurchaseOrder.FindAsync(id);
            if (purchaseOrder != null)
            {
                _context.PurchaseOrder.Remove(purchaseOrder);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PurchaseOrderExists(int id)
        {
            return (_context.PurchaseOrder?.Any(e => e.Id == id)).GetValueOrDefault();
        }

        private List<SelectListItem> GetProducts()
        {
            var lstProducts = new List<SelectListItem>();

            lstProducts = _context.MaterialMaster.AsNoTracking().Where(n => n.isactive == true).Select(n =>
            new SelectListItem
            {
                Value = n.Materialname,
                Text = n.Materialname
            }).ToList();

            var defItem = new SelectListItem()
            {
                Value = "",
                Text = "----Select Product----"
            };

            lstProducts.Insert(0, defItem);

            return lstProducts;
        }
        [HttpPost]


        //  Get max Order no from database
        public string getmaxorderno()
        {
            int maxId = _context.PurchaseOrder.Any() ? _context.PurchaseOrder.Max(e => e.Id) + 1 : 1;
            string orderno = "";
            if (maxId.ToString().Length == 1)
            {
                orderno = "P0000000" + maxId.ToString();
            }
            else if (maxId.ToString().Length == 2)
            {
                orderno = "P000000" + maxId.ToString();
            }
            else if (maxId.ToString().Length == 3)
            {
                orderno = "P00000" + maxId.ToString();
            }
            else if (maxId.ToString().Length == 4)
            {
                orderno = "P0000" + maxId.ToString();
            }
            else if (maxId.ToString().Length == 5)
            {
                orderno = "P000" + maxId.ToString();
            }
            else if (maxId.ToString().Length == 6)
            {
                orderno = "P00" + maxId.ToString();
            }
            else if (maxId.ToString().Length == 7)
            {
                orderno = "P0" + maxId.ToString();
            }
            else if (maxId.ToString().Length == 8)
            {
                orderno = "P" + maxId.ToString();
            }
            return orderno;
        }
        private List<SelectListItem> GetSegement()
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
                Text = "----Select Segement----"
            };

            lstProducts.Insert(0, defItem);

            return lstProducts;
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
        public IActionResult ActionName(string optionValue)
        {
            int maxid = _context.PurchaseOrder.Where(po => po.Customername == optionValue).Max(po => po.Id);
            var maxDateData = _context.PurchaseOrder.Where(po => po.Id == maxid).OrderBy(a => a.OrderNo).ToList();
            return PartialView("RepeatView", maxDateData);
        }
    }
}
