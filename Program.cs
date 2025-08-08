using AspNetCoreHero.ToastNotification;
using AspNetCoreHero.ToastNotification.Extensions;
using Microsoft.EntityFrameworkCore;
using Milk_Bakery.Data;
using Milk_Bakery.Models;
using System.Configuration;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddDbContext<MilkDbContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("AuthDbContextConnection"),
	sqlServerOptions => sqlServerOptions.CommandTimeout(500))); 

//builder.Services.AddDbContext<MilkDbContext>(options =>
//   options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
builder.Services.AddSession();
builder.Services.AddNotyf(config => { config.DurationInSeconds = 5; config.IsDismissable = true; config.Position = NotyfPosition.TopRight; });
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

app.Run();
