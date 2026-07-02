using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;

namespace WebApplication1.Controllers
{
    [Authorize(Roles = "Employee")]
    public class EmployeeController : Controller
    {
        private readonly StationeryContext _context;
        private readonly ILogger<EmployeeController> _logger;

        public EmployeeController(StationeryContext context, ILogger<EmployeeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Example in EmployeeController
        private int? EmployeeNumber => User.Identity.IsAuthenticated
            ? int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value)
            : null;

        private string? EmployeeRole => User.FindFirst(ClaimTypes.Role)?.Value;

        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Dashboard()
        {
            var employeeNumber = HttpContext.Session.GetInt32("EmployeeNumber");

            if (employeeNumber == null)
                return RedirectToAction("Login", "Account");

            var employee = _context.Employees.FirstOrDefault(e => e.EmployeeNumber == employeeNumber);
            if (employee != null)
            {
                ViewData["Name"] = employee.Name;
            }

            if (EmployeeNumber == null) return RedirectToAction("Login", "Account");

            // Fetch notifications
            var notifications = await _context.Notifications
                .Where(n => n.EmployeeNumber == EmployeeNumber && !n.IsRead)
                .ToListAsync();

            if (notifications.Any())
            {
                ViewBag.Notifications = notifications;
                // Mark notifications as read
                notifications.ForEach(n => n.IsRead = true);
                await _context.SaveChangesAsync();
            }

            // Show only pending requests
        var requests = await _context.Requests
    .Where(r => r.EmployeeNumber == EmployeeNumber) // Remove "&& r.Status == "Pending""
    .Include(r => r.Item)
        .ThenInclude(i => i.Category)
            .ThenInclude(c => c.Brand)
    .Include(r => r.Employee)
    .OrderBy(r => r.RequestID)
    .AsNoTracking()
    .ToListAsync();

            return View(requests);
        }

        // EmployeeController.cs
        public IActionResult Browse(int? brandId, int? categoryId)
        {
            // Get brands for dropdown
            ViewBag.Brands = new SelectList(_context.Brands, "BrandID", "BrandName");

            // Get categories for selected brand (if any)
            if (brandId.HasValue)
            {
                ViewBag.Categories = new SelectList(_context.Categories
                    .Where(c => c.BrandID == brandId)
                    .Select(c => new { c.CategoryID, c.CategoryName }),
                    "CategoryID", "CategoryName");
            }

            // Filter items
            var query = _context.Items
                .Include(i => i.Category)
                .ThenInclude(c => c.Brand)
                .AsQueryable();

            if (categoryId.HasValue)
            {
                query = query.Where(i => i.CategoryID == categoryId);
            }
            else if (brandId.HasValue)
            {
                query = query.Where(i => i.Category.BrandID == brandId);
            }

            return View(query.ToList());
        }


