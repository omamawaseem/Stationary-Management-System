using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
	public class Item
	{
		[Key]
		public int ItemID { get; set; }
		[Required]
		public string ItemName { get; set; }
		public decimal Cost { get; set; }
		public int QuantityAvailable { get; set; }

		public int? BrandID {  get; set; }
		public int CategoryID { get; set; }
		[ForeignKey("CategoryID")]
		public virtual Category Category { get; set; }
        [ForeignKey("BrandID")]
        public virtual Brand Brand { get; set; }
        public virtual ICollection<Request> Requests { get; set; }
     
    }
}
