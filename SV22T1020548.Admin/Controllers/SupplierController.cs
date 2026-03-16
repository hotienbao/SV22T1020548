using Microsoft.AspNetCore.Mvc;
using SV22T1020548.BusinessLayers;
using SV22T1020548.Models.Common;
using System.Threading.Tasks;

namespace SV22T1020548.Admin.Controllers
{
    public class SupplierController : Controller
    {
        // ========================================================
        // QUẢN LÝ NHÀ CUNG CẤP (SUPPLIER)
        // ========================================================

        /// <summary>
        /// Giao diện hiển thị danh sách nhà cung cấp
        /// </summary>
        public async Task<IActionResult> Index(string searchValue = "", int page = 1)
        {
            ViewBag.Title = "Quản lý nhà cung cấp";
            ViewBag.SearchValue = searchValue;

            // Khởi tạo thông tin tìm kiếm và phân trang
            var input = new PaginationSearchInput
            {
                Page = page,
                PageSize = 20, // Số dòng hiển thị trên mỗi trang
                SearchValue = searchValue ?? ""
            };

            // Gọi Business Layer để lấy dữ liệu từ DB
            var result = await PartnerDataService.ListSuppliersAsync(input);

            // Truyền kết quả ra View
            return View(result);
        }

        /// <summary>
        /// Giao diện form thêm mới nhà cung cấp
        /// </summary>
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung nhà cung cấp";
            // Tái sử dụng View "Edit" cho chức năng Create để tránh trùng lặp code
            return View("Edit");
        }

        /// <summary>
        /// Giao diện form cập nhật thông tin nhà cung cấp
        /// </summary>
        public IActionResult Edit(int id)
        {
            ViewBag.Title = "Cập nhật thông tin nhà cung cấp";
            // TODO: Truy vấn Database lấy thông tin của nhà cung cấp theo 'id' và truyền ra View
            return View();
        }

        /// <summary>
        /// Xử lý dữ liệu từ form Thêm/Sửa (POST)
        /// </summary>
        [HttpPost]
        public IActionResult Save(int supplierId, string supplierName, string contactName, string province, string address, string phone, string email)
        {
            // TODO: Kiểm tra tính hợp lệ của dữ liệu đầu vào (Validation)
            // VD: Tên nhà cung cấp không được để trống, email phải đúng định dạng...

            // TODO: Nếu supplierId == 0 -> Thực hiện lệnh Insert vào CSDL
            // TODO: Nếu supplierId > 0 -> Thực hiện lệnh Update vào CSDL

            // Sau khi lưu thành công thì quay về trang danh sách
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Giao diện xác nhận xóa nhà cung cấp (GET)
        /// </summary>
        [HttpGet]
        public IActionResult Delete(int id)
        {
            ViewBag.Title = "Xóa nhà cung cấp";
            // TODO: Lấy thông tin nhà cung cấp cần xóa để hiển thị xác nhận.
            // LƯU Ý: Cần kiểm tra xem nhà cung cấp này có đang cung cấp mặt hàng (Product) nào không.
            // Nếu có, hệ thống không nên cho phép xóa để tránh lỗi dữ liệu mồ côi (Foreign Key constraint).
            return View();
        }

        /// <summary>
        /// Thực hiện xóa nhà cung cấp sau khi xác nhận (POST)
        /// </summary>
        [HttpPost]
        public IActionResult Delete(int id, bool confirm)
        {
            // TODO: Thực thi câu lệnh Delete trong Database
            return RedirectToAction("Index");
        }
    }
}