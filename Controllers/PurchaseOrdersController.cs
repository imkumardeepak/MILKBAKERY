using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Milk_Bakery.Data;
using Newtonsoft.Json;
using Milk_Bakery.Models;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Office2010.Excel;

namespace Milk_Bakery.Controllers
{
    [Authentication]
    public class PurchaseOrdersController : Controller
    {
        private readonly MilkDbContext _context;
        public INotyfService _notifyService { get; }

        public PurchaseOrdersController(MilkDbContext context, INotyfService notyf)
        {
            _context = context;
            _notifyService = notyf;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.customer = GetCustomer();
            return View();

        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.PurchaseOrder == null)
            {
                return NotFound();
            }
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
            ViewBag.segemnt = lstProducts;
            ViewBag.customer = GetCustomer();
            var purchaseOrder = await _context.PurchaseOrder.Where(a => a.Id == id).Include(d => d.ProductDetails).FirstOrDefaultAsync();
            if (purchaseOrder == null)
            {
                return NotFound();
            }

            return View(purchaseOrder);
        }

        public IActionResult Create()
        {
            if (HttpContext.Session.GetString("role") == "Customer")
            {
                PurchaseOrder item = new PurchaseOrder();
                var customer = _context.Customer_Master.Where(a => a.phoneno == HttpContext.Session.GetString("UserName")).FirstOrDefault();
                item.Customername = customer.Name;



                var lstProducts = new List<SelectListItem>();



                lstProducts = _context.CustomerSegementMap.Where(a => a.Customername == customer.Name).Select(n =>
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
                ViewBag.segemnt = lstProducts;
                item.Segementname = lstProducts.Select(a => a.Text).FirstOrDefault();
                ViewBag.customer = GetCustomer();
                //item.ProductDetails.Add(new ProductDetail() { Id = 1 });
                var material = _context.MaterialMaster.AsNoTracking().Where(m => !m.Materialname.StartsWith("CRATES")).OrderBy(a => a.Id).ToList();

                int i = 0;
                foreach (var mat in material)
                {
                    i++;
                    item.ProductDetails.Add(new ProductDetail() { Id = i, ProductName = mat.Materialname, ProductCode = mat.material3partycode, Unit = mat.Unit, Rate = mat.price });
                }
                return View(item);
            }
            else if (HttpContext.Session.GetString("role") == "Sales")
            {
                PurchaseOrder item = new PurchaseOrder();

                ViewBag.customer = GetCustomer();
                ViewBag.segemnt = GetSegement();
                //item.ProductDetails.Add(new ProductDetail() { Id = 1 });
                var material = _context.MaterialMaster.AsNoTracking().Where(m => !m.Materialname.StartsWith("CRATES")).OrderBy(a => a.sequence).ToList();

                int i = 0;
                foreach (var mat in material)
                {
                    i++;
                    item.ProductDetails.Add(new ProductDetail() { Id = i, ProductName = mat.Materialname, ProductCode = mat.material3partycode, Unit = mat.Unit, Rate = mat.price });
                }
                return View(item);
            }
            else
            {
                PurchaseOrder item = new PurchaseOrder();
                //item.OrderNo = getmaxorderno();
                ViewBag.segemnt = GetSegement();
                ViewBag.customer = GetCustomer();
                //item.ProductDetails.Add(new ProductDetail() { Id = 1 });
                //var material = _context.MaterialMaster.AsNoTracking().ToList();
                //int i = 0;
                //foreach (var mat in material)
                //{
                //    i++;
                //    item.ProductDetails.Add(new ProductDetail() { Id = i, ProductName = mat.Materialname, ProductCode = mat.material3partycode, Unit = mat.Unit, Rate = mat.price });
                //}
                return View(item);
            }
            return View();

        }

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
                purchaseOrder.OrderDate = DateTime.Now.Date;
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
                purchaseOrder.OrderDate = DateTime.Now.Date;
                purchaseOrder.companycode = compaany.companycode;

                _context.Add(purchaseOrder);
                await _context.SaveChangesAsync();
                _notifyService.Success("Order Has Been Successfully Placed No " + purchaseOrder.OrderNo + "");
                return RedirectToAction(nameof(Index));

            }

