using ClosedXML.Excel;
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

        // USER
        public async Task<IActionResult> Index()
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username)) return RedirectToAction("Login", "Account");

            var orders = await _context.Orders
                .Where(o => o.UserName == username)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
            return View(orders);
        }

        public async Task<IActionResult> Details(int id)
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username)) return RedirectToAction("Login", "Account");

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserName == username);
            if (order == null) return NotFound();
            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username)) return RedirectToAction("Login", "Account");

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id && o.UserName == username);
            if (order == null) return NotFound();

            if (order.OrderStatus == "Pending")
            {
                order.OrderStatus = "Cancelled";
                await _context.SaveChangesAsync();
                TempData["Message"] = "Đơn hàng đã được hủy.";
            }
            else TempData["Error"] = "Không thể hủy đơn hàng ở trạng thái này.";
            return RedirectToAction("Details", new { id });
        }

        // ADMIN
        public async Task<IActionResult> AdminIndex()
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Admin") return RedirectToAction("AccessDenied", "Account");

            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
            return View(orders);
        }

        public async Task<IActionResult> AdminDetails(int id)
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Admin") return RedirectToAction("AccessDenied", "Account");

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();
            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Admin") return RedirectToAction("AccessDenied", "Account");

            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            order.OrderStatus = status;
            await _context.SaveChangesAsync();
            TempData["Message"] = "Cập nhật trạng thái thành công!";
            return RedirectToAction("AdminDetails", new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminCancel(int id)
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Admin") return RedirectToAction("AccessDenied", "Account");

            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            order.OrderStatus = "Cancelled";
            await _context.SaveChangesAsync();
            TempData["Message"] = "Đơn hàng đã được hủy bởi Admin.";
            return RedirectToAction("AdminDetails", new { id });
        }

        // EXPORT EXCEL
        public async Task<IActionResult> ExportToExcel()
        {
            var username = HttpContext.Session.GetString("Username");
            var role = HttpContext.Session.GetString("Role");

            IQueryable<Order> orders;
            if (role == "Admin")
                orders = _context.Orders.Include(o => o.OrderDetails);
            else
                orders = _context.Orders.Where(o => o.UserName == username).Include(o => o.OrderDetails);

            var list = await orders.ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Đơn hàng");
                worksheet.Cell(1, 1).Value = "Mã đơn";
                worksheet.Cell(1, 2).Value = "Ngày đặt";
                worksheet.Cell(1, 3).Value = "Khách hàng";
                worksheet.Cell(1, 4).Value = "SĐT";
                worksheet.Cell(1, 5).Value = "Địa chỉ";
                worksheet.Cell(1, 6).Value = "Tổng tiền";
                worksheet.Cell(1, 7).Value = "Trạng thái";

                for (int i = 0; i < list.Count; i++)
                {
                    var order = list[i];
                    worksheet.Cell(i + 2, 1).Value = order.Id;
                    worksheet.Cell(i + 2, 2).Value = order.OrderDate.ToString("dd/MM/yyyy HH:mm");
                    worksheet.Cell(i + 2, 3).Value = order.CustomerName;
                    worksheet.Cell(i + 2, 4).Value = order.Phone;
                    worksheet.Cell(i + 2, 5).Value = order.Address;
                    worksheet.Cell(i + 2, 6).Value = order.TotalAmount;
                    worksheet.Cell(i + 2, 7).Value = order.OrderStatus;
                }

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "DonHang.xlsx");
                }
            }
        }
    }
}