using Microsoft.AspNetCore.Mvc;

namespace SV22T1020548.Admin.Controllers
{
    public class AccountController : Controller
    {
        // ========================================================
        // 1. ĐĂNG NHẬP / ĐĂNG XUẤT (AUTHENTICATION)
        // ========================================================

        /// <summary>
        /// Giao diện đăng nhập hệ thống (GET)
        /// </summary>
        [HttpGet]
        public IActionResult Login()
        {
            // TODO: Kiểm tra nếu người dùng ĐÃ đăng nhập rồi (dựa vào Session hoặc Cookie) 
            // thì chuyển hướng thẳng vào trang chủ, không bắt đăng nhập lại.
            // if (User.Identity.IsAuthenticated) return RedirectToAction("Index", "Home");

            return View();
        }

        /// <summary>
        /// Xử lý thông tin đăng nhập từ Form (POST)
        /// </summary>
        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            // TODO: 1. Truy vấn Database để kiểm tra (email, password) có hợp lệ không.
            // TODO: 2. Nếu sai -> Trả về View kèm thông báo lỗi.
            // ModelState.AddModelError("", "Email hoặc mật khẩu không chính xác");
            // return View();

            // TODO: 3. Nếu đúng -> Khởi tạo Session hoặc Cookie Authentication ghi nhận đăng nhập thành công.

            // Chuyển hướng vào trang chủ của hệ thống sau khi đăng nhập thành công
            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Xử lý đăng xuất hệ thống
        /// </summary>
        public IActionResult Logout()
        {
            // TODO: Xóa toàn bộ Session hoặc Cookie ghi nhận đăng nhập của người dùng.
            // HttpContext.SignOutAsync();
            // HttpContext.Session.Clear();

            // Đăng xuất xong sẽ bị đẩy ra lại trang Login
            return RedirectToAction("Login");
        }


        // ========================================================
        // 2. ĐỔI MẬT KHẨU CÁ NHÂN CỦA USER ĐANG ĐĂNG NHẬP
        // ========================================================

        /// <summary>
        /// Giao diện đổi mật khẩu cá nhân (GET)
        /// </summary>
        [HttpGet]
        public IActionResult ChangePassword()
        {
            ViewBag.Title = "Đổi mật khẩu";
            return View();
        }

        /// <summary>
        /// Xử lý đổi mật khẩu sau khi điền Form (POST)
        /// </summary>
        [HttpPost]
        public IActionResult ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            // TODO: 1. Lấy thông tin tài khoản đang đăng nhập hiện tại (từ Session/Cookie).
            // TODO: 2. Kiểm tra mật khẩu cũ (oldPassword) có khớp với CSDL không.
            // TODO: 3. Kiểm tra newPassword có trùng khớp với confirmPassword không.
            // TODO: 4. Mã hóa (Hash) mật khẩu mới và lưu xuống Database.

            // Đổi mật khẩu thành công thì quay về trang chủ (hoặc có thể bắt đăng nhập lại)
            return RedirectToAction("Index", "Home");
        }
    }
}