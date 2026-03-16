using Microsoft.AspNetCore.Mvc;
using SV22T1020548.BusinessLayers;
using SV22T1020548.Models.Common;
using System.Threading.Tasks;

namespace SV22T1020548.Admin.Controllers
{
    public class ShipperController : Controller
    {
        // ========================================================
        // QUẢN LÝ NGƯỜI GIAO HÀNG (SHIPPER)
        // ========================================================

        /// <summary>
        /// Giao diện hiển thị danh sách người giao hàng
        /// </summary>
        public async Task<IActionResult> Index(string searchValue = "", int page = 1)
        {
            ViewBag.Title = "Quản lý nhân viên giao hàng";
            ViewBag.SearchValue = searchValue;

            // Khởi tạo thông tin tìm kiếm và phân trang
            var input = new PaginationSearchInput
            {
                Page = page,
                PageSize = 20, // Số dòng hiển thị trên mỗi trang
                SearchValue = searchValue ?? ""
            };

            // Gọi Business Layer để lấy dữ liệu từ DB
            var result = await PartnerDataService.ListShippersAsync(input);

            // Truyền kết quả ra View
            return View(result);
        }

        /// <summary>
        /// Giao diện form thêm mới người giao hàng
        /// </summary>
        public IActionResult Create()
        {
            ViewBag.Title = "Thêm mới nhân viên giao hàng";
            // Tái sử dụng View "Edit" cho chức năng Create
            return View("Edit");
        }

        /// <summary>
        /// Giao diện form cập nhật thông tin người giao hàng
        /// </summary>
        public IActionResult Edit(int id)
        {
            ViewBag.Title = "Cập nhật nhân viên giao hàng";
            // TODO: Lấy thông tin Shipper theo 'id' từ DB và truyền sang View
            return View();
        }

        /// <summary>
        /// Xử lý lưu dữ liệu từ form Thêm/Sửa (POST)
        /// </summary>
        [HttpPost]
        public IActionResult Save(int shipperId, string shipperName, string phone)
        {
            // TODO: Kiểm tra tính hợp lệ của dữ liệu đầu vào (Validation)
            // TODO: Nếu shipperId == 0 -> Thực hiện lệnh Insert vào CSDL
            // TODO: Nếu shipperId > 0 -> Thực hiện lệnh Update vào CSDL

            // Sau khi lưu xong, quay về trang danh sách
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Giao diện xác nhận xóa người giao hàng (GET)
        /// </summary>
        [HttpGet]
        public IActionResult Delete(int id)
        {
            ViewBag.Title = "Xóa nhân viên giao hàng";
            // TODO: Lấy thông tin Shipper cần xóa để hiển thị cảnh báo trên View
            // Lưu ý: Kiểm tra xem Shipper này có đang gắn với đơn hàng nào không trước khi cho phép xóa.
            return View();
        }

        /// <summary>
        /// Thực hiện xóa người giao hàng sau khi xác nhận (POST)
        /// </summary>
        [HttpPost]
        public IActionResult Delete(int id, bool confirm)
        {
            // TODO: Thực thi câu lệnh Delete trong Database
            return RedirectToAction("Index");
        }
    }
}