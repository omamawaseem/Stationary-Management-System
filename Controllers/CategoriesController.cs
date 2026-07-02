using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebApplication1.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class CategoriesController : Controller
    {
        private readonly StationeryContext _context;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(StationeryContext context, ILogger<CategoriesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Categories
        public async Task<IActionResult> Index()
{
    var categories = await _context.Categories
        .Include(c => c.Brand) // Eagerly load the Brand
        .ToListAsync(); // Removed AsNoTracking()

    return View(categories);
}
        // Get: Categories/Create
        public IActionResult Create()
        {
            var brands = _context.Brands.ToList();
            if (!brands.Any())
            {
                TempData["Warning"] = "No brands available. Please create a brand first.";
                return RedirectToAction("Index");
            }
            ViewBag.Brands = new SelectList(brands, "BrandID", "BrandName");
            return View();
        }


        // POST: Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CategoryID,CategoryName,BrandID")] Category category)
        {
            // Check if BrandID is valid
            if (category.BrandID == 0)
            {
                TempData["Error"] = "The Brand field is required.";
                ViewBag.Brands = new SelectList(await _context.Brands.ToListAsync(), "BrandID", "BrandName", category.BrandID);
                return View(category);
            }

            // Check if the selected BrandID exists
            var brandExists = await _context.Brands.AnyAsync(b => b.BrandID == category.BrandID);
            if (!brandExists)
            {
                TempData["Error"] = "The selected Brand is invalid.";
                ViewBag.Brands = new SelectList(await _context.Brands.ToListAsync(), "BrandID", "BrandName", category.BrandID);
                return View(category);
            }

            // Check for duplicate category name under the same brand
            bool categoryExists = await CategoryExists(category.CategoryName, category.BrandID);
            if (categoryExists)
            {
                TempData["Error"] = "A category with this name already exists under the selected brand.";
                ViewBag.Brands = new SelectList(await _context.Brands.ToListAsync(), "BrandID", "BrandName", category.BrandID);
                return View(category);
            }

            try
            {
                _context.Add(category);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Category created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category");
                TempData["Error"] = "An error occurred while creating the category.";
                ViewBag.Brands = new SelectList(await _context.Brands.ToListAsync(), "BrandID", "BrandName", category.BrandID);
                return View(category);
            }
        }
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var category = await _context.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CategoryID == id);

            if (category == null) return NotFound();

            ViewBag.Brands = new SelectList(await _context.Brands.ToListAsync(), "BrandID", "BrandName", category.BrandID);
            return View(category);
        }
        // POST: Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CategoryID,CategoryName,BrandID")] Category category)
        {
            if (id != category.CategoryID) return NotFound();

            // Check if BrandID is valid
            if (category.BrandID == 0)
            {
                TempData["Error"] = "The Brand field is required.";
                ViewBag.Brands = new SelectList(await _context.Brands.ToListAsync(), "BrandID", "BrandName", category.BrandID);
                return View(category);
            }

            // Check if the selected BrandID exists
            var brandExists = await _context.Brands.AnyAsync(b => b.BrandID == category.BrandID);
            if (!brandExists)
            {
                TempData["Error"] = "The selected Brand is invalid.";
                ViewBag.Brands = new SelectList(await _context.Brands.ToListAsync(), "BrandID", "BrandName", category.BrandID);
                return View(category);
            }

            // Check for duplicate category name under the same brand, excluding current category
            bool categoryExists = await CategoryExists(category.CategoryName, category.BrandID, category.CategoryID);
            if (categoryExists)
            {
                TempData["Error"] = "A category with this name already exists under the selected brand.";
                ViewBag.Brands = new SelectList(await _context.Brands.ToListAsync(), "BrandID", "BrandName", category.BrandID);
                return View(category);
            }

            try
            {
                _context.Update(category);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Category updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category");
                TempData["Error"] = "An error occurred while updating the category.";
                ViewBag.Brands = new SelectList(await _context.Brands.ToListAsync(), "BrandID", "BrandName", category.BrandID);
                return View(category);
            }
        }

        // GET: Categories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var category = await _context.Categories
                .Include(c => c.Brand)
                .Include(c => c.Items)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CategoryID == id);

            if (category == null) return NotFound();

            if (category.Items?.Any() == true)
            {
                TempData["Error"] = "Cannot delete category with existing items";
                return RedirectToAction(nameof(Index));
            }

            return View(category);
        }

        // POST: Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return RedirectToAction(nameof(Index));

            try
            {
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Category deleted successfully!";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error deleting category");
                TempData["Error"] = "Error deleting category. Remove associated items first.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(c => c.CategoryID == id);
        }

        private async Task<bool> CategoryExists(string name, int brandId, int? excludeId = null)
        {
            return await _context.Categories
                .AnyAsync(c => c.CategoryName.ToLower() == name.ToLower()
                            && c.BrandID == brandId
                            && c.CategoryID != excludeId);
        }

        private void PopulateBrandsDropDownList(object? selectedBrand = null)
        {
            var brandsQuery = _context.Brands
                .OrderBy(b => b.BrandName)
                .AsNoTracking();

            ViewBag.Brands = new SelectList(brandsQuery, "BrandID", "BrandName", selectedBrand);
        }
    }
}