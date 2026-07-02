using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
  
    public class Employee
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Key]
        [DisplayName("Employee ID")]
        [Required(ErrorMessage = "Employee Number is required.")]
        public int EmployeeNumber { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        public string Name { get; set; }

        [StringLength(100, ErrorMessage = "Password must be at most 100 characters.")]
        public string Password { get; set; }
        [EmailAddress]
        [Required(ErrorMessage = "Email is required.")]
        public string Email { get; set; }
        [DisplayName("Superior ID")]
        public int? SuperiorEmployeeNumber { get; set; }

        [Required]
        public string Role { get; set; }
        [ForeignKey("SuperiorEmployeeNumber")]
        public virtual Employee? Superior { get; set; }
        public virtual ICollection<Request> Requests { get; set; } = new List<Request>();

      
    }
}