        [HttpGet]
        public async Task<IActionResult> CreateRequest(int itemId)
        {
            if (EmployeeNumber == null) return RedirectToAction("Login", "Account");

            var item = await _context.Items
                .Include(i => i.Category)
                    .ThenInclude(c => c.Brand)
                .FirstOrDefaultAsync(i => i.ItemID == itemId);

            if (item == null)
            {
                TempData["Error"] = "Item not found";
                return RedirectToAction("Browse");
            }

            return View(new Request
            {
                EmployeeNumber = EmployeeNumber.Value,
                ItemID = itemId,
                Item = item,
                NeededDate = DateTime.Today.AddDays(1)
            });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRequest(Request request)
        {
            if (EmployeeNumber == null) return RedirectToAction("Login", "Account");

            var item = await GetItemWithDetails(request.ItemID);

            if (item == null)
            {
                TempData["Error"] = "Item not found";
                return RedirectToAction("Browse");
            }

            // Override critical fields from user context and business rules
            request.EmployeeNumber = EmployeeNumber.Value;
            request.Status = "Pending";
            request.Item = item; // Attach loaded item for validation messages

            // Remove validation for fields not bound to the form
            ModelState.Remove("EmployeeNumber");
            ModelState.Remove("Status");
            ModelState.Remove("Item");
            ModelState.Remove("Employee");

           
            try
            {
                _context.Requests.Add(request);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Request submitted successfully!";
                return RedirectToAction(nameof(Dashboard));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating request");
                ModelState.AddModelError("", "An error occurred. Please try again.");
                return View(request);
            }
        }

        [HttpGet]
        public JsonResult GetCategories(int brandId)
        {
            var categories = _context.Categories
                .Where(c => c.BrandID == brandId)
                .Select(c => new {
                    c.CategoryID,
                    c.CategoryName
                })
                .ToList();

            return Json(categories);
        }
    
    [HttpGet]
        public async Task<IActionResult> CancelRequest(int id)
        {
            if (EmployeeNumber == null) return RedirectToAction("Login", "Account");

            var request = await GetRequestWithDetails(id);

            if (request == null)
            {
                TempData["Error"] = "Request not found";
                return RedirectToAction(nameof(Dashboard));
            }

            return View(request);
        }

        [HttpPost]
        [ActionName("CancelRequest")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelRequestConfirmed(int id)
        {
            if (EmployeeNumber == null) return RedirectToAction("Login", "Account");

            var request = await _context.Requests
                .FirstOrDefaultAsync(r => r.RequestID == id && r.EmployeeNumber == EmployeeNumber);

            if (request == null)
            {
                TempData["Error"] = "Request not found";
                return RedirectToAction(nameof(Dashboard));
            }

            if (request.Status != "Pending")
            {
                TempData["Error"] = "Only pending requests can be cancelled";
                return RedirectToAction(nameof(Dashboard));
            }

            try
            {
                _context.Requests.Remove(request);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Request cancelled successfully";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error cancelling request");
                TempData["Error"] = "Error cancelling request";
            }

            return RedirectToAction(nameof(Dashboard));
        }
        [HttpGet]
        public async Task<IActionResult> DeleteRequest(int id)
        {
            if (EmployeeNumber == null) return RedirectToAction("Login", "Account");

            var request = await GetRequestWithDetails(id);

            if (request == null)
            {
                TempData["Error"] = "Request not found";
                return RedirectToAction(nameof(Dashboard));
            }

            return View(request);
        }
        [HttpPost]
        [ActionName("DeleteRequest")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRequestConfirmed(int id)
        {
            if (EmployeeNumber == null) return RedirectToAction("Login", "Account");

            var request = await _context.Requests
                .FirstOrDefaultAsync(r => r.RequestID == id && r.EmployeeNumber == EmployeeNumber);

            if (request == null)
            {
                TempData["Error"] = "Request not found";
                return RedirectToAction(nameof(Dashboard));
            }

           
            try
            {
                _context.Requests.Remove(request);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Request Deleted successfully";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error Deleted request");
                TempData["Error"] = "Error Delted request";
            }

            return RedirectToAction(nameof(Dashboard));
        }

        #region Helper Methods
        private async Task<Item?> GetItemWithDetails(int itemId)
{
    return await _context.Items
        .Include(i => i.Category)
            .ThenInclude(c => c.Brand)
        .FirstOrDefaultAsync(i => i.ItemID == itemId);
}

        private async Task<Request?> GetRequestWithDetails(int requestId)
        {
            if (EmployeeNumber == null) return null;

            return await _context.Requests
                .Include(r => r.Item)
                    .ThenInclude(i => i.Category)
                        .ThenInclude(c => c.Brand)
                .FirstOrDefaultAsync(r =>
                    r.RequestID == requestId &&
                    r.EmployeeNumber == EmployeeNumber.Value);
        }

        private async Task<(bool IsValid, string? Field, string? Message)> ValidateRequest(Request request)
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeNumber == EmployeeNumber);

            if (employee == null)
                return (false, "", "Employee not found");

            var spendingLimit = await _context.SpendingLimits
                .FirstOrDefaultAsync(s => s.Role == employee.Role);

            if (spendingLimit == null)
                return (false, "", "Spending limit not configured for your role");

            if (request.NeededDate < DateTime.Today)
                return (false, "NeededDate", "Needed date must be in the future");

            if (request.QuantityRequested > request.Item.QuantityAvailable)
                return (false, "QuantityRequested", $"Only {request.Item.QuantityAvailable} items available");

            var totalCost = request.Item.Cost * request.QuantityRequested;
            if (totalCost > spendingLimit.Limit)
                return (false, "QuantityRequested", $"Exceeds spending limit (Max: {spendingLimit.Limit:C})");

            return (true, null, null);
        }

        #endregion
    }
}