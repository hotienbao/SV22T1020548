using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020548.DataLayers.Interfaces;
using SV22T1020548.Models.Common;
using SV22T1020548.Models.Sales;

namespace SV22T1020548.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu cho Đơn hàng (Order) và Chi tiết đơn hàng (OrderDetails) trên SQL Server
    /// </summary>
    public class OrderRepository : IOrderRepository
    {
        private readonly string _connectionString;

        public OrderRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        // ==========================================================
        // 1. QUẢN LÝ ĐƠN HÀNG (ORDERS)
        // ==========================================================

        public async Task<int> AddAsync(Order data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                INSERT INTO Orders (CustomerID, OrderTime, DeliveryProvince, DeliveryAddress, EmployeeID, AcceptTime, ShipperID, ShippedTime, FinishedTime, Status)
                VALUES (@CustomerID, @OrderTime, @DeliveryProvince, @DeliveryAddress, @EmployeeID, @AcceptTime, @ShipperID, @ShippedTime, @FinishedTime, @Status);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var parameters = new
            {
                data.CustomerID,
                data.OrderTime,
                data.DeliveryProvince,
                data.DeliveryAddress,
                data.EmployeeID,
                data.AcceptTime,
                data.ShipperID,
                data.ShippedTime,
                data.FinishedTime,
                Status = (int)data.Status // Lưu trạng thái dưới dạng số nguyên
            };

            return await connection.ExecuteScalarAsync<int>(sql, parameters);
        }

        public async Task<bool> DeleteAsync(int orderID)
        {
            using var connection = new SqlConnection(_connectionString);
            // Xóa chi tiết đơn hàng trước khi xóa đơn hàng để tránh lỗi khóa ngoại
            string sql = @"
                DELETE FROM OrderDetails WHERE OrderID = @OrderID;
                DELETE FROM Orders WHERE OrderID = @OrderID;";

            int rowsAffected = await connection.ExecuteAsync(sql, new { OrderID = orderID });
            return rowsAffected > 0;
        }

        public async Task<OrderViewInfo?> GetAsync(int orderID)
        {
            using var connection = new SqlConnection(_connectionString);
            // Lấy thông tin đơn hàng kèm theo thông tin Khách hàng, Nhân viên, Người giao hàng
            string sql = @"
                SELECT  o.*, 
                        c.CustomerName, c.ContactName AS CustomerContactName, c.Address AS CustomerAddress, c.Phone AS CustomerPhone, c.Email AS CustomerEmail,
                        e.FullName AS EmployeeName,
                        s.ShipperName, s.Phone AS ShipperPhone
                FROM    Orders o
                        LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
                        LEFT JOIN Employees e ON o.EmployeeID = e.EmployeeID
                        LEFT JOIN Shippers s ON o.ShipperID = s.ShipperID
                WHERE   o.OrderID = @OrderID";

            return await connection.QueryFirstOrDefaultAsync<OrderViewInfo>(sql, new { OrderID = orderID });
        }

        public async Task<PagedResult<OrderViewInfo>> ListAsync(OrderSearchInput input)
        {
            using var connection = new SqlConnection(_connectionString);
            string searchValue = $"%{input.SearchValue}%";

            // Xử lý điều kiện tìm kiếm, trạng thái và khoảng thời gian
            string condition = @"
                (@SearchValue = N'%%' OR c.CustomerName LIKE @SearchValue OR e.FullName LIKE @SearchValue OR s.ShipperName LIKE @SearchValue)
                AND (@Status = 0 OR o.Status = @Status)
                AND (@DateFrom IS NULL OR o.OrderTime >= @DateFrom)
                AND (@DateTo IS NULL OR o.OrderTime <= @DateTo)";

            string countSql = $@"
                SELECT COUNT(*) 
                FROM Orders o
                     LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
                     LEFT JOIN Employees e ON o.EmployeeID = e.EmployeeID
                     LEFT JOIN Shippers s ON o.ShipperID = s.ShipperID
                WHERE {condition}";

            string dataSql = $@"
                SELECT  o.*, 
                        c.CustomerName, c.ContactName AS CustomerContactName, c.Address AS CustomerAddress, c.Phone AS CustomerPhone, c.Email AS CustomerEmail,
                        e.FullName AS EmployeeName,
                        s.ShipperName, s.Phone AS ShipperPhone
                FROM    Orders o
                        LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
                        LEFT JOIN Employees e ON o.EmployeeID = e.EmployeeID
                        LEFT JOIN Shippers s ON o.ShipperID = s.ShipperID
                WHERE   {condition}
                ORDER BY o.OrderTime DESC";

            if (input.PageSize > 0)
            {
                dataSql += " OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
            }

            // Gán giá trị tham số (Quy ước: Nếu input.Status = 0 thì coi như tìm tất cả trạng thái)
            var parameters = new
            {
                SearchValue = searchValue,
                Status = (int)input.Status,
                DateFrom = input.DateFrom,
                DateTo = input.DateTo, // Cẩn thận: Nếu DateTo chỉ có ngày (không có giờ), nên xử lý cộng thêm 23h:59m:59s ở tầng Controller
                Offset = input.Offset,
                PageSize = input.PageSize
            };

            int rowCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);
            var dataItems = await connection.QueryAsync<OrderViewInfo>(dataSql, parameters);

            return new PagedResult<OrderViewInfo>
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = rowCount,
                DataItems = dataItems.ToList()
            };
        }

        public async Task<bool> UpdateAsync(Order data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                UPDATE Orders 
                SET CustomerID = @CustomerID,
                    OrderTime = @OrderTime,
                    DeliveryProvince = @DeliveryProvince,
                    DeliveryAddress = @DeliveryAddress,
                    EmployeeID = @EmployeeID,
                    AcceptTime = @AcceptTime,
                    ShipperID = @ShipperID,
                    ShippedTime = @ShippedTime,
                    FinishedTime = @FinishedTime,
                    Status = @Status
                WHERE OrderID = @OrderID";

            int rowsAffected = await connection.ExecuteAsync(sql, data);
            return rowsAffected > 0;
        }

        // ==========================================================
        // 2. QUẢN LÝ CHI TIẾT ĐƠN HÀNG (ORDER DETAILS)
        // ==========================================================

        public async Task<bool> AddDetailAsync(OrderDetail data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                INSERT INTO OrderDetails (OrderID, ProductID, Quantity, SalePrice)
                VALUES (@OrderID, @ProductID, @Quantity, @SalePrice)";

            return await connection.ExecuteAsync(sql, data) > 0;
        }

        public async Task<bool> DeleteDetailAsync(int orderID, int productID)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = "DELETE FROM OrderDetails WHERE OrderID = @OrderID AND ProductID = @ProductID";

            return await connection.ExecuteAsync(sql, new { OrderID = orderID, ProductID = productID }) > 0;
        }

        public async Task<OrderDetailViewInfo?> GetDetailAsync(int orderID, int productID)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                SELECT  od.*, p.ProductName, p.Unit, p.Photo
                FROM    OrderDetails od
                        JOIN Products p ON od.ProductID = p.ProductID
                WHERE   od.OrderID = @OrderID AND od.ProductID = @ProductID";

            return await connection.QueryFirstOrDefaultAsync<OrderDetailViewInfo>(sql, new { OrderID = orderID, ProductID = productID });
        }

        public async Task<List<OrderDetailViewInfo>> ListDetailsAsync(int orderID)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                SELECT  od.*, p.ProductName, p.Unit, p.Photo
                FROM    OrderDetails od
                        JOIN Products p ON od.ProductID = p.ProductID
                WHERE   od.OrderID = @OrderID";

            var result = await connection.QueryAsync<OrderDetailViewInfo>(sql, new { OrderID = orderID });
            return result.ToList();
        }

        public async Task<bool> UpdateDetailAsync(OrderDetail data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                UPDATE OrderDetails 
                SET Quantity = @Quantity,
                    SalePrice = @SalePrice
                WHERE OrderID = @OrderID AND ProductID = @ProductID";

            return await connection.ExecuteAsync(sql, data) > 0;
        }
    }
}