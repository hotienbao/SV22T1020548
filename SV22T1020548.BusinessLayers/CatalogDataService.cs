using SV22T1020548.DataLayers;
using SV22T1020548.DataLayers.Interfaces;
using SV22T1020548.DataLayers.SQLServer;
using SV22T1020548.Models.Common;
using SV22T1020548.Models.Catalog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SV22T1020548.BusinessLayers
{
    /// <summary>
    /// Lớp cung cấp các chức năng tác nghiệp liên quan đến quản lý danh mục hàng hóa,
    /// bao gồm: Loại hàng (Category) và Mặt hàng (Product)
    /// </summary>
    public static class CatalogDataService
    {
        private static readonly IGenericRepository<Category> categoryDB;
        private static readonly IProductRepository productDB;

        /// <summary>
        /// Constructor
        /// </summary>
        static CatalogDataService()
        {
            categoryDB = new CategoryRepository(Configuration.ConnectionString);
            productDB = new ProductRepository(Configuration.ConnectionString);
        }

        //=====================================================================
        // LOẠI HÀNG (CATEGORY)
        //=====================================================================

        /// <summary>
        /// Lấy danh sách loại hàng theo phân trang và tìm kiếm
        /// </summary>
        public static async Task<PagedResult<Category>> ListCategoriesAsync(PaginationSearchInput input)
        {
            return await categoryDB.ListAsync(input);
        }

        /// <summary>
        /// Lấy thông tin một loại hàng theo mã
        /// </summary>
        public static async Task<Category?> GetCategoryAsync(int categoryID)
        {
            return await categoryDB.GetAsync(categoryID);
        }

        /// <summary>
        /// Bổ sung loại hàng
        /// </summary>
        public static async Task<int> AddCategoryAsync(Category category)
        {
            return await categoryDB.AddAsync(category);
        }

        /// <summary>
        /// Cập nhật loại hàng
        /// </summary>
        public static async Task<bool> UpdateCategoryAsync(Category category)
        {
            return await categoryDB.UpdateAsync(category);
        }

        /// <summary>
        /// Xóa loại hàng theo mã
        /// </summary>
        public static async Task<bool> DeleteCategoryAsync(int categoryID)
        {
            return await categoryDB.DeleteAsync(categoryID);
        }

        /// <summary>
        /// Kiểm tra xem loại hàng có đang được sử dụng không
        /// </summary>
        public static async Task<bool> IsCategoryUsedAsync(int categoryID)
        {
            return await categoryDB.IsUsed(categoryID);
        }

        //=====================================================================
        // MẶT HÀNG (PRODUCT)
        //=====================================================================

        /// <summary>
        /// Lấy danh sách mặt hàng (có tìm kiếm và phân trang)
        /// </summary>
        public static async Task<PagedResult<Product>> ListProductsAsync(ProductSearchInput input)
        {
            return await productDB.ListAsync(input);
        }

        /// <summary>
        /// Lấy thông tin mặt hàng theo mã
        /// </summary>
        public static async Task<Product?> GetProductAsync(int productID)
        {
            return await productDB.GetAsync(productID);
        }

        /// <summary>
        /// Thêm mới một mặt hàng
        /// </summary>
        public static async Task<int> AddProductAsync(Product product)
        {
            return await productDB.AddAsync(product);
        }

        /// <summary>
        /// Cập nhật thông tin mặt hàng
        /// </summary>
        public static async Task<bool> UpdateProductAsync(Product product)
        {
            return await productDB.UpdateAsync(product);
        }

        /// <summary>
        /// Xóa mặt hàng
        /// </summary>
        public static async Task<bool> DeleteProductAsync(int productID)
        {
            return await productDB.DeleteAsync(productID);
        }

        /// <summary>
        /// Kiểm tra xem mặt hàng có đang được sử dụng (có đơn hàng) hay không
        /// </summary>
        public static async Task<bool> IsProductUsedAsync(int productID)
        {
            return await productDB.IsUsedAsync(productID);
        }

        //=====================================================================
        // THUỘC TÍNH MẶT HÀNG (PRODUCT ATTRIBUTES)
        //=====================================================================

        /// <summary>
        /// Lấy danh sách thuộc tính của một mặt hàng
        /// </summary>
        public static async Task<List<ProductAttribute>> ListProductAttributesAsync(int productID)
        {
            return await productDB.ListAttributesAsync(productID);
        }

        /// <summary>
        /// Lấy thông tin một thuộc tính
        /// </summary>
        public static async Task<ProductAttribute?> GetProductAttributeAsync(long attributeID)
        {
            return await productDB.GetAttributeAsync(attributeID);
        }

        /// <summary>
        /// Thêm mới thuộc tính cho mặt hàng
        /// </summary>
        public static async Task<long> AddProductAttributeAsync(ProductAttribute attribute)
        {
            return await productDB.AddAttributeAsync(attribute);
        }

        /// <summary>
        /// Cập nhật thuộc tính mặt hàng
        /// </summary>
        public static async Task<bool> UpdateProductAttributeAsync(ProductAttribute attribute)
        {
            return await productDB.UpdateAttributeAsync(attribute);
        }

        /// <summary>
        /// Xóa thuộc tính mặt hàng
        /// </summary>
        public static async Task<bool> DeleteProductAttributeAsync(long attributeID)
        {
            return await productDB.DeleteAttributeAsync(attributeID);
        }

        //=====================================================================
        // ẢNH MẶT HÀNG (PRODUCT PHOTOS)
        //=====================================================================

        /// <summary>
        /// Lấy danh sách ảnh của một mặt hàng
        /// </summary>
        public static async Task<List<ProductPhoto>> ListProductPhotosAsync(int productID)
        {
            return await productDB.ListPhotosAsync(productID);
        }

        /// <summary>
        /// Lấy thông tin một ảnh mặt hàng
        /// </summary>
        public static async Task<ProductPhoto?> GetProductPhotoAsync(long photoID)
        {
            return await productDB.GetPhotoAsync(photoID);
        }

        /// <summary>
        /// Thêm mới ảnh cho mặt hàng
        /// </summary>
        public static async Task<long> AddProductPhotoAsync(ProductPhoto photo)
        {
            return await productDB.AddPhotoAsync(photo);
        }

        /// <summary>
        /// Cập nhật ảnh mặt hàng
        /// </summary>
        public static async Task<bool> UpdateProductPhotoAsync(ProductPhoto photo)
        {
            return await productDB.UpdatePhotoAsync(photo);
        }

        /// <summary>
        /// Xóa ảnh mặt hàng
        /// </summary>
        public static async Task<bool> DeleteProductPhotoAsync(long photoID)
        {
            return await productDB.DeletePhotoAsync(photoID);
        }
    }
}