using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
	public class Category
	{
        
        [Key]
		public int CategoryID { get; set; }

		[Required]
		public string CategoryName { get; set; }
         [Range(1, int.MaxValue, ErrorMessage = "The Brand field is required.")]
        [ForeignKey("Brand")]
        public int BrandID { get; set; }


        // Navigation properties
        public virtual Brand Brand { get; set; }
		public virtual ICollection<Item> Items { get; set; }
	}
}