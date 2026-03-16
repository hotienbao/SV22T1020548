using Microsoft.AspNetCore.Mvc;
using SV22T1020548.BusinessLayers;
using SV22T1020548.Models.Common;
using System.Threading.Tasks;

namespace SV22T1020548.Admin.Controllers
{
    public class EmployeeController : Controller
    {
        // ========================================================
        // 1. QUẢN LÝ THÔNG TIN CƠ BẢN (CRUD)
        // ========================================================

        /// <summary>
        /// Giao diện hiển thị danh sách nhân viên
        /// </summary>
        public async Task<IActionResult> Index(string searchValue = "", int page = 1)
        {
            ViewBag.Title = "Quản lý nhân viên";
            ViewBag.SearchValue = searchValue;

            // Khởi tạo thông tin tìm kiếm và phân trang
            var input = new PaginationSearchInput
            {
                Page = page,
                PageSize = 10, // Số dòng hiển thị (nhân viên thường để 10 hoặc 15 dòng)
                SearchValue = searchValue ?? ""
            };

            // Gọi Business Layer để lấy dữ liệu từ DB (Dùng HRDataService cho Nhân viên)
            var result = await HRDataService.ListEmployeesAsync(input);

            // Truyền kết quả ra View
            return View(result);
        }

        /// <summary>
        /// Giao diện form thêm mới nhân viên
        /// </summary>
        public IActionResult Create()
        {
            ViewBag.Title = "Thêm mới nhân viên";
            // Tái sử dụng View "Edit" cho chức năng Create
            return View("Edit");
        }

        /// <summary>
        /// Giao diện form cập nhật thông tin nhân viên
        /// </summary>
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật thông tin nhân viên";
            var employee = await HRDataService.GetEmployeeAsync(id);
            return View(employee);
        }

        /// <summary>
        /// Xử lý dữ liệu từ form Thêm/Sửa nhân viên (POST)
        /// </summary>
        [HttpPost]
        public IActionResult Save(int employeeId, string fullName, string email, string phone /*, ...các trường khác*/)
        {
            // TODO: Validation dữ liệu đầu vào
            // TODO: Nếu employeeId == 0 -> Insert
            // TODO: Nếu employeeId > 0 -> Update

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Giao diện xác nhận xóa nhân viên (GET)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            ViewBag.Title = "Xóa nhân viên";
            var employee = await HRDataService.GetEmployeeAsync(id);
            return View(employee);
        }

        /// <summary>
        /// Thực hiện xóa nhân viên sau khi xác nhận (POST)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Delete(int id, bool confirm)
        {
            if (confirm)
            {
                await HRDataService.DeleteEmployeeAsync(id);
            }
            return RedirectToAction("Index");
        }

        // ========================================================
        // 2. QUẢN LÝ BẢO MẬT & PHÂN QUYỀN
        // ========================================================

        /// <summary>
        /// Giao diện đổi mật khẩu của nhân viên (Dành cho Admin reset pass)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ChangePassword(int id)
        {
            ViewBag.Title = "Đổi mật khẩu nhân viên";
            var employee = await HRDataService.GetEmployeeAsync(id);
            return View(employee);
        }

        /// <summary>
        /// Xử lý cập nhật mật khẩu mới (POST)
        /// </summary>
        [HttpPost]
        public IActionResult ChangePassword(int id, string newPassword, string confirmPassword)
        {
            // TODO: Kiểm tra newPassword == confirmPassword
            // TODO: Mã hóa mật khẩu (Hashing) và lưu xuống Database

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Giao diện thay đổi quyền (Role) của nhân viên
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ChangeRole(int id)
        {
            ViewBag.Title = "Thay đổi quyền nhân viên";
            var employee = await HRDataService.GetEmployeeAsync(id);
            return View(employee);
        }

        /// <summary>
        /// Xử lý cập nhật quyền mới (POST)
        /// </summary>
        [HttpPost]
        public IActionResult ChangeRole(int id, string role)
        {
            // TODO: Kiểm tra tính hợp lệ của Role (VD: "Admin", "Staff")
            // TODO: Cập nhật Role mới vào Database

            return RedirectToAction("Index");
        }
    }
}