using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Data;
using WebBanHang.Models;

namespace WebBanHang.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var banners = await _context.Banners
                .Where(b => b.IsActive)
                .OrderBy(b => b.SortOrder)
                .ToListAsync();

            var products = await _context.Products
                .Include(p => p.Category)
                .Take(8)
                .ToListAsync();

            ViewBag.Products = products;
            return View(banners);
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}