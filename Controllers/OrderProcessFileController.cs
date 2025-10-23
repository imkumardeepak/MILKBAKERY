using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AspNetCoreHero.ToastNotification.Abstractions;
using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Milk_Bakery.Data;
using Milk_Bakery.Models;

namespace Milk_Bakery.Controllers
{
	[Authentication]
	public class OrderProcessFileController : Controller
	{
		private readonly MilkDbContext _context;
		private readonly INotyfService _notyfService;
		private readonly AppSettings _appSettings;

		public OrderProcessFileController(MilkDbContext context, INotyfService notyfService, IOptions<AppSettings> appSettings)
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
			List<PurchaseOrder> models = _context.PurchaseOrder.Where(a => a.verifyflag == 1 && a.processflag == 0).ToList();

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
		public IActionResult ActionName(string optionValue, string otherValue, DateTime FromDate, DateTime ToDate, string selectbox)
		{
			if (selectbox == "unprocess")
			{
				var category = _context.Company_SegementMap.Where(a => a.Companyname == otherValue && a.Segementname == optionValue).FirstOrDefault();
				if (category != null)
				{
					List<PurchaseOrder> models = _context.PurchaseOrder.Where(a => a.processflag == 0 && a.Segementname == optionValue && a.companycode == category.companycode && a.OrderDate.Date >= FromDate && a.OrderDate.Date <= ToDate).ToList();
					return PartialView("_FileGenerationPartial", models);
				}
				else
				{
					return PartialView();
				}



			}
			else
			{
				var category = _context.Company_SegementMap.Where(a => a.Companyname == otherValue).FirstOrDefault();
				if (category != null)
				{
					List<PurchaseOrder> models = _context.PurchaseOrder.Where(a => a.processflag == 1 && a.Segementname == optionValue && a.companycode == category.companycode && a.OrderDate.Date >= FromDate && a.OrderDate.Date <= ToDate).ToList();

					return PartialView("_ShowProcess", models);
				}
				else
				{
					return PartialView();
				}
			}

		}


