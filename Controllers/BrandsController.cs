using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using Microsoft.AspNetCore.Authorization;

namespace WebApplication1.Controllers
{
	[Authorize(Roles = "Admin,Manager")]
	public class BrandsController : Controller
	{
		private readonly StationeryContext _context;

		public BrandsController(StationeryContext context)
		{
			_context = context;
		}
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        // GET: Brands
        public async Task<IActionResult> Index()
		{
			return View(await _context.Brands.ToListAsync());
		}

		// GET: Brands/Create
		public IActionResult Create()
		{
			return View();
        }

		 [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
		[HttpPost]
		[ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("BrandID,BrandName")] Brand brand)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (BrandNameExists(brand.BrandName))
                    {
                        ModelState.AddModelError("BrandName", "Brand name already exists");
                        return View(brand);
                    }

                    _context.Add(brand);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Brand created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException ex)
                {
                    ModelState.AddModelError("", $"Database error: {ex.Message}");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Unexpected error: {ex.Message}");
                }
            }
            return View(brand);
        }
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        // GET: Brands/Edit/5
        public async Task<IActionResult> Edit(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var brand = await _context.Brands.FindAsync(id);
			if (brand == null)
			{
				return NotFound();
			}
			return View(brand);
		}
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        // POST: Brands/Edit/5
        [HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id, [Bind("BrandID,BrandName")] Brand brand)
		{
			if (id != brand.BrandID)
			{
				return NotFound();
			}

			if (ModelState.IsValid)
			{
				try
				{
					if (BrandNameExists(brand.BrandName, brand.BrandID))
					{
						ModelState.AddModelError("BrandName", "Brand name already exists");
						return View(brand);
					}

					_context.Update(brand);
					await _context.SaveChangesAsync();
					TempData["Success"] = "Brand updated successfully!";
				}
				catch (DbUpdateConcurrencyException)
				{
					if (!BrandExists(brand.BrandID))
					{
						return NotFound();
					}
					else
					{
						throw;
					}
				}
				return RedirectToAction(nameof(Index));
			}
			return View(brand);
		}
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        // GET: Brands/Delete/5
        public async Task<IActionResult> Delete(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var brand = await _context.Brands
				.Include(b => b.Categories)
				.FirstOrDefaultAsync(m => m.BrandID == id);

			if (brand == null)
			{
				return NotFound();
			}

			if (brand.Categories?.Any() == true)
			{
				TempData["Error"] = "Cannot delete brand with existing categories";
				return RedirectToAction(nameof(Index));
			}

			return View(brand);
		}
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        // POST: Brands/Delete/5
        [HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(int id)
		{
			var brand = await _context.Brands.FindAsync(id);
			if (brand != null)
			{
				try
				{
					_context.Brands.Remove(brand);
					await _context.SaveChangesAsync();
					TempData["Success"] = "Brand deleted successfully!";
				}
				catch (DbUpdateException)
				{
					TempData["Error"] = "Error deleting brand. Remove associated categories first.";
				}
			}
			return RedirectToAction(nameof(Index));
		}

		private bool BrandExists(int id)
		{
			return _context.Brands.Any(e => e.BrandID == id);
		}

        private bool BrandNameExists(string name, int? excludeId = null)
        {
            return _context.Brands
                .Any(b => b.BrandName.ToLower() == name.ToLower()
                       && b.BrandID != excludeId);
        }
    }
}