using Microsoft.AspNetCore.Mvc;
using SV22T1020548.BusinessLayers;
using SV22T1020548.Models.Common;
using SV22T1020548.Models.Sales;
using SV22T1020548.Shop.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SV22T1020548.Shop.Controllers
{
    public class OrderController : Controller
    {
        private const string CART_KEY = "cart";
        private const int PAGE_SIZE = 10;

        private WebUserData? CurrentUser =>
            HttpContext.Session.GetObject<WebUserData>("user");

        private List<CartItem> GetCart() =>
            HttpContext.Session.GetObject<List<CartItem>>(CART_KEY) ?? new List<CartItem>();

        private IActionResult RedirectToLogin(string returnUrl)
        {
            TempData["ErrorMessage"] = "Vui lòng đăng nhập để tiếp tục";
            return RedirectToAction("Login", "Account", new { returnUrl });
        }

        // ==================== CHECKOUT ====================

        [HttpGet]
        public IActionResult Checkout()
        {
            var user = CurrentUser;
            if (user == null) return RedirectToLogin("/Order/Checkout");

            var cart = GetCart();
            if (cart.Count == 0)
            {
                TempData["ErrorMessage"] = "Giỏ hàng trống. Vui lòng thêm sản phẩm trước khi thanh toán.";
                return RedirectToAction("Index", "Cart");
            }

            // Pre-fill địa chỉ từ thông tin khách hàng
            ViewBag.DefaultAddress = user.Address;
            ViewBag.DefaultProvince = user.Province;

            return View(cart);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(string deliveryProvince, string deliveryAddress)
        {
            var user = CurrentUser;
            if (user == null) return RedirectToLogin("/Order/Checkout");

            var cart = GetCart();
            if (cart.Count == 0)
            {
                TempData["ErrorMessage"] = "Giỏ hàng trống. Vui lòng thêm sản phẩm trước khi thanh toán.";
                return RedirectToAction("Index", "Cart");
            }

            foreach (var cartItem in cart)
            {
                var stockProduct = await CatalogDataService.GetProductAsync(cartItem.ProductID);
                if (stockProduct == null || cartItem.Quantity > stockProduct.Quantity)
                {
                    TempData["ErrorMessage"] = stockProduct == null
                        ? $"Sản phẩm '{cartItem.ProductName}' không còn tồn tại."
                        : $"Sản phẩm này chỉ còn [{stockProduct.Quantity}] cái trong kho!";
                    return RedirectToAction("Index", "Cart");
                }
            }

            bool hasError = false;
            if (string.IsNullOrWhiteSpace(deliveryProvince))
            {
                ModelState.AddModelError("deliveryProvince", "Vui lòng chọn tỉnh/thành phố!");
                hasError = true;
            }
            if (string.IsNullOrWhiteSpace(deliveryAddress))
            {
                ModelState.AddModelError("deliveryAddress", "Vui lòng nhập địa chỉ giao hàng!");
                hasError = true;
            }
            if (hasError)
            {
                ViewBag.DefaultAddress = user.Address;
                ViewBag.DefaultProvince = user.Province;
                return View(cart);
            }

            var details = new List<OrderDetail>();
            foreach (var item in cart)
            {
                if (item.Quantity <= 0)
                {
                    ModelState.AddModelError("", $"Số lượng sản phẩm '{item.ProductName}' không hợp lệ.");
                    ViewBag.DefaultAddress = user.Address;
                    ViewBag.DefaultProvince = user.Province;
                    return View(cart);
                }

                var product = await CatalogDataService.GetProductAsync(item.ProductID);
                if (product == null)
                {
                    ModelState.AddModelError("", $"Sản phẩm '{item.ProductName}' không còn tồn tại.");
                    ViewBag.DefaultAddress = user.Address;
                    ViewBag.DefaultProvince = user.Province;
                    return View(cart);
                }

                decimal salePrice = item.SalePrice > 0 ? item.SalePrice : product.Price;
                if (salePrice <= 0)
                {
                    ModelState.AddModelError("", $"Giá bán của sản phẩm '{product.ProductName}' không hợp lệ.");
                    ViewBag.DefaultAddress = user.Address;
                    ViewBag.DefaultProvince = user.Province;
                    return View(cart);
                }

                details.Add(new OrderDetail
                {
                    ProductID = item.ProductID,
                    Quantity = item.Quantity,
                    SalePrice = salePrice
                });
            }

            var order = new Order
            {
                CustomerID = user.CustomerID,
                DeliveryProvince = deliveryProvince.Trim(),
                DeliveryAddress = deliveryAddress.Trim(),
                OrderTime = DateTime.Now,
                Status = OrderStatusEnum.New
            };

            int orderID;
            try
            {
                orderID = await SalesDataService.CreateOrderAsync(order, details);
            }
            catch
            {
                ModelState.AddModelError("", "Không thể tạo đơn hàng do lỗi hệ thống. Vui lòng thử lại!");
                ViewBag.DefaultAddress = user.Address;
                ViewBag.DefaultProvince = user.Province;
                return View(cart);
            }
            if (orderID <= 0)
            {
                ModelState.AddModelError("", "Không thể tạo đơn hàng. Vui lòng thử lại!");
                ViewBag.DefaultAddress = user.Address;
                ViewBag.DefaultProvince = user.Province;
                return View(cart);
            }

            // Xóa giỏ hàng sau khi đặt thành công
            HttpContext.Session.Remove(CART_KEY);

            TempData["SuccessMessage"] = $"Đặt hàng thành công! Mã đơn hàng của bạn: #{orderID}";
            return RedirectToAction("History");
        }

        // ==================== LỊCH SỬ MUA HÀNG ====================

        public async Task<IActionResult> History(int page = 1, int status = 0)
        {
            var user = CurrentUser;
            if (user == null) return RedirectToLogin("/Order/History");

            OrderStatusEnum? filterStatus = status == 0 ? (OrderStatusEnum?)null : (OrderStatusEnum)status;
            var pagedData = await SalesDataService.ListOrdersByCustomerAsync(user.CustomerID, page, PAGE_SIZE, filterStatus);
            int total = pagedData.RowCount;
            int totalPages = pagedData.PageCount;
            page = pagedData.Page;

            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = total;
            ViewBag.Status = status;

            return View(pagedData.DataItems);
        }

        // ==================== CHI TIẾT ĐƠN HÀNG ====================

        public async Task<IActionResult> Detail(int id)
        {
            var user = CurrentUser;
            if (user == null) return RedirectToLogin($"/Order/Detail/{id}");

            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng!";
                return RedirectToAction("History");
            }

            // Bảo mật: chỉ cho xem đơn hàng của chính mình
            if (order.CustomerID != user.CustomerID)
            {
                TempData["ErrorMessage"] = "Bạn không có quyền xem đơn hàng này!";
                return RedirectToAction("History");
            }

            var details = await SalesDataService.ListDetailsAsync(id);
            ViewBag.Details = details ?? new List<OrderDetailViewInfo>();

            return View(order);
        }
    }
}
