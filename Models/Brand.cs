using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
	public class Brand
	{
        public Brand()
        {
            Categories = new HashSet<Category>();
        }
        [Key]
		public int BrandID { get; set; }

		[Required]
		public string BrandName { get; set; }

		// Navigation property
		public virtual ICollection<Category> Categories { get; set; }
	}
}