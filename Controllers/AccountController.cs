using Microsoft.AspNetCore.Mvc;
using System.Linq;
using WebApplication1.Data;
using WebApplication1.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;

namespace WebApplication1.Controllers
{
    public class AccountController : Controller
    {
        private readonly StationeryContext _context;

        public AccountController(StationeryContext context)
        {
            _context = context;
        }
       
        // GET: /Account/Login
        public IActionResult Login()
        {
            // Redirect if already logged in
            if (HttpContext.Session.GetInt32("EmployeeNumber") != null)
            {
                var role = HttpContext.Session.GetString("Role");
                return RedirectToDashboard(role);
            }
            return View();
        }

        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        [HttpPost]
        public async Task<IActionResult> Login(int employeeNumber, string password)
        {

            var employee = _context.Employees.FirstOrDefault(e => e.EmployeeNumber == employeeNumber && e.Password == password);
            if (employee != null)
            {
                // Create claims
                var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, employee.EmployeeNumber.ToString()),
            new Claim(ClaimTypes.Name, employee.Name),
            new Claim(ClaimTypes.Role, employee.Role)
        };

                var claimsIdentity = new ClaimsIdentity(claims, "CookieAuth");
                var authProperties = new AuthenticationProperties();

                // Sign in user
                await HttpContext.SignInAsync(
                    "CookieAuth",
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                // Optional: Set session variables if needed elsewhere
                HttpContext.Session.SetInt32("EmployeeNumber", employee.EmployeeNumber);
                HttpContext.Session.SetString("Role", employee.Role);
                HttpContext.Session.SetString("Name", employee.Name);

                return RedirectToDashboard(employee.Role);
            }

            TempData["ErrorMessage"] = "Invalid Employee Number or Password";
            return View();
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("CookieAuth");
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]

        // GET: /Account/ChangePassword
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ChangePassword(string currentPassword, string newPassword)
        {
            var employeeNumber = HttpContext.Session.GetInt32("EmployeeNumber");
            if (employeeNumber == null)
                return RedirectToAction("Login");

            var employee = _context.Employees.Find(employeeNumber);
            if (employee != null && employee.Password == currentPassword)
            {
                employee.Password = newPassword;
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Password changed successfully!"; // Success message
                return RedirectToDashboard(employee.Role);
            }
            else
            {
                TempData["ErrorMessage"] = "Current password is incorrect."; // Error message
            }
            return View();
        }

        private IActionResult RedirectToDashboard(string? role)
        {
            return role switch
            {
                "Admin" => RedirectToAction("Dashboard", "Admin"),
                "Manager" => RedirectToAction("Dashboard", "Manager"),
                _ => RedirectToAction("Dashboard", "Employee")
            };
        }
    }
}