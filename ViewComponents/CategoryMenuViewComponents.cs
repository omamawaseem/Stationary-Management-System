// ViewComponents/CategoryMenuViewComponent.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;


public class CategoryMenuViewComponent : ViewComponent
{
    private readonly StationeryContext _context;

    public CategoryMenuViewComponent(StationeryContext context)
    {
        _context = context;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var brands = await _context.Brands
            .Include(b => b.Categories)
            .OrderBy(b => b.BrandName)
            .ToListAsync();

        return View(brands);
    }
}