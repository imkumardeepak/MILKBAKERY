using AspNetCore.Reporting;
using AspNetCore.Reporting.ReportExecutionService;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Milk_Bakery.Data;
using Milk_Bakery.Models;
using System.Collections.Immutable;
using System.Data;
using static System.Runtime.InteropServices.JavaScript.JSType;
using DataTable = System.Data.DataTable;

namespace Milk_Bakery.Controllers
{
    public class RouteReportController : Controller
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly MilkDbContext _context;


        public RouteReportController(IWebHostEnvironment webHostEnvironment, MilkDbContext milkDbContext)
        {
            _webHostEnvironment = webHostEnvironment;
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            _context = milkDbContext;
        }
        public IActionResult Index()
        {
            ViewBag.segemnt = GetSegement();
            ViewBag.customer = GetCustomer();
            return View();
        }



        [HttpPost]
        public async Task<IActionResult> ActionNameAsync(string Customer, string Segement, DateTime FromDate, DateTime ToDate)
        {
            if (Customer == "All Route")
            {
                try
                {

                    List<string> materialShortNames = await _context.MaterialMaster.Where(a => !a.Materialname.Contains("CRATES FOR") && a.segementname == Segement).Select(a => a.ShortName).Distinct().ToListAsync();


                    // Step 2: Combine data into a single dataset
                    DataTable finaldata = new DataTable();
                    finaldata.Columns.Add("Srno");

                    finaldata.Columns.Add("CustomerName");
                    foreach (var mat in materialShortNames)
                    {
                        finaldata.Columns.Add(mat);
                    }
                    finaldata.Columns.Add("Total", typeof(int));


                    //temp dt  
                    DataTable combinedDataTable = new DataTable();

                    combinedDataTable.Columns.Add("Srno");

                    combinedDataTable.Columns.Add("CustomerName");
                    foreach (var mat in materialShortNames)
                    {
                        combinedDataTable.Columns.Add(mat);
                    }
                    combinedDataTable.Columns.Add("Total", typeof(int));

                    List<RouteMaster> routeMaster = _context.RouteMaster.Distinct().OrderBy(a => a.ShortCode).ToList();


                    //each route
                    foreach (var route in routeMaster)
                    {
                        //find route customer
                        List<Customer_Master> routecustomer = await _context.Customer_Master.Where(a => a.route == route.Route).Distinct().ToListAsync();

                        List<PurchaseOrder> purchaseOrdersInRouteCustomers = await _context.PurchaseOrder
                            .Where(po =>
                                po.OrderDate.Date >= FromDate.Date && po.OrderDate.Date <= ToDate.Date && po.Segementname == Segement).Include(a => a.ProductDetails)
                            .ToListAsync();

                        // Perform in-memory comparison using LINQ to Objects
                        List<string> distinctCustomers = purchaseOrdersInRouteCustomers.Where(po => routecustomer.Any(rc => rc.Name == po.Customername)).Select(po => po.Customername).Distinct().OrderBy(customerName => customerName).ToList();


                        if (distinctCustomers.Count > 0)
                        {
                            DataRow row2 = combinedDataTable.NewRow();
                            row2["CustomerName"] = route.ShortCode + "-" + route.Route.ToUpper();
                            combinedDataTable.Rows.Add(row2);


                            Int32 i = 1;
                            foreach (var purchaseOrder in distinctCustomers)
                            {
                                var query = from c in _context.PurchaseOrder
                                            join m in _context.ProductDetails on c.Id equals m.PurchaseOrderId
                                            where c.Customername == purchaseOrder && c.OrderDate >= FromDate.Date &&
                  c.OrderDate <= ToDate.Date
                                            group new { c, m } by new { c.Customername, m.ProductName } into grouped
                                            select new
                                            {
                                                Customername = grouped.Key.Customername,
                                                ProductName = grouped.Key.ProductName,
                                                TotalQuantity = grouped.Sum(x => x.m.qty)
                                            };

                                var result = query.ToList();

                                DataRow row = combinedDataTable.NewRow();

                                row["Srno"] = i;

                                row["CustomerName"] = purchaseOrder;
                                Int32 sum = 0;
                                foreach (var purchaseDetail in result)
                                {
                                    var mat = _context.MaterialMaster.Where(p => p.Materialname == purchaseDetail.ProductName).FirstOrDefault();
                                    if (mat != null)
                                    {
                                        row[mat.ShortName] = purchaseDetail.TotalQuantity;
                                        sum = sum + purchaseDetail.TotalQuantity;
                                    }



                                }
                                row["Total"] = sum;
                                combinedDataTable.Rows.Add(row);
                                i++;
                            }

                            DataRow row1 = combinedDataTable.NewRow();
                            row1["CustomerName"] = "Routewise Total";
                            foreach (DataColumn column in combinedDataTable.Columns)
                            {
                                int? sum = 0;
                                if (column.ColumnName != "Srno" && column.ColumnName != "CustomerName" && column.ColumnName != "Routewise Total")
                                {
                                    sum = CalculateColumnSum(combinedDataTable, column.ColumnName);
                                    row1[column.ColumnName] = sum;
                                }

                            }
                            combinedDataTable.Rows.Add(row1);

                            finaldata.Merge(combinedDataTable);

                            combinedDataTable.Rows.Clear();
                        }


                    }



                    if (finaldata.Rows.Count > 0)
                    {
                        DataRow row4 = finaldata.NewRow();


                        foreach (DataRow row in finaldata.Rows)
                        {
                            int? routewiseTotalSum = 0;
                            if (row["CustomerName"].ToString() == "Routewise Total")
                            {

                                foreach (DataColumn column in finaldata.Columns)
                                {

                                    if (column.ColumnName != "Srno" && column.ColumnName != "CustomerName" && column.ColumnName != "Routewise Total")
                                    {
                                        routewiseTotalSum = finaldata.AsEnumerable()
   .Where(row => row.Field<string>("CustomerName") != "Routewise Total")
   .Sum(row => ConvertToInt(row.Field<object>(column)));
                                        row4[column.ColumnName] = routewiseTotalSum;

                                    }

                                }
                            }
                        }
                        row4["Srno"] = null;
                        row4["CustomerName"] = "OverAll Total";
                        finaldata.Rows.Add(row4);
                        return PartialView("ReportView", finaldata);
                    }
                    else
                    {
                        return View();
                    }
                }
                catch (Exception ex)
                {
                    return Ok(ex.Message);
                }
            }
            else
            {
                //single route
                try
                {

                    List<string> materialShortNames = await _context.MaterialMaster
          .Where(a => !a.Materialname.Contains("CRATES FOR") && a.segementname == Segement)
          .Select(a => a.ShortName)
          .Distinct()
          .ToListAsync();

                    List<Customer_Master> routecustomer = await _context.Customer_Master
          .Where(a => a.route == Customer)
          .Distinct()
          .ToListAsync();

                    List<PurchaseOrder> purchaseOrdersInRouteCustomers = await _context.PurchaseOrder
                        .Where(po =>
                            po.OrderDate.Date >= FromDate.Date && po.OrderDate.Date <= ToDate.Date && po.Segementname == Segement).Include(a => a.ProductDetails)
                        .ToListAsync();

                    // Perform in-memory comparison using LINQ to Objects
                    List<string> distinctCustomers = purchaseOrdersInRouteCustomers.Where(po => routecustomer.Any(rc => rc.Name == po.Customername)).Select(po => po.Customername).Distinct().OrderBy(customerName => customerName).ToList();


                    // Step 2: Combine data into a single dataset
                    DataTable combinedDataTable = new DataTable();

                    combinedDataTable.Columns.Add("Srno");

                    combinedDataTable.Columns.Add("CustomerName");
                    foreach (var mat in materialShortNames)
                    {
                        combinedDataTable.Columns.Add(mat);
                    }
                    combinedDataTable.Columns.Add("Total");

                    DataRow row2 = combinedDataTable.NewRow();
                    row2["CustomerName"] = Customer.ToUpper();
                    combinedDataTable.Rows.Add(row2);

                    Int32 i = 1;
                    foreach (var purchaseOrder in distinctCustomers)
                    {
                        var query = from c in _context.PurchaseOrder
                                    join m in _context.ProductDetails on c.Id equals m.PurchaseOrderId
                                    where c.Customername == purchaseOrder && c.OrderDate >= FromDate.Date &&
                                    c.OrderDate <= ToDate.Date
                                    group new { c, m } by new { c.Customername, m.ProductName } into grouped
                                    select new
                                    {
                                        Customername = grouped.Key.Customername,
                                        ProductName = grouped.Key.ProductName,
                                        TotalQuantity = grouped.Sum(x => x.m.qty)
                                    };

                        var result = query.ToList();
                        DataRow row = combinedDataTable.NewRow();

                        row["Srno"] = i;

                        row["CustomerName"] = purchaseOrder;
                        Int32 sum = 0;
                        foreach (var purchaseDetail in result)
                        {
                            var mat = _context.MaterialMaster.Where(p => p.Materialname == purchaseDetail.ProductName).FirstOrDefault();
                            row[mat.ShortName] = purchaseDetail.TotalQuantity;
                            sum = sum + purchaseDetail.TotalQuantity;

                        }
                        row["Total"] = sum;
                        combinedDataTable.Rows.Add(row);
                        i++;
                    }
                    DataRow row1 = combinedDataTable.NewRow();
                    row1["CustomerName"] = "Routewise Total";
                    foreach (DataColumn column in combinedDataTable.Columns)
                    {
                        if (column.ColumnName != "Srno" && column.ColumnName != "CustomerName")
                        {
                            decimal sum = 0;
                            foreach (DataRow row in combinedDataTable.Rows)
                            {
                                if (row[column] != DBNull.Value)
                                {
                                    sum += Convert.ToDecimal(row[column]);
                                }
                            }
                            row1[column.ColumnName] = sum;
                        }
                    }
                    combinedDataTable.Rows.Add(row1);


                    if (combinedDataTable.Rows.Count > 0)
                    {

                        return PartialView("ReportView", combinedDataTable);
                    }
                    else
                    {
                        return View();
                    }
                }
                catch (Exception ex)
                {
                    return View();
                }
            }



        }
        private static int ConvertToInt(object value)
        {
            if (value == DBNull.Value || value == null)
                return 0;

            if (value is int intValue)
                return intValue;

            if (int.TryParse(value.ToString(), out int parsedValue))
                return parsedValue;
            return 0;
        }
        static int? CalculateColumnSum(DataTable dataTable, string columnName)
        {
            int? sum = dataTable
                .AsEnumerable()
                .Where(row => row[columnName] != DBNull.Value) // Exclude DBNull values
               .Sum(row => Convert.ToInt32(row[columnName])); // Use nullable type for the sum

            return sum;
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

            var lstProducts = new List<SelectListItem>();

            lstProducts = _context.RouteMaster.AsNoTracking().Select(n =>
            new SelectListItem
            {
                Value = n.Route,
                Text = n.Route
            }).ToList();

            var defItem = new SelectListItem()
            {
                Value = "All Route",
                Text = "All Route"
            };

            lstProducts.Insert(0, defItem);

            return lstProducts;

        }


