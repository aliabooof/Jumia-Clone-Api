﻿using Jumia_Api.Domain.Models;

namespace Jumia_Api.Domain.Interfaces.Repositories
{
    public interface IProductRepo:IGenericRepo<Product>
    {
        public Task<IEnumerable<Product>> GetAvailableProductsAsync();
        public Task<List<Product>> GetProductsByCategoryIdsAsync(List<int> categoryIds,
                                                                Dictionary<string, string> attributeFilters = null,
                                                                decimal? minPrice = null,
                                                                decimal? maxPrice = null);
        public Task<IEnumerable<Product>> GetProductsBySellerId(int sellerId);
        public Task<IEnumerable<Product>> GetAvailableProductsBySellerId(int sellerId);
        Task<IEnumerable<Product>> SearchAsync(string searchTerm);

        public Task<Product?> GetWithVariantsAndAttributesAsync(int productId);
        public Task<List<Product>> GetbyIdsWithVariantsAndAttributesAsync(List<int> productIds);
        public Task<IEnumerable<Product>> GetAllWithVariantsAndAttributesAsync();
        public Task Deactivate(int productId);
        public Task Activate(int productId);

    }
}
