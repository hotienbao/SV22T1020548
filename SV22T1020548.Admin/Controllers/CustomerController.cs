using Microsoft.AspNetCore.Mvc;
using SV22T1020548.BusinessLayers;
using SV22T1020548.Models.Common;
using System.Threading.Tasks;

namespace SV22T1020548.Admin.Controllers
{
    public class CustomerController : Controller
    {
        // ========================================================
        // 1. QUẢN LÝ THÔNG TIN KHÁCH HÀNG (CRUD)
        // ========================================================

        /// <summary>
        /// Giao diện hiển thị danh sách khách hàng
        /// </summary>
        public async Task<IActionResult> Index(string searchValue = "", int page = 1)
        {
            ViewBag.Title = "Quản lý khách hàng";
            ViewBag.SearchValue = searchValue;

            // Khởi tạo điều kiện tìm kiếm và phân trang
            var input = new PaginationSearchInput
            {
                Page = page,
                PageSize = 20, // Số dòng trên 1 trang (có thể tùy chỉnh)
                SearchValue = searchValue ?? ""
            };

            // Gọi Business Layer để lấy dữ liệu từ Database
            var result = await PartnerDataService.ListCustomersAsync(input);

            // Truyền dữ liệu ra View
            return View(result);
        }

        /// <summary>
        /// Giao diện form thêm mới khách hàng
        /// </summary>
        public IActionResult Create()
        {
            ViewBag.Title = "Thêm mới khách hàng";
            // Tái sử dụng giao diện của View "Edit"
            return View("Edit");
        }

        /// <summary>
        /// Giao diện form cập nhật thông tin khách hàng
        /// </summary>
        public IActionResult Edit(int id)
        {
            ViewBag.Title = "Cập nhật thông tin khách hàng";
            // TODO: Lấy dữ liệu của khách hàng theo 'id' từ DB và truyền ra View   
            return View();
        }

        /// <summary>
        /// Xử lý dữ liệu từ form Thêm/Sửa (POST)
        /// </summary>
        [HttpPost]
        public IActionResult Save(int customerId, string customerName, string contactName, string province, string address, string phone, string email)
        {
            // TODO: Kiểm tra tính hợp lệ của dữ liệu đầu vào (Validation)
            // TODO: Nếu customerId == 0 -> Thực hiện lệnh Insert vào CSDL
            // TODO: Nếu customerId > 0 -> Thực hiện lệnh Update vào CSDL

            // Lưu thành công thì quay về trang danh sách
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Giao diện xác nhận xóa khách hàng (GET)
        /// </summary>
        [HttpGet]
        public IActionResult Delete(int id)
        {
            ViewBag.Title = "Xóa khách hàng";
            // TODO: Lấy thông tin khách hàng cần xóa hiển thị ra giao diện.
            // Cần kiểm tra xem khách hàng này đã có đơn hàng nào chưa để quyết định cho phép xóa hay không.
            return View();
        }

        /// <summary>
        /// Thực hiện xóa khách hàng sau khi xác nhận (POST)
        /// </summary>
        [HttpPost]
        public IActionResult Delete(int id, bool confirm)
        {
            // TODO: Thực thi câu lệnh Delete trong Database

            return RedirectToAction("Index");
        }

        // ========================================================
        // 2. QUẢN LÝ TÀI KHOẢN KHÁCH HÀNG
        // ========================================================

        /// <summary>
        /// Giao diện đổi mật khẩu của khách hàng (GET)
        /// </summary>
        [HttpGet]
        public IActionResult ChangePassword(int id)
        {
            ViewBag.Title = "Đổi mật khẩu khách hàng";
            // TODO: Lấy thông tin khách hàng để hiển thị email/tên đang được đổi mật khẩu
            return View();
        }

        /// <summary>
        /// Xử lý đổi mật khẩu (POST)
        /// </summary>
        [HttpPost]
        public IActionResult ChangePassword(int id, string newPassword, string confirmPassword)
        {
            // TODO: Kiểm tra newPassword có khớp với confirmPassword không
            // TODO: Mã hóa mật khẩu và cập nhật vào Database

            return RedirectToAction("Index");
        }
    }
}