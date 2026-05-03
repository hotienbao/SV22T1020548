using System.Globalization;
using Microsoft.Extensions.FileProviders;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddControllersWithViews();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.Name = "SV22T1020548.Shop.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    // Tránh session cookie hết hạn quá sớm => người dùng không bị yêu cầu đăng nhập lại
    options.Cookie.MaxAge = TimeSpan.FromDays(7);
});

var app = builder.Build();

// Middleware order is critical
app.UseStaticFiles();

// Share physical images folder from Admin project (same physical files)
var adminImagePath = Path.Combine(
    builder.Environment.ContentRootPath,
    "..",
    "SV22T1020548.Admin",
    "wwwroot",
    "images");

if (Directory.Exists(adminImagePath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(adminImagePath),
        RequestPath = "/images"
    });
}

app.UseRouting();
app.UseSession();          // Session MUST come before controllers
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Culture: Vietnamese
var cultureInfo = new CultureInfo("vi-VN");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

// Initialize Business Layer
string connectionString = builder.Configuration.GetConnectionString("LiteCommerceDB") ?? "";
SV22T1020548.BusinessLayers.Configuration.Initialize(connectionString);

app.Run();