        [HttpPost]
        public async Task<IActionResult> Export(string Customer, string Segement, DateTime FromDate, DateTime ToDate)
        {
            if (Customer == "All Route")
            {
                try
                {

                    List<string> materialShortNames = await _context.MaterialMaster.Where(a => !a.Materialname.Contains("CRATES FOR") && a.segementname == Segement).Select(a => a.ShortName).Distinct().ToListAsync();


                    // Step 2: Combine data into a single dataset
                    DataTable finaldata = new DataTable();
                    finaldata.Columns.Add("Srno");

                    finaldata.Columns.Add("CustomerName");
                    foreach (var mat in materialShortNames)
                    {
                        finaldata.Columns.Add(mat, typeof(int));
                    }
                    finaldata.Columns.Add("Total", typeof(int));


                    //temp dt  
                    DataTable combinedDataTable = new DataTable();

                    combinedDataTable.Columns.Add("Srno");

                    combinedDataTable.Columns.Add("CustomerName");
                    foreach (var mat in materialShortNames)
                    {
                        combinedDataTable.Columns.Add(mat, typeof(int));
                    }
                    combinedDataTable.Columns.Add("Total", typeof(int));

                    List<RouteMaster> routeMaster = _context.RouteMaster.Distinct().OrderBy(a => a.ShortCode).ToList();


                    //each route
                    foreach (var route in routeMaster)
                    {
                        //find route customer
                        List<Customer_Master> routecustomer = await _context.Customer_Master.Where(a => a.route == route.Route).Distinct().ToListAsync();

                        List<PurchaseOrder> purchaseOrdersInRouteCustomers = await _context.PurchaseOrder
                            .Where(po =>
                                po.OrderDate.Date >= FromDate.Date && po.OrderDate.Date <= ToDate.Date && po.Segementname == Segement).Include(a => a.ProductDetails)
                            .ToListAsync();

                        // Perform in-memory comparison using LINQ to Objects
                        List<string> distinctCustomers = purchaseOrdersInRouteCustomers.Where(po => routecustomer.Any(rc => rc.Name == po.Customername)).Select(po => po.Customername).Distinct().OrderBy(customerName => customerName).ToList();


                        if (distinctCustomers.Count > 0)
                        {
                            DataRow row2 = combinedDataTable.NewRow();
                            row2["CustomerName"] = route.ShortCode + "-" + route.Route.ToUpper();
                            combinedDataTable.Rows.Add(row2);


                            Int32 i = 1;
                            foreach (var purchaseOrder in distinctCustomers)
                            {
                                var query = from c in _context.PurchaseOrder
                                            join m in _context.ProductDetails on c.Id equals m.PurchaseOrderId
                                            where c.Customername == purchaseOrder && c.OrderDate >= FromDate.Date &&
                  c.OrderDate <= ToDate.Date
                                            group new { c, m } by new { c.Customername, m.ProductName } into grouped
                                            select new
                                            {
                                                Customername = grouped.Key.Customername,
                                                ProductName = grouped.Key.ProductName,
                                                TotalQuantity = grouped.Sum(x => x.m.qty)
                                            };

                                var result = query.ToList();

                                DataRow row = combinedDataTable.NewRow();

                                row["Srno"] = i;

                                row["CustomerName"] = purchaseOrder;
                                Int32 sum = 0;
                                foreach (var purchaseDetail in result)
                                {
                                    var mat = _context.MaterialMaster.Where(p => p.Materialname == purchaseDetail.ProductName).FirstOrDefault();
                                    if (mat != null)
                                    {
                                        row[mat.ShortName] = purchaseDetail.TotalQuantity;
                                        sum = sum + purchaseDetail.TotalQuantity;
                                    }


                                }
                                row["Total"] = sum;
                                combinedDataTable.Rows.Add(row);
                                i++;
                            }

                            DataRow row1 = combinedDataTable.NewRow();
                            row1["CustomerName"] = "Routewise Total";
                            foreach (DataColumn column in combinedDataTable.Columns)
                            {
                                int? sum = 0;
                                if (column.ColumnName != "Srno" && column.ColumnName != "CustomerName" && column.ColumnName != "Routewise Total")
                                {
                                    sum = CalculateColumnSum(combinedDataTable, column.ColumnName);
                                    row1[column.ColumnName] = Convert.ToInt32(sum);
                                }

                            }
                            combinedDataTable.Rows.Add(row1);

                            finaldata.Merge(combinedDataTable);

                            combinedDataTable.Rows.Clear();
                        }


                    }



                    if (finaldata.Rows.Count > 0)
                    {
                        DataRow row4 = finaldata.NewRow();


                        foreach (DataRow row in finaldata.Rows)
                        {
                            int? routewiseTotalSum = 0;
                            if (row["CustomerName"].ToString() == "Routewise Total")
                            {

                                foreach (DataColumn column in finaldata.Columns)
                                {

                                    if (column.ColumnName != "Srno" && column.ColumnName != "CustomerName" && column.ColumnName != "Routewise Total")
                                    {
                                        routewiseTotalSum = finaldata.AsEnumerable()
    .Where(row => row.Field<string>("CustomerName") != "Routewise Total")
    .Sum(row => ConvertToInt(row.Field<object>(column)));
                                        row4[column.ColumnName] = routewiseTotalSum;
                                    }

                                }
                            }
                        }
                        row4["Srno"] = null;
                        row4["CustomerName"] = "OverAll Total";
                        finaldata.Rows.Add(row4);


                        using (var workbook = new XLWorkbook())
                        {
                            var worksheet = workbook.Worksheets.Add("Sheet1");

                            int rowNumber = 1; // Change this to the row number you want
                            int columnNumber = 5; // Change this to the column number you want (1-based index)

                            // Access the cell
                            var titleCell = worksheet.Cell(rowNumber, columnNumber);

                            // Set the title text and formatting
                            titleCell.Value = "For The Period " + FromDate.Date.ToString("dd-MM-yyyy") + " To " + ToDate.Date.ToString("dd-MM-yyyy") + ""; // Replace with your desired title
                            titleCell.Style.Font.Bold = true;
                            titleCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; // Center align the title
                                                                                                       // titleCell.Style.Fill.BackgroundColor = XLColor.LightGreen; // Customize the background color if needed
                            titleCell.Style.Font.FontSize = 14; // Adjust font size if needed
                            titleCell.Style.Font.FontColor = XLColor.Black; // Set font color if needed

                            // Merge the cells for the title row
                            var lastColumn = worksheet.ColumnsUsed().Last().ColumnNumber(); // Find the last used column
                            var rangeToMerge = worksheet.Range(rowNumber, columnNumber, rowNumber, lastColumn); // Merge from column 1 to the last used column
                            rangeToMerge.Merge();

                            var headerRow = worksheet.Row(2);
                            for (int j = 0; j < finaldata.Columns.Count; j++)
                            {
                                headerRow.Cell(j + 1).Value = finaldata.Columns[j].ColumnName;

                                if (finaldata.Columns[j].ColumnName == "CustomerName")
                                {
                                    worksheet.Column(j + 1).Width = 20; // Adjust the width as needed
                                }
                                else
                                {
                                    worksheet.Column(j + 1).Width = 5; // Default width for other columns
                                }
                            }
                            headerRow.Style.Font.Bold = true;
                            headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;
                            headerRow.Style.Border.BottomBorder = XLBorderStyleValues.Thin; // Add bottom border to header row

                            // Insert a blank row after the header
                            // worksheet.Row(2).InsertRowsBelow(1);

                            // Populate the worksheet with data from the DataTable
                            for (int i = 0; i < finaldata.Rows.Count; i++)
                            {
                                var isRoutewiseTotal = finaldata.Rows[i]["CustomerName"].ToString() == "Routewise Total";
                                var isOverallTotal = finaldata.Rows[i]["CustomerName"].ToString() == "OverAll Total";
                                for (int j = 0; j < finaldata.Columns.Count; j++)
                                {
                                    object value = finaldata.Rows[i][j];
                                    var cell = worksheet.Cell(i + 3, j + 1);
                                    if (value != DBNull.Value)
                                    {
                                        // Convert the value to the appropriate type
                                        if (value is DateTime)
                                        {
                                            worksheet.Cell(i + 3, j + 1).Value = (DateTime)value;
                                        }
                                        else if (value is double)
                                        {
                                            worksheet.Cell(i + 3, j + 1).Value = (double)value;
                                        }
                                        else if (value is int)
                                        {
                                            worksheet.Cell(i + 3, j + 1).Value = (int)value;
                                        }
                                        else
                                        {
                                            worksheet.Cell(i + 3, j + 1).Value = value.ToString();
                                        }
                                        if (isRoutewiseTotal)
                                        {
                                            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                                        }
                                        else if (isOverallTotal)
                                        {
                                            cell.Style.Fill.BackgroundColor = XLColor.Khaki;
                                        }
                                        cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin; // Add outer border to each cell
                                        cell.Style.Alignment.WrapText = true; // Enable word wrap
                                    }
                                    // Apply borders to all cells
                                    var allCells = worksheet.RangeUsed();
                                    allCells.Style.Border.InsideBorder = XLBorderStyleValues.Thin; // Add inner borders to all cells

                                    // Apply border around the entire worksheet
                                    var entireRange = worksheet.Range(worksheet.FirstCellUsed(), worksheet.LastCellUsed());
                                    entireRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin; // Add outer border around the entire range

                                }
                            }

                            using (var memoryStream = new MemoryStream())
                            {
                                workbook.SaveAs(memoryStream);
                                memoryStream.Seek(0, SeekOrigin.Begin);

                                // Return the Excel content as a response
                                return File(memoryStream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "report.xlsx");
                            }
                        }

                    }
                    else
                    {
                        return View();
                    }
                }
                catch (Exception ex)
                {
                    return View();
                }
            }
            else
            {
                //single route
                try
                {

                    List<string> materialShortNames = await _context.MaterialMaster
          .Where(a => !a.Materialname.Contains("CRATES FOR") && a.segementname == Segement)
          .Select(a => a.ShortName)
          .Distinct()
          .ToListAsync();

                    List<Customer_Master> routecustomer = await _context.Customer_Master
          .Where(a => a.route == Customer)
          .Distinct()
          .ToListAsync();

                    List<PurchaseOrder> purchaseOrdersInRouteCustomers = await _context.PurchaseOrder
                        .Where(po =>
                            po.OrderDate.Date >= FromDate.Date && po.OrderDate.Date <= ToDate.Date && po.Segementname == Segement).Include(a => a.ProductDetails)
                        .ToListAsync();

                    // Perform in-memory comparison using LINQ to Objects
                    List<string> distinctCustomers = purchaseOrdersInRouteCustomers.Where(po => routecustomer.Any(rc => rc.Name == po.Customername)).Select(po => po.Customername).Distinct().OrderBy(customerName => customerName).ToList();

                    // Step 2: Combine data into a single dataset
                    DataTable combinedDataTable = new DataTable();

                    combinedDataTable.Columns.Add("Srno");

                    combinedDataTable.Columns.Add("CustomerName");
                    foreach (var mat in materialShortNames)
                    {
                        combinedDataTable.Columns.Add(mat, typeof(int));
                    }
                    combinedDataTable.Columns.Add("Total", typeof(int));

                    DataRow row2 = combinedDataTable.NewRow();
                    row2["CustomerName"] = Customer.ToUpper();
                    combinedDataTable.Rows.Add(row2);

                    Int32 i = 1;
                    foreach (var purchaseOrder in distinctCustomers)
                    {
                        var query = from c in _context.PurchaseOrder
                                    join m in _context.ProductDetails on c.Id equals m.PurchaseOrderId
                                    where c.Customername == purchaseOrder && c.OrderDate >= FromDate.Date &&
          c.OrderDate <= ToDate.Date
                                    group new { c, m } by new { c.Customername, m.ProductName } into grouped
                                    select new
                                    {
                                        Customername = grouped.Key.Customername,
                                        ProductName = grouped.Key.ProductName,
                                        TotalQuantity = grouped.Sum(x => x.m.qty)
                                    };

                        var result = query.ToList();
                        DataRow row = combinedDataTable.NewRow();

                        row["Srno"] = i;

                        row["CustomerName"] = purchaseOrder;
                        Int32 sum = 0;
                        foreach (var purchaseDetail in result)
                        {
                            var mat = _context.MaterialMaster.Where(p => p.Materialname == purchaseDetail.ProductName).FirstOrDefault();
                            if (mat != null)
                            {
                                row[mat.ShortName] = purchaseDetail.TotalQuantity;
                                sum = sum + purchaseDetail.TotalQuantity;
                            }

                        }
                        row["Total"] = sum;
                        combinedDataTable.Rows.Add(row);
                        i++;
                    }
                    DataRow row1 = combinedDataTable.NewRow();
                    row1["CustomerName"] = "Routewise Total";
                    foreach (DataColumn column in combinedDataTable.Columns)
                    {
                        if (column.ColumnName != "Srno" && column.ColumnName != "CustomerName")
                        {
                            decimal sum = 0;
                            foreach (DataRow row in combinedDataTable.Rows)
                            {
                                if (row[column] != DBNull.Value)
                                {
                                    sum += Convert.ToDecimal(row[column]);
                                }
                            }
                            row1[column.ColumnName] = Convert.ToInt32(sum);
                        }
                    }
                    combinedDataTable.Rows.Add(row1);


                    if (combinedDataTable.Rows.Count > 0)
                    {

                        using (var workbook = new XLWorkbook())
                        {
                            var worksheet = workbook.Worksheets.Add("Sheet1");

                            int rowNumber = 1; // Change this to the row number you want
                            int columnNumber = 5; // Change this to the column number you want (1-based index)

                            // Access the cell
                            var titleCell = worksheet.Cell(rowNumber, columnNumber);

                            // Set the title text and formatting
                            titleCell.Value = "For The Period " + FromDate.Date.ToString("dd-MM-yyyy") + " To " + ToDate.Date.ToString("dd-MM-yyyy") + ""; // Replace with your desired title
                            titleCell.Style.Font.Bold = true;
                            titleCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; // Center align the title
                                                                                                       // titleCell.Style.Fill.BackgroundColor = XLColor.LightGreen; // Customize the background color if needed
                            titleCell.Style.Font.FontSize = 14; // Adjust font size if needed
                            titleCell.Style.Font.FontColor = XLColor.Black; // Set font color if needed

                            // Merge the cells for the title row
                            var lastColumn = worksheet.ColumnsUsed().Last().ColumnNumber(); // Find the last used column
                            var rangeToMerge = worksheet.Range(rowNumber, columnNumber, rowNumber, lastColumn); // Merge from column 1 to the last used column
                            rangeToMerge.Merge();

                            var headerRow = worksheet.Row(2);
                            for (int j = 0; j < combinedDataTable.Columns.Count; j++)
                            {
                                headerRow.Cell(j + 1).Value = combinedDataTable.Columns[j].ColumnName;

                                if (combinedDataTable.Columns[j].ColumnName == "CustomerName")
                                {
                                    worksheet.Column(j + 1).Width = 20; // Adjust the width as needed
                                }
                                else
                                {
                                    worksheet.Column(j + 1).Width = 5; // Default width for other columns
                                }
                            }
                            headerRow.Style.Font.Bold = true;
                            headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;
                            headerRow.Style.Border.BottomBorder = XLBorderStyleValues.Thin; // Add bottom border to header row

                            // Insert a blank row after the header
                            // worksheet.Row(2).InsertRowsBelow(1);

                            // Populate the worksheet with data from the DataTable
                            for (int k = 0; k < combinedDataTable.Rows.Count; k++)
                            {
                                var isRoutewiseTotal = combinedDataTable.Rows[k]["CustomerName"].ToString() == "Routewise Total";
                                var isOverallTotal = combinedDataTable.Rows[k]["CustomerName"].ToString() == "OverAll Total";
                                for (int j = 0; j < combinedDataTable.Columns.Count; j++)
                                {
                                    object value = combinedDataTable.Rows[k][j];
                                    var cell = worksheet.Cell(k + 3, j + 1);
                                    if (value != DBNull.Value)
                                    {
                                        // Convert the value to the appropriate type
                                        if (value is DateTime)
                                        {
                                            worksheet.Cell(k + 3, j + 1).Value = (DateTime)value;
                                        }
                                        else if (value is double)
                                        {
                                            worksheet.Cell(k + 3, j + 1).Value = (double)value;
                                        }
                                        else if (value is int)
                                        {
                                            worksheet.Cell(k + 3, j + 1).Value = (int)value;
                                        }
                                        else
                                        {
                                            worksheet.Cell(k + 3, j + 1).Value = value.ToString();
                                        }
                                        if (isRoutewiseTotal)
                                        {
                                            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                                        }
                                        else if (isOverallTotal)
                                        {
                                            cell.Style.Fill.BackgroundColor = XLColor.Khaki;
                                        }
                                        cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin; // Add outer border to each cell
                                        cell.Style.Alignment.WrapText = true; // Enable word wrap
                                    }
                                    // Apply borders to all cells
                                    var allCells = worksheet.RangeUsed();
                                    allCells.Style.Border.InsideBorder = XLBorderStyleValues.Thin; // Add inner borders to all cells

                                    // Apply border around the entire worksheet
                                    var entireRange = worksheet.Range(worksheet.FirstCellUsed(), worksheet.LastCellUsed());
                                    entireRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin; // Add outer border around the entire range

                                }
                            }

                            using (var memoryStream = new MemoryStream())
                            {
                                workbook.SaveAs(memoryStream);
                                memoryStream.Seek(0, SeekOrigin.Begin);

                                // Return the Excel content as a response
                                return File(memoryStream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "report.xlsx");
                            }
                        }
                    }
                    else
                    {
                        return View();
                    }
                }
                catch (Exception ex)
                {
                    return View();
                }
            }



        }
    }
}

