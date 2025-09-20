using Microsoft.EntityFrameworkCore;
using Milk_Bakery.Data;
using Milk_Bakery.Models;
using System;
using System.Linq;

namespace Milk_Bakery.Data
{
	public static class DbInitializer
	{
		public static void Initialize(MilkDbContext context)
		{
			context.Database.EnsureCreated();

			// Seed Roles if none exist
			if (!context.Roles.Any())
			{
				var roles = new Role[]
				{
					new Role{RoleName="Admin"},
					new Role{RoleName="Manager"},
					new Role{RoleName="Customer"},
					new Role{RoleName="Plant"},
					new Role{RoleName="Dealer"},
					new Role{RoleName="Sales"},
				};

				foreach (Role r in roles)
				{
					context.Roles.Add(r);
				}
				context.SaveChanges();
			}

			// Seed PageAccess if none exist
			if (!context.PageAccesses.Any())
			{
				var pageAccesses = new PageAccess[]
				{
					new PageAccess{PageName="CategoryMasters", HasAccess=true, RoleId=context.Roles.Single(r => r.RoleName == "Manager").Id},
					new PageAccess{PageName="Sub_CategoryMaster", HasAccess=true, RoleId=context.Roles.Single(r => r.RoleName == "Manager").Id},
					new PageAccess{PageName="ProductMaster", HasAccess=true, RoleId=context.Roles.Single(r => r.RoleName == "Manager").Id},
					new PageAccess{PageName="PlantMaster", HasAccess=true, RoleId=context.Roles.Single(r => r.RoleName == "Manager").Id},
					new PageAccess{PageName="UserManage", HasAccess=true, RoleId=context.Roles.Single(r => r.RoleName == "Admin").Id},
				};

				foreach (PageAccess p in pageAccesses)
				{
					context.PageAccesses.Add(p);
				}
				context.SaveChanges();
			}

			// Seed MenuItems if none exist
			if (!context.MenuItems.Any())
			{
				var menuItems = new MenuItem[]
				{
					new MenuItem{Name="Home", Controller="Home", Action="Index"},
					new MenuItem{Name="Masters", Controller="", Action=""},
					new MenuItem{Name="Category", Controller="CategoryMasters", Action="Index", ParentId=2},
					new MenuItem{Name="Sub-Category", Controller="Sub_CategoryMaster", Action="Index", ParentId=2},
					new MenuItem{Name="Company Master", Controller="CompanyMasters", Action="Index", ParentId=2},
					new MenuItem{Name="Customer Master", Controller="Customer_Master", Action="Index", ParentId=2},
					new MenuItem{Name="Employee Master", Controller="EmployeeMasters", Action="Index", ParentId=2},
					new MenuItem{Name="CratesType Master", Controller="CratesTypes", Action="Index", ParentId=2},
					new MenuItem{Name="Material Master", Controller="MaterialMasters", Action="Index", ParentId=2},
					new MenuItem{Name="Segement Master", Controller="SegementMasters", Action="Index", ParentId=2},
					new MenuItem{Name="Comp-Sege_Mapp", Controller="Company_SegementMap", Action="Index", ParentId=2},
					new MenuItem{Name="Cust_Sege_Mapp", Controller="CustomerSegementMaps", Action="Index", ParentId=2},
					new MenuItem{Name="Customer Map", Controller="Cust2CustMap", Action="Index", ParentId=2},
					new MenuItem{Name="Employee Map", Controller="EmpToCustMaps", Action="Index", ParentId=2},
					new MenuItem{Name="Department Master", Controller="DepartmentMasters", Action="Index", ParentId=2},
					new MenuItem{Name="Designation Master", Controller="DesignationMasters", Action="Index", ParentId=2},
					new MenuItem{Name="Grade Master", Controller="GradeMasters", Action="Index", ParentId=2},
					new MenuItem{Name="Route Master", Controller="RouteMasters", Action="Index", ParentId=2},
					new MenuItem{Name="Unit Master", Controller="UnitMasters", Action="Index", ParentId=2},
					new MenuItem{Name="User Manage", Controller="Users", Action="Index", ParentId=2},
					new MenuItem{Name="Dealer Master", Controller="DealerMasters", Action="Index", ParentId=2},
					new MenuItem{Name="Crates Manage", Controller="CratesManages", Action="Index", ParentId=2},
					new MenuItem{Name="Operation", Controller="", Action=""},
					new MenuItem{Name="Place Order", Controller="PurchaseOrders", Action="Create", ParentId=23},
					new MenuItem{Name="Order View", Controller="PurchaseOrders", Action="Index", ParentId=23},
					new MenuItem{Name="File Generation", Controller="OrderProcessFile", Action="Index", ParentId=23},
					new MenuItem{Name="Order Repeat", Controller="RepeatOrder", Action="Index", ParentId=23},
					new MenuItem{Name="Order Re-Process", Controller="ReProcess", Action="Index", ParentId=23},
					new MenuItem{Name="Dealer Order Entry", Controller="DealerOrders", Action="Index", ParentId=23},
					new MenuItem{Name="Import File", Controller="", Action=""},
					new MenuItem{Name="Upload Transaction", Controller="CustTrans", Action="Index", ParentId=30},
					new MenuItem{Name="Report", Controller="", Action=""},
					new MenuItem{Name="Bill Summary", Controller="Outstanding", Action="Index", ParentId=32},
					new MenuItem{Name="Route Summary Report", Controller="RouteReport", Action="Index", ParentId=32},
					new MenuItem{Name="Tracking Report", Controller="VisitReport", Action="Index", ParentId=32},
					new MenuItem{Name="Crates Tracking Report", Controller="CratesTrackingReport", Action="Index", ParentId=32},
					new MenuItem{Name="Gate Pass", Controller="GatePass", Action="Index", ParentId=32},
					new MenuItem{Name="Variance Order Report", Controller="VarianceOrderReport", Action="Index", ParentId=32},
				};

				foreach (MenuItem m in menuItems)
				{
					context.MenuItems.Add(m);
				}
				context.SaveChanges();
			}

			// Seed Admin User if none exist
			if (!context.Users.Any(u => u.phoneno == "1234567890"))
			{
				var adminUser = new User
				{
					name = "Admin",
					phoneno = "1234567890",
					Password = "1234", // Storing passwords in plain text is not secure. This should be hashed in a real application.
					Role = "Admin"
				};
				context.Users.Add(adminUser);
				context.SaveChanges();
			}
		}
	}
}