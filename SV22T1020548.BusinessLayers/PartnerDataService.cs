using SV22T1020548.DataLayers;
using SV22T1020548.DataLayers.Interfaces;
using SV22T1020548.DataLayers.SQLServer;
using SV22T1020548.Models.Common;
using SV22T1020548.Models.HR;
using SV22T1020548.Models.Partner;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SV22T1020548.BusinessLayers
{
    /// <summary>
    /// Lớp cung cấp các chức năng tác nghiệp liên quan đến dữ liệu của các đối tác của hệ thống,
    /// bao gồm: Customer, Supplier, Shipper, Employee 
    /// </summary>
    public static class PartnerDataService
    {
        private static readonly IGenericRepository<Supplier> supplierDB;
        private static readonly IGenericRepository<Shipper> shipperDB;
        private static readonly ICustomerRepository customerDB;

        // Bổ sung khai báo cho Employee
        private static readonly IGenericRepository<Employee> employeeDB;

        /// <summary>
        /// Constructor
        /// </summary>
        static PartnerDataService()
        {
            supplierDB = new SupplierRepository(Configuration.ConnectionString);
            shipperDB = new ShipperRepository(Configuration.ConnectionString);
            customerDB = new CustomerRepository(Configuration.ConnectionString);

            // Bổ sung khởi tạo cho Employee
            employeeDB = new EmployeeRepository(Configuration.ConnectionString);
        }

        //=====================================================================
        // NHÀ CUNG CẤP (SUPPLIER)
        //=====================================================================

        /// <summary>
        /// Lấy danh sách nhà cung cấp theo phân trang và tìm kiếm
        /// </summary>
        public static async Task<PagedResult<Supplier>> ListSuppliersAsync(PaginationSearchInput input)
        {
            return await supplierDB.ListAsync(input);
        }

        /// <summary>
        /// Lấy thông tin một nhà cung cấp theo mã
        /// </summary>
        public static async Task<Supplier?> GetSupplierAsync(int supplierID)
        {
            return await supplierDB.GetAsync(supplierID);
        }

        /// <summary>
        /// Bổ sung nhà cung cấp
        /// </summary>
        public static async Task<int> AddSupplierAsync(Supplier supplier)
        {
            return await supplierDB.AddAsync(supplier);
        }

        /// <summary>
        /// Cập nhật thông tin nhà cung cấp
        /// </summary>
        public static async Task<bool> UpdateSupplierAsync(Supplier supplier)
        {
            return await supplierDB.UpdateAsync(supplier);
        }

        /// <summary>
        /// Xóa một nhà cung cấp theo mã
        /// </summary>
        public static async Task<bool> DeleteSupplierAsync(int supplierID)
        {
            return await supplierDB.DeleteAsync(supplierID);
        }

        /// <summary>
        /// Kiểm tra xem nhà cung cấp có đang được tham chiếu bởi dữ liệu khác không
        /// </summary>
        public static async Task<bool> IsSupplierUsedAsync(int supplierID)
        {
            // Tùy theo Interface của bạn đặt tên là IsUsed hay IsUsedAsync
            return await supplierDB.IsUsed(supplierID);
        }

        //=====================================================================
        // NGƯỜI GIAO HÀNG (SHIPPER)
        //=====================================================================

        /// <summary>
        /// Lấy danh sách shipper theo phân trang và tìm kiếm
        /// </summary>
        public static async Task<PagedResult<Shipper>> ListShippersAsync(PaginationSearchInput input)
        {
            return await shipperDB.ListAsync(input);
        }

        /// <summary>
        /// Lấy thông tin một shipper theo mã
        /// </summary>
        public static async Task<Shipper?> GetShipperAsync(int shipperID)
        {
            return await shipperDB.GetAsync(shipperID);
        }

        /// <summary>
        /// Bổ sung shipper
        /// </summary>
        public static async Task<int> AddShipperAsync(Shipper shipper)
        {
            return await shipperDB.AddAsync(shipper);
        }

        /// <summary>
        /// Cập nhật shipper
        /// </summary>
        public static async Task<bool> UpdateShipperAsync(Shipper shipper)
        {
            return await shipperDB.UpdateAsync(shipper);
        }

        /// <summary>
        /// Xóa một shipper theo mã
        /// </summary>
        public static async Task<bool> DeleteShipperAsync(int shipperID)
        {
            return await shipperDB.DeleteAsync(shipperID);
        }

        /// <summary>
        /// Kiểm tra xem shipper có đang được tham chiếu bởi dữ liệu khác không
        /// </summary>
        public static async Task<bool> IsShipperUsedAsync(int shipperID)
        {
            return await shipperDB.IsUsed(shipperID);
        }

        //=====================================================================
        // KHÁCH HÀNG (CUSTOMER)
        //=====================================================================

        /// <summary>
        /// Lấy danh sách khách hàng theo phân trang và tìm kiếm
        /// </summary>
        public static async Task<PagedResult<Customer>> ListCustomersAsync(PaginationSearchInput input)
        {
            return await customerDB.ListAsync(input);
        }

        /// <summary>
        /// Lấy thông tin một khách hàng theo mã
        /// </summary>
        public static async Task<Customer?> GetCustomerAsync(int customerID)
        {
            return await customerDB.GetAsync(customerID);
        }

        /// <summary>
        /// Bổ sung khách hàng
        /// </summary>
        public static async Task<int> AddCustomerAsync(Customer customer)
        {
            return await customerDB.AddAsync(customer);
        }

        /// <summary>
        /// Cập nhật khách hàng
        /// </summary>
        public static async Task<bool> UpdateCustomerAsync(Customer customer)
        {
            return await customerDB.UpdateAsync(customer);
        }

        /// <summary>
        /// Xóa một khách hàng theo mã
        /// </summary>
        public static async Task<bool> DeleteCustomerAsync(int customerID)
        {
            return await customerDB.DeleteAsync(customerID);
        }

        /// <summary>
        /// Kiểm tra xem khách hàng có đang được tham chiếu bởi dữ liệu khác không
        /// </summary>
        public static async Task<bool> IsCustomerUsedAsync(int customerID)
        {
            return await customerDB.IsUsed(customerID);
        }

        /// <summary>
        /// Kiểm tra tính hợp lệ của email theo quy tắc nghiệp vụ cho Customer.
        /// </summary>
        public static async Task<bool> ValidateCustomerEmailAsync(string email, int id = 0)
        {
            return await customerDB.ValidateEmailAsync(email, id);
        }

        /// <summary>
        /// Kiểm tra xem email của khách hàng đã được sử dụng bởi bản ghi khác hay chưa.
        /// </summary>
        public static async Task<bool> IsCustomerEmailInUseAsync(string email, int id = 0)
        {
            bool isValid = await customerDB.ValidateEmailAsync(email, id);
            return !isValid;
        }

        //=====================================================================
        // NHÂN VIÊN (EMPLOYEE)
        //=====================================================================

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
        /// Bổ sung nhân viên
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
            // Tùy theo Interface của bạn đặt tên là IsUsed hay IsUsedAsync
            return await employeeDB.IsUsed(employeeID);
        }

        //=====================================================================
        // NOTE:
        // - Các phương thức trên chỉ là lớp bọc (facade) gọi tiếp xuống repository.
        // - Nếu cần thêm logic nghiệp vụ (validation, mapping, transaction...), hãy bổ sung tại đây.
        //=====================================================================
    }
}