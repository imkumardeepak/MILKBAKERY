using Microsoft.EntityFrameworkCore;
using Milk_Bakery.Models;
using static Milk_Bakery.Models.InvoiceDetails;

namespace Milk_Bakery.Data
{
	public class MilkDbContext : DbContext
	{
		public MilkDbContext(DbContextOptions<MilkDbContext> options) : base(options)
		{
		}

		public DbSet<Customer_Master> Customer_Master { get; set; } = default!;
		public DbSet<PlantMaster> PlantMaster { get; set; } = default!;
		public DbSet<CompanyMaster> CompanyMaster { get; set; } = default!;
		public DbSet<CategoryMaster> CategoryMaster { get; set; } = default!;
		public DbSet<DepartmentMaster> DepartmentMaster { get; set; } = default!;
		public DbSet<DesignationMaster> DesignationMaster { get; set; } = default!;
		public DbSet<GradeMaster> GradeMaster { get; set; } = default!;
		public DbSet<RouteMaster> RouteMaster { get; set; } = default!;
		public DbSet<UnitMaster> UnitMaster { get; set; } = default!;
		public DbSet<Sub_CategoryMaster> Sub_CategoryMaster { get; set; } = default!;
		public DbSet<EmployeeMaster> EmployeeMaster { get; set; } = default!;
		public DbSet<SegementMaster> SegementMaster { get; set; } = default!;
		public DbSet<Company_SegementMap> Company_SegementMap { get; set; } = default!;
		public DbSet<CustomerSegementMap> CustomerSegementMap { get; set; } = default!;
		public DbSet<MaterialMaster> MaterialMaster { get; set; } = default!;
		public DbSet<PurchaseOrder> PurchaseOrder { get; set; } = default!;
		public DbSet<ProductDetail> ProductDetails { get; set; } = default!;
		public DbSet<CustTransaction> custTransactions { get; set; } = default!;
		public DbSet<User> Users { get; set; } = default!;
		public DbSet<Cust2CustMap> Cust2CustMap { get; set; } = default!;
		public DbSet<Mappedcust> mappedcusts { get; set; } = default!;
		public DbSet<EmpToCustMap> EmpToCustMap { get; set; } = default!;
		public DbSet<Cust2EmpMap> cust2EmpMaps { get; set; } = default!;
		public DbSet<VisitEntery> VisitEntery { get; set; } = default!;
		public DbSet<CratesType> CratesTypes { get; set; } = default!;
		public DbSet<DealerMaster> DealerMasters { get; set; } = default!;
		public DbSet<DealerBasicOrder> DealerBasicOrders { get; set; } = default!;
		public DbSet<DealerOrder> DealerOrders { get; set; } = default!;
		public DbSet<DealerOrderItem> DealerOrderItems { get; set; } = default!;

		public DbSet<CratesManage> CratesManages { get; set; } = default!;
		public DbSet<Invoice> Invoices { get; set; } = default!;
		public DbSet<InvoiceMaterialDetail> InvoiceMaterials { get; set; } = default!;

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			modelBuilder.Entity<DealerBasicOrder>()
				.HasOne(dbo => dbo.DealerMaster)
				.WithMany(dm => dm.DealerBasicOrders)
				.HasForeignKey(dbo => dbo.DealerId)
				.OnDelete(DeleteBehavior.Cascade);


			modelBuilder.Entity<DealerOrderItem>()
				.HasOne(doi => doi.DealerOrder)
				.WithMany(so => so.DealerOrderItems)
				.HasForeignKey(doi => doi.DealerOrderId)
				.OnDelete(DeleteBehavior.Cascade);

			// Composite index on OrderDate, DealerId, DistributorId, and ProcessFlag
			modelBuilder.Entity<DealerOrder>()
			   .HasIndex(a => new { a.OrderDate, a.DealerId, a.DistributorId, a.ProcessFlag });
			// Composite index on DealerOrderId, SapCode, and ShortCode
			modelBuilder.Entity<DealerOrderItem>()
				.HasIndex(m => new { m.DealerOrderId, m.SapCode, m.ShortCode });

			modelBuilder.Entity<InvoiceMaterialDetail>()
				.HasOne(m => m.Invoice)
				.WithMany(i => i.InvoiceMaterials)
				.HasForeignKey(m => m.InvoiceId)
				.OnDelete(DeleteBehavior.Cascade);

			modelBuilder.Entity<InvoiceMaterialDetail>()
				.HasIndex(m => new { m.InvoiceId, m.MaterialId });

			modelBuilder.Entity<Invoice>()
				.HasIndex(m => new { m.InvoiceId, m.ShipToCode, m.BillToCode, m.InvoiceDate, m.VehicleNo });
		}
	}
}