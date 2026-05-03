using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020548.Admin.AppCodes;
using SV22T1020548.BusinessLayers;
using SV22T1020548.Models.Catalog;
using SV22T1020548.Models.Common;
using SV22T1020548.Models.Sales;
using SV22T1020548.Admin;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace SV22T1020548.Admin.Controllers
{
    [Authorize]   // phải đăng nhập
    public class OrderController : Controller
    {
        private const string ORDER_SEARCH = "OrderSearchInput";
        private const string PRODUCT_SEARCH = "SearchProductToSale";
        private const string SHOPPING_CART = "ShoppingCart";
        private const int PAGE_SIZE = 20;

        // ─── Lấy EmployeeID từ Cookie Claim ──────────────────────────────────
        private int CurrentEmployeeID
        {
            get
            {
                _ = int.TryParse(User.FindFirst("UserId")?.Value ?? "0", out int id);
                return id;
            }
        }

        // ─── GIỎ HÀNG (session) ──────────────────────────────────────────────
        private List<OrderDetailViewInfo> GetCart() =>
            ApplicationContext.GetSessionData<List<OrderDetailViewInfo>>(SHOPPING_CART)
            ?? new List<OrderDetailViewInfo>();

        private void SaveCart(List<OrderDetailViewInfo> cart) =>
            ApplicationContext.SetSessionData(SHOPPING_CART, cart);

        // Partial – trả HTML giỏ hàng (dùng bởi AJAX)
        public IActionResult ShowCart() =>
            PartialView("ShowCart", GetCart());

        [HttpGet]
        public IActionResult ClearCartConfirm() => View("ClearCart");

        [HttpPost]
        public IActionResult ClearCart()
        {
            SaveCart(new List<OrderDetailViewInfo>());
            return Json(new ApiResult(1, "Đã xóa giỏ hàng"));
        }

        // Đã đổi tên hàm thành DeleteCartItemPost, nhưng URL gọi vẫn là DeleteCartItem
        [HttpPost]
        [ActionName("DeleteCartItem")]
        public IActionResult DeleteCartItemPost(int productID)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(m => m.ProductID == productID);
            if (item != null) cart.Remove(item);
            SaveCart(cart);
            return Json(new ApiResult(1, "Đã xóa mặt hàng"));
        }

        public IActionResult EditCartItem(int productId)
        {
            var item = GetCart().FirstOrDefault(m => m.ProductID == productId);
            if (item == null) return RedirectToAction("Create");
            return View(item);
        }

        [HttpGet]
        public IActionResult DeleteCartItem(int productId = 0)  // GET – mở confirm modal
        {
            var item = GetCart().FirstOrDefault(m => m.ProductID == productId);
            if (item == null) return Content("");
            return View(item);
        }

        // ─── DANH SÁCH ĐƠN HÀNG ─────────────────────────────────────────────
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<OrderSearchInput>(ORDER_SEARCH)
                        ?? new OrderSearchInput
                        {
                            Page = 1,
                            PageSize = PAGE_SIZE,
                            SearchValue = "",
                            Status = null,
                            DateFrom = null,
                            DateTo = null
                        };
            return View(input);
        }

        public async Task<IActionResult> Search(OrderSearchInput input)
        {
            // Xử lý DateTo: nếu chỉ có ngày thì cộng thêm 23:59:59 để tìm đúng
            if (input.DateTo.HasValue)
                input.DateTo = input.DateTo.Value.Date.AddDays(1).AddSeconds(-1);

            var result = await SalesDataService.ListOrdersAsync(input);
            ApplicationContext.SetSessionData(ORDER_SEARCH, input);
            return PartialView("Search", result);
        }

        // ─── CHI TIẾT ĐƠN HÀNG ──────────────────────────────────────────────
        public async Task<IActionResult> Details(int id)
        {
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null) return RedirectToAction("Index");

            ViewBag.Details = await SalesDataService.ListDetailsAsync(id)
                               ?? new List<OrderDetailViewInfo>();
            ViewBag.Shippers = (await PartnerDataService.ListShippersAsync(
                                new PaginationSearchInput { Page = 1, PageSize = 0, SearchValue = "" }))
                               .DataItems;
            return View(order);
        }

        // ─── LẬP ĐƠN HÀNG ───────────────────────────────────────────────────
        public IActionResult Create()
        {
            // Luôn reset bộ lọc khi mở màn hình tạo đơn để đảm bảo có danh sách gợi ý mặc định.
            var input = new ProductSearchInput
            {
                Page = 1,
                PageSize = PAGE_SIZE,
                SearchValue = "",
                CategoryID = 0,
                SupplierID = 0,
                MinPrice = 0,
                MaxPrice = 0
            };
            ApplicationContext.SetSessionData(PRODUCT_SEARCH, input);
            return View(input);
        }

        public async Task<IActionResult> SearchProduct(ProductSearchInput input)
        {
            input ??= new ProductSearchInput();
            input.Page = input.Page <= 0 ? 1 : input.Page;
            input.PageSize = input.PageSize <= 0 ? PAGE_SIZE : input.PageSize;
            input.SearchValue ??= "";
            var result = await CatalogDataService.ListProductsAsync(input);
            ApplicationContext.SetSessionData(PRODUCT_SEARCH, input);
            return PartialView("SearchProduct", result);
        }

        // AJAX lấy danh sách khách hàng (dropdown động)
        public async Task<IActionResult> GetCustomers(string searchValue = "")
        {
            var data = await PartnerDataService.ListCustomersAsync(
                new PaginationSearchInput { Page = 1, PageSize = 0, SearchValue = searchValue });

            return Json(data.DataItems.Select(c => new
            {
                id = c.CustomerID,
                name = c.CustomerName,
                email = c.Email,
                contactName = c.ContactName,
                phone = c.Phone,
                address = c.Address,
                province = c.Province
            }));
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder(
            int customerID, string deliveryProvince, string deliveryAddress)
        {
            var cart = GetCart();
            if (!cart.Any())
                return Json(new ApiResult(0, "Giỏ hàng trống. Vui lòng chọn mặt hàng!"));
            if (customerID <= 0)
                return Json(new ApiResult(0, "Vui lòng tìm và chọn khách hàng!"));
            if (string.IsNullOrWhiteSpace(deliveryProvince))
                return Json(new ApiResult(0, "Vui lòng chọn tỉnh/thành giao hàng!"));
            if (string.IsNullOrWhiteSpace(deliveryAddress))
                return Json(new ApiResult(0, "Vui lòng nhập địa chỉ giao hàng!"));
            var customer = await PartnerDataService.GetCustomerAsync(customerID);
            if (customer == null)
                return Json(new ApiResult(0, "Khách hàng không tồn tại hoặc đã bị xóa."));

            var order = new Order
            {
                CustomerID = customerID,
                DeliveryProvince = deliveryProvince.Trim(),
                DeliveryAddress = deliveryAddress.Trim(),
                OrderTime = DateTime.Now,
                Status = OrderStatusEnum.New,
                EmployeeID = CurrentEmployeeID > 0 ? CurrentEmployeeID : (int?)null
            };

            var details = cart.Select(item => new OrderDetail
            {
                ProductID = item.ProductID,
                Quantity = item.Quantity,
                SalePrice = item.SalePrice
            }).ToList();

            int orderID = await SalesDataService.CreateOrderAsync(order, details);
            if (orderID <= 0)
                return Json(new ApiResult(0, "Tạo đơn hàng thất bại. Vui lòng kiểm tra lại tồn kho sản phẩm."));

            // Clear giỏ hàng sau khi lập đơn thành công
            SaveCart(new List<OrderDetailViewInfo>());
            return Json(new ApiResult(1, orderID.ToString()));
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int productID, int quantity, string salePrice = "0")
        {
            decimal normalizedSalePrice = ParseMoneyInput(salePrice);
            if (quantity <= 0)
                return Json(new ApiResult(0, "Số lượng phải lớn hơn 0!"));

            var cart = GetCart();
            var item = cart.FirstOrDefault(m => m.ProductID == productID);

            if (item != null)
            {
                var product = await CatalogDataService.GetProductAsync(productID);
                if (product == null)
                    return Json(new ApiResult(0, "Mặt hàng không tồn tại!"));

                int newQty = item.Quantity + quantity;
                if (newQty > product.Quantity)
                    return Json(new ApiResult(0, $"Sản phẩm này chỉ còn [{product.Quantity}] cái trong kho!"));

                item.Quantity = newQty;
                item.SalePrice = normalizedSalePrice > 0 ? normalizedSalePrice : item.SalePrice;
            }
            else
            {
                var product = await CatalogDataService.GetProductAsync(productID);
                if (product == null)
                    return Json(new ApiResult(0, "Mặt hàng không tồn tại!"));
                if (quantity > product.Quantity)
                    return Json(new ApiResult(0, $"Sản phẩm này chỉ còn [{product.Quantity}] cái trong kho!"));

                cart.Add(new OrderDetailViewInfo
                {
                    ProductID = productID,
                    ProductName = product.ProductName ?? "",
                    SalePrice = normalizedSalePrice > 0 ? normalizedSalePrice : product.Price,
                    Quantity = quantity,
                    Photo = product.Photo ?? "",
                    Unit = product.Unit ?? ""
                });
            }
            SaveCart(cart);
            return Json(new ApiResult(1, "Thêm vào giỏ hàng thành công"));
        }

        [HttpPost]
        public async Task<IActionResult> UpdateCartItem(int productID, int quantity, string salePrice = "0")
        {
            decimal normalizedSalePrice = ParseMoneyInput(salePrice);
            if (quantity <= 0)
                return Json(new ApiResult(0, "Số lượng phải lớn hơn 0!"));

            var cart = GetCart();
            var item = cart.FirstOrDefault(m => m.ProductID == productID);
            if (item != null)
            {
                var product = await CatalogDataService.GetProductAsync(productID);
                if (product == null)
                    return Json(new ApiResult(0, "Mặt hàng không tồn tại!"));
                if (quantity > product.Quantity)
                    return Json(new ApiResult(0, $"Sản phẩm này chỉ còn [{product.Quantity}] cái trong kho!"));

                item.Quantity = quantity;
                item.SalePrice = normalizedSalePrice > 0 ? normalizedSalePrice : item.SalePrice;
            }
            SaveCart(cart);
            return Json(new ApiResult(1, "Cập nhật thành công"));
        }

        private static decimal ParseMoneyInput(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0;

            string raw = value.Trim().Replace(".", "").Replace(" ", "");
            if (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.GetCultureInfo("vi-VN"), out var viValue))
                return viValue;
            if (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var invariantValue))
                return invariantValue;
            return 0;
        }

        // ─── CHUYỂN TRẠNG THÁI ───────────────────────────────────────────────

        [HttpGet]
        public IActionResult Accept(int id) => View(id);

        [HttpPost]
        public async Task<IActionResult> Accept(int id, string _ = "")
        {
            var order = await SalesDataService.GetOrderAsync(id);
            if (order != null && order.Status == OrderStatusEnum.New)
            {
                order.Status = OrderStatusEnum.Accepted;
                order.AcceptTime = DateTime.Now;
                order.EmployeeID = CurrentEmployeeID > 0 ? CurrentEmployeeID : order.EmployeeID;
                bool ok = await SalesDataService.UpdateOrderAsync(order);
                if (ok)
                    TempData["SuccessMessage"] = "Duyệt đơn hàng thành công.";
                else
                    TempData["ErrorMessage"] = "Không thể duyệt đơn hàng. Vui lòng thử lại.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể duyệt đơn hàng do trạng thái không hợp lệ.";
            }
            return RedirectToAction("Details", new { id });
        }

        [HttpGet]
        public async Task<IActionResult> Shipping(int id)
        {
            var shippers = await PartnerDataService.ListShippersAsync(
                new PaginationSearchInput { Page = 1, PageSize = 0, SearchValue = "" });
            ViewBag.Shippers = shippers.DataItems;
            return View(id);
        }

        [HttpPost]
        public async Task<IActionResult> Shipping(int id, int shipperID)
        {
            if (shipperID <= 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn người giao hàng!";
                return RedirectToAction("Details", new { id });
            }
            var order = await SalesDataService.GetOrderAsync(id);
            if (order != null && order.Status == OrderStatusEnum.Accepted)
            {
                order.Status = OrderStatusEnum.Shipping;
                order.ShipperID = shipperID;
                order.ShippedTime = DateTime.Now;
                bool ok = await SalesDataService.UpdateOrderAsync(order);
                if (ok)
                    TempData["SuccessMessage"] = "Chuyển trạng thái giao hàng thành công.";
                else
                    TempData["ErrorMessage"] = "Không thể cập nhật trạng thái giao hàng.";
            }
            else
            {
                TempData["ErrorMessage"] = "Đơn hàng chưa ở trạng thái có thể giao.";
            }
            return RedirectToAction("Details", new { id });
        }

        [HttpGet]
        public IActionResult Finish(int id) => View(id);

        [HttpPost]
        public async Task<IActionResult> Finish(int id, string _ = "")
        {
            var order = await SalesDataService.GetOrderAsync(id);
            if (order != null && order.Status == OrderStatusEnum.Shipping)
            {
                order.Status = OrderStatusEnum.Completed;
                order.FinishedTime = DateTime.Now;
                bool ok = await SalesDataService.UpdateOrderAsync(order);
                if (ok)
                    TempData["SuccessMessage"] = "Hoàn tất đơn hàng thành công.";
                else
                    TempData["ErrorMessage"] = "Không thể hoàn tất đơn hàng.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể hoàn tất do đơn hàng chưa ở trạng thái giao hàng.";
            }
            return RedirectToAction("Details", new { id });
        }

        [HttpGet]
        public IActionResult Reject(int id) => View(id);

        [HttpPost]
        public async Task<IActionResult> Reject(int id, string _ = "")
        {
            var order = await SalesDataService.GetOrderAsync(id);
            if (order != null && order.Status == OrderStatusEnum.New)
            {
                bool ok = await SalesDataService.ChangeOrderStatusAsync(id, OrderStatusEnum.Rejected, CurrentEmployeeID);
                if (ok)
                    TempData["SuccessMessage"] = "Từ chối đơn hàng thành công.";
                else
                    TempData["ErrorMessage"] = "Không thể từ chối đơn hàng.";
            }
            else
            {
                TempData["ErrorMessage"] = "Chỉ có thể từ chối đơn hàng ở trạng thái Chờ duyệt.";
            }
            return RedirectToAction("Details", new { id });
        }

        [HttpGet]
        public IActionResult Cancel(int id) => View(id);

        [HttpPost]
        public async Task<IActionResult> Cancel(int id, string _ = "")
        {
            var order = await SalesDataService.GetOrderAsync(id);
            if (order != null
                && order.Status != OrderStatusEnum.Completed
                && order.Status != OrderStatusEnum.Cancelled
                && order.Status != OrderStatusEnum.Rejected)
            {
                bool ok = await SalesDataService.ChangeOrderStatusAsync(id, OrderStatusEnum.Cancelled, CurrentEmployeeID);
                if (ok)
                    TempData["SuccessMessage"] = "Hủy đơn hàng thành công.";
                else
                    TempData["ErrorMessage"] = "Không thể hủy đơn hàng.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể hủy đơn hàng ở trạng thái hiện tại.";
            }
            return RedirectToAction("Details", new { id });
        }

        [HttpGet]
        public IActionResult Delete(int id) => View(id);

        [HttpPost]
        public async Task<IActionResult> Delete(int id, string _ = "")
        {
            var order = await SalesDataService.GetOrderAsync(id);
            if (order != null
                && (order.Status == OrderStatusEnum.New
                    || order.Status == OrderStatusEnum.Cancelled
                    || order.Status == OrderStatusEnum.Rejected))
            {
                bool ok = await SalesDataService.DeleteOrderAsync(id);
                if (ok)
                {
                    TempData["SuccessMessage"] = "Xóa đơn hàng thành công.";
                    return RedirectToAction("Index");
                }
                TempData["ErrorMessage"] = "Không thể xóa đơn hàng. Dữ liệu có thể đã thay đổi.";
                return RedirectToAction("Details", new { id });
            }
            TempData["ErrorMessage"] = "Chỉ có thể xóa đơn hàng ở trạng thái: Chờ duyệt / Đã hủy / Từ chối.";
            return RedirectToAction("Details", new { id });
        }
    }
}