            return View(purchaseOrder);
        }

        // GET: PurchaseOrders/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.PurchaseOrder == null)
            {
                return NotFound();
            }

            ViewBag.customer = GetCustomer();
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
            ViewBag.segemnt = lstProducts;
            var purchaseOrder = await _context.PurchaseOrder.Where(a => a.Id == id).Include(d => d.ProductDetails).FirstOrDefaultAsync();
            var product = _context.ProductDetails.Where(a => a.PurchaseOrderId == purchaseOrder.Id).AsEnumerable();
            var material = await _context.MaterialMaster.Where(a => !product.Any(p => p.ProductName == a.Materialname)).OrderBy(a => a.sequence).ToListAsync();
            material = material.Where(a => a.segementname == purchaseOrder.Segementname).ToList();
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


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PurchaseOrder purchaseOrder)
        {
            purchaseOrder.ProductDetails.RemoveAll(a => a.qty == 0);
            if (id != purchaseOrder.Id)
            {
                return NotFound();
            }


            try
            {
                var order = await _context.PurchaseOrder.Where(a => a.Id == id).Include(d => d.ProductDetails).AsNoTracking().FirstOrDefaultAsync();
                purchaseOrder.companycode = order.companycode;
                List<ProductDetail> poDetails = _context.ProductDetails.Where(d => d.PurchaseOrderId == purchaseOrder.Id).ToList();
                _context.ProductDetails.RemoveRange(poDetails);
                _context.SaveChanges();

                _context.Attach(purchaseOrder);
                _context.Entry(purchaseOrder).State = EntityState.Modified;
                _context.ProductDetails.AddRange(purchaseOrder.ProductDetails);
                _context.SaveChanges();
                _notifyService.Success("Order Update Sucessfully");
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

            return View(purchaseOrder);
        }

        // GET: PurchaseOrders/Delete/5
        public async Task<IActionResult> Delete(int? id)
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
            _notifyService.Success("Order Delete Sucessfully");
            return RedirectToAction(nameof(Index));
        }
        private bool PurchaseOrderExists(int id)
        {
            return (_context.PurchaseOrder?.Any(e => e.Id == id)).GetValueOrDefault();
        }
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

            //lstProducts = _context.SegementMaster.AsNoTracking().Select(n =>
            //new SelectListItem
            //{
            //    Value = n.SegementName,
            //    Text = n.SegementName
            //}).ToList();

            var defItem = new SelectListItem()
            {
                Value = "",
                Text = "----Select Segement----"
            };

            lstProducts.Insert(0, defItem);

            return lstProducts;
        }
        [HttpPost]
        public IActionResult ActionName(string optionValue, string optionValue1)
        {
            var category = _context.MaterialMaster.Where(a => a.segementname.Contains(optionValue) && !a.Materialname.StartsWith("CRATES") && a.isactive == true).OrderBy(a => a.sequence).ToList();
            var segement = _context.CustomerSegementMap.Where(a => a.SegementName == optionValue && a.Customername == optionValue1).AsNoTracking().FirstOrDefault();
            PurchaseOrder item = new PurchaseOrder();
            if (segement != null)
            {
                item.Segementname = segement.SegementName;
                item.CustomerCode = segement.custsegementcode;
                item.Segementcode = segement.segementcode3party;
            }

            int i = 0;
            foreach (var mat in category)
            {
                i++;
                item.ProductDetails.Add(new ProductDetail() { Id = i, ProductName = mat.Materialname, ProductCode = mat.material3partycode, Unit = mat.Unit, Rate = mat.price });
            }
            return PartialView("_PurchaseOrderPartial", item);
        }
        private List<SelectListItem> GetCustomer()
        {

            if (HttpContext.Session.GetString("role") == "Sales")
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
            else if (HttpContext.Session.GetString("role") == "Customer")
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
        [HttpGet]

        public ActionResult fill_form(string selectedValue)
        {

            List<SelectListItem> wbridge = _context.CustomerSegementMap.AsNoTracking()
                   .Where(n => n.Customername == selectedValue).OrderBy(n => n.SegementName)
                       .Select(n =>
                       new SelectListItem
                       {
                           Selected = true,
                           Value = n.SegementName,
                           Text = n.SegementName
                       }).ToList();

            return Json(wbridge);


        }

        [HttpPost]
        public IActionResult ActionName2(DateTime FromDate, DateTime ToDate, string SelectedOption)
        {
            if (SelectedOption == null)
            {
                return PartialView();
            }
            else
            {
                var cust = _context.Customer_Master.Where(a => a.Name == SelectedOption).AsNoTracking().FirstOrDefault();
                var category = _context.PurchaseOrder.Where(A => A.Customername == cust.Name && A.OrderDate.Date >= FromDate.Date && A.OrderDate.Date <= ToDate.Date).ToList();
                return PartialView("Indexdata", category);
            }
        }
    }
}
