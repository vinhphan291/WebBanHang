using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Data;
using WebBanHang.Models;

public class ProductsController : Controller
{
    private readonly AppDbContext _context;

    public ProductsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: Products/Create
    public IActionResult Create()
    {
        ViewData["CategoryId"] =
            new SelectList(_context.Categories, "Id", "Name");

        return View();
    }

    // POST: Products/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Product product)
    {
        if (ModelState.IsValid)
        {
            _context.Add(product);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        ViewData["CategoryId"] =
            new SelectList(_context.Categories, "Id", "Name", product.CategoryId);

        return View(product);
    }

    // GET: Products
    public async Task<IActionResult> Index()
    {
        var products = _context.Products.Include(p => p.Category);
        return View(await products.ToListAsync());
    }
}