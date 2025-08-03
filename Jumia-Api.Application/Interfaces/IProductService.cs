using Jumia_Api.Application.Common.Results;
using Jumia_Api.Application.Dtos.ProductDtos;
using Jumia_Api.Application.Dtos.ProductDtos.Get;
using Jumia_Api.Application.Dtos.ProductDtos.Post;
using Jumia_Api.Domain.Models;

namespace Jumia_Api.Application.Interfaces
{
    public interface IProductService
    {
        public Task<IEnumerable<ProductDetailsDto>> GetAllProductsWithDetailsAsync();
        public Task<IEnumerable<ProductsUIDto>> GetProductsBySellerIdAsync(int sellerId, string role);
        public Task<PagedResult<ProductsUIDto>> GetProductsByCategoriesAsync(string role, ProductFilterRequestDto productFilterRequestDto,
            int pageNumber=1,int pageSize=20);
        public Task<IEnumerable<ProductsUIDto>> SearchProductsAsync(string keyword);
        public Task<ProductVariantDto> FindVariantAsync(int productId, FindVariantRequestDto request);
        public Task<AttributeOptionsResponseDto> GetAttributeOptionsAsync(int productId, AttributeOptionsRequestDto request);
        public Task<ProductDetailsDto> GetProductDetailsAsync(int productId, string role);
        public Task<int> CreateProductAsync(AddProductDto request);
        public Task<IEnumerable<ProductsUIDto>> GetAllProductsAsync();

        public  Task ActivateProductAsync(int productId);
        public Task DeactivateProductAsync(int productId);


        public Task<bool> DeleteProductAsync(int productId);

        public Task<Product> GetProductByIdAsync(int id);


        public Task UpdateProductAsync(UpdateProductDto request);
        public Task UpdateProductStatus(int productId, string status);

    }
}
