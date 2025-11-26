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
			ViewBag.Route = GetRoute();
			return View();
		}

		[HttpGet]
		public JsonResult GetSubCategories(string segment)
		{
			// Only return subcategories for dairy segments
			if (string.IsNullOrEmpty(segment) || !segment.ToLower().Contains("dairy"))
			{
				return Json(new List<SelectListItem>());
			}

			// Get subcategories based on segment
			var subCategories = _context.MaterialMaster
				.Where(m => m.segementname == segment && m.isactive)
				.Select(m => m.subcategory)
				.Distinct()
				.OrderBy(sc => sc)
				.Select(sc => new SelectListItem
				{
					Value = sc,
					Text = sc
				})
				.ToList();

			return Json(subCategories);
		}

		[HttpPost]
		public async Task<IActionResult> ActionNameAsync(string Customer, string Segement, string SubCategory, DateTime FromDate, DateTime ToDate)
		{
			if (Customer == "All Route")
			{
				try
				{
					// Fetch and cache materials as a dictionary
					var materialQuery = _context.MaterialMaster
												.AsNoTracking()
												.Where(a => !a.Materialname.Contains("CRATES FOR")
															&& a.segementname == Segement
															&& a.isactive == true);

					// Apply subcategory filter if selected
					if (!string.IsNullOrEmpty(SubCategory))
					{
						materialQuery = materialQuery.Where(a => a.subcategory == SubCategory);
					}


					var materialMap = await materialQuery.OrderBy(a => a.subcategory)
												.ThenBy(a => a.sequence)
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
						// Create a dictionary to map customer names to their sequence numbers
						var customerSequenceMap = await _context.Customer_Master
							.AsNoTracking()
							.Where(a => a.route == route.Route && a.Division == Segement && a.IsActive == true)
							.Select(a => new { a.Name, a.Sequence })
							.Distinct()
							.ToDictionaryAsync(a => a.Name, a => a.Sequence);

						var routeCustomers = customerSequenceMap.Keys.ToList();

						// For dairy segments, get all customers even if they don't have orders
						bool isDairySegment = Segement.ToLower().Contains("dairy");
						List<string> distinctCustomers;

						if (isDairySegment)
						{
							// For dairy segments, use all customers in the route
							distinctCustomers = routeCustomers
								.OrderBy(name => customerSequenceMap.ContainsKey(name) ? customerSequenceMap[name] : int.MaxValue)
								.ToList();
						}
						else
						{
							// For non-dairy segments, only use customers with orders
							var purchaseOrders = await _context.PurchaseOrder
								.AsNoTracking()
								.Include(po => po.ProductDetails)
								.Where(po => po.OrderDate >= FromDate && po.OrderDate <= ToDate &&
								po.Segementname == Segement && routeCustomers.Contains(po.Customername))
								.ToListAsync();

							distinctCustomers = purchaseOrders
								.Select(po => po.Customername)
								.Distinct()
								.OrderBy(name => customerSequenceMap.ContainsKey(name) ? customerSequenceMap[name] : int.MaxValue)
								.ToList();
						}

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

							// Get purchase orders for this route
							var purchaseOrdersForRoute = await _context.PurchaseOrder
								.AsNoTracking()
								.Include(po => po.ProductDetails)
								.Where(po => po.OrderDate >= FromDate && po.OrderDate <= ToDate &&
								po.Segementname == Segement && routeCustomers.Contains(po.Customername))
								.ToListAsync();

							int i = 1;
							foreach (var customerName in distinctCustomers)
							{
								// Check if customer has orders in the date range
								var orderDetails = purchaseOrdersForRoute
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
								// For dairy segments, use the sequence from Customer Master instead of simple incrementing
								if (isDairySegment && customerSequenceMap.ContainsKey(customerName))
								{
									row["Srno"] = customerSequenceMap[customerName];
								}
								else
								{
									row["Srno"] = i;
								}
								row["CustomerName"] = customerName;

								int sum = 0;
								// Initialize all material columns to blank
								foreach (var mat in materialShortNames)
								{
									row[mat] = DBNull.Value;
								}

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
					var materialQuery = _context.MaterialMaster
						.AsNoTracking()
						.Where(a => !a.Materialname.Contains("CRATES FOR") && a.segementname == Segement && a.isactive == true);

					// Apply subcategory filter if selected
					if (!string.IsNullOrEmpty(SubCategory))
					{
						materialQuery = materialQuery.Where(a => a.subcategory == SubCategory);
					}

					var materialMap = await materialQuery.OrderBy(a => a.subcategory)
												.ThenBy(a => a.sequence)
												.ToDictionaryAsync(a => a.Materialname, a => a.ShortName);

					var materialShortNames = materialMap.Values.Distinct().ToList();

					// Create a dictionary to map customer names to their sequence numbers
					var customerSequenceMap = await _context.Customer_Master
						.AsNoTracking()
						.Where(a => a.route == Customer && a.Division == Segement && a.IsActive == true)
						.Select(a => new { a.Name, a.Sequence })
						.Distinct()
						.ToDictionaryAsync(a => a.Name, a => a.Sequence);

					var routeCustomers = customerSequenceMap.Keys.ToList();

					// For dairy segments, get all customers even if they don't have orders
					bool isDairySegment = Segement.ToLower().Contains("dairy");
					List<string> distinctCustomers;

					if (isDairySegment)
					{
						// For dairy segments, use all customers in the route
						distinctCustomers = routeCustomers
							.OrderBy(name => customerSequenceMap.ContainsKey(name) ? customerSequenceMap[name] : int.MaxValue)
							.ToList();
					}
					else
					{
						// For non-dairy segments, only use customers with orders
						var purchaseOrdersForNonDairy = await _context.PurchaseOrder
							.AsNoTracking()
							.Include(po => po.ProductDetails)
							.Where(po => po.OrderDate >= FromDate && po.OrderDate <= ToDate && po.Segementname == Segement && routeCustomers.Contains(po.Customername))
							.ToListAsync();

						distinctCustomers = purchaseOrdersForNonDairy
							.Select(po => po.Customername)
							.Distinct()
							.OrderBy(name => customerSequenceMap.ContainsKey(name) ? customerSequenceMap[name] : int.MaxValue)
							.ToList();
					}

					DataTable combinedDataTable = new DataTable();
					combinedDataTable.Columns.Add("Srno");
					combinedDataTable.Columns.Add("CustomerName");
					foreach (var mat in materialShortNames)
						combinedDataTable.Columns.Add(mat);
					combinedDataTable.Columns.Add("Total");

					DataRow heading = combinedDataTable.NewRow();
					heading["CustomerName"] = Customer.ToUpper();
					combinedDataTable.Rows.Add(heading);

					// Get purchase orders
					var purchaseOrders = await _context.PurchaseOrder
						.AsNoTracking()
						.Include(po => po.ProductDetails)
						.Where(po => po.OrderDate >= FromDate && po.OrderDate <= ToDate && po.Segementname == Segement && routeCustomers.Contains(po.Customername))
						.ToListAsync();

					int i = 1;
					foreach (var customerName in distinctCustomers)
					{
						// Check if customer has orders in the date range
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
						// For dairy segments, use the sequence from Customer Master instead of simple incrementing
						if (isDairySegment && customerSequenceMap.ContainsKey(customerName))
						{
							row["Srno"] = customerSequenceMap[customerName];
						}
						else
						{
							row["Srno"] = i;
						}
						row["CustomerName"] = customerName;

						int sum = 0;
						// Initialize all material columns to blank
						foreach (var mat in materialShortNames)
						{
							row[mat] = DBNull.Value;
						}

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
		private List<SelectListItem> GetRoute()
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
		public async Task<IActionResult> Export(string Customer, string Segement, string SubCategory, DateTime FromDate, DateTime ToDate)
		{
			try
			{
				// Material Map
				var materialQuery = _context.MaterialMaster
					.AsNoTracking()
					.Where(a => !a.Materialname.Contains("CRATES FOR") && a.segementname == Segement && a.isactive == true);

				// Apply subcategory filter if selected
				if (!string.IsNullOrEmpty(SubCategory))
				{
					materialQuery = materialQuery.Where(a => a.subcategory == SubCategory);
				}

				var materialMap = await materialQuery.OrderBy(a => a.subcategory)
												.ThenBy(a => a.sequence)
												.ToDictionaryAsync(a => a.Materialname, a => a.ShortName);

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
						// Create a dictionary to map customer names to their sequence numbers
						var customerSequenceMap = await _context.Customer_Master
							.AsNoTracking()
							.Where(c => c.route == route.Route && c.Division == Segement && c.IsActive == true)
							.Select(a => new { a.Name, a.Sequence })
							.Distinct()
							.ToDictionaryAsync(a => a.Name, a => a.Sequence);

						var routeCustomers = customerSequenceMap.Keys.ToList();

						// For dairy segments, get all customers even if they don't have orders
						bool isDairySegment = Segement.ToLower().Contains("dairy");
						List<string> distinctCustomers;

						if (isDairySegment)
						{
							// For dairy segments, use all customers in the route
							distinctCustomers = routeCustomers
								.OrderBy(name => customerSequenceMap.ContainsKey(name) ? customerSequenceMap[name] : int.MaxValue)
								.ToList();
						}
						else
						{
							// For non-dairy segments, only use customers with orders
							var purchaseOrders = await _context.PurchaseOrder
								.AsNoTracking()
								.Include(po => po.ProductDetails)
								.Where(po => po.OrderDate >= FromDate && po.OrderDate <= ToDate && po.Segementname == Segement && routeCustomers.Contains(po.Customername))
								.ToListAsync();

							distinctCustomers = purchaseOrders
								.Select(po => po.Customername)
								.Distinct()
								.OrderBy(name => customerSequenceMap.ContainsKey(name) ? customerSequenceMap[name] : int.MaxValue)
								.ToList();
						}

						if (!distinctCustomers.Any()) continue;

						DataTable routeData = new DataTable();
						routeData.Columns.Add("Srno");
						routeData.Columns.Add("CustomerName");
						foreach (var mat in materialShortNames)
							routeData.Columns.Add(mat, typeof(int));
						routeData.Columns.Add("Total", typeof(int));

						// Add route heading row
						DataRow headingRow = routeData.NewRow();
						headingRow["CustomerName"] = route.ShortCode + "-" + route.Route.ToUpper();
						routeData.Rows.Add(headingRow);

						// Get purchase orders for this route
						var purchaseOrdersForRoute = await _context.PurchaseOrder
							.AsNoTracking()
							.Include(po => po.ProductDetails)
							.Where(po => po.OrderDate >= FromDate && po.OrderDate <= ToDate && po.Segementname == Segement && routeCustomers.Contains(po.Customername))
							.ToListAsync();

						int i = 1;
						foreach (var cust in distinctCustomers)
						{
							// Check if customer has orders in the date range
							var details = purchaseOrdersForRoute
								.Where(po => po.Customername == cust)
								.SelectMany(po => po.ProductDetails)
								.GroupBy(p => p.ProductName)
								.Select(g => new { ProductName = g.Key, Total = g.Sum(x => x.qty) })
								.ToList();

							DataRow row = routeData.NewRow();
							// For dairy segments, use the sequence from Customer Master instead of simple incrementing
							if (isDairySegment && customerSequenceMap.ContainsKey(cust))
							{
								row["Srno"] = customerSequenceMap[cust];
							}
							else
							{
								row["Srno"] = i;
							}
							row["CustomerName"] = cust;
							i++;

							int totalQty = 0;
							// Initialize all material columns to blank
							foreach (var mat in materialShortNames)
							{
								row[mat] = DBNull.Value;
							}

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

						// Routewise total
						DataRow routeTotal = routeData.NewRow();
						routeTotal["CustomerName"] = "Routewise Total";
						foreach (DataColumn col in routeData.Columns)
						{
							if (col.ColumnName != "Srno" && col.ColumnName != "CustomerName")
							{
								int total = routeData.AsEnumerable()
									.Where(r => r[col] != DBNull.Value && r["CustomerName"].ToString() != "Routewise Total")
									.Sum(r => Convert.ToInt32(r[col]));
								routeTotal[col.ColumnName] = total;
							}
						}
						routeData.Rows.Add(routeTotal);
						finalData.Merge(routeData);
					}

					// Overall total
					if (finalData.Rows.Count > 0)
					{
						DataRow overall = finalData.NewRow();
						overall["CustomerName"] = "OverAll Total";

						foreach (DataColumn column in finalData.Columns)
						{
							if (column.ColumnName != "Srno" && column.ColumnName != "CustomerName")
							{
								int total = finalData.AsEnumerable()
									.Where(r => r["CustomerName"].ToString() == "Routewise Total" && r[column] != DBNull.Value)
									.Sum(r => Convert.ToInt32(r[column]));
								overall[column.ColumnName] = total;
							}
						}
						finalData.Rows.Add(overall);
					}
				}
				else
				{
					// Single Route
					// Create a dictionary to map customer names to their sequence numbers
					var customerSequenceMap = await _context.Customer_Master
						.AsNoTracking()
						.Where(c => c.route == Customer && c.Division == Segement && c.IsActive == true)
						.Select(a => new { a.Name, a.Sequence })
						.Distinct()
						.ToDictionaryAsync(a => a.Name, a => a.Sequence);

					var routeCustomers = customerSequenceMap.Keys.ToList();

					// For dairy segments, get all customers even if they don't have orders
					bool isDairySegment = Segement.ToLower().Contains("dairy");
					List<string> distinctCustomers;

					if (isDairySegment)
					{
						// For dairy segments, use all customers in the route
						distinctCustomers = routeCustomers
							.OrderBy(name => customerSequenceMap.ContainsKey(name) ? customerSequenceMap[name] : int.MaxValue)
							.ToList();
					}
					else
					{
						// For non-dairy segments, only use customers with orders
						var purchaseOrderQueryForNonDairy = _context.PurchaseOrder
							.AsNoTracking()
							.Include(po => po.ProductDetails)
							.Where(po => po.OrderDate >= FromDate && po.OrderDate <= ToDate && po.Segementname == Segement && routeCustomers.Contains(po.Customername));

						var purchaseOrdersForNonDairy = await purchaseOrderQueryForNonDairy.ToListAsync();

						distinctCustomers = purchaseOrdersForNonDairy
							.Select(po => po.Customername)
							.Distinct()
							.OrderBy(name => customerSequenceMap.ContainsKey(name) ? customerSequenceMap[name] : int.MaxValue)
							.ToList();
					}

					// Add header row
					DataRow heading = finalData.NewRow();
					heading["CustomerName"] = Customer.ToUpper();
					finalData.Rows.Add(heading);

					// Get purchase orders
					var purchaseOrderQuery = _context.PurchaseOrder
						.AsNoTracking()
						.Include(po => po.ProductDetails)
						.Where(po => po.OrderDate >= FromDate && po.OrderDate <= ToDate && po.Segementname == Segement && routeCustomers.Contains(po.Customername));

					var purchaseOrders = await purchaseOrderQuery.ToListAsync();

					int i = 1;
					foreach (var cust in distinctCustomers)
					{
						// Check if customer has orders in the date range
						var details = purchaseOrders
							.Where(po => po.Customername == cust)
							.SelectMany(po => po.ProductDetails)
							.GroupBy(p => p.ProductName)
							.Select(g => new { ProductName = g.Key, Total = g.Sum(x => x.qty) })
							.ToList();

						DataRow row = finalData.NewRow();
						// For dairy segments, use the sequence from Customer Master instead of simple incrementing
						if (isDairySegment && customerSequenceMap.ContainsKey(cust))
						{
							row["Srno"] = customerSequenceMap[cust];
						}
						else
						{
							row["Srno"] = i;
						}
						row["CustomerName"] = cust;
						i++;

						int totalQty = 0;
						// Initialize all material columns to blank
						foreach (var mat in materialShortNames)
						{
							row[mat] = DBNull.Value;
						}

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

					// Routewise total
					DataRow routeTotal = finalData.NewRow();
					routeTotal["CustomerName"] = "Routewise Total";
					foreach (DataColumn col in finalData.Columns)
					{
						if (col.ColumnName != "Srno" && col.ColumnName != "CustomerName")
						{
							int total = finalData.AsEnumerable()
								.Where(r => r[col] != DBNull.Value && r["CustomerName"].ToString() != "Routewise Total")
								.Sum(r => Convert.ToInt32(r[col]));
							routeTotal[col.ColumnName] = total;
						}
					}
					finalData.Rows.Add(routeTotal);
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
						// Handle DBNull values for Excel export
						cell.Value = (row[j] == DBNull.Value || row[j] == null) ? "" : row[j].ToString();
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
	}
}

