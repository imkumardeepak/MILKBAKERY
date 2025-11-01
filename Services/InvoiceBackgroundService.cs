using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
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
					await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error occurred while processing invoices with setflag=0 at: {time}", DateTimeOffset.Now);
				}
			}
		}

		private async Task ProcessInvoicesWithSetFlagZero()
		{
			try
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

								// Dictionary to store material crates count by crate type
								var crateTypeCounts = new Dictionary<string, int>();

								// Find customer details
								var customer = await dbContext.Customer_Master.FirstOrDefaultAsync(c => c.shortname == invoice.ShipToCode);
								if (customer == null)
								{
									_logger.LogWarning("Customer not found for invoice ID: {invoiceId}, ShipToCode: {shipToCode}",
										invoice.InvoiceId, invoice.ShipToCode);
									continue; // Skip processing if customer not found
								}

								// Process each invoice material and build crate type counts
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

									// Get crate type from material
									string crateTypeName = material.CratesTypes ?? "";
									if (!string.IsNullOrEmpty(crateTypeName))
									{
										// Add to dictionary or increment count
										if (crateTypeCounts.ContainsKey(crateTypeName))
										{
											crateTypeCounts[crateTypeName] += line.QuantityCases;
										}
										else
										{
											crateTypeCounts[crateTypeName] = line.QuantityCases;
										}
									}
									else
									{
										_logger.LogWarning("No crate type specified for material ID: {materialId}, Material Code: {materialCode}",
											material.Id, material.material3partycode);
									}
								}

								// Process each crate type from the dictionary
								foreach (var kvp in crateTypeCounts)
								{
									string crateTypeName = kvp.Key;
									int crateCount = kvp.Value;

									// Find matching crate type in our crate types list
									var matchingCrateType = dbContext.CratesTypes.FirstOrDefault(c => c.Cratestype == crateTypeName);
									if (matchingCrateType != null)
									{
										// Get customer segments
										var customerSegments = await dbContext.CustomerSegementMap
											.Where(csm => csm.Customername == customer.Name)
											.ToListAsync();

										// Create a crates record for each segment the customer belongs to
										foreach (var segment in customerSegments)
										{
											// Check if a record already exists for this customer/segment/crate combination
											var existingRecord = await dbContext.CratesManages
												.Where(cm => cm.CustomerId == customer.Id &&
															cm.SegmentCode == segment.custsegementcode &&
															cm.DispDate == invoice.InvoiceDate.Date &&
															cm.CratesTypeId == matchingCrateType.Id)
												.OrderByDescending(cm => cm.DispDate)
												.FirstOrDefaultAsync();

											if (existingRecord != null)
											{
												// Update existing record
												existingRecord.Outward += crateCount;
												existingRecord.Balance = existingRecord.Opening +
																				   existingRecord.Outward -
																				   existingRecord.Inward;
												_logger.LogInformation("Updated existing large crates record for customer ID: {customerId}, Date: {date}, Outward: {outward}",
													customer.Id, invoice.InvoiceDate.Date, existingRecord.Outward);
											}
											else
											{
												var topRecord = await dbContext.CratesManages.Where(a => a.CustomerId == customer.Id && a.SegmentCode == segment.custsegementcode && a.CratesTypeId == matchingCrateType.Id)
											.OrderByDescending(a => a.DispDate)
											.FirstOrDefaultAsync();

												if (topRecord != null)
												{
													// Create new record
													var largeCratesManage = new CratesManage()
													{
														CustomerId = customer.Id,
														SegmentCode = segment.custsegementcode,
														DispDate = invoice.InvoiceDate.Date,
														Opening = topRecord != null ? topRecord.Balance : 0,
														Outward = crateCount,
														Inward = 0,
														Balance = (topRecord?.Balance ?? 0) + crateCount,
														CratesTypeId = matchingCrateType != null ? matchingCrateType.Id : (int?)null
													};
													dbContext.CratesManages.Add(largeCratesManage);
													_logger.LogInformation("Created new large crates record for customer ID: {customerId}, Date: {date}, Outward: {outward}",
														customer.Id, invoice.InvoiceDate.Date, crateCount);
												}
												else
												{
													// Create new record
													var largeCratesManage = new CratesManage()
													{
														CustomerId = customer.Id,
														SegmentCode = segment.custsegementcode,
														DispDate = DateTime.Now.Date,
														Opening = 0,
														Outward = crateCount,
														Inward = 0,
														Balance = crateCount,
														CratesTypeId = matchingCrateType != null ? matchingCrateType.Id : (int?)null

													};

													dbContext.CratesManages.Add(largeCratesManage);
													_logger.LogInformation("Created new large crates record for customer ID: {customerId}, Date: {date}, Outward: {outward}",
														customer.Id, invoice.InvoiceDate.Date, crateCount);
												}
											}
										}
									}
									else
									{
										_logger.LogWarning("Crate type '{crateType}' not found in crate types list", crateTypeName);
									}
								}

								// Mark invoice as processed
								invoice.setflag = 1;
								dbContext.Invoices.Update(invoice);
							}
							catch (Exception ex)
							{
								_logger.LogError(ex, "Error processing invoice ID: {invoiceId}, Invoice No: {invoiceNo}",
									invoice.InvoiceId, invoice.InvoiceNo);
								// Continue with next invoice even if current one fails
							}
						}

						// Save all changes
						await dbContext.SaveChangesAsync();
						_logger.LogInformation("Successfully processed {count} invoices", invoices.Count);
					}
					else
					{
						_logger.LogInformation("No invoices found with setflag=0 at: {time}", DateTimeOffset.Now);
					}
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error occurred while processing invoices in background service");
			}
		}

		public override async Task StopAsync(CancellationToken cancellationToken)
		{
			_logger.LogInformation("Invoice Background Service is stopping at: {time}", DateTimeOffset.Now);
			await base.StopAsync(cancellationToken);
		}
	}
}