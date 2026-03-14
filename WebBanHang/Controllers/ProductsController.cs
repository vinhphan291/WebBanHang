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

    // CREATE
    public IActionResult Create()
    {
        ViewData["CategoryId"] =
            new SelectList(_context.Categories, "Id", "Name");

        return View();
    }

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

    // INDEX
    public async Task<IActionResult> Index()
    {
        var products = _context.Products.Include(p => p.Category);
        return View(await products.ToListAsync());
    }

    // DETAILS
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
            return NotFound();

        var product = await _context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (product == null)
            return NotFound();

        return View(product);
    }

    // EDIT
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
            return NotFound();

        var product = await _context.Products.FindAsync(id);

        if (product == null)
            return NotFound();

        ViewData["CategoryId"] =
            new SelectList(_context.Categories, "Id", "Name", product.CategoryId);

        return View(product);
    }
}