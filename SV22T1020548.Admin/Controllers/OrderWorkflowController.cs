using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020548.BusinessLayers;
using SV22T1020548.Models.HR;
using SV22T1020548.Models.Sales;
using System;
using System.Threading.Tasks;

namespace SV22T1020548.Admin.Controllers
{
    /// <summary>
    /// Controller tách riêng để xử lý các tác vụ "nghiệp vụ" cần form extra (ví dụ: lý do từ chối, khôi phục đơn).
    /// Việc tách giúp không phá các action cũ trong <see cref="OrderController"/>.
    /// </summary>
    [Authorize]
    public class OrderWorkflowController : Controller
    {
        private int CurrentEmployeeID =>
            int.TryParse(User.FindFirst("UserId")?.Value ?? "0", out int id) ? id : 0;

        private IActionResult RedirectToOrderDetails(int id)
            => RedirectToAction("Details", "Order", new { id });

        [HttpGet]
        public IActionResult RejectWithNote(int id)
            => View("~/Views/Order/RejectWithNote.cshtml", id);

        [HttpPost]
        public async Task<IActionResult> RejectWithNote(int id, string rejectNote = "")
        {
            rejectNote = rejectNote?.Trim() ?? "";
            var order = await SalesDataService.GetOrderAsync(id);

            if (order == null || order.Status != OrderStatusEnum.New)
            {
                TempData["ErrorMessage"] = "Chỉ có thể từ chối đơn hàng ở trạng thái Chờ duyệt.";
                return RedirectToOrderDetails(id);
            }

            if (string.IsNullOrWhiteSpace(rejectNote))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập lý do từ chối đơn hàng.";
                return RedirectToOrderDetails(id);
            }

            bool ok = await SalesDataService.ChangeOrderStatusAsync(
                id,
                OrderStatusEnum.Rejected,
                CurrentEmployeeID,
                rejectNote);

            TempData[ok ? "SuccessMessage" : "ErrorMessage"] =
                ok ? "Từ chối đơn hàng thành công." : "Không thể từ chối đơn hàng.";

            return RedirectToOrderDetails(id);
        }

        [HttpGet]
        public IActionResult Restore(int id)
            => View("~/Views/Order/Restore.cshtml", id);

        [HttpPost]
        public async Task<IActionResult> Restore(int id, string _ = "")
        {
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("Index", "Order");
            }

            if (order.Status != OrderStatusEnum.Cancelled && order.Status != OrderStatusEnum.Rejected)
            {
                TempData["ErrorMessage"] = "Chỉ có thể khôi phục đơn hàng đã hủy/từ chối.";
                return RedirectToOrderDetails(id);
            }

            var employee = await HRDataService.GetEmployeeAsync(CurrentEmployeeID);
            if (employee?.IsWorking != true)
            {
                TempData["ErrorMessage"] = "Nhân viên hiện tại đã nghỉ việc nên không thể khôi phục đơn.";
                return RedirectToOrderDetails(id);
            }

            bool ok = await SalesDataService.RestoreOrderAsync(id, CurrentEmployeeID);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] =
                ok ? "Khôi phục đơn hàng thành công." : "Không thể khôi phục đơn hàng. Vui lòng thử lại.";

            return RedirectToOrderDetails(id);
        }
    }
}

