using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Data;
using WebBanHang.Models;
using System.Linq;
using System.Threading.Tasks;

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
            // Lấy danh sách banner đang active, sắp xếp theo thứ tự
            var banners = await _context.Banners
                .Where(b => b.IsActive)
                .OrderBy(b => b.SortOrder)
                .ToListAsync();

            // Lấy sản phẩm để hiển thị bên dưới
            var products = await _context.Products
                .Include(p => p.Category)
                .Take(8) // Lấy 8 sản phẩm
                .ToListAsync();

            ViewBag.Products = products;

            return View(banners); // Model là danh sách banner
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}