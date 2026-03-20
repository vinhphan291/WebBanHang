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

        private List<CartItem> GetCart()
        {
            var session = HttpContext.Session;
            string jsonCart = session.GetString(CartSessionKey);
            if (string.IsNullOrEmpty(jsonCart)) return new List<CartItem>();
            return JsonSerializer.Deserialize<List<CartItem>>(jsonCart);
        }

        private void SaveCart(List<CartItem> cart)
        {
            var session = HttpContext.Session;
            string jsonCart = JsonSerializer.Serialize(cart);
            session.SetString(CartSessionKey, jsonCart);
        }

        private bool CanBuy()
        {
            var username = HttpContext.Session.GetString("Username");
            var role = HttpContext.Session.GetString("Role");
            return !string.IsNullOrEmpty(username) && role != "Admin";
        }

        [HttpPost]
        public IActionResult AddToCart(int productId)
        {
            if (!CanBuy())
            {
                TempData["Message"] = "Admin không thể mua hàng.";
                return RedirectToAction("Index", "Products");
            }

            var product = _context.Products.Find(productId);
            if (product == null) return NotFound();

            var cart = GetCart();
            var existingItem = cart.FirstOrDefault(x => x.ProductId == productId);
            if (existingItem != null) existingItem.Quantity++;
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

        public IActionResult Index()
        {
            var cart = GetCart();
            return View(cart);
        }

        [HttpPost]
        public IActionResult UpdateCart(int productId, int quantity)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(x => x.ProductId == productId);
            if (item != null)
            {
                if (quantity > 0) item.Quantity = quantity;
                else cart.Remove(item);
            }
            SaveCart(cart);
            return RedirectToAction("Index");
        }

        public IActionResult RemoveFromCart(int productId)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(x => x.ProductId == productId);
            if (item != null) cart.Remove(item);
            SaveCart(cart);
            return RedirectToAction("Index");
        }

        public IActionResult Checkout()
        {
            if (!CanBuy()) return RedirectToAction("Index", "Products");
            var cart = GetCart();
            if (!cart.Any()) return RedirectToAction("Index");
            return View(new Order());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(Order model)
        {
            if (!CanBuy()) return RedirectToAction("Index", "Products");

            var cart = GetCart();
            if (!cart.Any()) return RedirectToAction("Index");

            if (ModelState.IsValid)
            {
                var username = HttpContext.Session.GetString("Username");
                model.UserName = username;
                model.TotalAmount = cart.Sum(x => x.Price * x.Quantity);
                model.OrderDate = DateTime.Now;
                model.OrderStatus = "Pending";

                _context.Orders.Add(model);
                await _context.SaveChangesAsync();

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

                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product != null && product.StockQuantity >= item.Quantity)
                        product.StockQuantity -= item.Quantity;
                }
                await _context.SaveChangesAsync();

                SaveCart(new List<CartItem>());
                return RedirectToAction("OrderConfirmation", new { id = model.Id });
            }
            return View(model);
        }

        public async Task<IActionResult> OrderConfirmation(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();
            return View(order);
        }
    }
}