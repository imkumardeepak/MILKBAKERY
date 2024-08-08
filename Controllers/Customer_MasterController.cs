using AspNetCoreHero.ToastNotification.Abstractions;
using CsvHelper.Configuration;
using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Milk_Bakery.Data;
using Milk_Bakery.Models;
using System.Globalization;

namespace Milk_Bakery.Controllers
{
    [Authentication]
    public class Customer_MasterController : Controller
    {
        private readonly MilkDbContext _context;
        public INotyfService _notifyService { get; }

        public Customer_MasterController(MilkDbContext context, INotyfService notifyService)
        {
            _context = context;
            _notifyService = notifyService;
        }
        public async Task<IActionResult> Index()
        {
            return _context.Customer_Master != null ?
                          View(await _context.Customer_Master.ToListAsync()) :
                          Problem("Entity set 'MilkDbContext.Customer_Master'  is null.");
        }
        public async Task<IActionResult> AddOrEdit(int id = 0)
        {
            ViewBag.route = GetRoute();
            ViewBag.segement = GetSegement();
            if (id == 0)
            {
                Customer_Master customer = new Customer_Master();
                customer.Id = id;
                return View(customer);
            }
            else
            {
                if (id == null || _context.Customer_Master == null)
                {
                    return NotFound();
                }

                var customer_Master = await _context.Customer_Master.FindAsync(id);
                if (customer_Master == null)
                {
                    return NotFound();
                }
                return View(customer_Master);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOrEdit(int id, Customer_Master customer_Master)
        {

            //insert
            if (id == 0)
            {
                if (ModelState.IsValid)
                {
                    var validate = _context.Customer_Master.Where(a => a.Name == customer_Master.Name).FirstOrDefault();
                    if (validate != null)
                    {
                        _notifyService.Error("Already Added In Database");
                    }
                    else
                    {
                        _context.Add(customer_Master);
                        await _context.SaveChangesAsync();
                        var user = new User();
                        user.phoneno = customer_Master.phoneno;
                        user.Password = "1234";
                        user.Role = "Customer";
                        user.name = customer_Master.Name;
                        _context.Add(user);
                        await _context.SaveChangesAsync();
                        _notifyService.Success("Record saved sucessfully");
                        return RedirectToAction(nameof(Index));
                    }
                }
                else
                {
                    _notifyService.Error("Modal State Is InValid");
                }
            }
            else
            {
                //update
                if (ModelState.IsValid)
                {
                    var customer = _context.Customer_Master.Where(x => x.Id == id).AsNoTracking().FirstOrDefault();
                    var userdetails = _context.Users.Where(a => a.phoneno == customer.phoneno).AsNoTracking().FirstOrDefault();
                    _context.Update(customer_Master);
                    await _context.SaveChangesAsync();
                    var user = new User();
                    user.Id = userdetails.Id;
                    user.phoneno = customer_Master.phoneno;
                    user.Password = userdetails.Password;
                    user.Role = "Customer";
                    user.name = customer_Master.Name;
                    _context.Update(user);
                    await _context.SaveChangesAsync();
                    _notifyService.Success("Record Update sucessfully");
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    _notifyService.Error("Modal State Is InValid");
                }
            }
            ViewBag.route = GetRoute();
            ViewBag.segement = GetSegement();
            return View(customer_Master);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (_context.Customer_Master == null)
            {
                return Problem("Entity set 'MilkDbContext.Customer_Master'  is null.");
            }
            var customer_Master = await _context.Customer_Master.FindAsync(id);
            if (customer_Master != null)
            {
                _context.Customer_Master.Remove(customer_Master);
            }

            await _context.SaveChangesAsync();
            _notifyService.Success("Record Delete sucessfully");
            return RedirectToAction(nameof(Index));
        }

        private bool Customer_MasterExists(int id)
        {
            return (_context.Customer_Master?.Any(e => e.Id == id)).GetValueOrDefault();
        }
        private List<SelectListItem> GetRoute()
        {
            var lstProducts = new List<SelectListItem>();

            lstProducts = _context.RouteMaster.AsNoTracking().Select(n =>
            new SelectListItem
            {
                Value = n.Route,
                Text = n.ShortCode + " " + n.Route
            }).ToList();

            var defItem = new SelectListItem()
            {
                Value = "",
                Text = "----Select Route----"
            };

            lstProducts.Insert(0, defItem);

            return lstProducts;
        }
        [HttpGet]
        public IActionResult Upload()
        {
            return View();
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
                Text = "----Select Division----"
            };

            lstProducts.Insert(0, defItem);

            return lstProducts;
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile csvFile)
        {
            if (csvFile == null || csvFile.Length <= 0)
            {
                ModelState.AddModelError("csvFile", "Please select a CSV file to upload.");
                return View();
            }
            if (!csvFile.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("csvFile", "Only CSV files are allowed.");
                return View();
            }
            var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true, // Set this to 'true' if your CSV file has a header row, 'false' if not.
                MissingFieldFound = null
            };
            using (var reader = new StreamReader(csvFile.OpenReadStream()))
            using (var csv = new CsvHelper.CsvReader(reader, csvConfig))
            {
                csv.Read();
                csv.ReadHeader();

                var records = new List<Customer_Master>();
                User user = new User();
                while (csv.Read())
                {
                    var person = csv.GetRecord<Customer_Master>();
                    user.phoneno = person.phoneno;
                    user.Id = 0;
                    user.Role = "Customer";
                    user.Password = "1234";
                    user.name = person.Name;
                    _context.AddAsync(user);
                    await _context.SaveChangesAsync();
                    records.Add(person);

                }
                _context.AddRange(records);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index"); // Redirect to a success page or another view
        }



    }
}
