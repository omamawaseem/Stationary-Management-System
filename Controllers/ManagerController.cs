using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [Authorize(Roles = "Manager")]
    public class ManagerController : Controller
    {
        private readonly StationeryContext _context;
        private readonly ILogger<ManagerController> _logger;

        public ManagerController(StationeryContext context, ILogger<ManagerController> logger)
        {
            _context = context;
            _logger = logger;
        }

        private int? GetManagerNumber()
        {
            return HttpContext.Session.GetInt32("EmployeeNumber");
        }

        private IActionResult? CheckSession()
        {
            if (GetManagerNumber() == null)
                return RedirectToAction("Login", "Account");
            return null;
        }
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public IActionResult Dashboard()
        {
            var employeeNumber = HttpContext.Session.GetInt32("EmployeeNumber");
            var items = _context.Items.ToList();
            var brands = _context.Brands.ToList();
            var categories = _context.Categories.ToList();
            if (employeeNumber == null)
                return RedirectToAction("Login", "Account");

            var employee = _context.Employees.FirstOrDefault(e => e.EmployeeNumber == employeeNumber);
            if (employee != null)
            {
                ViewData["Name"] = employee.Name;
            }
            var sessionCheck = CheckSession();
            if (sessionCheck != null) return sessionCheck;

            int managerNumber = GetManagerNumber().Value;

            var employees = _context.Employees
                .Where(e => e.SuperiorEmployeeNumber == managerNumber)
                .ToList();

            var requests = _context.Requests
                .Where(r => employees.Select(e => e.EmployeeNumber).Contains(r.EmployeeNumber) && r.Status == "Pending")
                .ToList();
            ViewBag.Items = items;
            ViewBag.Brands = brands;
            ViewBag.Categories = categories;
            ViewBag.Employees = employees;
            return View(requests);
        }

        public IActionResult ViewEmployees()
        {
            var sessionCheck = CheckSession();
            if (sessionCheck != null) return sessionCheck;

            int managerNumber = GetManagerNumber().Value;

            var employees = _context.Employees
                .Where(e => e.SuperiorEmployeeNumber == managerNumber)
                .ToList();

            return View(employees);
        }

        // GET: /Manager/ApproveRequest/5
        public IActionResult ApproveRequest(int id)
        {
            var sessionCheck = CheckSession();
            if (sessionCheck != null) return sessionCheck;

            var request = _context.Requests.Find(id);
            if (request == null || !IsRequestUnderManager(request))
                return NotFound();

            return View(request);
        }

        // POST: /Manager/ApproveRequest/5
        [HttpPost]
        public IActionResult ApproveRequestConfirm(int id)
        {
            var sessionCheck = CheckSession();
            if (sessionCheck != null) return sessionCheck;

            var request = _context.Requests.Find(id);
            if (request != null && request.Status == "Pending" && IsRequestUnderManager(request))
            {
                request.Status = "Approved";
                _context.SaveChanges();

                // Create notification
                var notification = new Notification
                {
                    EmployeeNumber = request.EmployeeNumber,
                    Message = $"Request #{id} has been approved.",
                    DateCreated = DateTime.Now,
                    IsRead = false
                };

                _context.Notifications.Add(notification);
                _context.SaveChanges();

                TempData["Notification"] = "Request approved.";
            }
            return RedirectToAction("Dashboard");
        }

        // GET: /Manager/RejectRequest/5
        public IActionResult RejectRequest(int id)
        {
            var sessionCheck = CheckSession();
            if (sessionCheck != null) return sessionCheck;

            var request = _context.Requests.Find(id);
            if (request == null || !IsRequestUnderManager(request))
                return NotFound();

            return View(request);
        }

        // POST: /Manager/RejectRequest/5
        [HttpPost]
        public IActionResult RejectRequestConfirm(int id)
        {
            var sessionCheck = CheckSession();
            if (sessionCheck != null) return sessionCheck;

            var request = _context.Requests.Find(id);
            if (request != null && request.Status == "Pending" && IsRequestUnderManager(request))
            {
                request.Status = "Rejected";
                _context.SaveChanges();

                // Create notification
                var notification = new Notification
                {
                    EmployeeNumber = request.EmployeeNumber,
                    Message = $"Request #{id} has been rejected.",
                    DateCreated = DateTime.Now,
                    IsRead = false
                };
                _context.Notifications.Add(notification);
                _context.SaveChanges();

                TempData["Notification"] = "Request rejected.";
            }
            return RedirectToAction("Dashboard");
        }
        // GET: /Manager/AddEmployee
        public IActionResult AddEmployeeBy()
        {
            var sessionCheck = CheckSession();
            if (sessionCheck != null) return sessionCheck;
        
            return View();
        }

        // POST: /Manager/AddEmployee
        [HttpPost]
        public IActionResult AddEmployeeBy(Employee employee)
        {
            var sessionCheck = CheckSession();
            if (sessionCheck != null) return sessionCheck;

            try
            {
                int managerNumber = GetManagerNumber().Value;

                // Check for duplicate employee number
                if (_context.Employees.Any(e => e.EmployeeNumber == employee.EmployeeNumber))
                {
                    ModelState.AddModelError("EmployeeNumber", "Employee number already exists.");
                    return View(employee);
                }
                // Check for Unique Email
                if (_context.Employees.Any(e => e.Email.ToLower() == employee.Email.ToLower()))
                {
                    ModelState.AddModelError("Email", "Email exist make it unique (eg: example123@gmail.com).");
                    return View(employee);
                }
                // Assign manager (Superior) based on the logged-in manager
                employee.SuperiorEmployeeNumber = managerNumber;
                employee.Role = "Employee";
                _context.Employees.Add(employee);
                _context.SaveChanges();

                TempData["SuccessMessage"] = "Employee added successfully.";
                return RedirectToAction("ViewEmployees");
            }
            catch (Exception ex)
            {
                // Log the error
                _logger.LogError(ex, "Error adding employee");
                ModelState.AddModelError("", "An error occurred while saving. Please try again.");
              
                return View(employee);
            }
        }
        // POST: /Manager/DeleteEmployee/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteEmployee(int id)
        {
            var sessionCheck = CheckSession();
            if (sessionCheck != null) return sessionCheck;

            int managerNumber = GetManagerNumber().Value;
            var employee = _context.Employees
                .Include(e => e.Requests)  // Include related data if needed
                .FirstOrDefault(e => e.EmployeeNumber == id && e.SuperiorEmployeeNumber == managerNumber);

            if (employee == null)
            {
                TempData["ErrorMessage"] = "Employee not found or unauthorized to delete.";
                return RedirectToAction("ViewEmployees");
            }

            try
            {
                // Check for dependent records
                if (_context.Requests.Any(r => r.EmployeeNumber == id))
                {
                    TempData["ErrorMessage"] = "Cannot delete employee with existing requests.";
                    return RedirectToAction("ViewEmployees");
                }

                _context.Employees.Remove(employee);
                _context.SaveChanges();
                TempData["SuccessMessage"] = $"{employee.Name} deleted successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting employee");
                TempData["ErrorMessage"] = "Error deleting employee. Please try again.";
            }

            return RedirectToAction("ViewEmployees");
        }
        private bool IsRequestUnderManager(Request request)
        {
            int managerNumber = GetManagerNumber().Value;
            var employee = _context.Employees
                .FirstOrDefault(e => e.EmployeeNumber == request.EmployeeNumber);

            return employee?.SuperiorEmployeeNumber == managerNumber;
        }
    }
}
