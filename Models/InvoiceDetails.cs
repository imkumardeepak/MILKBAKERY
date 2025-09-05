using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Milk_Bakery.Models
{
	public class InvoiceDetails
	{
		public class Invoice
		{
			[Key]
			public int InvoiceId { get; set; }

			[Required]
			[MaxLength(50)]
			public string InvoiceNo { get; set; }

			public DateTime InvoiceDate { get; set; }

			[MaxLength(50)]
			public string CustomerRefPO { get; set; } // PO No.

			[Column(TypeName = "decimal(18,2)")]
			public decimal TotalAmount { get; set; }

			public DateTime OrderDate { get; set; }

			
			[MaxLength(200)]
			public string BillToName { get; set; }

			[MaxLength(50)]
			public string BillToCode { get; set; }

			[MaxLength(200)]
			public string ShipToName { get; set; }

			[MaxLength(50)]
			public string ShipToCode { get; set; }

			[MaxLength(100)]
			public string ShipToRoute { get; set; }

			[MaxLength(200)]
			public string CompanyName { get; set; }

			[MaxLength(50)]
			public string CompanyCode { get; set; }

			[MaxLength(50)]
			public string VehicleNo { get; set; }

		
			public List<InvoiceMaterialDetail> InvoiceMaterials { get; set; }
		}

		public class InvoiceMaterialDetail
		{
			[Key]
			public int MaterialId { get; set; }

			// Foreign Key to Invoice
			public int InvoiceId { get; set; }

			public Invoice? Invoice { get; set; }

			[MaxLength(200)]
			public string ProductDescription { get; set; }

			[MaxLength(50)]
			public string MaterialSapCode { get; set; }

			[MaxLength(50)]
			public string? Batch { get; set; }

			public int UnitPerCase { get; set; }
			public int QuantityCases { get; set; }
			public int QuantityUnits { get; set; }
			[MaxLength(20)]
			public string UOM { get; set; }

		}
	}
}
