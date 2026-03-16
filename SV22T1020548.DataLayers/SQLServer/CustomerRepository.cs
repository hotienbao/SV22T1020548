using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020548.DataLayers.Interfaces;
using SV22T1020548.Models.Common;
using SV22T1020548.Models.Partner;

namespace SV22T1020548.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu cho khách hàng (Customer) trên SQL Server
    /// </summary>
    public class CustomerRepository : ICustomerRepository
    {
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo repository với chuỗi kết nối
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối đến CSDL SQL Server</param>
        public CustomerRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Thêm mới một khách hàng vào CSDL
        /// </summary>
        /// <param name="data">Thông tin khách hàng cần thêm</param>
        /// <returns>Mã khách hàng vừa được tạo (CustomerID)</returns>
        public async Task<int> AddAsync(Customer data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                INSERT INTO Customers (CustomerName, ContactName, Province, Address, Phone, Email, IsLocked)
                VALUES (@CustomerName, @ContactName, @Province, @Address, @Phone, @Email, @IsLocked);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var parameters = new
            {
                data.CustomerName,
                data.ContactName,
                data.Province,
                data.Address,
                data.Phone,
                data.Email,
                data.IsLocked
            };

            return await connection.ExecuteScalarAsync<int>(sql, parameters);
        }

        /// <summary>
        /// Xóa một khách hàng theo mã (ID)
        /// </summary>
        /// <param name="id">Mã khách hàng cần xóa</param>
        /// <returns>True nếu xóa thành công, ngược lại False</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = "DELETE FROM Customers WHERE CustomerID = @CustomerID";

            var parameters = new { CustomerID = id };

            int rowsAffected = await connection.ExecuteAsync(sql, parameters);
            return rowsAffected > 0;
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một khách hàng theo mã (ID)
        /// </summary>
        /// <param name="id">Mã khách hàng</param>
        /// <returns>Đối tượng Customer, trả về null nếu không tìm thấy</returns>
        public async Task<Customer?> GetAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = "SELECT * FROM Customers WHERE CustomerID = @CustomerID";

            var parameters = new { CustomerID = id };

            return await connection.QueryFirstOrDefaultAsync<Customer>(sql, parameters);
        }

        /// <summary>
        /// Kiểm tra xem khách hàng có đang được sử dụng hay không 
        /// (Kiểm tra xem khách hàng này đã có đơn hàng nào chưa)
        /// </summary>
        /// <param name="id">Mã khách hàng cần kiểm tra</param>
        /// <returns>True nếu đang có đơn hàng (không được xóa), ngược lại False</returns>
        public async Task<bool> IsUsed(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            // Bảng Orders có khóa ngoại CustomerID tham chiếu đến Customers
            string sql = @"
                IF EXISTS (SELECT 1 FROM Orders WHERE CustomerID = @CustomerID)
                    SELECT 1;
                ELSE
                    SELECT 0;";

            var parameters = new { CustomerID = id };

            return await connection.ExecuteScalarAsync<bool>(sql, parameters);
        }

        /// <summary>
        /// Tìm kiếm và lấy danh sách khách hàng dưới dạng phân trang
        /// </summary>
        /// <param name="input">Thông tin tìm kiếm và phân trang</param>
        /// <returns>Đối tượng PagedResult chứa danh sách dữ liệu và thông tin trang</returns>
        public async Task<PagedResult<Customer>> ListAsync(PaginationSearchInput input)
        {
            using var connection = new SqlConnection(_connectionString);
            string searchValue = $"%{input.SearchValue}%";

            // 1. Câu lệnh đếm tổng số dòng thỏa mãn điều kiện tìm kiếm
            string countSql = @"
                SELECT COUNT(*) 
                FROM Customers 
                WHERE (@SearchValue = N'%%') 
                   OR (CustomerName LIKE @SearchValue) 
                   OR (ContactName LIKE @SearchValue)
                   OR (Phone LIKE @SearchValue)
                   OR (Email LIKE @SearchValue)";

            // 2. Câu lệnh lấy dữ liệu có phân trang
            string dataSql = @"
                SELECT * FROM Customers 
                WHERE (@SearchValue = N'%%') 
                   OR (CustomerName LIKE @SearchValue) 
                   OR (ContactName LIKE @SearchValue)
                   OR (Phone LIKE @SearchValue)
                   OR (Email LIKE @SearchValue)
                ORDER BY CustomerName";

            if (input.PageSize > 0)
            {
                dataSql += " OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
            }

            var parameters = new
            {
                SearchValue = searchValue,
                Offset = input.Offset,
                PageSize = input.PageSize
            };

            int rowCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);
            var dataItems = await connection.QueryAsync<Customer>(dataSql, parameters);

            return new PagedResult<Customer>
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = rowCount,
                DataItems = dataItems.ToList()
            };
        }

        /// <summary>
        /// Cập nhật thông tin của một khách hàng
        /// </summary>
        /// <param name="data">Dữ liệu khách hàng đã chỉnh sửa</param>
        /// <returns>True nếu cập nhật thành công, ngược lại False</returns>
        public async Task<bool> UpdateAsync(Customer data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                UPDATE Customers 
                SET CustomerName = @CustomerName,
                    ContactName = @ContactName,
                    Province = @Province,
                    Address = @Address,
                    Phone = @Phone,
                    Email = @Email,
                    IsLocked = @IsLocked
                WHERE CustomerID = @CustomerID";

            var parameters = new
            {
                data.CustomerName,
                data.ContactName,
                data.Province,
                data.Address,
                data.Phone,
                data.Email,
                data.IsLocked,
                data.CustomerID
            };

            int rowsAffected = await connection.ExecuteAsync(sql, parameters);
            return rowsAffected > 0;
        }

        /// <summary>
        /// Kiểm tra xem địa chỉ email đã tồn tại trong CSDL hay chưa (tránh trùng lặp)
        /// </summary>
        /// <param name="email">Email cần kiểm tra</param>
        /// <param name="id">Mã khách hàng (0 nếu thêm mới, khác 0 nếu cập nhật)</param>
        /// <returns>True nếu email hợp lệ (không bị trùng), False nếu email đã được sử dụng</returns>
        public async Task<bool> ValidateEmailAsync(string email, int id = 0)
        {
            using var connection = new SqlConnection(_connectionString);

            // Đếm số lượng khách hàng có cùng Email nhưng khác CustomerID hiện tại
            string sql = @"
                SELECT COUNT(*) 
                FROM Customers 
                WHERE Email = @Email AND CustomerID <> @CustomerID";

            var parameters = new
            {
                Email = email,
                CustomerID = id
            };

            int count = await connection.ExecuteScalarAsync<int>(sql, parameters);

            // Nếu count == 0 tức là không bị trùng -> hợp lệ (true)
            return count == 0;
        }
    }
}