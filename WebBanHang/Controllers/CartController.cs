using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using WebBanHang.Data;
using WebBanHang.Models;
using Microsoft.EntityFrameworkCore;

namespace WebBanHang.Controllers
{
    public class CartController : Controller
    {
        private readonly AppDbContext _context;
        private const string CartSessionKey = "Cart";

        public CartController(AppDbContext context)
        {
            _context = context;
        }

        // Lấy giỏ hàng từ Session
        private List<CartItem> GetCart()
        {
            var session = HttpContext.Session;
            string jsonCart = session.GetString(CartSessionKey);
            if (string.IsNullOrEmpty(jsonCart))
            {
                return new List<CartItem>();
            }
            return JsonSerializer.Deserialize<List<CartItem>>(jsonCart);
        }

        // Lưu giỏ hàng vào Session
        private void SaveCart(List<CartItem> cart)
        {
            var session = HttpContext.Session;
            string jsonCart = JsonSerializer.Serialize(cart);
            session.SetString(CartSessionKey, jsonCart);
        }

        // Kiểm tra đăng nhập
        private bool IsLoggedIn()
        {
            return !string.IsNullOrEmpty(HttpContext.Session.GetString("Username"));
        }

        // Thêm sản phẩm vào giỏ
        [HttpPost]
        public IActionResult AddToCart(int productId)
        {
            // Kiểm tra đăng nhập
            if (!IsLoggedIn())
            {
                TempData["Message"] = "Vui lòng đăng nhập để mua hàng.";
                return RedirectToAction("Login", "Account");
            }

            var product = _context.Products.Find(productId);
            if (product == null)
            {
                return NotFound();
            }

            var cart = GetCart();
            var existingItem = cart.FirstOrDefault(x => x.ProductId == productId);
            if (existingItem != null)
            {
                existingItem.Quantity++;
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Price = product.Price,
                    Quantity = 1,
                    ImageUrl = product.ImageUrl
                });
            }
            SaveCart(cart);

            TempData["Message"] = "Đã thêm sản phẩm vào giỏ hàng!";
            return RedirectToAction("Index", "Products");
        }

        // Hiển thị giỏ hàng
        public IActionResult Index()
        {
            var cart = GetCart();
            return View(cart);
        }

        // Cập nhật số lượng
        [HttpPost]
        public IActionResult UpdateCart(int productId, int quantity)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(x => x.ProductId == productId);
            if (item != null)
            {
                if (quantity > 0)
                {
                    item.Quantity = quantity;
                }
                else
                {
                    cart.Remove(item);
                }
            }
            SaveCart(cart);
            return RedirectToAction("Index");
        }

        // Xóa sản phẩm khỏi giỏ
        public IActionResult RemoveFromCart(int productId)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(x => x.ProductId == productId);
            if (item != null)
            {
                cart.Remove(item);
            }
            SaveCart(cart);
            return RedirectToAction("Index");
        }

        // Thanh toán (GET)
        public IActionResult Checkout()
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            var cart = GetCart();
            if (!cart.Any())
            {
                return RedirectToAction("Index");
            }
            return View(new Order());
        }

        // Thanh toán (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(Order model)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            var cart = GetCart();
            if (!cart.Any())
            {
                return RedirectToAction("Index");
            }

            if (ModelState.IsValid)
            {
                // Tính tổng tiền
                model.TotalAmount = cart.Sum(x => x.Price * x.Quantity);
                model.OrderDate = DateTime.Now;
                model.OrderStatus = "Pending";

                _context.Orders.Add(model);
                await _context.SaveChangesAsync();

                // Lưu chi tiết đơn hàng
                foreach (var item in cart)
                {
                    var orderDetail = new OrderDetail
                    {
                        OrderId = model.Id,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.Price
                    };
                    _context.OrderDetails.Add(orderDetail);

                    // Giảm số lượng tồn kho (nếu có)
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product != null && product.StockQuantity >= item.Quantity)
                    {
                        product.StockQuantity -= item.Quantity;
                    }
                }
                await _context.SaveChangesAsync();

                // Xóa giỏ hàng
                SaveCart(new List<CartItem>());

                return RedirectToAction("OrderConfirmation", new { id = model.Id });
            }
            return View(model);
        }

        // Xác nhận đơn hàng
        public IActionResult OrderConfirmation(int id)
        {
            var order = _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefault(o => o.Id == id);
            if (order == null)
            {
                return NotFound();
            }
            return View(order);
        }
    }
}