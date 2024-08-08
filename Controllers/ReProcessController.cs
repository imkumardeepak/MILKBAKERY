using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AspNetCoreHero.ToastNotification.Abstractions;
using AspNetCoreHero.ToastNotification.Notyf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Milk_Bakery.Data;
using Milk_Bakery.Models;

namespace Milk_Bakery.Controllers
{
    [Authentication]
    public class ReProcessController : Controller
    {
        private readonly MilkDbContext _context;
        private readonly INotyfService _notyfService;
        private readonly AppSettings _appSettings;

        public ReProcessController(MilkDbContext context, INotyfService notyfService, IOptions<AppSettings> appSettings)
        {
            _context = context;
            _notyfService = notyfService;
            _appSettings = appSettings.Value;
        }

        // GET: OrderProcessFile
        public async Task<IActionResult> Index()
        {
            ViewBag.segement = GetSegement();
            ViewBag.company = GetCompany();
            List<PurchaseOrder> models = _context.PurchaseOrder.Where(a => a.verifyflag == 1 && a.processflag == 1).ToList();

            return View(models);
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
                Value = null,
                Text = "----Select Segement----"
            };

            lstProducts.Insert(0, defItem);

            return lstProducts;
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
                Value = null,
                Text = "----Select Company----"
            };

            lstProducts.Insert(0, defItem);

