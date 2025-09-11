using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Milk_Bakery.Data;
using Milk_Bakery.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Milk_Bakery.Services
{
	public class InvoiceBackgroundService : BackgroundService
	{
		private readonly ILogger<InvoiceBackgroundService> _logger;
		private readonly IServiceProvider _serviceProvider;

		public InvoiceBackgroundService(ILogger<InvoiceBackgroundService> logger, IServiceProvider serviceProvider)
		{
			_logger = logger;
			_serviceProvider = serviceProvider;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			_logger.LogInformation("Invoice Background Service started at: {time}", DateTimeOffset.Now);

			while (!stoppingToken.IsCancellationRequested)
			{
				try
				{
					_logger.LogInformation("Invoice Background Service running at: {time}", DateTimeOffset.Now);
					await ProcessInvoicesWithSetFlagZero();

					// Wait for 10 minutes before next execution
					await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error occurred while processing invoices with setflag=0 at: {time}", DateTimeOffset.Now);
				}
			}
		}

		private async Task ProcessInvoicesWithSetFlagZero()
		{
			using (var scope = _serviceProvider.CreateScope())
			{
				var dbContext = scope.ServiceProvider.GetRequiredService<MilkDbContext>();

				// Get invoices where setflag is 0
				var invoices = await dbContext.Invoices
					.Where(i => i.setflag == 0)
					.Include(i => i.InvoiceMaterials)
					.ToListAsync();

				if (invoices.Any())
				{
					_logger.LogInformation("Found {count} invoices with setflag=0 at: {time}", invoices.Count, DateTimeOffset.Now);

					// Process each invoice
					foreach (var invoice in invoices)
					{
						try
						{
							_logger.LogInformation("Processing invoice ID: {invoiceId}, Invoice No: {invoiceNo}",
								invoice.InvoiceId, invoice.InvoiceNo);

							int large_crates = 0;
							int small_crates = 0;

							// Find customer details
							var customer = await dbContext.Customer_Master.FirstOrDefaultAsync(c => c.shortname == invoice.ShipToCode);
							if (customer == null)
							{
								_logger.LogWarning("Customer not found for invoice ID: {invoiceId}, ShipToCode: {shipToCode}",
									invoice.InvoiceId, invoice.ShipToCode);
								continue; // Skip processing if customer not found
							}

							// Process each invoice material
							foreach (var line in invoice.InvoiceMaterials)
							{
								// Find material details
								var material = await dbContext.MaterialMaster.FirstOrDefaultAsync(p => p.material3partycode == line.MaterialSapCode);

								if (material == null)
								{
									_logger.LogWarning("Material not found for invoice ID: {invoiceId}, Material Code: {materialCode}",
										invoice.InvoiceId, line.MaterialSapCode);
									continue; // Skip processing if material not found
								}

								// Count crates based on CratesCode
								if (material.CratesCode == "L")
								{
									large_crates += line.QuantityCases; // Assuming we count cases, not just incrementing by 1
								}
								else if (material.CratesCode == "S")
								{
									small_crates += line.QuantityCases; // Assuming we count cases, not just incrementing by 1
								}
								else
								{
									_logger.LogWarning("Unknown CratesCode '{cratesCode}' for material ID: {materialId}, Material Code: {materialCode}",
										material.CratesCode, material.Id, material.material3partycode);
								}
							}

							// Find segment code for the customer
							var customerSegment = await dbContext.CustomerSegementMap
								.FirstOrDefaultAsync(cs => cs.Customername == customer.Name);

							if (customerSegment == null)
							{
								_logger.LogWarning("Customer segment not found for customer ID: {customerId}, Name: {customerName}",
									customer.Id, customer.Name);
								continue; // Skip processing if customer segment not found
							}

							// Find crates types
							var largeCratesType = await dbContext.CratesTypes.FirstOrDefaultAsync(ct => ct.CratesCode == "L");
							var smallCratesType = await dbContext.CratesTypes.FirstOrDefaultAsync(ct => ct.CratesCode == "S");

							if (largeCratesType == null || smallCratesType == null)
							{
								_logger.LogWarning("");
								return;
							}

							// Create or update crates management records for large crates
							if (large_crates > 0)
							{
								var existingLargeCratesRecord = await dbContext.CratesManages
									.FirstOrDefaultAsync(cm => cm.CustomerId == customer.Id &&
															  cm.SegmentCode == customerSegment.custsegementcode &&
															  cm.DispDate.Date == invoice.InvoiceDate.Date &&
															  cm.CratesTypeId.HasValue &&
															  cm.CratesTypeId.Value == (largeCratesType != null ? largeCratesType.Id : 0));

								if (existingLargeCratesRecord != null)
								{
									// Update existing record
									existingLargeCratesRecord.Outward += large_crates;
									existingLargeCratesRecord.Balance = existingLargeCratesRecord.Opening +
																	   existingLargeCratesRecord.Outward -
																	   existingLargeCratesRecord.Inward;
									_logger.LogInformation("Updated existing large crates record for customer ID: {customerId}, Date: {date}, Outward: {outward}",
										customer.Id, invoice.InvoiceDate.Date, existingLargeCratesRecord.Outward);
								}
								else
								{
									// Check if a small crates record exists for the same date and customer
									var topRecord = await dbContext.CratesManages.Where(a => a.CustomerId == customer.Id && a.SegmentCode == customerSegment.custsegementcode && a.CratesTypeId == largeCratesType.Id)
											.OrderByDescending(a => a.DispDate)
											.FirstOrDefaultAsync();

									// Create new record
									var largeCratesManage = new CratesManage()
									{
										CustomerId = customer.Id,
										SegmentCode = customerSegment.custsegementcode,
										DispDate = invoice.InvoiceDate.Date,
										Opening = topRecord != null ? topRecord.Balance : 0,
										Outward = large_crates,
										Inward = 0,
										Balance = (topRecord?.Balance ?? 0) + large_crates,
										CratesTypeId = largeCratesType != null ? largeCratesType.Id : (int?)null
									};
									dbContext.CratesManages.Add(largeCratesManage);
									_logger.LogInformation("Created new large crates record for customer ID: {customerId}, Date: {date}, Outward: {outward}",
										customer.Id, invoice.InvoiceDate.Date, large_crates);
								}
							}

							// Create or update crates management records for small crates
							if (small_crates > 0)
							{
								var existingSmallCratesRecord = await dbContext.CratesManages
									.FirstOrDefaultAsync(cm => cm.CustomerId == customer.Id &&
															  cm.SegmentCode == customerSegment.custsegementcode &&
															  cm.DispDate.Date == invoice.InvoiceDate.Date &&
															  cm.CratesTypeId.HasValue &&
															  cm.CratesTypeId.Value == (smallCratesType != null ? smallCratesType.Id : 0));

								if (existingSmallCratesRecord != null)
								{
									// Update existing record
									existingSmallCratesRecord.Outward += small_crates;
									existingSmallCratesRecord.Balance = existingSmallCratesRecord.Opening +
																	   existingSmallCratesRecord.Outward -
																	   existingSmallCratesRecord.Inward;
									_logger.LogInformation("Updated existing small crates record for customer ID: {customerId}, Date: {date}, Outward: {outward}",
										customer.Id, invoice.InvoiceDate.Date, existingSmallCratesRecord.Outward);
								}
								else
								{
									var topRecord = await dbContext.CratesManages.Where(a => a.CustomerId == customer.Id && a.SegmentCode == customerSegment.custsegementcode && a.CratesTypeId == smallCratesType.Id)
											.OrderByDescending(a => a.DispDate)
											.FirstOrDefaultAsync();
									// Create new record
									var smallCratesManage = new CratesManage()
									{
										CustomerId = customer.Id,
										SegmentCode = customerSegment.custsegementcode,
										DispDate = invoice.InvoiceDate.Date,
										Opening = topRecord != null ? topRecord.Balance : 0,
										Outward = small_crates,
										Inward = 0,
										Balance = (topRecord?.Balance ?? 0) + small_crates,
										CratesTypeId = smallCratesType != null ? smallCratesType.Id : (int?)null
									};
									dbContext.CratesManages.Add(smallCratesManage);
									_logger.LogInformation("Created new small crates record for customer ID: {customerId}, Date: {date}, Outward: {outward}",
										customer.Id, invoice.InvoiceDate.Date, small_crates);
								}
							}

							// Mark invoice as processed
							invoice.setflag = 1;
							_logger.LogInformation("Marked invoice ID: {invoiceId}, Invoice No: {invoiceNo} as processed",
								invoice.InvoiceId, invoice.InvoiceNo);
						}
						catch (Exception ex)
						{
							_logger.LogError(ex, "Error processing invoice ID: {invoiceId}, Invoice No: {invoiceNo}",
								invoice.InvoiceId, invoice.InvoiceNo);
							// Continue with next invoice even if current one fails
						}
					}

					// Save all changes to the database
					await dbContext.SaveChangesAsync();
					_logger.LogInformation("Completed processing {count} invoices with setflag=0", invoices.Count);
				}
				else
				{
					_logger.LogInformation("No invoices found with setflag=0 at: {time}", DateTimeOffset.Now);
				}
			}
		}

		public override async Task StopAsync(CancellationToken cancellationToken)
		{
			_logger.LogInformation("Invoice Background Service is stopping at: {time}", DateTimeOffset.Now);
			await base.StopAsync(cancellationToken);
		}
	}
}