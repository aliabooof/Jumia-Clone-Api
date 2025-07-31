using Jumia_Api.Application.Common.Results;
using Jumia_Api.Application.Dtos.ProductDtos;
using Jumia_Api.Domain.Interfaces.Repositories;
using Jumia_Api.Domain.Models;
using Jumia_Api.Infrastructure.Presistence.Context;
using Microsoft.EntityFrameworkCore;
using SendGrid.Helpers.Mail;

namespace Jumia_Api.Infrastructure.Presistence.Repositories
{
    public class ProductRepo : GenericRepo<Product>, IProductRepo
    {
        public ProductRepo(JumiaDbContext context) : base(context)
        {
        }
     

        public override async Task Delete(int id)
          => await _dbSet
            .Where(e => e.ProductId == id)
            .ExecuteUpdateAsync(setters => setters
            .SetProperty(p => p.IsAvailable, false));


        public async Task<IEnumerable<Product>> GetAvailableProductsAsync()
            => await _dbSet
                .Where(p => p.IsAvailable)
                .Include(p => p.Seller)
                    .Include(p => p.Category)
                    .Include(p => p.ProductImages)
                    .Include(p => p.ProductVariants)
                    .ThenInclude(v => v.Attributes)
                    .Include(p => p.productAttributeValues)
                    .ThenInclude(av => av.ProductAttribute)
                    .ToListAsync();
                

       

        public async Task<PagedResult<Product>> GetProductsByCategoryIdsAsync(List<int> categoryIds,
                                                                Dictionary<string, string> attributeFilters = null,
                                                                decimal? minPrice = null,
                                                                decimal? maxPrice = null,
                                                                int pageNumber=1,
                                                                int pageSize =20)
        {
            var query = _dbSet
                .Where(p => categoryIds.Contains(p.CategoryId)).Include(p=>p.ProductVariants)
                .Include(p => p.Seller)
                    .Include(p => p.Category)
                    .Include(p => p.ProductImages)
                    .Include(p => p.ProductVariants)
                    .ThenInclude(v => v.Attributes)
                    .Include(p => p.productAttributeValues)
                    .ThenInclude(av => av.ProductAttribute)
                    
                .AsQueryable();

            if (attributeFilters != null && attributeFilters.Any())
            {
                foreach (var filter in attributeFilters)
                {
                    string attributeName = filter.Key;
                    List<string> attributeValues = filter.Value.Split(',').ToList();

                    query = query.Where(p =>
                                     p.productAttributeValues.Any(av =>
                                         av.ProductAttribute.Name == attributeName &&
                                         attributeValues.Contains(av.Value))
                                     ||
                                     p.ProductVariants.Any(v =>
                                         v.Attributes.Any(va =>
                                             va.AttributeName == attributeName &&
                                              attributeValues.Contains(va.AttributeValue))) );
                }

            }
            if (minPrice.HasValue)
                query = query.Where(p => p.BasePrice >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(p => p.BasePrice <= maxPrice.Value);

            var totalItems = await query.CountAsync();

            var products = await query.Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            return new PagedResult<Product>(products, totalItems, pageNumber, pageSize);
        }





        public async Task<IEnumerable<Product>> GetProductsBySellerId(int sellerId)
            => await _dbSet
                    .Where(p => p.SellerId == sellerId)
                    .Include(p => p.Category)
                    .Include(p => p.ProductImages)
                    .Include(p => p.ProductVariants)
                    .ThenInclude(v => v.Attributes)
                    .Include(p => p.productAttributeValues)
                    .ThenInclude(av => av.ProductAttribute)
                .AsNoTracking()
                .ToListAsync();

        public async Task<IEnumerable<Product>> GetAvailableProductsBySellerId(int sellerId)
           => await _dbSet
                    .Where(p => p.SellerId == sellerId&&p.IsAvailable)
                    .AsNoTracking()
                    .ToListAsync();

        public async Task<IEnumerable<Product>> SearchAsync(string searchTerm)
                => await _dbSet
                    .Where(p => p.Name.Contains(searchTerm) || p.Description.Contains(searchTerm))
                    .ToListAsync();

        public async Task<Product?> GetWithVariantsAndAttributesAsync(int productId)
        {

            return await _dbSet
            .AsSplitQuery()
            .Include(p => p.Seller)
                    .Include(p => p.Category)
                    .Include(p => p.ProductImages)
                    .Include(p => p.ProductVariants)
                    .ThenInclude(v => v.Attributes)
                    .Include(p => p.productAttributeValues)
                    .ThenInclude(av => av.ProductAttribute)
                    .FirstOrDefaultAsync(p => p.ProductId == productId);
        }

        public async Task<IEnumerable<Product>> GetAllWithVariantsAndAttributesAsync()
        {
            return await _dbSet
                .AsSplitQuery()
                .Include(p => p.Seller)
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Include(p => p.ProductVariants)
                .ThenInclude(v => v.Attributes)
                .Include(p => p.productAttributeValues)
                .ThenInclude(av => av.ProductAttribute)
                .ToListAsync();
        }
        

        public async Task Activate(int productId)
        {
            var product = await _dbSet.FindAsync(productId);
            if (product != null)
            {
                product.IsAvailable = true;
                product.UpdatedAt = DateTime.UtcNow;
               
            }
        }
        
        public async Task Deactivate(int productId)
        {
            var product = await _dbSet.FindAsync(productId);
            if (product != null)
            {
                product.IsAvailable = false;
                product.UpdatedAt = DateTime.UtcNow;
               
            }
        }

        public async Task<List<Product>> GetbyIdsWithVariantsAndAttributesAsync(List<int> productIds)
        {
            return await _dbSet.Where(p=>productIds.Contains(p.ProductId))
            .AsSplitQuery()
            .Include(p => p.Seller)
                    .Include(p => p.Category)
                    .Include(p => p.ProductImages)
                    .Include(p => p.ProductVariants)
                    .ThenInclude(v => v.Attributes)
                    .Include(p => p.productAttributeValues)
                    .ThenInclude(av => av.ProductAttribute)
                    .ToListAsync();
        }


        public async Task<Product?> GetProductByIdAsync(int id)
        {
           return await _dbSet.FirstOrDefaultAsync(p => p.ProductId == id);

        }

        
    }
}

