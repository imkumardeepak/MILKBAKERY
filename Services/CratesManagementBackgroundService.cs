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
	public class CratesManagementBackgroundService : BackgroundService
	{
		private readonly ILogger<CratesManagementBackgroundService> _logger;
		private readonly IServiceProvider _serviceProvider;

		public CratesManagementBackgroundService(ILogger<CratesManagementBackgroundService> logger, IServiceProvider serviceProvider)
		{
			_logger = logger;
			_serviceProvider = serviceProvider;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			_logger.LogInformation("Crates Management Background Service started at: {time}", DateTimeOffset.Now);

			while (!stoppingToken.IsCancellationRequested)
			{
				try
				{
					// Calculate the time until next midnight
					var now = DateTime.Now;
					var nextRun = now.Date.AddDays(1); // Tomorrow at 00:00:00
					var delay = nextRun - now;

					_logger.LogInformation("Crates Management Background Service will run at: {time}", nextRun);
					_logger.LogInformation("Waiting for {hours} hours and {minutes} minutes", delay.Hours, delay.Minutes);

					// Wait until next midnight
					await Task.Delay(delay, stoppingToken);

					_logger.LogInformation("Crates Management Background Service running at: {time}", DateTimeOffset.Now);
					await ProcessDailyCratesManagement();

					// After processing, wait for a short time before the next calculation
					await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error occurred in Crates Management Background Service at: {time}", DateTimeOffset.Now);
				}
			}
		}

		private async Task ProcessDailyCratesManagement()
		{
			try
			{
				using (var scope = _serviceProvider.CreateScope())
				{
					var dbContext = scope.ServiceProvider.GetRequiredService<MilkDbContext>();
					var currentDate = DateTime.Today;

					_logger.LogInformation("Starting daily crates management process for date: {date}", currentDate);

					// Get all unique customer-segment-crate combinations that have records
					var existingCombinations = await dbContext.CratesManages
						.Select(cm => new { cm.CustomerId, cm.SegmentCode, cm.CratesTypeId })
						.Distinct()
						.ToListAsync();

					_logger.LogInformation("Found {count} existing customer-segment-crate combinations", existingCombinations.Count);

					int recordsCreated = 0;

					// Process each combination
					foreach (var combination in existingCombinations)
					{
						try
						{
							// Check if a record already exists for today
							var existingRecord = await dbContext.CratesManages
								.Where(cm => cm.CustomerId == combination.CustomerId &&
											cm.SegmentCode == combination.SegmentCode &&
											cm.CratesTypeId == combination.CratesTypeId &&
											cm.DispDate == currentDate)
								.FirstOrDefaultAsync();

							// If no record exists for today, create one
							if (existingRecord == null)
							{
								// Get the last available balance for this combination
								var lastRecord = await dbContext.CratesManages
									.Where(cm => cm.CustomerId == combination.CustomerId &&
												cm.SegmentCode == combination.SegmentCode &&
												cm.CratesTypeId == combination.CratesTypeId &&
												cm.DispDate < currentDate)
									.OrderByDescending(cm => cm.DispDate)
									.FirstOrDefaultAsync();

								// Create a new entry with the last balance as opening balance
								var newRecord = new CratesManage
								{
									CustomerId = combination.CustomerId,
									SegmentCode = combination.SegmentCode,
									DispDate = currentDate,
									Opening = lastRecord?.Balance ?? 0,
									Outward = 0,
									Inward = 0,
									Balance = lastRecord?.Balance ?? 0,
									CratesTypeId = combination.CratesTypeId
								};

								dbContext.CratesManages.Add(newRecord);
								recordsCreated++;

								_logger.LogInformation("Created new crates record for CustomerId: {customerId}, SegmentCode: {segmentCode}, CrateTypeId: {crateTypeId}, Date: {date}, Opening Balance: {opening}",
									combination.CustomerId, combination.SegmentCode, combination.CratesTypeId, currentDate, newRecord.Opening);
							}
						}
						catch (Exception ex)
						{
							_logger.LogError(ex, "Error processing combination for CustomerId: {customerId}, SegmentCode: {segmentCode}, CrateTypeId: {crateTypeId}",
								combination.CustomerId, combination.SegmentCode, combination.CratesTypeId);
						}
					}

					// Save all changes
					await dbContext.SaveChangesAsync();
					_logger.LogInformation("Successfully created {count} new crates records for date: {date}", recordsCreated, currentDate);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error occurred while processing daily crates management");
			}
		}

		public override async Task StopAsync(CancellationToken cancellationToken)
		{
			_logger.LogInformation("Crates Management Background Service is stopping at: {time}", DateTimeOffset.Now);
			await base.StopAsync(cancellationToken);
		}
	}
}