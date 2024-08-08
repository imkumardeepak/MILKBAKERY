using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Milk_Bakery.Data;
using Milk_Bakery.Models;

namespace Milk_Bakery.Controllers
{
    [Authentication]
    public class VisitReportController : Controller
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly MilkDbContext _context;


        public VisitReportController(IWebHostEnvironment webHostEnvironment, MilkDbContext milkDbContext)
        {
            _webHostEnvironment = webHostEnvironment;
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            _context = milkDbContext;
        }
        public IActionResult Index()
        {

            ViewBag.customer = GetCustomer();
            return View();
        }
        private List<SelectListItem> GetCustomer()
        {
            var lstProducts = new List<SelectListItem>();
            if (HttpContext.Session.GetString("role") == "Manager")
            {
                var segement = _context.EmployeeMaster.Where(a => a.PhoneNumber == HttpContext.Session.GetString("UserName")).FirstOrDefault();
                lstProducts = _context.EmployeeMaster.Where(a => a.UserType == "Sales" && a.Segment == segement.Segment).AsNoTracking().Select(n =>
          new SelectListItem
          {
              Value = n.FirstName,
              Text = n.FirstName
          }).ToList();

                var defItem = new SelectListItem()
                {
                    Value = "",
                    Text = "--Select--"
                };

                lstProducts.Insert(0, defItem);

                return lstProducts;
            }
            else
            {
                lstProducts = _context.EmployeeMaster.Where(a => a.UserType == "Sales").AsNoTracking().Select(n =>
          new SelectListItem
          {
              Value = n.FirstName,
              Text = n.FirstName
          }).ToList();

                var defItem = new SelectListItem()
                {
                    Value = "",
                    Text = "--Select--"
                };

                lstProducts.Insert(0, defItem);

                return lstProducts;
            }

        }
        [HttpPost]
        public async Task<IActionResult> Report(string salesname, DateTime dateInput)
        {
            if (salesname == null)
            {
                return View();
            }
            else
            {
                List<VisitEntery> models = _context.VisitEntery.Where(a => a.Salesname == salesname && a.dateTime.Date >= dateInput.Date && a.dateTime.Date <= dateInput.Date).OrderBy(a => a.dateTime).ToList();
                return PartialView("VisitReportView", models);
            }

        }


    }
}
