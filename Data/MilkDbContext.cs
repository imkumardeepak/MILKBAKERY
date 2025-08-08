using Microsoft.EntityFrameworkCore;
using Milk_Bakery.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace Milk_Bakery.Data
{
	public class MilkDbContext : DbContext
	{
		public MilkDbContext(DbContextOptions<MilkDbContext> options) : base(options)
		{
		}
		public DbSet<Milk_Bakery.Models.Customer_Master> Customer_Master { get; set; } = default!;
		public DbSet<Milk_Bakery.Models.PlantMaster> PlantMaster { get; set; } = default!;
		public DbSet<Milk_Bakery.Models.CompanyMaster> CompanyMaster { get; set; } = default!;
		public DbSet<Milk_Bakery.Models.CategoryMaster> CategoryMaster { get; set; } = default!;
		public DbSet<Milk_Bakery.Models.DepartmentMaster> DepartmentMaster { get; set; } = default!;
		public DbSet<Milk_Bakery.Models.DesignationMaster> DesignationMaster { get; set; } = default!;
		public DbSet<Milk_Bakery.Models.GradeMaster> GradeMaster { get; set; } = default!;
		public DbSet<Milk_Bakery.Models.RouteMaster> RouteMaster { get; set; } = default!;
		public DbSet<Milk_Bakery.Models.UnitMaster> UnitMaster { get; set; } = default!;
		public DbSet<Milk_Bakery.Models.Sub_CategoryMaster> Sub_CategoryMaster { get; set; } = default!;
		public DbSet<Milk_Bakery.Models.EmployeeMaster> EmployeeMaster { get; set; } = default!;
		public DbSet<Milk_Bakery.Models.SegementMaster> SegementMaster { get; set; } = default!;
		public DbSet<Milk_Bakery.Models.Company_SegementMap> Company_SegementMap { get; set; } = default!;
		public DbSet<Milk_Bakery.Models.CustomerSegementMap> CustomerSegementMap { get; set; } = default!;
		public DbSet<Milk_Bakery.Models.MaterialMaster> MaterialMaster { get; set; } = default!;
		public DbSet<Milk_Bakery.Models.PurchaseOrder> PurchaseOrder { get; set; } = default!;
		public DbSet<Milk_Bakery.Models.ProductDetail> ProductDetails { get; set; } = default!;
		public DbSet<Milk_Bakery.Models.CustTransaction> custTransactions { get; set; } = default!;
		public DbSet<Milk_Bakery.Models.User> Users { get; set; } = default!;
		public DbSet<Milk_Bakery.Models.Cust2CustMap> Cust2CustMap { get; set; } = default!;
		public DbSet<Milk_Bakery.Models.Mappedcust> mappedcusts { get; set; } = default!;
		public DbSet<Milk_Bakery.Models.EmpToCustMap> EmpToCustMap { get; set; } = default!;
		public DbSet<Milk_Bakery.Models.Cust2EmpMap> cust2EmpMaps { get; set; } = default!;
		public DbSet<Milk_Bakery.Models.VisitEntery> VisitEntery { get; set; } = default!;
		public DbSet<CratesType> CratesTypes { get; set; } = default!;


	}
}
