using AspNetCoreHero.ToastNotification;
using AspNetCoreHero.ToastNotification.Extensions;
using Microsoft.EntityFrameworkCore;
using Milk_Bakery.Data;
using Milk_Bakery.Middleware;
using Milk_Bakery.Models;
using Milk_Bakery.Services;
using System.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add CORS service
builder.Services.AddCors(options =>
{
	options.AddPolicy("AllowAll",
		builder =>
		{
			builder.AllowAnyOrigin()
				   .AllowAnyMethod()
				   .AllowAnyHeader();
		});
});

builder.Services.AddDbContext<MilkDbContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("AuthDbContextConnection"),
	sqlServerOptions => sqlServerOptions.CommandTimeout(500)));

builder.Services.AddMemoryCache();
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
builder.Services.AddSession();
builder.Services.AddNotyf(config => { config.DurationInSeconds = 5; config.IsDismissable = true; config.Position = NotyfPosition.TopRight; });

// Register services
builder.Services.AddScoped<IInvoiceMappingService, InvoiceMappingService>();

// Register background services
builder.Services.AddHostedService<InvoiceBackgroundService>();
builder.Services.AddHostedService<CratesManagementBackgroundService>();

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddMvc();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Home/Error");
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}

// Add global exception handling middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Use CORS middleware
app.UseCors("AllowAll");

app.UseNotyf();
app.UseSession();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseResponseCaching();
app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}");

//using (var scope = app.Services.CreateScope())
//{
//	var services = scope.ServiceProvider;
//	try
//	{
//		var context = services.GetRequiredService<MilkDbContext>();
//		DbInitializer.Initialize(context);
//	}
//	catch (Exception ex)
//	{
//		var logger = services.GetRequiredService<ILogger<Program>>();
//		logger.LogError(ex, "An error occurred while seeding the database.");
//	}
//}

app.Run();