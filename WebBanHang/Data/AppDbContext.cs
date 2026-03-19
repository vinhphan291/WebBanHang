using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;

namespace WebBanHang.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // DbSet cho các bảng
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Cấu hình Product
            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);

            // Seed dữ liệu Users
            modelBuilder.Entity<User>().HasData(
                new User { Id = 1, Username = "admin", Password = "123", Role = "Admin" },
                new User { Id = 2, Username = "user", Password = "123", Role = "User" }
            );

            // Cấu hình quan hệ Order - OrderDetail
            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Order)
                .WithMany(o => o.OrderDetails)
                .HasForeignKey(od => od.OrderId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa Order thì xóa luôn OrderDetail

            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Product)
                .WithMany() // Không cần navigation property từ Product sang OrderDetail
                .HasForeignKey(od => od.ProductId)
                .OnDelete(DeleteBehavior.Restrict); // Không cho xóa Product nếu đã có OrderDetail
        }
    }
}