		[HttpPost]
		public IActionResult ActionName1(string optionValue)
		{
			if (optionValue == "----Select Segement----")
			{
				_notyfService.Error("Select Segement");
			}
			else
			{
				// Generate the content for the text file
				string fileContent = "";
				List<PurchaseOrder> purchase = _context.PurchaseOrder.Where(a => a.verifyflag == 1 && a.processflag == 0 && a.Segementname == optionValue).AsNoTracking().ToList();


				foreach (var item in purchase)
				{
					var segment = _context.SegementMaster.Where(a => a.SegementName == optionValue).FirstOrDefault();
					var companydetails = _context.Company_SegementMap.Where(a => a.Segementname == item.Segementname).AsNoTracking().FirstOrDefault();
					var cusdetails = _context.CustomerSegementMap.Where(a => a.SegementName == item.Segementname).AsNoTracking().FirstOrDefault();
					var prod_details = _context.ProductDetails.Where(a => a.PurchaseOrderId == item.Id).ToList();
					int i = 0;
					foreach (var prod in prod_details)
					{
						i++;
						fileContent += companydetails.companycode + " " + cusdetails.segementcode3party + "-" + companydetails.companycode + "-" + cusdetails.custsegementcode + " " + cusdetails.custsegementcode.Substring(cusdetails.custsegementcode.Length - 4) + item.OrderNo.Substring(item.OrderNo.Length - 4) + " " + item.OrderNo + "/" + item.OrderDate.ToShortDateString().Replace("/", "").Replace("-", "") + " " + item.OrderDate.ToString("dd/MM/yyyy") + " " + item.OrderDate.ToString("dd/MM/yyyy") + " " + item.OrderDate.ToString("dd/MM/yyyy") + "              " + i + " " + prod.ProductCode + "           " + prod.qty + " " + "N" + " " + segment.Segement_Code + Environment.NewLine;
					}


					//update process flag 

					PurchaseOrder order = new PurchaseOrder();
					order.processflag = 1;
					order.Id = item.Id;
					order.OrderDate = item.OrderDate;
					order.OrderNo = item.OrderNo;
					order.verifyflag = 1;
					order.Customername = item.Customername;
					order.CustomerCode = item.CustomerCode;
					order.Segementname = item.Segementname;
					order.Segementcode = item.Segementcode;
					_context.PurchaseOrder.Update(order);
					_context.SaveChanges();
				}



				// Convert the content to bytes
				byte[] fileBytes = Encoding.UTF8.GetBytes(fileContent);

				// Set the content type and file name
				string contentType = "text/plain";
				string fileName = "sample.txt";
				_notyfService.Success("File Generated SuccessFully");
				// Return the file as a response
				return File(fileBytes, contentType, fileName);
			}
			return Ok();
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
					var purchase = _context.PurchaseOrder.Where(a => a.OrderNo == item.OrderNo && a.processflag == 0).AsNoTracking().FirstOrDefault();
					var segment = _context.SegementMaster.Where(a => a.SegementName == purchase.Segementname).AsNoTracking().FirstOrDefault();
					var companydetails = _context.Company_SegementMap.Where(a => a.Segementname == purchase.Segementname).AsNoTracking().FirstOrDefault();
					var material = _context.MaterialMaster.Where(a => a.segementname == purchase.Segementname && a.Materialname.StartsWith("CRATES FOR")).AsNoTracking().FirstOrDefault();
					var cusdetails = _context.CustomerSegementMap.Where(a => a.SegementName == purchase.Segementname && a.Customername == purchase.Customername).AsNoTracking().FirstOrDefault();
					var prod_details = _context.ProductDetails.Where(a => a.PurchaseOrderId == purchase.Id).AsNoTracking().OrderBy(a => a.Unit).ToList();
					bool check = prod_details.Any(detail => detail.Unit.Contains("Crates"));
					int i = 0;
					int total = 0;
					foreach (var prod in prod_details)
					{
						if (prod.Unit.Contains("Crates"))
						{
							i++;
							const int totalFieldWidth = 26;
							int productCodeLength = prod.ProductCode.Length;
							int qtyPadding = totalFieldWidth - productCodeLength;
							qtyPadding = Math.Max(qtyPadding, 1);
							fileContent += companydetails.companycode + " " + cusdetails.segementcode3party + "-" + companydetails.companycode + "-" + cusdetails.custsegementcode + " " + cusdetails.custsegementcode.Substring(cusdetails.custsegementcode.Length - 4) + purchase.OrderNo.Substring(purchase.OrderNo.Length - 4) + " " + purchase.OrderNo + "/" + purchase.OrderDate.ToString("dd/MM/yy").Replace("/", "").Replace("-", "") + " " + purchase.OrderDate.ToString("dd/MM/yy") + " " + purchase.OrderDate.ToString("dd/MM/yy") + " " + purchase.OrderDate.ToString("dd/MM/yy") + i.ToString().PadLeft(20) + " " + prod.ProductCode.PadRight(productCodeLength) + prod.qty.ToString().PadLeft(qtyPadding) + " " + "N" + " " + segment.Segement_Code + Environment.NewLine;
							total = total + prod.qty;
						}
						else
						{
							i++;
							const int totalFieldWidth = 26;
							int productCodeLength = prod.ProductCode.Length;
							int qtyPadding = totalFieldWidth - productCodeLength;
							qtyPadding = Math.Max(qtyPadding, 1); // Ensure at least 1 space for qty
							fileContent += companydetails.companycode + " " + cusdetails.segementcode3party + "-" + companydetails.companycode + "-" + cusdetails.custsegementcode + " " + cusdetails.custsegementcode.Substring(cusdetails.custsegementcode.Length - 4) + purchase.OrderNo.Substring(purchase.OrderNo.Length - 4) + " " + purchase.OrderNo + "/" + purchase.OrderDate.ToString("dd/MM/yy").Replace("/", "").Replace("-", "") + " " + purchase.OrderDate.ToString("dd/MM/yy") + " " + purchase.OrderDate.ToString("dd/MM/yy") + " " + purchase.OrderDate.ToString("dd/MM/yy") + i.ToString().PadLeft(20) + " " + prod.ProductCode.PadRight(productCodeLength) + prod.qty.ToString().PadLeft(qtyPadding) + " " + "N" + " " + segment.Segement_Code + Environment.NewLine;
						}

					}
					if (check == true)
					{
						i = i + 1;
						//fileContent += companydetails.companycode + " " + cusdetails.segementcode3party + "-" + companydetails.companycode + "-" + cusdetails.custsegementcode + " " + cusdetails.custsegementcode.Substring(cusdetails.custsegementcode.Length - 4) + purchase.OrderNo.Substring(purchase.OrderNo.Length - 4) + " " + purchase.OrderNo + "/" + purchase.OrderDate.ToString("dd/MM/yy").Replace("/", "").Replace("-", "") + " " + purchase.OrderDate.ToString("dd/MM/yy") + " " + purchase.OrderDate.ToString("dd/MM/yy") + " " + purchase.OrderDate.ToString("dd/MM/yy") + i.ToString().PadLeft(20) + " " + material.material3partycode.PadRight(15) + total.ToString().PadLeft(11) + " " + "N" + " " + segment.Segement_Code + Environment.NewLine;
					}

					//update process flag 

					PurchaseOrder order = new PurchaseOrder();
					order.processflag = 1;
					order.Id = purchase.Id;
					order.OrderDate = purchase.OrderDate;
					order.OrderNo = purchase.OrderNo;
					order.verifyflag = 1;
					order.Customername = purchase.Customername;
					order.CustomerCode = purchase.CustomerCode;
					order.Segementname = purchase.Segementname;
					order.Segementcode = purchase.Segementcode;
					order.companycode = companydetails.companycode;
					_context.PurchaseOrder.Update(order);
					_context.SaveChanges();
				}