            return lstProducts;
        }

        [HttpPost]
        public IActionResult SaveSelectedRows(List<PurchaseOrder> models)
        {

            // Filter the selected rows
            List<PurchaseOrder> selected = models.Where(m => m.IsSelected).OrderBy(M => M.OrderNo).ToList();

            if (selected.Count > 0)
            {
                string fileContent = "";
                string carates = "";


                // List<PurchaseOrder> purchase = _context.PurchaseOrder.Where(a => a.verifyflag == 1 && a.processflag == 0 && a.Segementname == optionValue).AsNoTracking().ToList();


                foreach (var item in selected)
                {
                    var purchase = _context.PurchaseOrder.Where(a => a.OrderNo == item.OrderNo && a.processflag == 1).AsNoTracking().FirstOrDefault();
                    var segment = _context.SegementMaster.Where(a => a.SegementName == purchase.Segementname).FirstOrDefault();
                    var companydetails = _context.Company_SegementMap.Where(a => a.Segementname == purchase.Segementname).AsNoTracking().FirstOrDefault();
                    var material = _context.MaterialMaster.Where(a => a.segementname == purchase.Segementname && a.material3partycode.StartsWith("UTN")).AsNoTracking().FirstOrDefault();
                    var cusdetails = _context.CustomerSegementMap.Where(a => a.SegementName == purchase.Segementname && a.Customername == purchase.Customername).AsNoTracking().FirstOrDefault();
                    var prod_details = _context.ProductDetails.Where(a => a.PurchaseOrderId == purchase.Id).OrderBy(a => a.Unit).ToList();
                    bool check = prod_details.Any(detail => detail.Unit.Contains("Crates"));
                    int i = 0;
                    int total = 0;
                    foreach (var prod in prod_details)
                    {
                        if (prod.Unit.Contains("Crates"))
                        {
                            i++;
                            fileContent += companydetails.companycode + " " + cusdetails.segementcode3party + "-" + companydetails.companycode + "-" + cusdetails.custsegementcode + " " + cusdetails.custsegementcode.Substring(cusdetails.custsegementcode.Length - 4) + purchase.OrderNo.Substring(purchase.OrderNo.Length - 4) + " " + purchase.OrderNo + "/" + purchase.OrderDate.ToString("dd/MM/yy").Replace("/", "").Replace("-", "") + " " + purchase.OrderDate.ToString("dd/MM/yy") + " " + purchase.OrderDate.ToString("dd/MM/yy") + " " + purchase.OrderDate.ToString("dd/MM/yy") + i.ToString().PadLeft(20) + " " + prod.ProductCode.PadRight(15) + prod.qty.ToString().PadLeft(11) + " " + "N" + " " + segment.Segement_Code + Environment.NewLine;
                            total = total + prod.qty;
                        }
                        else
                        {
                            i++;
                            fileContent += companydetails.companycode + " " + cusdetails.segementcode3party + "-" + companydetails.companycode + "-" + cusdetails.custsegementcode + " " + cusdetails.custsegementcode.Substring(cusdetails.custsegementcode.Length - 4) + purchase.OrderNo.Substring(purchase.OrderNo.Length - 4) + " " + purchase.OrderNo + "/" + purchase.OrderDate.ToString("dd/MM/yy").Replace("/", "").Replace("-", "") + " " + purchase.OrderDate.ToString("dd/MM/yy") + " " + purchase.OrderDate.ToString("dd/MM/yy") + " " + purchase.OrderDate.ToString("dd/MM/yy") + i.ToString().PadLeft(20) + " " + prod.ProductCode.PadRight(15) + prod.qty.ToString().PadLeft(11) + " " + "N" + " " + segment.Segement_Code + Environment.NewLine;
                        }

                    }
                    if (check == true)
                    {
                        i = i + 1;
                        fileContent += companydetails.companycode + " " + cusdetails.segementcode3party + "-" + companydetails.companycode + "-" + cusdetails.custsegementcode + " " + cusdetails.custsegementcode.Substring(cusdetails.custsegementcode.Length - 4) + purchase.OrderNo.Substring(purchase.OrderNo.Length - 4) + " " + purchase.OrderNo + "/" + purchase.OrderDate.ToString("dd/MM/yy").Replace("/", "").Replace("-", "") + " " + purchase.OrderDate.ToString("dd/MM/yy") + " " + purchase.OrderDate.ToString("dd/MM/yy") + " " + purchase.OrderDate.ToString("dd/MM/yy") + i.ToString().PadLeft(20) + " " + material.material3partycode.PadRight(15) + total.ToString().PadLeft(11) + " " + "N" + " " + segment.Segement_Code + Environment.NewLine;
                    }

                }

                //path
                //string folderPath = @"C:\SAPFILE";
                string filename = Request.HttpContext.Session.GetString("UserName") + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss").Replace("/", "_").Replace(":", "_").Replace(" ", "_") + ".txt";
                Response.Headers.Add("Content-Disposition", $"attachment; filename={filename}");
                Response.ContentType = "text/plain";

                // Convert the content to bytes and send it to the response stream
                byte[] contentBytes = Encoding.UTF8.GetBytes(fileContent);
                var fileResult = File(contentBytes, "text/plain");
                TempData["FileDownloaded"] = true;
                _notyfService.Success("File Generated SuccessFully");
               
                return fileResult;


            }
            else
            {
                _notyfService.Warning("Select The Row");
                return RedirectToAction("Index");
            }
        }
        public IActionResult ActionName(string optionValue, string otherValue, DateTime FromDate, DateTime ToDate)
        {
            var category = _context.Company_SegementMap.Where(a => a.Companyname == otherValue).FirstOrDefault();
            if (category != null)
            {
                List<PurchaseOrder> models = _context.PurchaseOrder.Where(a => a.processflag == 1 && a.Segementname == optionValue && a.companycode == category.companycode && a.OrderDate.Date >= FromDate && a.OrderDate.Date <= ToDate).ToList();
                return PartialView("_ReFileGeneration", models);
            }
            else
            {
                return PartialView();
            }



        }


        public ActionResult fill_form(string selectedValue)
        {

            List<SelectListItem> wbridge = _context.Company_SegementMap.AsNoTracking()
                   .Where(n => n.Companyname == selectedValue).OrderBy(n => n.Segementname)
                       .Select(n =>
                       new SelectListItem
                       {
                           Selected = true,
                           Value = n.Segementname,
                           Text = n.Segementname
                       }).ToList();

            return Json(wbridge);


        }
    }
}
