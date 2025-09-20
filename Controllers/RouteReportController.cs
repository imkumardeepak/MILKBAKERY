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
					// Fetch and cache materials as a dictionary
					var materialMap = await _context.MaterialMaster
						.AsNoTracking()
						.Where(a => !a.Materialname.Contains("CRATES FOR") && a.segementname == Segement)
						.ToDictionaryAsync(a => a.Materialname, a => a.ShortName);

					var materialShortNames = materialMap.Values.Distinct().ToList();

					DataTable finaldata = new DataTable();
					finaldata.Columns.Add("Srno");
					finaldata.Columns.Add("CustomerName");
					foreach (var mat in materialShortNames)
						finaldata.Columns.Add(mat);
					finaldata.Columns.Add("Total", typeof(int));

					var routeMaster = await _context.RouteMaster
						.AsNoTracking()
						.Distinct()
						.OrderBy(a => a.ShortCode)
						.ToListAsync();

					foreach (var route in routeMaster)
					{
						var routeCustomers = await _context.Customer_Master
											.AsNoTracking()
											.Where(a => a.route == route.Route)
											.Select(a => new { a.Name, a.Sequence })
											.Distinct()
											.OrderBy(x => x.Sequence)
											.Select(x => x.Name)
											.ToListAsync();

						var purchaseOrders = await _context.PurchaseOrder
							.AsNoTracking()
							.Include(po => po.ProductDetails)
							.Where(po => po.OrderDate >= FromDate && po.OrderDate <= ToDate &&
							po.Segementname == Segement && routeCustomers.Contains(po.Customername))
							.ToListAsync();

						var distinctCustomers = purchaseOrders
							.Select(po => po.Customername)
							.Distinct()
							.OrderBy(name => name)
							.ToList();

						if (distinctCustomers.Count > 0)
						{
							DataTable combinedDataTable = new DataTable();
							combinedDataTable.Columns.Add("Srno");
							combinedDataTable.Columns.Add("CustomerName");
							foreach (var mat in materialShortNames)
								combinedDataTable.Columns.Add(mat);
							combinedDataTable.Columns.Add("Total", typeof(int));

							// Add route heading row
							DataRow headingRow = combinedDataTable.NewRow();
							headingRow["CustomerName"] = route.ShortCode + "-" + route.Route.ToUpper();
							combinedDataTable.Rows.Add(headingRow);

							int i = 1;
							foreach (var customerName in distinctCustomers)
							{
								var orderDetails = purchaseOrders
									.Where(po => po.Customername == customerName)
									.SelectMany(po => po.ProductDetails)
									.GroupBy(p => new { p.PurchaseOrder.Customername, p.ProductName })
									.Select(g => new
									{
										g.Key.Customername,
										g.Key.ProductName,
										TotalQuantity = g.Sum(p => p.qty)
									})
									.ToList();

								DataRow row = combinedDataTable.NewRow();
								row["Srno"] = i;
								row["CustomerName"] = customerName;
								int sum = 0;
								foreach (var item in orderDetails)
								{
									if (materialMap.TryGetValue(item.ProductName, out var shortName))
									{
										row[shortName] = item.TotalQuantity;
										sum += item.TotalQuantity;
									}
								}
								row["Total"] = sum;
								combinedDataTable.Rows.Add(row);
								i++;
							}

							// Routewise total
							DataRow routeTotal = combinedDataTable.NewRow();
							routeTotal["CustomerName"] = "Routewise Total";
							foreach (DataColumn col in combinedDataTable.Columns)
							{
								if (col.ColumnName != "Srno" && col.ColumnName != "CustomerName")
								{
									int total = combinedDataTable.AsEnumerable()
										.Where(r => r[col] != DBNull.Value && r["CustomerName"].ToString() != "Routewise Total")
										.Sum(r => Convert.ToInt32(r[col]));
									routeTotal[col.ColumnName] = total;
								}
							}
							combinedDataTable.Rows.Add(routeTotal);
							finaldata.Merge(combinedDataTable);
						}
					}

					if (finaldata.Rows.Count > 0)
					{
						DataRow overall = finaldata.NewRow();
						overall["CustomerName"] = "OverAll Total";

						foreach (DataColumn column in finaldata.Columns)
						{
							if (column.ColumnName != "Srno" && column.ColumnName != "CustomerName")
							{
								int total = finaldata.AsEnumerable()
									.Where(r => r["CustomerName"].ToString() == "Routewise Total" && r[column] != DBNull.Value)
									.Sum(r => Convert.ToInt32(r[column]));
								overall[column.ColumnName] = total;
							}
						}

						finaldata.Rows.Add(overall);
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
				// Single route logic
				try
				{
					var materialMap = await _context.MaterialMaster
						.AsNoTracking()
						.Where(a => !a.Materialname.Contains("CRATES FOR") && a.segementname == Segement)
						.ToDictionaryAsync(a => a.Materialname, a => a.ShortName);

					var materialShortNames = materialMap.Values.Distinct().ToList();

					var routeCustomers = await _context.Customer_Master
						.AsNoTracking()
						.Where(a => a.route == Customer)
						.Select(a => a.Name)
						.Distinct()
						.ToListAsync();

					var purchaseOrders = await _context.PurchaseOrder
						.AsNoTracking()
						.Include(po => po.ProductDetails)
						.Where(po => po.OrderDate >= FromDate && po.OrderDate <= ToDate && po.Segementname == Segement && routeCustomers.Contains(po.Customername))
						.ToListAsync();

					var distinctCustomers = purchaseOrders
						.Select(po => po.Customername)
						.Distinct()
						.OrderBy(name => name)
						.ToList();

					DataTable combinedDataTable = new DataTable();
					combinedDataTable.Columns.Add("Srno");
					combinedDataTable.Columns.Add("CustomerName");
					foreach (var mat in materialShortNames)
						combinedDataTable.Columns.Add(mat);
					combinedDataTable.Columns.Add("Total");

					DataRow heading = combinedDataTable.NewRow();
					heading["CustomerName"] = Customer.ToUpper();
					combinedDataTable.Rows.Add(heading);

					int i = 1;
					foreach (var customerName in distinctCustomers)
					{
						var orderDetails = purchaseOrders
							.Where(po => po.Customername == customerName)
							.SelectMany(po => po.ProductDetails)
							.GroupBy(p => new { p.PurchaseOrder.Customername, p.ProductName })
							.Select(g => new
							{
								g.Key.Customername,
								g.Key.ProductName,
								TotalQuantity = g.Sum(p => p.qty)
							})
							.ToList();

						DataRow row = combinedDataTable.NewRow();
						row["Srno"] = i;
						row["CustomerName"] = customerName;

						int sum = 0;
						foreach (var item in orderDetails)
						{
							if (materialMap.TryGetValue(item.ProductName, out var shortName))
							{
								row[shortName] = item.TotalQuantity;
								sum += item.TotalQuantity;
							}
						}
						row["Total"] = sum;
						combinedDataTable.Rows.Add(row);
						i++;
					}

					DataRow routeTotal = combinedDataTable.NewRow();
					routeTotal["CustomerName"] = "Routewise Total";
					foreach (DataColumn col in combinedDataTable.Columns)
					{
						if (col.ColumnName != "Srno" && col.ColumnName != "CustomerName")
						{
							int total = combinedDataTable.AsEnumerable()
								.Where(r => r[col] != DBNull.Value && r["CustomerName"].ToString() != "Routewise Total")
								.Sum(r => Convert.ToInt32(r[col]));
							routeTotal[col.ColumnName] = total;
						}
					}
					combinedDataTable.Rows.Add(routeTotal);

					return PartialView("ReportView", combinedDataTable);
				}
				catch (Exception ex)
				{
					return View(); // Optionally log ex.Message
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
			try
			{
				// Material Map
				var materialMap = await _context.MaterialMaster
					.AsNoTracking()
					.Where(a => !a.Materialname.Contains("CRATES FOR") && a.segementname == Segement)
					.ToDictionaryAsync(m => m.Materialname, m => m.ShortName);

				var materialShortNames = materialMap.Values.Distinct().ToList();

				DataTable finalData = new DataTable();
				finalData.Columns.Add("Srno");
				finalData.Columns.Add("CustomerName");
				foreach (var mat in materialShortNames)
					finalData.Columns.Add(mat, typeof(int));
				finalData.Columns.Add("Total", typeof(int));

				if (Customer == "All Route")
				{
					var routes = await _context.RouteMaster
						.AsNoTracking()
						.OrderBy(r => r.ShortCode)
						.ToListAsync();

					foreach (var route in routes)
					{
						var routeCustomers = await _context.Customer_Master
							.AsNoTracking()
							.Where(c => c.route == route.Route)
							.Select(c => c.Name)
							.Distinct()
							.ToListAsync();

						var purchaseOrders = await _context.PurchaseOrder
							.AsNoTracking()
							.Include(po => po.ProductDetails)
							.Where(po => po.OrderDate >= FromDate && po.OrderDate <= ToDate && po.Segementname == Segement && routeCustomers.Contains(po.Customername))
							.ToListAsync();

						var distinctCustomers = purchaseOrders.Select(po => po.Customername).Distinct().OrderBy(x => x).ToList();
						if (!distinctCustomers.Any()) continue;

						DataTable routeData = CreateTableStructure(materialShortNames);
						routeData.Rows.Add(CreateHeaderRow(route.ShortCode + "-" + route.Route.ToUpper(), routeData));

						int i = 1;
						foreach (var cust in distinctCustomers)
						{
							var details = purchaseOrders
								.Where(po => po.Customername == cust)
								.SelectMany(po => po.ProductDetails)
								.GroupBy(p => p.ProductName)
								.Select(g => new { ProductName = g.Key, Total = g.Sum(x => x.qty) })
								.ToList();

							var row = routeData.NewRow();
							row["Srno"] = i++;
							row["CustomerName"] = cust;
							int totalQty = 0;

							foreach (var d in details)
							{
								if (materialMap.TryGetValue(d.ProductName, out var shortName))
								{
									row[shortName] = d.Total;
									totalQty += d.Total;
								}
							}
							row["Total"] = totalQty;
							routeData.Rows.Add(row);
						}

						routeData.Rows.Add(CreateTotalRow(routeData));
						finalData.Merge(routeData);
					}
					finalData.Rows.Add(CreateOverallRow(finalData));
				}
				else
				{
					// Single Route
					var routeCustomers = await _context.Customer_Master
						.AsNoTracking()
						.Where(c => c.route == Customer)
						.Select(c => c.Name)
						.Distinct()
						.ToListAsync();

					var purchaseOrders = await _context.PurchaseOrder
						.AsNoTracking()
						.Include(po => po.ProductDetails)
						.Where(po => po.OrderDate >= FromDate && po.OrderDate <= ToDate && po.Segementname == Segement && routeCustomers.Contains(po.Customername))
						.ToListAsync();

					var distinctCustomers = purchaseOrders.Select(po => po.Customername).Distinct().OrderBy(x => x).ToList();

					finalData.Rows.Add(CreateHeaderRow(Customer.ToUpper(), finalData));

					int i = 1;
					foreach (var cust in distinctCustomers)
					{
						var details = purchaseOrders
							.Where(po => po.Customername == cust)
							.SelectMany(po => po.ProductDetails)
							.GroupBy(p => p.ProductName)
							.Select(g => new { ProductName = g.Key, Total = g.Sum(x => x.qty) })
							.ToList();

						var row = finalData.NewRow();
						row["Srno"] = i++;
						row["CustomerName"] = cust;
						int totalQty = 0;

						foreach (var d in details)
						{
							if (materialMap.TryGetValue(d.ProductName, out var shortName))
							{
								row[shortName] = d.Total;
								totalQty += d.Total;
							}
						}
						row["Total"] = totalQty;
						finalData.Rows.Add(row);
					}
					finalData.Rows.Add(CreateTotalRow(finalData));
				}

				if (finalData.Rows.Count == 0)
					return View();

				// Export to Excel
				using var workbook = new XLWorkbook();
				var worksheet = workbook.Worksheets.Add("Sheet1");

				// Title
				var titleCell = worksheet.Cell(1, 1);
				titleCell.Value = $"For The Period {FromDate:dd-MM-yyyy} To {ToDate:dd-MM-yyyy}";
				titleCell.Style.Font.Bold = true;
				titleCell.Style.Font.FontSize = 14;
				titleCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
				worksheet.Range(1, 1, 1, finalData.Columns.Count).Merge();

				// Headers
				var headerRow = worksheet.Row(2);
				for (int j = 0; j < finalData.Columns.Count; j++)
				{
					headerRow.Cell(j + 1).Value = finalData.Columns[j].ColumnName;
					headerRow.Cell(j + 1).Style.Font.Bold = true;
					headerRow.Cell(j + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
					worksheet.Column(j + 1).Width = finalData.Columns[j].ColumnName == "CustomerName" ? 20 : 8;
				}

				// Data rows
				for (int i = 0; i < finalData.Rows.Count; i++)
				{
					var row = finalData.Rows[i];
					for (int j = 0; j < finalData.Columns.Count; j++)
					{
						var cell = worksheet.Cell(i + 3, j + 1);
						cell.Value = row[j]?.ToString();
						cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

						string custName = row["CustomerName"].ToString();
						if (custName == "Routewise Total")
							cell.Style.Fill.BackgroundColor = XLColor.LightGray;
						else if (custName == "OverAll Total")
							cell.Style.Fill.BackgroundColor = XLColor.Khaki;
					}
				}

				worksheet.RangeUsed().Style.Border.InsideBorder = XLBorderStyleValues.Thin;
				worksheet.RangeUsed().Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

				using var ms = new MemoryStream();
				workbook.SaveAs(ms);
				ms.Position = 0;
				return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "report.xlsx");
			}
			catch (Exception ex)
			{
				// Optionally log the error
				return View(); // or return StatusCode(500, ex.Message);
			}
		}
		private DataTable CreateTableStructure(List<string> materials)
		{
			var table = new DataTable();
			table.Columns.Add("Srno");
			table.Columns.Add("CustomerName");
			foreach (var mat in materials)
				table.Columns.Add(mat, typeof(int));
			table.Columns.Add("Total", typeof(int));
			return table;
		}

		private DataRow CreateHeaderRow(string name, DataTable table)
		{
			var row = table.NewRow();
			row["CustomerName"] = name;
			return row;
		}

		private DataRow CreateTotalRow(DataTable table)
		{
			var row = table.NewRow();
			row["CustomerName"] = "Routewise Total";
			foreach (DataColumn col in table.Columns)
			{
				if (col.ColumnName == "Srno" || col.ColumnName == "CustomerName") continue;
				int sum = table.AsEnumerable()
					.Where(r => r[col] != DBNull.Value && r["CustomerName"].ToString() != "Routewise Total")
					.Sum(r => Convert.ToInt32(r[col]));
				row[col.ColumnName] = sum;
			}
			return row;
		}

		private DataRow CreateOverallRow(DataTable table)
		{
			var row = table.NewRow();
			row["CustomerName"] = "OverAll Total";
			foreach (DataColumn col in table.Columns)
			{
				if (col.ColumnName == "Srno" || col.ColumnName == "CustomerName") continue;
				int sum = table.AsEnumerable()
					.Where(r => r["CustomerName"].ToString() == "Routewise Total" && r[col] != DBNull.Value)
					.Sum(r => Convert.ToInt32(r[col]));
				row[col.ColumnName] = sum;
			}
			return row;
		}

	}
}