				//path
				//string folderPath = @"C:\SAPFILE";
				string filename = Request.HttpContext.Session.GetString("UserName") + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss").Replace("/", "_").Replace(":", "_").Replace(" ", "_") + ".txt";
				// Set the content type and headers for the response
				Response.Headers.Add("Content-Disposition", $"attachment; filename={filename}");
				Response.ContentType = "text/plain";

				// Convert the content to bytes and send it to the response stream
				byte[] contentBytes = Encoding.UTF8.GetBytes(fileContent);
				var fileResult = File(contentBytes, "text/plain");

				// Store a flag in TempData to indicate download initiation
				TempData["DownloadInitiated"] = true;
				_notyfService.Success("File Generated SuccessFully");
				// Return the file result
				return fileResult;
			}
			else
			{
				_notyfService.Warning("Select The Row");
				return RedirectToAction("Index");
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

		public async Task<IActionResult> Update(int id)
		{



			if (id == null || _context.DepartmentMaster == null)
			{
				return NotFound();
			}

			var DepartmentMaster = await _context.PurchaseOrder.Where(a => a.Id == id).Include(d => d.ProductDetails).FirstOrDefaultAsync();
			if (DepartmentMaster == null)
			{
				return NotFound();
			}
			return View(DepartmentMaster);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Update(int id, PurchaseOrder purchaseOrder)
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
				purchaseOrder.verifyflag = 1;
				_context.Attach(purchaseOrder);
				_context.Entry(purchaseOrder).State = EntityState.Modified;
				_context.ProductDetails.AddRange(purchaseOrder.ProductDetails);
				_context.SaveChanges();
			}
			catch (DbUpdateConcurrencyException)
			{

			}
			var company = _context.CompanyMaster.Where(a => a.ShortName == purchaseOrder.companycode).FirstOrDefault();
			return ActionName(purchaseOrder.Segementname, company.Name, purchaseOrder.OrderDate, purchaseOrder.OrderDate, "unprocess");

		}

		public async Task<IActionResult> View(int id)
		{



			if (id == null || _context.DepartmentMaster == null)
			{
				return NotFound();
			}

			var DepartmentMaster = await _context.PurchaseOrder.Where(a => a.Id == id).Include(d => d.ProductDetails).FirstOrDefaultAsync();
			if (DepartmentMaster == null)
			{
				return NotFound();
			}
			return View(DepartmentMaster);
		}

	}
}
