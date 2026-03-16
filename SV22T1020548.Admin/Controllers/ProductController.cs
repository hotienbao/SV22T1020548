using Microsoft.AspNetCore.Mvc;
using SV22T1020548.BusinessLayers;
using SV22T1020548.Models.Common;
using SV22T1020548.Models.Catalog;
using System.Threading.Tasks;

namespace SV22T1020548.Admin.Controllers
{
    public class ProductController : Controller
    {
        // ========================================================
        // 1. QUẢN LÝ THÔNG TIN MẶT HÀNG (SẢN PHẨM CHÍNH)
        // ========================================================

        /// <summary>
        /// Giao diện danh sách các mặt hàng
        /// </summary>
        public async Task<IActionResult> Index(int categoryID = 0, int supplierID = 0, string searchValue = "", int page = 1)
        {
            ViewBag.Title = "Quản lý mặt hàng";

            // Lưu lại các tham số để đổ lại ra View (Form search & Phân trang)
            ViewBag.SearchValue = searchValue;
            ViewBag.CategoryID = categoryID;
            ViewBag.SupplierID = supplierID;

            // Truyền điều kiện tìm kiếm (Cần phải có class ProductSearchInput)
            // Nếu bạn không có class này, bạn cần tự tạo nó trong thư mục Models.Common
            var input = new ProductSearchInput
            {
                Page = page,
                PageSize = 20, // Hiển thị 20 sản phẩm 1 trang
                SearchValue = searchValue ?? "",
                CategoryID = categoryID,
                SupplierID = supplierID
            };

            // Lấy danh sách sản phẩm
            var result = await CatalogDataService.ListProductsAsync(input);

            // Lấy danh sách Loại hàng và Nhà cung cấp để hiển thị Dropdown
            // (Dùng PageSize thật lớn để lấy toàn bộ danh sách lên dropdown)
            var categories = await CatalogDataService.ListCategoriesAsync(new PaginationSearchInput { Page = 1, PageSize = 9999, SearchValue = "" });
            ViewBag.Categories = categories.DataItems;

            var suppliers = await PartnerDataService.ListSuppliersAsync(new PaginationSearchInput { Page = 1, PageSize = 9999, SearchValue = "" });
            ViewBag.Suppliers = suppliers.DataItems;

            return View(result);
        }

        // ... [Vui lòng giữ nguyên các hàm Create, Edit, Delete, Photos, Attributes ở bên dưới như code gốc của bạn]

        public IActionResult Create() { ViewBag.Title = "Thêm mặt hàng"; return View("Edit"); }
        public IActionResult Edit(int id) { ViewBag.Title = "Cập nhật mặt hàng"; return View(); }
        public IActionResult Delete(int id) { ViewBag.Title = "Xóa mặt hàng"; return View(); }
        // ... (các hàm khác)
    }
}