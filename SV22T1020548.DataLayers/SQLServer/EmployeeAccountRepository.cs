using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020548.DataLayers.Interfaces;
using SV22T1020548.Models.Security;

namespace SV22T1020548.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý tài khoản đăng nhập cho Nhân viên (dùng bảng Employees)
    /// </summary>
    public class EmployeeAccountRepository : IUserAccountRepository
    {
        private readonly string _connectionString;

        public EmployeeAccountRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<UserAccount?> Authorize(string userName, string password)
        {
            using var connection = new SqlConnection(_connectionString);
            // Kiểm tra Email và Password trong bảng Employees. Yêu cầu IsWorking = 1 (đang làm việc)
            string sql = @"
                SELECT  EmployeeID AS UserId,
                        Email AS UserName,
                        FullName AS DisplayName,
                        Email,
                        Photo,
                        RoleNames
                FROM    Employees
                WHERE   Email = @Email AND Password = @Password AND IsWorking = 1";

            var parameters = new
            {
                Email = userName,
                Password = password
            };

            // Dapper sẽ tự động map các cột được Select vào các Property tương ứng của đối tượng UserAccount
            return await connection.QueryFirstOrDefaultAsync<UserAccount>(sql, parameters);
        }

        public async Task<bool> ChangePassword(string userName, string password)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                UPDATE  Employees 
                SET     Password = @Password 
                WHERE   Email = @Email";

            var parameters = new
            {
                Email = userName,
                Password = password
            };

            return await connection.ExecuteAsync(sql, parameters) > 0;
        }
    }
}