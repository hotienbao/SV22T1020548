using Microsoft.AspNetCore.Mvc;
using SV22T1020548.BusinessLayers;
using SV22T1020548.Models.Catalog;
using SV22T1020548.Models.Common;
using System;
using System.Threading.Tasks;

namespace SV22T1020548.Shop.Controllers
{
    public class ProductController : Controller
    {
        private const int PAGE_SIZE = 12;

        public async Task<IActionResult> Index(
            string searchValue = "",
            int categoryID = 0,
            decimal minPrice = 0,
            decimal maxPrice = 0,
            int page = 1)
        {
            bool isAjax = string.Equals(
                Request.Headers["X-Requested-With"],
                "XMLHttpRequest",
                StringComparison.OrdinalIgnoreCase);

            // Load danh sách loại hàng cho filter (chỉ cần khi render full page)
            if (!isAjax)
            {
                var categories = await CatalogDataService.ListCategoriesAsync(
                    new PaginationSearchInput { Page = 1, PageSize = 0, SearchValue = "" });
                ViewBag.Categories = categories.DataItems;
            }

            // Lưu lại filter để hiển thị trên form
            ViewBag.SearchValue = searchValue;
            ViewBag.CategoryID = categoryID;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;

            var input = new ProductSearchInput
            {
                Page = page,
                PageSize = PAGE_SIZE,
                SearchValue = searchValue,
                CategoryID = categoryID,
                MinPrice = minPrice,
                MaxPrice = maxPrice
            };

            var data = await CatalogDataService.ListProductsAsync(input);

            if (isAjax)
                return PartialView("_ProductGrid", data);

            return View(data);
        }

        public async Task<IActionResult> Detail(int id)
        {
            var product = await CatalogDataService.GetProductAsync(id);
            if (product == null)
                return RedirectToAction("Index");

            // Load ảnh bổ sung nếu có
            var photos = await CatalogDataService.ListProductPhotosAsync(id);
            ViewBag.Photos = photos;

            // Load thuộc tính
            var attrs = await CatalogDataService.ListProductAttributesAsync(id);
            ViewBag.Attributes = attrs;

            return View(product);
        }
    }
}
