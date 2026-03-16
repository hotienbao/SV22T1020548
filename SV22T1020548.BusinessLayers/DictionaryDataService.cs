using SV22T1020548.DataLayers;
using SV22T1020548.DataLayers.Interfaces;
using SV22T1020548.DataLayers.SQLServer;
using SV22T1020548.Models.Common;
using SV22T1020548.Models.DataDictionary;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SV22T1020548.BusinessLayers
{
    /// <summary>
    /// Lớp cung cấp các chức năng liên quan đến dữ liệu từ điển/danh mục dùng chung
    /// (Ví dụ: Tỉnh thành, Quốc gia...)
    /// </summary>
    public static class DictionaryDataService
    {
        private static readonly IDataDictionaryRepository<Province> provinceDB;

        /// <summary>
        /// Constructor
        /// </summary>
        static DictionaryDataService()
        {
            provinceDB = new ProvinceRepository(Configuration.ConnectionString);
        }

        /// <summary>
        /// Lấy danh sách toàn bộ các Tỉnh/Thành phố
        /// </summary>
        /// <returns>Danh sách Tỉnh/Thành phố</returns>
        public static async Task<List<Province>> ListProvincesAsync()
        {
            return await provinceDB.ListAsync();
        }

        // (Bạn có thể bổ sung các hàm CRUD cho Tỉnh/Thành nếu hệ thống yêu cầu quản lý Tỉnh/Thành)
    }
}