using Jumia_Api.Application.Common.Results;
using Jumia_Api.Domain.Models;

namespace Jumia_Api.Domain.Interfaces.Repositories
{
    public interface IProductRepo:IGenericRepo<Product>
    {
        public Task<IEnumerable<Product>> GetAvailableProductsAsync();
        public Task<PagedResult<Product>> GetProductsByCategoryIdsAsync(List<int> categoryIds,
                                                                Dictionary<string, string> attributeFilters = null,
                                                                decimal? minPrice = null,
                                                                decimal? maxPrice = null,
                                                                int pageNumber = 1,
                                                                int pageSize = 20);
        public Task<IEnumerable<Product>> GetProductsBySellerId(int sellerId);
        public Task<IEnumerable<Product>> GetAvailableProductsBySellerId(int sellerId);
        Task<IEnumerable<Product>> SearchAsync(string searchTerm);

        public Task<Product?> GetWithVariantsAndAttributesAsync(int productId);
        public Task<List<Product>> GetbyIdsWithVariantsAndAttributesAsync(List<int> productIds);
        public Task<IEnumerable<Product>> GetAllWithVariantsAndAttributesAsync();
        public Task Deactivate(int productId);
        public Task Activate(int productId);

        public Task<Product> GetProductByIdAsync(int id);
        public Task UpdateProductStatus(int productId, string status);

        //public Task DeleteProductAsync(Product product);

    }
}
