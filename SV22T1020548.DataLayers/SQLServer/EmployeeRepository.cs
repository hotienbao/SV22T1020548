using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020548.DataLayers.Interfaces;
using SV22T1020548.Models.Common;
using SV22T1020548.Models.HR;

namespace SV22T1020548.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu cho Nhân viên trên SQL Server
    /// </summary>
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly string _connectionString;

        public EmployeeRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<int> AddAsync(Employee data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                INSERT INTO Employees (FullName, BirthDate, Address, Phone, Email, Photo, IsWorking)
                VALUES (@FullName, @BirthDate, @Address, @Phone, @Email, @Photo, @IsWorking);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var parameters = new
            {
                data.FullName,
                data.BirthDate,
                data.Address,
                data.Phone,
                data.Email,
                data.Photo,
                data.IsWorking
            };

            return await connection.ExecuteScalarAsync<int>(sql, parameters);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = "DELETE FROM Employees WHERE EmployeeID = @EmployeeID";
            return await connection.ExecuteAsync(sql, new { EmployeeID = id }) > 0;
        }

        public async Task<Employee?> GetAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = "SELECT * FROM Employees WHERE EmployeeID = @EmployeeID";
            return await connection.QueryFirstOrDefaultAsync<Employee>(sql, new { EmployeeID = id });
        }

        public async Task<bool> IsUsed(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            // Nhân viên không được xóa nếu đã từng phụ trách/duyệt một đơn hàng nào đó
            string sql = @"
                IF EXISTS (SELECT 1 FROM Orders WHERE EmployeeID = @EmployeeID)
                    SELECT 1;
                ELSE
                    SELECT 0;";

            return await connection.ExecuteScalarAsync<bool>(sql, new { EmployeeID = id });
        }

        public async Task<PagedResult<Employee>> ListAsync(PaginationSearchInput input)
        {
            using var connection = new SqlConnection(_connectionString);
            string searchValue = $"%{input.SearchValue}%";

            string condition = @"
                (@SearchValue = N'%%') OR 
                (FullName LIKE @SearchValue) OR 
                (Email LIKE @SearchValue) OR 
                (Phone LIKE @SearchValue)";

            string countSql = $"SELECT COUNT(*) FROM Employees WHERE {condition}";
            string dataSql = $@"
                SELECT * FROM Employees 
                WHERE {condition} 
                ORDER BY FullName";

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
            var dataItems = await connection.QueryAsync<Employee>(dataSql, parameters);

            return new PagedResult<Employee>
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = rowCount,
                DataItems = dataItems.ToList()
            };
        }

        public async Task<bool> UpdateAsync(Employee data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                UPDATE Employees 
                SET FullName = @FullName,
                    BirthDate = @BirthDate,
                    Address = @Address,
                    Phone = @Phone,
                    Email = @Email,
                    Photo = @Photo,
                    IsWorking = @IsWorking
                WHERE EmployeeID = @EmployeeID";

            return await connection.ExecuteAsync(sql, data) > 0;
        }

        public async Task<bool> ValidateEmailAsync(string email, int id = 0)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                SELECT COUNT(*) 
                FROM Employees 
                WHERE Email = @Email AND EmployeeID <> @EmployeeID";

            int count = await connection.ExecuteScalarAsync<int>(sql, new { Email = email, EmployeeID = id });
            return count == 0;
        }
    }
}