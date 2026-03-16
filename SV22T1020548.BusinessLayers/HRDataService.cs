using SV22T1020548.DataLayers;
using SV22T1020548.DataLayers.Interfaces;
using SV22T1020548.DataLayers.SQLServer;
using SV22T1020548.Models.Common;
using SV22T1020548.Models.HR;
using System.Threading.Tasks;

namespace SV22T1020548.BusinessLayers
{
    /// <summary>
    /// Lớp cung cấp các chức năng tác nghiệp liên quan đến quản lý nhân sự (Human Resources)
    /// bao gồm: Nhân viên (Employee)
    /// </summary>
    public static class HRDataService
    {
        private static readonly IGenericRepository<Employee> employeeDB;

        /// <summary>
        /// Constructor
        /// </summary>
        static HRDataService()
        {
            employeeDB = new EmployeeRepository(Configuration.ConnectionString);
        }

        /// <summary>
        /// Lấy danh sách nhân viên theo phân trang và tìm kiếm
        /// </summary>
        public static async Task<PagedResult<Employee>> ListEmployeesAsync(PaginationSearchInput input)
        {
            return await employeeDB.ListAsync(input);
        }

        /// <summary>
        /// Lấy thông tin một nhân viên theo mã
        /// </summary>
        public static async Task<Employee?> GetEmployeeAsync(int employeeID)
        {
            return await employeeDB.GetAsync(employeeID);
        }

        /// <summary>
        /// Bổ sung nhân viên mới
        /// </summary>
        public static async Task<int> AddEmployeeAsync(Employee employee)
        {
            return await employeeDB.AddAsync(employee);
        }

        /// <summary>
        /// Cập nhật thông tin nhân viên
        /// </summary>
        public static async Task<bool> UpdateEmployeeAsync(Employee employee)
        {
            return await employeeDB.UpdateAsync(employee);
        }

        /// <summary>
        /// Xóa một nhân viên theo mã
        /// </summary>
        public static async Task<bool> DeleteEmployeeAsync(int employeeID)
        {
            return await employeeDB.DeleteAsync(employeeID);
        }

        /// <summary>
        /// Kiểm tra xem nhân viên có đang được tham chiếu bởi dữ liệu khác không
        /// </summary>
        public static async Task<bool> IsEmployeeUsedAsync(int employeeID)
        {
            return await employeeDB.IsUsed(employeeID);
        }
    }
}