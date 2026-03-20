using Microsoft.EntityFrameworkCore;
using Rotativa.AspNetCore;
using WebBanHang.Data;
using Rotativa.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// trước build
builder.Services.AddSession();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();
RotativaConfiguration.Setup(app.Environment.WebRootPath, "wkhtmltopdf");
// Middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 
app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();