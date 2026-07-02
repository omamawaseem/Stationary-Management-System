using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebApplication1.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class ItemsController : Controller
    {
        private readonly StationeryContext _context;
        private readonly ILogger<ItemsController> _logger;

        public ItemsController(StationeryContext context, ILogger<ItemsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Items
        public async Task<IActionResult> Index()
        {
            var items = await _context.Items
                .Include(i => i.Category)
                    .ThenInclude(c => c.Brand)
                .AsNoTracking()
                .ToListAsync();

            return View(items);
        }

        // GET: Items/Create
        public IActionResult Create()
        {
            ViewBag.Brands = new SelectList(_context.Brands, "BrandID", "BrandName");
            return View(new Item());
        }

        // POST: Items/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ItemID,ItemName,Cost,QuantityAvailable,CategoryID")] Item item)
        {
            try
            {
               
                    if (await ItemNameExists(item.ItemName, item.CategoryID))
                    {
                        ModelState.AddModelError("ItemName", "Item already exists in this category");
                        PopulateBrandsDropDownList();
                        return View(item);
                    }

                    _context.Add(item);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Item created successfully!";
                    return RedirectToAction(nameof(Index));
                }
            
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error creating item");
                ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
            }

            PopulateBrandsDropDownList();
            return View(item);
        }

        // GET: Items/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var item = await _context.Items
                .Include(i => i.Category)
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.ItemID == id);

            if (item == null) return NotFound();

            PopulateBrandsDropDownList(item.Category.BrandID);
            return View(item);
        }

        // POST: Items/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ItemID,ItemName,Cost,QuantityAvailable,CategoryID")] Item item)
        {
            if (id != item.ItemID) return NotFound();

           
                try
                {
                    if (await ItemNameExists(item.ItemName, item.CategoryID, item.ItemID))
                    {
                        ModelState.AddModelError("ItemName", "Item already exists in this category");
                        PopulateBrandsDropDownList();
                        return View(item);
                    }

                    _context.Update(item);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Item updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    if (!ItemExists(item.ItemID))
                    {
                        return NotFound();
                    }
                    _logger.LogError(ex, "Concurrency error updating item");
                    throw;
                }
            

            PopulateBrandsDropDownList();
            return View(item);
        }

        // GET: Items/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var item = await _context.Items
                .Include(i => i.Category)
                    .ThenInclude(c => c.Brand)
                .Include(i => i.Requests)
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.ItemID == id);

            if (item == null) return NotFound();

            if (item.Requests?.Any() == true)
            {
                TempData["Error"] = "Cannot delete item with existing requests";
                return RedirectToAction(nameof(Index));
            }

            return View(item);
        }

        // POST: Items/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _context.Items.FindAsync(id);
            if (item == null) return RedirectToAction(nameof(Index));

            try
            {
                _context.Items.Remove(item);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Item deleted successfully!";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error deleting item");
                TempData["Error"] = "Error deleting item. Remove associated requests first.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ItemExists(int id)
        {
            return _context.Items.Any(i => i.ItemID == id);
        }

        private async Task<bool> ItemNameExists(string name, int categoryId, int? excludeId = null)
        {
            return await _context.Items
                .AnyAsync(i => i.ItemName == name
                            && i.CategoryID == categoryId
                            && i.ItemID != excludeId);
        }

        private void PopulateBrandsDropDownList(object? selectedBrand = null)
        {
            var brandsQuery = _context.Brands
                .OrderBy(b => b.BrandName)
                .AsNoTracking();

            ViewBag.Brands = new SelectList(brandsQuery, "BrandID", "BrandName", selectedBrand);
        }

        [HttpGet]
        public async Task<JsonResult> GetCategoriesByBrand(int brandId)
        {
            var categories = await _context.Categories
                .Where(c => c.BrandID == brandId)
                .OrderBy(c => c.CategoryName)
                .AsNoTracking()
                .ToListAsync();

            return Json(categories);
        }
    }
}