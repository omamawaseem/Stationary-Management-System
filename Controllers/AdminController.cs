using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {


        private readonly StationeryContext _context;

        public AdminController(StationeryContext context)
        {
            _context = context;
        }
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        // GET: /Admin/Dashboard
        public IActionResult Dashboard()
        {
            var employeeNumber = HttpContext.Session.GetInt32("EmployeeNumber");

            if (employeeNumber == null)
                return RedirectToAction("Login", "Account");

            var employee = _context.Employees.FirstOrDefault(e => e.EmployeeNumber == employeeNumber);
            if (employee != null)
            {
                ViewData["Name"] = employee.Name;
            }
            var items = _context.Items.ToList();
            var brands = _context.Brands.ToList();
            var categories = _context.Categories.ToList();


            ViewBag.Items = items;
            ViewBag.Brands = brands;
            ViewBag.Categories = categories;


            return View();
        }


        public IActionResult ViewManagers()
        {
            var managers = _context.Employees.Where(e => e.Role == "Manager").ToList();
            return View(managers);
        }

        public IActionResult ViewEmployees()
        {
            var employees = _context.Employees
                .Where(e => e.Role == "Employee")
                .Include(e => e.Superior)
                .ToList();
            return View(employees);
        }
        // POST: /Admin/DeleteManager
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteManager(int id)
        {
            var manager = _context.Employees.FirstOrDefault(e => e.EmployeeNumber == id && e.Role == "Manager");
            if (manager == null)
            {
                return NotFound();
            }

            // Clear SuperiorId for subordinates
            var subordinates = _context.Employees.Where(e => e.SuperiorEmployeeNumber == id).ToList();
            foreach (var sub in subordinates)
            {
                sub.SuperiorEmployeeNumber = null;
            }

            _context.Employees.Remove(manager);
            _context.SaveChanges();
            TempData["Notification"] = "Manager removed successfully.";
            return RedirectToAction("ViewManagers");
        }

        // POST: /Admin/DeleteEmployee
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteEmployee(int id)
        {
            var employee = _context.Employees.FirstOrDefault(e => e.EmployeeNumber == id && e.Role == "Employee");
            if (employee == null)
            {
                return NotFound();
            }

            _context.Employees.Remove(employee);
            _context.SaveChanges();
            TempData["Notification"] = "Employee removed successfully.";
            return RedirectToAction("ViewEmployees");
        }
        // GET: /Admin/EditManager/{id}
        public IActionResult EditManager(int id)
        {
            var employee = _context.Employees.Find(id);
            if (employee == null)
            {
                return NotFound();
            }

            // Fetch both Managers and Admins for the superiors list
            var managers = _context.Employees
                                  .Where(e => e.Role == "Manager" || e.Role == "Admin")
                                  .ToList();
            ViewBag.Managers = managers;

            return View(employee);
        }

        // POST: /Admin/EditManager/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditManager(int id, Employee model)
        {
            var manager = _context.Employees.Find(id);
            if (manager == null || manager.Role != "Manager")
            {
                return NotFound();
            }
            ModelState.Remove("Password");
            if (ModelState.IsValid)
            {
                manager.Name = model.Name;
                manager.Email = model.Email;
                manager.Role = model.Role;
                manager.SuperiorEmployeeNumber = model.SuperiorEmployeeNumber;
                _context.SaveChanges();
                TempData["Notification"] = "Manager updated successfully.";
                return RedirectToAction("ViewManagers");
            }
            return View(model);
        }

        // GET: /Admin/EditEmployee/{id}
        public IActionResult EditEmployee(int id)
        {
            var employee = _context.Employees.Find(id);
            if (employee == null)
            {
                return NotFound();
            }

            // Fetch both Managers and Admins for the superiors list
            var managers = _context.Employees
                                  .Where(e => e.Role == "Manager" || e.Role == "Admin")
                                  .ToList();
            ViewBag.Managers = managers;

            return View(employee);
        }

        // POST: /Admin/EditEmployee/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditEmployee(int id, Employee model)
        {
            var employee = _context.Employees.Find(id);
            if (employee == null || employee.Role != "Employee")
            {
                return NotFound();
            }
            ModelState.Remove("Password");
            if (ModelState.IsValid)
            {
                
                employee.Name = model.Name;
                employee.Email = model.Email;
                employee.Role = model.Role;
                employee.SuperiorEmployeeNumber = model.SuperiorEmployeeNumber;
                _context.SaveChanges();
                TempData["Notification"] = "Employee updated successfully.";
                return RedirectToAction("ViewEmployees");
            }

            ViewBag.Managers = _context.Employees.Where(e => e.Role == "Manager").ToList();
            return View(model);
        }
        // GET: /Admin/AddManager
        public IActionResult AddManager()
        {
            return View();
        }

        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        // POST: /Admin/AddManager
        [HttpPost]
        public IActionResult AddManager(Employee employee)
        {
            var adminEmployeeNumber = HttpContext.Session.GetInt32("EmployeeNumber");
            if (adminEmployeeNumber == null)
            {
                return RedirectToAction("Login", "Account");
            }

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
            employee.Role = "Manager";
            employee.SuperiorEmployeeNumber=adminEmployeeNumber.Value;
            _context.Employees.Add(employee);
            _context.SaveChanges();
            TempData["Notification"] = "Manager added successfully.";
            return RedirectToAction("ViewManagers");
        }
       
        // GET: /Admin/AddEmployee
        public IActionResult AddEmployee()
        {
            var superiors = _context.Employees
                        .Where(e => e.Role == "Manager" || e.Role == "Admin")
                        .ToList();
            ViewBag.Superiors = new SelectList(superiors, "EmployeeNumber", "Name");
            return View();
        }

        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        // POST: /Admin/AddEmployee
        [HttpPost]
        public IActionResult AddEmployee(Employee employee)
        {
           
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
            employee.Role = "Employee";
            _context.Employees.Add(employee);
                _context.SaveChanges();
                TempData["Notification"] = "Employee added successfully.";
                return RedirectToAction("ViewEmployees");
        
            

        }
        // GET: /Admin/Reports
        public IActionResult Reports()
        {
            var reportData = _context.Requests
                .Where(r => r.Status == "Approved")
                .Select(r => new ReportViewModel
                {
                    ItemName = r.Item.ItemName,
                    BrandName = r.Item.Category.Brand.BrandName,
                    CategoryName = r.Item.Category.CategoryName,
                    EmployeeName = r.Employee.Name,
                    QuantityRequested = r.QuantityRequested,
                    TotalCost = r.QuantityRequested * r.Item.Cost
                })
                .ToList();

            return View(reportData);
        }
    }
}