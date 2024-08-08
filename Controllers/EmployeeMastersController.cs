using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using Microsoft.EntityFrameworkCore;
using Milk_Bakery.Data;
using Milk_Bakery.Models;
using CsvHelper;
using CsvHelper.Configuration;

namespace Milk_Bakery.Controllers
{
    [Authentication]
    public class EmployeeMastersController : Controller
    {
        private readonly MilkDbContext _context;
        public INotyfService _notifyService { get; }

        public EmployeeMastersController(MilkDbContext context, INotyfService notifyService)
        {
            _context = context;
            _notifyService = notifyService;
        }
        public async Task<IActionResult> Index()
        {
            return _context.EmployeeMaster != null ?
                          View(await _context.EmployeeMaster.ToListAsync()) :
                          Problem("Entity set 'MilkDbContext.EmployeeMaster'  is null.");
        }
        public async Task<IActionResult> AddOrEdit(int id = 0)
        {
            ViewBag.route = GetRoute();
            ViewBag.Grade = GetGrade();
            ViewBag.desgination = Getdesignation();
            ViewBag.depart = Getdepartment();
            ViewBag.Segement = GetSegement();
            if (id == 0)
                return View();
            else
            {
                if (id == null || _context.EmployeeMaster == null)
                {
                    return NotFound();
                }

                var EmployeeMaster = await _context.EmployeeMaster.FindAsync(id);
                if (EmployeeMaster == null)
                {
                    return NotFound();
                }
                return View(EmployeeMaster);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOrEdit(int id, EmployeeMaster EmployeeMaster)
        {
            //insert
            if (id == 0)
            {
                if (ModelState.IsValid)
                {
                    var validate = _context.EmployeeMaster.Where(a => a.FirstName == EmployeeMaster.FirstName).FirstOrDefault();
                    if (validate != null)
                    {
                        _notifyService.Error("Already Added In Database");
                    }
                    else
                    {
                        _context.Add(EmployeeMaster);
                        await _context.SaveChangesAsync();
                        var user = new User();
                        user.phoneno = EmployeeMaster.PhoneNumber;
                        user.Password = "1234";
                        user.Role = EmployeeMaster.UserType;
                        user.name = EmployeeMaster.FirstName;
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
                    var empl = _context.EmployeeMaster.Where(x => x.Id == id).AsNoTracking().FirstOrDefault();
                    var userdetails = _context.Users.Where(a => a.phoneno == empl.PhoneNumber).AsNoTracking().FirstOrDefault();
                    _context.Update(EmployeeMaster);
                    await _context.SaveChangesAsync();
                    var user = new User();
                    user.Id = userdetails.Id;
                    user.phoneno = EmployeeMaster.PhoneNumber;
                    user.Password = userdetails.Password;
                    user.Role = EmployeeMaster.UserType;
                    user.name = EmployeeMaster.FirstName;
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
            ViewBag.Grade = GetGrade();
            ViewBag.desgination = Getdesignation();
            ViewBag.depart = Getdepartment();
            ViewBag.Segement = GetSegement();
            return View(EmployeeMaster);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (_context.EmployeeMaster == null)
            {
                return Problem("Entity set 'MilkDbContext.EmployeeMaster'  is null.");
            }
            var EmployeeMaster = await _context.EmployeeMaster.FindAsync(id);
            if (EmployeeMaster != null)
            {
                _context.EmployeeMaster.Remove(EmployeeMaster);
            }
            await _context.SaveChangesAsync();  
            _notifyService.Success("Record Delete sucessfully");
            return RedirectToAction(nameof(Index));
        }

        private bool EmployeeMasterExists(int id)
        {
            return (_context.EmployeeMaster?.Any(e => e.Id == id)).GetValueOrDefault();
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
        private List<SelectListItem> GetGrade()
        {
            var lstProducts = new List<SelectListItem>();

            lstProducts = _context.GradeMaster.AsNoTracking().Select(n =>
            new SelectListItem
            {
                Value = n.Grade_Name,
                Text = n.Grade_Name
            }).ToList();

            var defItem = new SelectListItem()
            {
                Value = "",
                Text = "----Select Grade----"
            };

            lstProducts.Insert(0, defItem);

            return lstProducts;
        }
        private List<SelectListItem> Getdesignation()
        {
            var lstProducts = new List<SelectListItem>();

            lstProducts = _context.DesignationMaster.AsNoTracking().Select(n =>
            new SelectListItem
            {
                Value = n.Designation_Name,
                Text = n.Designation_Name
            }).ToList();

            var defItem = new SelectListItem()
            {
                Value = "",
                Text = "----Select Designation----"
            };

            lstProducts.Insert(0, defItem);

            return lstProducts;
        }
        private List<SelectListItem> Getdepartment()
        {
            var lstProducts = new List<SelectListItem>();

            lstProducts = _context.DepartmentMaster.AsNoTracking().Select(n =>
            new SelectListItem
            {
                Value = n.Department_Name,
                Text = n.Department_Name
            }).ToList();

            var defItem = new SelectListItem()
            {
                Value = "",
                Text = "----Select Department----"
            };

            lstProducts.Insert(0, defItem);

            return lstProducts;
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
        [HttpGet]
        public IActionResult Upload()
        {
            return View();
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

                var records = new List<EmployeeMaster>();
                while (csv.Read())
                {
                    var person = csv.GetRecord<EmployeeMaster>();
                    var user = new User();
                    user.phoneno = person.PhoneNumber;
                    user.Password = "1234";
                    user.Role = person.UserType;
                    user.Id = 0;
                    user.name = person.FirstName;
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
