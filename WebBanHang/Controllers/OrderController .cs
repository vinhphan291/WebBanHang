using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Data;
using WebBanHang.Models;

namespace WebBanHang.Controllers
{
    public class OrderController : Controller
    {
        private readonly AppDbContext _context;

        public OrderController(AppDbContext context)
        {
            _context = context;
        }

        // ===== USER =====
        public async Task<IActionResult> Index()
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Login", "Account");

            var orders = await _context.Orders
                .Where(o => o.UserName == username)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
            return View(orders);
        }

        public async Task<IActionResult> Details(int id)
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Login", "Account");

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserName == username);

            if (order == null) return NotFound();
            return View(order);
        }

        // User hủy đơn hàng (chỉ khi trạng thái Pending)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Login", "Account");

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == id && o.UserName == username);

            if (order == null) return NotFound();

            if (order.OrderStatus == "Pending")
            {
                order.OrderStatus = "Cancelled";
                await _context.SaveChangesAsync();
                TempData["Message"] = "Đơn hàng đã được hủy.";
            }
            else
            {
                TempData["Error"] = "Không thể hủy đơn hàng ở trạng thái này.";
            }
            return RedirectToAction("Details", new { id });
        }

        // ===== ADMIN =====
        public async Task<IActionResult> AdminIndex()
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Admin")
                return RedirectToAction("AccessDenied", "Account");

            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
            return View(orders);
        }

        public async Task<IActionResult> AdminDetails(int id)
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Admin")
                return RedirectToAction("AccessDenied", "Account");

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();
            return View(order);
        }

        // Admin cập nhật trạng thái
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Admin")
                return RedirectToAction("AccessDenied", "Account");

            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            order.OrderStatus = status;
            await _context.SaveChangesAsync();
            TempData["Message"] = "Cập nhật trạng thái thành công!";
            return RedirectToAction("AdminDetails", new { id });
        }

        // Admin hủy đơn hàng (có thể hủy bất kỳ lúc nào)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminCancel(int id)
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Admin")
                return RedirectToAction("AccessDenied", "Account");

            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            order.OrderStatus = "Cancelled";
            await _context.SaveChangesAsync();
            TempData["Message"] = "Đơn hàng đã được hủy bởi Admin.";
            return RedirectToAction("AdminDetails", new { id });
        }
    }
}