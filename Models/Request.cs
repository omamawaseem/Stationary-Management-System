
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
	public class Request
	{
		[Key]
		public int RequestID { get; set; }
		public int EmployeeNumber { get; set; }
		public int ItemID { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
        public int QuantityRequested { get; set; }

        [DataType(DataType.Date)]
        [FutureDate(ErrorMessage = "Needed date must be in the future.")]
        public DateTime NeededDate { get; set; }
        public string Status { get; set; }

		[ForeignKey("EmployeeNumber")]
		public virtual Employee Employee { get; set; }
		[ForeignKey("ItemID")]
		public virtual Item Item { get; set; }

	}
}