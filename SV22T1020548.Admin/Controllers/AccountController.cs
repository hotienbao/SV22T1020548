using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020548.BusinessLayers;
using SV22T1020548.Models.HR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SV22T1020548.Admin.Controllers
{
    [Authorize]   // toàn bộ Admin phải đăng nhập
    public class AccountController : Controller
    {
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string UserName, string Password)
        {
            ViewBag.Username = UserName;

            UserName = UserName?.Trim().ToLowerInvariant() ?? "";
            Password = Password?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(UserName) || string.IsNullOrWhiteSpace(Password))
            {
                ModelState.AddModelError("", "Vui lòng nhập đầy đủ thông tin!");
                return View();
            }

            var userAccount = await UserAccountDataService.AuthorizeAsync(UserName, Password);
            if (userAccount == null)
            {
                ModelState.AddModelError("", "Sai tài khoản hoặc mật khẩu!");
                return View();
            }

            // Chỉ nhân viên (có RoleNames) mới được vào Admin
            if (string.IsNullOrWhiteSpace(userAccount.RoleNames))
            {
                ModelState.AddModelError("", "Tài khoản này không có quyền truy cập Admin!");
                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name,  userAccount.UserName    ?? UserName),
                new Claim(ClaimTypes.Email, userAccount.Email       ?? ""),
                new Claim("FullName",       userAccount.DisplayName ?? ""),
                new Claim("Photo",          userAccount.Photo       ?? "nophoto.png"),
                // UserId = EmployeeID (SQL: EmployeeID AS UserId trong EmployeeAccountRepository)
                new Claim("UserId",         userAccount.UserId      ?? "0"),
            };

            foreach (var role in userAccount.RoleNames.Split(';'))
                if (!string.IsNullOrWhiteSpace(role))
                    claims.Add(new Claim(ClaimTypes.Role, role.Trim()));

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));

            return RedirectToAction("Index", "Home");
        }

        // ==================== ĐĂNG KÝ (NHÂN VIÊN / ADMIN) ====================

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            return View(new Employee());
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(Employee data, string password, string confirmPassword)
        {
            data ??= new Employee();
            data.FullName = data.FullName?.Trim() ?? "";
            data.Email = data.Email?.Trim().ToLowerInvariant() ?? "";
            data.Phone = data.Phone?.Trim() ?? "";
            data.Address = data.Address?.Trim();
            password = password?.Trim() ?? "";
            confirmPassword = confirmPassword?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(data.FullName))
                ModelState.AddModelError("FullName", "Họ và tên không được để trống!");

            if (string.IsNullOrWhiteSpace(data.Email))
                ModelState.AddModelError("Email", "Email không được để trống!");
            else
            {
                try { _ = new MailAddress(data.Email); }
                catch { ModelState.AddModelError("Email", "Email không hợp lệ!"); }
            }

            if (!string.IsNullOrWhiteSpace(data.Phone))
            {
                int digits = data.Phone.Count(char.IsDigit);
                if (digits < 7 || digits > 20)
                    ModelState.AddModelError("Phone", "Số điện thoại phải có từ 7 đến 20 chữ số!");
            }

            if (string.IsNullOrWhiteSpace(password))
                ModelState.AddModelError("Password", "Mật khẩu không được để trống!");
            else if (password.Length < 6)
                ModelState.AddModelError("Password", "Mật khẩu phải có ít nhất 6 ký tự!");

            if (password != confirmPassword)
                ModelState.AddModelError("ConfirmPassword", "Xác nhận mật khẩu không khớp!");

            if (!ModelState.IsValid)
                return View(data);

            // Uniqueness
            if (await HRDataService.InUseEmployeeEmailAsync(data.Email, 0))
            {
                ModelState.AddModelError("Email", "Email này đã được sử dụng!");
                return View(data);
            }

            if (!string.IsNullOrWhiteSpace(data.Phone) && await HRDataService.InUseEmployeePhoneAsync(data.Phone, 0))
            {
                ModelState.AddModelError("Phone", "Số điện thoại này đã được sử dụng!");
                return View(data);
            }

            // Tạo nhân viên tối thiểu để có thể đăng nhập Admin
            var employee = new Employee
            {
                FullName = data.FullName,
                BirthDate = data.BirthDate,
                Address = data.Address,
                Phone = data.Phone,
                Email = data.Email,
                Photo = string.IsNullOrWhiteSpace(data.Photo) ? "nophoto.svg" : data.Photo?.Trim(),
                IsWorking = true,
                RoleNames = string.IsNullOrWhiteSpace(data.RoleNames) ? "admin" : data.RoleNames?.Trim()
            };

            int newId;
            try
            {
                newId = await HRDataService.AddEmployeeWithPasswordAsync(employee, password);
            }
            catch
            {
                ModelState.AddModelError("", "Đăng ký thất bại do lỗi hệ thống.");
                return View(data);
            }

            if (newId <= 0)
            {
                ModelState.AddModelError("", "Đăng ký thất bại, vui lòng thử lại!");
                return View(data);
            }

            TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
            return RedirectToAction("Login");
        }

        // ==================== QUÊN MẬT KHẨU ====================

        [AllowAnonymous]
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email, string newPassword, string confirmPassword)
        {
            email = email?.Trim().ToLowerInvariant() ?? "";
            newPassword = newPassword?.Trim() ?? "";
            confirmPassword = confirmPassword?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(email))
                ModelState.AddModelError("Email", "Vui lòng nhập email.");
            else
            {
                try { _ = new MailAddress(email); }
                catch { ModelState.AddModelError("Email", "Email không hợp lệ."); }
            }

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                ModelState.AddModelError("NewPassword", "Mật khẩu mới phải có ít nhất 6 ký tự.");
            if (newPassword != confirmPassword)
                ModelState.AddModelError("ConfirmPassword", "Xác nhận mật khẩu không khớp.");

            if (!ModelState.IsValid)
                return View();

            bool changed;
            try
            {
                changed = await UserAccountDataService.ChangePasswordAsync(email, newPassword);
            }
            catch
            {
                ModelState.AddModelError("", "Không thể đặt lại mật khẩu do lỗi hệ thống.");
                return View();
            }

            if (!changed)
            {
                ModelState.AddModelError("Email", "Email không tồn tại hoặc tài khoản không khả dụng.");
                return View();
            }

            TempData["SuccessMessage"] = "Đặt lại mật khẩu thành công. Bạn có thể đăng nhập ngay.";
            return RedirectToAction("Login");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult ChangePassword() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(
            string oldPassword, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(oldPassword) ||
                string.IsNullOrWhiteSpace(newPassword) ||
                string.IsNullOrWhiteSpace(confirmPassword))
            {
                ModelState.AddModelError("", "Vui lòng nhập đầy đủ thông tin!");
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("", "Xác nhận mật khẩu không khớp!");
                return View();
            }

            var username = User.Identity?.Name ?? "";
            var check = await UserAccountDataService.AuthorizeAsync(username, oldPassword);
            if (check == null)
            {
                ModelState.AddModelError("", "Mật khẩu cũ không đúng!");
                return View();
            }

            bool ok = await UserAccountDataService.ChangePasswordAsync(username, newPassword);
            if (!ok)
            {
                ModelState.AddModelError("", "Đổi mật khẩu thất bại!");
                return View();
            }

            return RedirectToAction("Logout");
        }
    }
}
