using Microsoft.AspNetCore.Mvc;
using SV22T1020548.BusinessLayers;
using SV22T1020548.Models.Common;
using System.Threading.Tasks;

namespace SV22T1020548.Admin.Controllers
{
    public class CategoryController : Controller
    {
        // ========================================================
        // QUẢN LÝ LOẠI HÀNG HÓA (CATEGORY)
        // ========================================================

        /// <summary>
        /// Giao diện hiển thị danh sách loại hàng hóa
        /// </summary>
        public async Task<IActionResult> Index(string searchValue = "", int page = 1)
        {
            ViewBag.Title = "Quản lý loại hàng hóa";
            ViewBag.SearchValue = searchValue;

            var input = new PaginationSearchInput
            {
                Page = page,
                PageSize = 10,
                SearchValue = searchValue ?? ""
            };

            // Gọi CatalogDataService để lấy dữ liệu thật
            var result = await CatalogDataService.ListCategoriesAsync(input);
            return View(result);
        }

        /// <summary>
        /// Giao diện form thêm mới loại hàng hóa
        /// </summary>
        public IActionResult Create()
        {
            ViewBag.Title = "Thêm loại hàng hóa";
            return View("Edit");
        }

        /// <summary>
        /// Giao diện form cập nhật thông tin loại hàng hóa
        /// </summary>
        public IActionResult Edit(int id)
        {
            ViewBag.Title = "Cập nhật loại hàng hóa";
            // TODO: Lấy dữ liệu của Category có mã là 'id' từ Database và truyền ra View
            return View();
        }

        /// <summary>
        /// Xử lý lưu dữ liệu từ Form
        /// </summary>
        [HttpPost]
        public IActionResult Save(int categoryId, string categoryName, string description)
        {
            // TODO: Xử lý lưu dữ liệu
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Giao diện xác nhận xóa loại hàng hóa (GET)
        /// </summary>
        [HttpGet]
        public IActionResult Delete(int id)
        {
            ViewBag.Title = "Xóa loại hàng hóa";
            return View();
        }

        /// <summary>
        /// Thực thi lệnh xóa sau khi người dùng nhấn xác nhận (POST)
        /// </summary>
        [HttpPost]
        public IActionResult Delete(int id, bool confirm)
        {
            // TODO: Thực hiện câu lệnh Delete trong Database
            return RedirectToAction("Index");
        }
    }
}