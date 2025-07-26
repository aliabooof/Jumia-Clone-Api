using AutoMapper;
using Jumia_Api.Application.Dtos.CategoryDtos;
using Jumia_Api.Application.Dtos.ProductDtos;
using Jumia_Api.Application.Interfaces;
using Jumia_Api.Domain.Interfaces.UnitOfWork;
using Jumia_Api.Domain.Models;
using Microsoft.Extensions.Logging;


namespace Jumia_Api.Application.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CategoryService> _logger;
 
        public CategoryService(IUnitOfWork unitOfWork, ILogger<CategoryService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;

        }

        public async Task<CategoryResponseDto> CreateCategoryAsync(CreateCategoryDto createDto)
        {
            try
            {
                _logger.LogInformation($"Creating new category: {createDto.Name}");

                if (string.IsNullOrWhiteSpace(createDto.Name))
                {
                    _logger.LogWarning("Category creation failed - empty name");
                    throw new ArgumentException("Category name cannot be empty");
                }
                if (createDto.ParentCategoryId.HasValue)
                {
                    var parentExists = await _unitOfWork.CategoryRepo.GetByIdAsync(createDto.ParentCategoryId.Value) != null;
                    if (!parentExists)
                    {
                        _logger.LogWarning($"Category creation failed - invalid parent ID: {createDto.ParentCategoryId}");
                        throw new ArgumentException("Specified parent category does not exist");
                    }
                }

                var category = new Category
                {
                    Name = createDto.Name,
                    Description = createDto.Description,
                    ImageSrc = createDto.ImageSrc,
                    ParentCategoryId = createDto.ParentCategoryId
                };

                await _unitOfWork.CategoryRepo.AddAsync(category);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Category created successfully - ID: {category.Id}");
                return ConvertToDto(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category");
                throw;
            }
        }

        public async Task DeleteCategoryAsync(int id)
        {
            try
            {
                _logger.LogInformation($"Deleting category ID: {id}");

                var category = await _unitOfWork.CategoryRepo.GetByIdAsync(id);
                if (category == null)
                {
                    _logger.LogWarning($"Delete failed - category not found: {id}");
                    throw new KeyNotFoundException($"Category with ID {id} not found");
                }

                if (await _unitOfWork.CategoryRepo.HasChildrenAsync(id))
                {
                    _logger.LogWarning($"Delete failed - category has children: {id}");
                    throw new InvalidOperationException("Cannot delete category with subcategories");
                }

                await _unitOfWork.CategoryRepo.Delete(id);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Category deleted successfully - ID: {id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting category ID: {id}");
                throw;
            }
        }

        public async Task<IEnumerable<CategoryResponseDto>> GetAllCategoriesAsync(bool includeSubcategories = false)
        {
            try
            {
                _logger.LogInformation("Getting all categories" +
                    (includeSubcategories ? " with subcategories" : ""));

                IEnumerable<Category> categories;
                if (includeSubcategories)
                {
                    categories = await _unitOfWork.CategoryRepo.GetCategoriesWithSubCategoriesAsync();
                }
                else
                {
                    categories = await _unitOfWork.CategoryRepo.GetAllAsync();
                }

                return categories.Select(ConvertToDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all categories");
                throw;
            }
        }

        public async Task<CategoryResponseDto> GetCategoryByIdAsync(int id, bool includeSubcategories = false)
        {
            try
            {
                _logger.LogInformation($"Getting category by ID: {id}" +
                    (includeSubcategories ? " with subcategories" : ""));

                var category = includeSubcategories
                    ? await _unitOfWork.CategoryRepo.GetCategoryWithSubCategoriesAsync(id)
                    : await _unitOfWork.CategoryRepo.GetByIdAsync(id);

                if (category == null)
                {
                    _logger.LogWarning($"Category not found: {id}");
                    throw new KeyNotFoundException($"Category with ID {id} not found");
                }

                return ConvertToDto(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting category ID: {id}");
                throw;
            }
        }

        public async Task<IEnumerable<CategoryResponseDto>> GetMainCategoriesAsync()
        {
            try
            {
                _logger.LogInformation("Getting main categories");
                var categories = await _unitOfWork.CategoryRepo.GetMainCategoriesAsync();
                return categories.Select(ConvertToDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting main categories");
                throw;
            }
        }

        public async Task<CategoryResponseDto> UpdateCategoryAsync(int id, UpdateCategoryDto updateDto)
        {
            try
            {
                _logger.LogInformation($"Updating category ID: {id}");

                var existingCategory = await _unitOfWork.CategoryRepo.GetByIdAsync(id);
                if (existingCategory == null)
                {
                    _logger.LogWarning($"Update failed - category not found: {id}");
                    throw new KeyNotFoundException($"Category with ID {id} not found");
                }

                if (string.IsNullOrWhiteSpace(updateDto.Name))
                {
                    _logger.LogWarning("Update failed - empty category name");
                    throw new ArgumentException("Category name cannot be empty");
                }

                if (updateDto.ParentCategoryId.HasValue)
                {
                    if (updateDto.ParentCategoryId == id)
                    {
                        _logger.LogWarning("Update failed - category cannot be its own parent");
                        throw new ArgumentException("Category cannot be its own parent");
                    }

                    var parentExists = await _unitOfWork.CategoryRepo
                        .GetByIdAsync(updateDto.ParentCategoryId.Value) != null;
                    if (!parentExists)
                    {
                        _logger.LogWarning($"Update failed - invalid parent ID: {updateDto.ParentCategoryId}");
                        throw new ArgumentException("Specified parent category does not exist");
                    }
                }

                existingCategory.Name = updateDto.Name;
                existingCategory.Description = updateDto.Description;
                existingCategory.ImageSrc = updateDto.ImageSrc;
                existingCategory.ParentCategoryId = updateDto.ParentCategoryId;

                _unitOfWork.CategoryRepo.Update(existingCategory);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Category updated successfully - ID: {id}");
                return ConvertToDto(existingCategory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating category ID: {id}");
                throw;
            }
        }


        public async Task<bool> CategoryNameExistsAsync(string name)
        {
            try
            {
                _logger.LogDebug($"Checking if category name exists: {name}");
                var categories = await _unitOfWork.CategoryRepo.GetAllAsync();
                return categories.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking category name: {name}");
                throw;
            }
        }

        public async Task<IEnumerable<int>> GetCategoryAndDescendantIdsAsync(int categoryId)
        {
            try
            {
                _logger.LogInformation($"Getting category and descendants IDs for: {categoryId}");
                var descendants = await _unitOfWork.CategoryRepo.GetDescendantCategoryIdsAsync(categoryId);
                return new[] { categoryId }.Concat(descendants);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting descendants for category ID: {categoryId}");
                throw;
            }
        }

        public async Task MoveCategoryToNewParentAsync(int categoryId, MoveCategoryDto moveDto)
        {
            try
            {
                _logger.LogInformation($"Moving category {categoryId} to new parent {moveDto.NewParentCategoryId}");

                var category = await _unitOfWork.CategoryRepo.GetByIdAsync(categoryId);
                if (category == null)
                {
                    _logger.LogWarning($"Move failed - category not found: {categoryId}");
                }

                if (moveDto.NewParentCategoryId.HasValue)
                {
                    if (moveDto.NewParentCategoryId == categoryId)
                    {
                        _logger.LogWarning("Move failed - category cannot be its own parent");
                    }

                    var parentExists = await _unitOfWork.CategoryRepo
                        .GetByIdAsync(moveDto.NewParentCategoryId.Value) != null;
                    if (!parentExists)
                    {
                        _logger.LogWarning($"Move failed - invalid parent ID: {moveDto.NewParentCategoryId}");
                    }

                    var descendants = await GetCategoryAndDescendantIdsAsync(categoryId);
                    if (descendants.Contains(moveDto.NewParentCategoryId.Value))
                    {
                        _logger.LogWarning("Move failed - would create circular reference");
                    }
                }

                category.ParentCategoryId = moveDto.NewParentCategoryId;
                _unitOfWork.CategoryRepo.Update(category);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Category moved successfully - ID: {categoryId}, New Parent: {moveDto.NewParentCategoryId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error moving category ID: {categoryId}");
                throw;
            }
        }

        private CategoryResponseDto ConvertToDto(Category category)
        {
            if (category == null) return null;

            return new CategoryResponseDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                ImageSrc = category.ImageSrc,
                ParentCategoryId = category.ParentCategoryId,
                ParentCategory = ConvertToDto(category.ParentCategory),
                SubCategories = category.SubCategories?.Select(ConvertToDto).ToList()
            };
        }


        public async Task<IEnumerable<ProductAttribute>> GetCategoriesAttributes(int parentId)
        {
            var descendants = await GetCategoryAndDescendantIdsAsync(parentId);
            var attributes = await _unitOfWork.ProductAttributeRepo.GetAttributesForCategoriesAsync(descendants.ToList());
            return attributes;
        }
    }
}
