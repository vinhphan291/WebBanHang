using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;

namespace WebBanHang.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<Banner> Banners { get; set; }
        public DbSet<ProductReview> ProductReviews { get; set; }   // SỬ DỤNG ProductReview

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);

            // Seed Users
            modelBuilder.Entity<User>().HasData(
                new User { Id = 1, Username = "admin", Password = "123", Role = "Admin" },
                new User { Id = 2, Username = "user", Password = "123", Role = "User" }
            );

            // Seed Banners
            modelBuilder.Entity<Banner>().HasData(
                new Banner { Id = 1, Title = "iPhone 14 giảm giá sốc", Description = "Giảm ngay 2 triệu", ImageUrl = "/images/banner1.jpg", Link = "/Products/Index", SortOrder = 1, IsActive = true },
                new Banner { Id = 2, Title = "Laptop Dell XPS", Description = "Ưu đãi lớn - Trả góp 0%", ImageUrl = "/images/banner2.jpg", Link = "/Products/Index", SortOrder = 2, IsActive = true },
                new Banner { Id = 3, Title = "Phụ kiện chính hãng", Description = "Tai nghe, sạc dự phòng giảm đến 30%", ImageUrl = "/images/banner3.jpg", Link = "/Products/Index", SortOrder = 3, IsActive = true }
            );

            // Order - OrderDetail
            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Order)
                .WithMany(o => o.OrderDetails)
                .HasForeignKey(od => od.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Product)
                .WithMany()
                .HasForeignKey(od => od.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // Cấu hình ProductReview
            modelBuilder.Entity<ProductReview>()
                .HasOne(r => r.Product)
                .WithMany()
                .HasForeignKey(r => r.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        }


    }
}