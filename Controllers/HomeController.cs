using AspNetCoreHero.ToastNotification.Abstractions;
using Humanizer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Milk_Bakery.Data;
using Milk_Bakery.Models;
using Newtonsoft.Json;
using System.Data;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace Milk_Bakery.Controllers
{

	public class HomeController : Controller
	{
		private readonly ILogger<HomeController> _logger;
		private readonly MilkDbContext _context;
		public INotyfService _notifyService { get; }

		public HomeController(ILogger<HomeController> logger, MilkDbContext milkDb, INotyfService notyf)
		{
			_logger = logger;
			_context = milkDb;
			_notifyService = notyf;
		}

		[Authentication]
		public async Task<IActionResult> Index()
		{
			try
			{
				ViewBag.pendingverify = _context.PurchaseOrder.Where(a => a.verifyflag == 0 && a.processflag == 0).ToList().Count();
				ViewBag.pendingprocess = _context.PurchaseOrder.Where(a => a.verifyflag == 1 && a.processflag == 0).ToList().Count();
				ViewBag.totalprocess = _context.PurchaseOrder.Where(a => a.verifyflag == 1 && a.processflag == 1).ToList().Count();
				ViewBag.totalmoney = "₹" + _context.ProductDetails.Sum(a => a.Price);
				var purchase = await _context.PurchaseOrder.AsNoTracking().OrderByDescending(a => a.Id).Take(100).ToListAsync();
				return View(purchase);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
				return View();
			}


		}

		public IActionResult Privacy()
		{
			return View();
		}

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}
		// Get Action
		public IActionResult Login()
		{
			if (HttpContext.Session.GetString("UserName") == null)
			{
				return View();
			}
			else
			{
				return RedirectToAction("Index");
			}
		}
		[Authentication]
		public IActionResult ChangePassword()
		{
			if (HttpContext.Session.GetString("UserName") != null)
			{
				var data = new User();
				data.phoneno = ViewBag.phoneno;
				return View(data);
			}
			else
			{
				return RedirectToAction("Index");
			}
		}

		public IActionResult change(User user)
		{
			if (HttpContext.Session.GetString("UserName") != null)
			{
				var data = _context.Users.Where(a => a.phoneno == user.phoneno).AsNoTracking().FirstOrDefault();
				if (data != null)
				{
					var modify = new User();
					modify.Id = data.Id;
					modify.Role = data.Role;
					modify.phoneno = user.phoneno;
					modify.Password = user.Password;
					modify.name = data.name;
					_context.Update(modify);
					_context.SaveChanges();
					_notifyService.Success("Password Update Successfully");
					return RedirectToAction("Index");

				}
			}
			else
			{
				return RedirectToAction("Index");
			}
			return RedirectToAction("Index");
		}
		//Post Action
		[HttpPost]
		public ActionResult Login(User u)
		{
			if (HttpContext.Session.GetString("UserName") == null)
			{

				var obj = _context.Users.Where(a => a.phoneno.Equals(u.phoneno) && a.Password.Equals(u.Password)).FirstOrDefault();
				if (obj != null)
				{
					HttpContext.Session.SetString("UserName", obj.phoneno.ToString());
					HttpContext.Session.SetString("role", obj.Role.ToString());
					HttpContext.Session.SetString("name", obj.name.ToString());
					_notifyService.Success("Login Success");
					return RedirectToAction("Index");
				}
			}
			else
			{
				_notifyService.Error("Login Failed");
				return RedirectToAction("Login");
			}
			return View();
		}
		public ActionResult Logout()
		{
			HttpContext.Session.Clear();
			HttpContext.Session.Remove("UserName");
			return RedirectToAction("Login");
		}

		[HttpPost]
		public JsonResult NewChart()
		{
			DateTime startDate = DateTime.Now.Date.AddDays(-7); // Get the start date 7 days ago
			DateTime endDate = DateTime.Now.Date; // Get today's date

			var query = from order in _context.PurchaseOrder
						join customer in _context.ProductDetails on order.Id equals customer.PurchaseOrderId
						select new
						{
							orderdate = order.OrderDate,
							total = customer.Price,


						};

			var sumByDay = query.Where(o => o.orderdate >= startDate && o.orderdate <= endDate).GroupBy(o => o.orderdate.Date)
								   .Select(g => new
								   {
									   OrderDate = g.Key,
									   TotalSum = g.Sum(o => o.total)
								   }).OrderBy(o => o.OrderDate.Date).ToList();



			List<object> iData = new List<object>();
			//Creating sample data  
			DataTable dt = new DataTable();
			dt.Columns.Add("Date", System.Type.GetType("System.String"));
			dt.Columns.Add("Amount", System.Type.GetType("System.Double"));

			foreach (var item in sumByDay)
			{
				DataRow dr = dt.NewRow();
				dr["Date"] = item.OrderDate.ToShortDateString();
				dr["Amount"] = item.TotalSum;
				dt.Rows.Add(dr);
			}

			//Looping and extracting each DataColumn to List<Object>  
			foreach (DataColumn dc in dt.Columns)
			{
				List<object> x = new List<object>();
				x = (from DataRow drr in dt.Rows select drr[dc.ColumnName]).ToList();
				iData.Add(x);
			}
			//Source data returned as JSON  
			return Json(iData);
		}
	}
}