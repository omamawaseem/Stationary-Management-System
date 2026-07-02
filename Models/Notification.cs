namespace WebApplication1.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public int EmployeeNumber { get; set; }
        public string Message { get; set; }
        public DateTime DateCreated { get; set; }
        public bool IsRead { get; set; }
    }
}
