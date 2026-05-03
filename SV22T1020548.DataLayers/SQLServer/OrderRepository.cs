using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020548.Models;
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

            return await connection.QueryFirstOrDefaultAsync<OrderViewInfoWithRejectNote>(sql, new { OrderID = orderID });
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
                        s.ShipperName, s.Phone AS ShipperPhone,
                        ISNULL((
                            SELECT SUM(od.Quantity * od.SalePrice)
                            FROM OrderDetails od
                            WHERE od.OrderID = o.OrderID
                        ), 0) AS TotalAmount
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
                Status = (int)(input.Status ?? 0),
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

        public async Task<PagedResult<OrderViewInfo>> ListByCustomerAsync(int customerID, int page, int pageSize, OrderStatusEnum? status = null)
        {
            using var connection = new SqlConnection(_connectionString);
            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 10 : pageSize;
            int offset = (page - 1) * pageSize;

            string condition = @"
                o.CustomerID = @CustomerID
                AND (@Status IS NULL OR o.Status = @Status)";

            string countSql = $@"
                SELECT COUNT(*)
                FROM Orders o
                WHERE {condition}";

            string dataSql = $@"
                SELECT  o.*,
                        c.CustomerName, c.ContactName AS CustomerContactName, c.Address AS CustomerAddress, c.Phone AS CustomerPhone, c.Email AS CustomerEmail,
                        e.FullName AS EmployeeName,
                        s.ShipperName, s.Phone AS ShipperPhone,
                        ISNULL((
                            SELECT SUM(od.Quantity * od.SalePrice)
                            FROM OrderDetails od
                            WHERE od.OrderID = o.OrderID
                        ), 0) AS TotalAmount
                FROM Orders o
                LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
                LEFT JOIN Employees e ON o.EmployeeID = e.EmployeeID
                LEFT JOIN Shippers s ON o.ShipperID = s.ShipperID
                WHERE {condition}
                ORDER BY o.OrderTime DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            var parameters = new
            {
                CustomerID = customerID,
                Status = status.HasValue ? (int?)status.Value : null,
                Offset = offset,
                PageSize = pageSize
            };

            int rowCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);
            var dataItems = await connection.QueryAsync<OrderViewInfo>(dataSql, parameters);

            return new PagedResult<OrderViewInfo>
            {
                Page = page,
                PageSize = pageSize,
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

        public async Task<int> InitOrderAsync(Order order, List<OrderDetail> details)
        {
            if (order == null || details == null || details.Count == 0)
                return 0;

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();
            try
            {
                string insertOrderSql = @"
                    INSERT INTO Orders (CustomerID, OrderTime, DeliveryProvince, DeliveryAddress, EmployeeID, AcceptTime, ShipperID, ShippedTime, FinishedTime, Status)
                    VALUES (@CustomerID, @OrderTime, @DeliveryProvince, @DeliveryAddress, @EmployeeID, @AcceptTime, @ShipperID, @ShippedTime, @FinishedTime, @Status);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                int orderID = await connection.ExecuteScalarAsync<int>(insertOrderSql, new
                {
                    order.CustomerID,
                    order.OrderTime,
                    order.DeliveryProvince,
                    order.DeliveryAddress,
                    order.EmployeeID,
                    order.AcceptTime,
                    order.ShipperID,
                    order.ShippedTime,
                    order.FinishedTime,
                    Status = (int)order.Status
                }, transaction);

                // B1: Lưu toàn bộ chi tiết đơn hàng.
                foreach (var item in details)
                {
                    if (item.Quantity <= 0 || item.SalePrice <= 0)
                        throw new InvalidOperationException("Chi tiết đơn hàng không hợp lệ.");

                    string detailSql = @"
                        INSERT INTO OrderDetails (OrderID, ProductID, Quantity, SalePrice)
                        VALUES (@OrderID, @ProductID, @Quantity, @SalePrice)";
                    int detailRows = await connection.ExecuteAsync(detailSql, new
                    {
                        OrderID = orderID,
                        item.ProductID,
                        item.Quantity,
                        item.SalePrice
                    }, transaction);

                    if (detailRows == 0)
                        throw new InvalidOperationException("Không thể lưu chi tiết đơn hàng.");
                }

                // B2: Trừ tồn kho sau khi lưu chi tiết, vẫn nằm trong cùng transaction.
                foreach (var item in details)
                {
                    string stockSql = @"
                        UPDATE Products
                        SET Quantity = Quantity - @Quantity
                        WHERE ProductID = @ProductID
                          AND Quantity >= @Quantity;";
                    int stockRows = await connection.ExecuteAsync(stockSql, new { item.ProductID, item.Quantity }, transaction);
                    if (stockRows == 0)
                        throw new InvalidOperationException("Tồn kho không đủ để lập đơn.");
                }

                transaction.Commit();
                return orderID;
            }
            catch
            {
                if (transaction.Connection != null)
                    transaction.Rollback();
                return 0;
            }
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderID, OrderStatusEnum newStatus, int? employeeID = null)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();
            try
            {
                var current = await connection.QueryFirstOrDefaultAsync<Order>(
                    "SELECT * FROM Orders WHERE OrderID = @OrderID",
                    new { OrderID = orderID }, transaction);

                if (current == null)
                {
                    transaction.Rollback();
                    return false;
                }

                if ((newStatus == OrderStatusEnum.Cancelled || newStatus == OrderStatusEnum.Rejected)
                    && current.Status != OrderStatusEnum.Cancelled
                    && current.Status != OrderStatusEnum.Rejected)
                {
                    var details = await connection.QueryAsync<OrderDetail>(
                        "SELECT OrderID, ProductID, Quantity, SalePrice FROM OrderDetails WHERE OrderID = @OrderID",
                        new { OrderID = orderID }, transaction);

                    foreach (var detail in details)
                    {
                        await connection.ExecuteAsync(
                            @"UPDATE Products
                              SET Quantity = Quantity + @Quantity
                              WHERE ProductID = @ProductID",
                            new { detail.ProductID, detail.Quantity }, transaction);
                    }
                }

                string updateSql = @"
                    UPDATE Orders
                    SET Status = @Status,
                        EmployeeID = CASE WHEN @EmployeeID IS NULL THEN EmployeeID ELSE @EmployeeID END,
                        AcceptTime = CASE WHEN @Status = @AcceptedStatus THEN ISNULL(AcceptTime, GETDATE()) ELSE AcceptTime END,
                        ShippedTime = CASE WHEN @Status = @ShippingStatus THEN ISNULL(ShippedTime, GETDATE()) ELSE ShippedTime END,
                        FinishedTime = CASE WHEN @Status = @CompletedStatus THEN ISNULL(FinishedTime, GETDATE()) ELSE FinishedTime END
                    WHERE OrderID = @OrderID";

                int rows = await connection.ExecuteAsync(updateSql, new
                {
                    OrderID = orderID,
                    Status = (int)newStatus,
                    EmployeeID = employeeID,
                    AcceptedStatus = (int)OrderStatusEnum.Accepted,
                    ShippingStatus = (int)OrderStatusEnum.Shipping,
                    CompletedStatus = (int)OrderStatusEnum.Completed
                }, transaction);

                transaction.Commit();
                return rows > 0;
            }
            catch
            {
                transaction.Rollback();
                return false;
            }
        }

        // Overload hỗ trợ RejectNote khi từ chối đơn hàng.
        public async Task<bool> UpdateOrderStatusAsync(
            int orderID,
            OrderStatusEnum newStatus,
            int? employeeID = null,
            string? rejectNote = null)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();
            try
            {
                var current = await connection.QueryFirstOrDefaultAsync<Order>(
                    "SELECT * FROM Orders WHERE OrderID = @OrderID",
                    new { OrderID = orderID }, transaction);

                if (current == null)
                {
                    transaction.Rollback();
                    return false;
                }

                if ((newStatus == OrderStatusEnum.Cancelled || newStatus == OrderStatusEnum.Rejected)
                    && current.Status != OrderStatusEnum.Cancelled
                    && current.Status != OrderStatusEnum.Rejected)
                {
                    var details = await connection.QueryAsync<OrderDetail>(
                        "SELECT OrderID, ProductID, Quantity, SalePrice FROM OrderDetails WHERE OrderID = @OrderID",
                        new { OrderID = orderID }, transaction);

                    foreach (var detail in details)
                    {
                        await connection.ExecuteAsync(
                            @"UPDATE Products
                              SET Quantity = Quantity + @Quantity
                              WHERE ProductID = @ProductID",
                            new { detail.ProductID, detail.Quantity }, transaction);
                    }
                }

                string updateSql = @"
                    UPDATE Orders
                    SET Status = @Status,
                        EmployeeID = CASE WHEN @EmployeeID IS NULL THEN EmployeeID ELSE @EmployeeID END,
                        RejectNote = CASE WHEN @Status = @RejectedStatus THEN @RejectNote ELSE NULL END,
                        AcceptTime = CASE WHEN @Status = @AcceptedStatus THEN ISNULL(AcceptTime, GETDATE()) ELSE AcceptTime END,
                        ShippedTime = CASE WHEN @Status = @ShippingStatus THEN ISNULL(ShippedTime, GETDATE()) ELSE ShippedTime END,
                        FinishedTime = CASE WHEN @Status = @CompletedStatus THEN ISNULL(FinishedTime, GETDATE()) ELSE FinishedTime END
                    WHERE OrderID = @OrderID";

                int rows = await connection.ExecuteAsync(updateSql, new
                {
                    OrderID = orderID,
                    Status = (int)newStatus,
                    EmployeeID = employeeID,
                    RejectedStatus = (int)OrderStatusEnum.Rejected,
                    RejectNote = string.IsNullOrWhiteSpace(rejectNote) ? null : rejectNote.Trim(),
                    AcceptedStatus = (int)OrderStatusEnum.Accepted,
                    ShippingStatus = (int)OrderStatusEnum.Shipping,
                    CompletedStatus = (int)OrderStatusEnum.Completed
                }, transaction);

                transaction.Commit();
                return rows > 0;
            }
            catch
            {
                transaction.Rollback();
                return false;
            }
        }

        // Khôi phục đơn về trạng thái New (đồng thời trừ kho lại) khi đơn đang Cancelled/Rejected.
        public async Task<bool> RestoreOrderAsync(int orderID, int? employeeID = null)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();
            try
            {
                var current = await connection.QueryFirstOrDefaultAsync<Order>(
                    "SELECT * FROM Orders WHERE OrderID = @OrderID",
                    new { OrderID = orderID }, transaction);

                if (current == null)
                {
                    transaction.Rollback();
                    return false;
                }

                if (current.Status != OrderStatusEnum.Cancelled &&
                    current.Status != OrderStatusEnum.Rejected)
                {
                    transaction.Rollback();
                    return false;
                }

                var details = await connection.QueryAsync<OrderDetail>(
                    "SELECT OrderID, ProductID, Quantity, SalePrice FROM OrderDetails WHERE OrderID = @OrderID",
                    new { OrderID = orderID }, transaction);

                string stockSql = @"
                    UPDATE Products
                    SET Quantity = Quantity - @Quantity
                    WHERE ProductID = @ProductID
                      AND Quantity >= @Quantity;";

                foreach (var detail in details)
                {
                    int stockRows = await connection.ExecuteAsync(
                        stockSql,
                        new { detail.ProductID, detail.Quantity },
                        transaction);

                    if (stockRows == 0)
                        throw new InvalidOperationException("Tồn kho không đủ để khôi phục đơn hàng.");
                }

                string updateSql = @"
                    UPDATE Orders
                    SET Status = @Status,
                        EmployeeID = CASE WHEN @EmployeeID IS NULL THEN EmployeeID ELSE @EmployeeID END,
                        AcceptTime = NULL,
                        ShippedTime = NULL,
                        FinishedTime = NULL,
                        RejectNote = NULL
                    WHERE OrderID = @OrderID";

                int rows = await connection.ExecuteAsync(updateSql, new
                {
                    OrderID = orderID,
                    Status = (int)OrderStatusEnum.New,
                    EmployeeID = employeeID
                }, transaction);

                transaction.Commit();
                return rows > 0;
            }
            catch
            {
                transaction.Rollback();
                return false;
            }
        }

        public async Task<int> CountAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Orders");
        }

        public async Task<decimal> GetTodayRevenueAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                SELECT ISNULL(SUM(od.Quantity * od.SalePrice), 0)
                FROM Orders o
                JOIN OrderDetails od ON o.OrderID = od.OrderID
                WHERE o.Status = @CompletedStatus
                  AND CAST(o.FinishedTime AS DATE) = CAST(GETDATE() AS DATE)";
            return await connection.ExecuteScalarAsync<decimal>(sql, new { CompletedStatus = (int)OrderStatusEnum.Completed });
        }

        public async Task<List<PendingOrderItem>> ListPendingOrdersAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                SELECT TOP 10
                       o.OrderID,
                       ISNULL(c.CustomerName, N'Khách vãng lai') AS CustomerName,
                       o.OrderTime,
                       ISNULL(SUM(od.Quantity * od.SalePrice), 0) AS TotalAmount,
                       o.Status
                FROM Orders o
                LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
                LEFT JOIN OrderDetails od ON o.OrderID = od.OrderID
                WHERE o.Status IN (@NewStatus, @ShippingStatus)
                GROUP BY o.OrderID, c.CustomerName, o.OrderTime, o.Status
                ORDER BY o.OrderTime DESC";

            var data = await connection.QueryAsync<PendingOrderItem>(sql, new
            {
                NewStatus = (int)OrderStatusEnum.New,
                ShippingStatus = (int)OrderStatusEnum.Shipping
            });
            return data.ToList();
        }

        public async Task<List<TopProductItem>> ListTopProductsAsync(int top = 5)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                SELECT TOP (@Top)
                       od.ProductID,
                       p.ProductName,
                       SUM(od.Quantity) AS SoldQuantity
                FROM OrderDetails od
                JOIN Orders o ON od.OrderID = o.OrderID
                JOIN Products p ON od.ProductID = p.ProductID
                WHERE o.Status = @CompletedStatus
                GROUP BY od.ProductID, p.ProductName
                ORDER BY SUM(od.Quantity) DESC, p.ProductName";

            var data = await connection.QueryAsync<TopProductItem>(sql, new
            {
                Top = top <= 0 ? 5 : top,
                CompletedStatus = (int)OrderStatusEnum.Completed
            });
            return data.ToList();
        }

        public async Task<List<decimal>> ListRevenueByMonthsAsync(int year)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                SELECT MONTH(o.FinishedTime) AS [Month],
                       ISNULL(SUM(od.Quantity * od.SalePrice), 0) AS Revenue
                FROM Orders o
                JOIN OrderDetails od ON o.OrderID = od.OrderID
                WHERE o.Status = @CompletedStatus
                  AND YEAR(o.FinishedTime) = @Year
                GROUP BY MONTH(o.FinishedTime)
                ORDER BY [Month]";

            var rows = await connection.QueryAsync<(int Month, decimal Revenue)>(sql, new
            {
                CompletedStatus = (int)OrderStatusEnum.Completed,
                Year = year
            });

            var result = Enumerable.Repeat(0m, 12).ToList();
            foreach (var row in rows)
            {
                if (row.Month >= 1 && row.Month <= 12)
                    result[row.Month - 1] = row.Revenue;
            }
            return result;
        }
    }
}