using SV22T1020548.DataLayers;
using SV22T1020548.DataLayers.Interfaces;
using SV22T1020548.DataLayers.SQLServer;
using SV22T1020548.Models.Common;
using SV22T1020548.Models.Sales;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SV22T1020548.BusinessLayers
{
    /// <summary>
    /// Lớp cung cấp các chức năng tác nghiệp liên quan đến quản lý bán hàng
    /// bao gồm: Đơn hàng (Order) và Chi tiết đơn hàng (OrderDetail)
    /// </summary>
    public static class SalesDataService
    {
        private static readonly IOrderRepository orderDB;

        /// <summary>
        /// Constructor
        /// </summary>
        static SalesDataService()
        {
            orderDB = new OrderRepository(Configuration.ConnectionString);
        }

        //=====================================================================
        // ĐƠN HÀNG (ORDER)
        //=====================================================================

        /// <summary>
        /// Lấy danh sách đơn hàng có tìm kiếm và phân trang
        /// </summary>
        /// <param name="input">Thông tin tìm kiếm và phân trang</param>
        /// <returns>PagedResult chứa danh sách OrderViewInfo</returns>
        public static async Task<PagedResult<OrderViewInfo>> ListOrdersAsync(OrderSearchInput input)
        {
            return await orderDB.ListAsync(input);
        }

        /// <summary>
        /// Lấy thông tin một đơn hàng theo mã
        /// </summary>
        /// <param name="orderID">Mã đơn hàng</param>
        /// <returns>OrderViewInfo nếu tồn tại, ngược lại null</returns>
        public static async Task<OrderViewInfo?> GetOrderAsync(int orderID)
        {
            return await orderDB.GetAsync(orderID);
        }

        /// <summary>
        /// Khởi tạo một đơn hàng mới (Trạng thái mặc định là vừa tiếp nhận)
        /// </summary>
        /// <param name="order">Thông tin đơn hàng cần tạo</param>
        /// <returns>Mã đơn hàng được tạo</returns>
        public static async Task<int> InitOrderAsync(Order order)
        {
            return await orderDB.AddAsync(order);
        }

        /// <summary>
        /// Cập nhật đơn hàng (thường dùng để cập nhật trạng thái đơn hàng)
        /// </summary>
        /// <param name="order">Thông tin đơn hàng cần cập nhật</param>
        /// <returns>True nếu cập nhật thành công, ngược lại false</returns>
        public static async Task<bool> UpdateOrderAsync(Order order)
        {
            return await orderDB.UpdateAsync(order);
        }

        /// <summary>
        /// Xóa đơn hàng (Thường chỉ cho phép xóa khi đơn hàng ở trạng thái Init hoặc Cancelled)
        /// </summary>
        /// <param name="orderID">Mã đơn hàng cần xóa</param>
        /// <returns>True nếu xóa thành công, ngược lại false</returns>
        public static async Task<bool> DeleteOrderAsync(int orderID)
        {
            return await orderDB.DeleteAsync(orderID);
        }

        //=====================================================================
        // CHI TIẾT ĐƠN HÀNG (ORDER DETAILS)
        //=====================================================================

        /// <summary>
        /// Lấy danh sách chi tiết của một đơn hàng
        /// </summary>
        /// <param name="orderID">Mã đơn hàng</param>
        /// <returns>Danh sách OrderDetailViewInfo</returns>
        public static async Task<List<OrderDetailViewInfo>> ListOrderDetailsAsync(int orderID)
        {
            return await orderDB.ListDetailsAsync(orderID);
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một mặt hàng trong đơn hàng
        /// </summary>
        /// <param name="orderID">Mã đơn hàng</param>
        /// <param name="productID">Mã mặt hàng</param>
        /// <returns>OrderDetailViewInfo nếu tồn tại, ngược lại null</returns>
        public static async Task<OrderDetailViewInfo?> GetOrderDetailAsync(int orderID, int productID)
        {
            return await orderDB.GetDetailAsync(orderID, productID);
        }

        /// <summary>
        /// Thêm mới chi tiết đơn hàng (thêm mặt hàng vào đơn hàng)
        /// </summary>
        /// <param name="orderDetail">Thông tin chi tiết đơn hàng cần thêm</param>
        /// <returns>True nếu thêm thành công, ngược lại false</returns>
        public static async Task<bool> AddOrderDetailAsync(OrderDetail orderDetail)
        {
            return await orderDB.AddDetailAsync(orderDetail);
        }

        /// <summary>
        /// Cập nhật chi tiết đơn hàng (như số lượng, giá bán)
        /// </summary>
        /// <param name="orderDetail">Thông tin chi tiết đơn hàng cần cập nhật</param>
        /// <returns>True nếu cập nhật thành công, ngược lại false</returns>
        public static async Task<bool> UpdateOrderDetailAsync(OrderDetail orderDetail)
        {
            return await orderDB.UpdateDetailAsync(orderDetail);
        }

        /// <summary>
        /// Xóa một mặt hàng khỏi đơn hàng
        /// </summary>
        /// <param name="orderID">Mã đơn hàng</param>
        /// <param name="productID">Mã mặt hàng cần xóa</param>
        /// <returns>True nếu xóa thành công, ngược lại false</returns>
        public static async Task<bool> DeleteOrderDetailAsync(int orderID, int productID)
        {
            return await orderDB.DeleteDetailAsync(orderID, productID);
        }
    }
}