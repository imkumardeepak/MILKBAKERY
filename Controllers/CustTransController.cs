using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Milk_Bakery.Data;
using Milk_Bakery.Models;
using System.Data;

namespace Milk_Bakery.Controllers
{
    [Authentication]
    public class CustTransController : Controller
    {
        private readonly MilkDbContext _context;
        private Microsoft.AspNetCore.Hosting.IHostingEnvironment Environment;
        public INotyfService _notifyService { get; }

        public CustTransController(MilkDbContext context, INotyfService notyf, Microsoft.AspNetCore.Hosting.IHostingEnvironment _environment)
        {
            _context = context;
            _notifyService = notyf;
            Environment = _environment;
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Index(IFormFile file)
        {
            DataTable dt = new DataTable();
            if (file == null || file.Length <= 0)
            {
                _notifyService.Error("Please Select txt file");
                return RedirectToAction(nameof(Index));
            }
            else
            {
                string fileExtension = Path.GetExtension(file.FileName).ToLower();
                if (fileExtension != ".txt")
                {
                    _notifyService.Error("Please upload a valid txt file");
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    string path = Path.Combine(this.Environment.WebRootPath, "Uploads");
                    if (Directory.Exists(path))
                    {
                        Directory.Delete(path, true);
                    }
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);

                    }
                    string fileName = Path.GetFileName(file.FileName);
                    string filePath = Path.Combine(path, fileName);

                    using (FileStream stream = new FileStream(filePath, FileMode.Create))
                    {
                        file.CopyTo(stream);
                    }
                    string csvData = System.IO.File.ReadAllText(filePath);
                    bool firstRow = true;
                    foreach (string row in csvData.Split("\r\n"))
                    {
                        if (!string.IsNullOrEmpty(row))
                        {
                            if (!string.IsNullOrEmpty(row))
                            {
                                if (firstRow)
                                {
                                    dt.Columns.Add("Companycode", typeof(string));
                                    dt.Columns.Add("CustomerCode", typeof(string));
                                    dt.Columns.Add("Date", typeof(string));
                                    dt.Columns.Add("Outstanding", typeof(double));
                                    dt.Columns.Add("Invoice", typeof(double));
                                    dt.Columns.Add("Paid", typeof(double));
                                    firstRow = false;
                                    dt.Rows.Add();
                                    int i = 0;
                                    foreach (string cell in row.Split('^'))
                                    {
                                        dt.Rows[dt.Rows.Count - 1][i] = cell.Trim();
                                        i++;
                                    }
                                }
                                else
                                {
                                    dt.Rows.Add();
                                    int i = 0;
                                    foreach (string cell in row.Split('^'))
                                    {
                                        dt.Rows[dt.Rows.Count - 1][i] = cell.Trim();
                                        i++;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return View(dt);
        }




        public IActionResult InsertProducts()
        {
            string folderPath = Path.Combine(Environment.WebRootPath, "Uploads");

            if (Directory.Exists(folderPath))
            {
                string[] files = Directory.GetFiles(folderPath);
                string fileName = Path.GetFileName(files[0]);
                string fileContent = System.IO.File.ReadAllText(files[0]);
                List<CustTransaction> transactions = new List<CustTransaction>();
                List<CustTransaction> existingData = new List<CustTransaction>();
                using (var reader = new StreamReader(files[0]))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        // Split the line into fields (assuming fields are separated by a specific delimiter, e.g., tab or comma)
                        string[] fields = line.Split('^'); // Adjust the delimiter as per your file format

                        var company = _context.Company_SegementMap.Where(a => a.companycode == fields[0].Trim()).FirstOrDefault();
                        var customer = _context.CustomerSegementMap.Where(a => a.custsegementcode == fields[1].Trim()).FirstOrDefault();

                        if (company != null && customer != null)
                        {
                            // Create an Employee object from the fields
                            CustTransaction employee = new CustTransaction
                            {
                                cmpcode = fields[0].Trim(),
                                partycode = fields[1].Trim(),
                                edate = DateTime.Parse(fields[2].Trim()),
                                outstandingampunt = Convert.ToDecimal(fields[3].Trim()),
                                invoiceamount = Convert.ToDecimal(fields[4].Trim()),
                                recipectamount = Convert.ToDecimal(fields[5].Trim()),
                                lastupdate = DateTime.Now,
                                cmpname = company.Companyname,
                                customername = customer.Customername,

                            };
                            transactions.Add(employee);
                            var find = _context.custTransactions.Where(item => item.edate.Date == DateTime.Parse(fields[2].Trim()).Date && item.customername == customer.Customername).FirstOrDefault();
                            if (find != null)
                            {
                                _context.custTransactions.Remove(find);
                                _context.SaveChanges();
                                //existingData.Add(find);
                            }
                        }
                    }
                }

                // Delete the previous records for today's date if any exist.
                //if (existingData.Count > 0)
                //{
                //    _context.custTransactions.RemoveRange(existingData);
                //    _context.SaveChanges();
                //}
                _context.custTransactions.AddRange(transactions);
                _context.SaveChanges();
                _notifyService.Success("Successfully Upload");
                return RedirectToAction(nameof(Index));
            }
            else
            {
                _notifyService.Error("error Upload");
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
