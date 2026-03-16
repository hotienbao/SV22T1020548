using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020548.DataLayers.Interfaces;
using SV22T1020548.Models.Security;
// using SV22T1020548.Models; // Nhớ bỏ comment dòng này và trỏ đúng đến namespace chứa class UserAccount

namespace SV22T1020548.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các chức năng liên quan đến tài khoản của Khách hàng (Customer)
    /// </summary>
    public class CustomerAccountRepository : IUserAccountRepository
    {
        private readonly string _connectionString;

        public CustomerAccountRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Kiểm tra thông tin đăng nhập của Khách hàng (Hàm bất đồng bộ)
        /// </summary>
        public async Task<UserAccount?> Authorize(string userName, string password)
        {
            using var connection = new SqlConnection(_connectionString);

            // Đối với khách hàng, UserName chính là Email.
            string sql = @"
                SELECT  CustomerID AS UserID, 
                        Email AS UserName, 
                        CustomerName AS FullName, 
                        Email AS Email, 
                        '' AS Photo, 
                        '' AS RoleNames
                FROM Customers 
                WHERE Email = @Email AND Password = @Password AND IsLocked = 0";

            var parameters = new
            {
                Email = userName,
                Password = password
            };

            // Dùng QueryFirstOrDefaultAsync thay vì QueryFirstOrDefault
            return await connection.QueryFirstOrDefaultAsync<UserAccount>(sql, parameters);
        }

        /// <summary>
        /// Đổi mật khẩu của Khách hàng (Hàm bất đồng bộ nhận 2 tham số)
        /// </summary>
        public async Task<bool> ChangePassword(string userName, string password)
        {
            using var connection = new SqlConnection(_connectionString);

            // Cập nhật mật khẩu mới cho tài khoản có Email tương ứng
            string sql = @"
                UPDATE Customers 
                SET Password = @Password 
                WHERE Email = @Email";

            var parameters = new
            {
                Email = userName,
                Password = password
            };

            // Dùng ExecuteAsync thay vì Execute
            int rowsAffected = await connection.ExecuteAsync(sql, parameters);
            return rowsAffected > 0;
        }
    }
}