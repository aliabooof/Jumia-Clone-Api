using AutoMapper;
using Jumia_Api.Application.Common.Results;
using Jumia_Api.Application.Dtos.ProductDtos;
using Jumia_Api.Application.Dtos.ProductDtos.Get;
using Jumia_Api.Application.Dtos.ProductDtos.Post;
using Jumia_Api.Application.Interfaces;
using Jumia_Api.Domain.Interfaces.Repositories;
using Jumia_Api.Domain.Interfaces.UnitOfWork;
using Jumia_Api.Domain.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SendGrid.Helpers.Errors.Model;

namespace Jumia_Api.Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ProductService> _logger;
        private readonly IMapper _mapper;
        private readonly IFileService _fileService;
        public ProductService(IUnitOfWork unitOfWork, ILogger<ProductService> logger, IMapper mapper, IFileService fileService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _mapper = mapper;
            _fileService = fileService;
        }


        public  async Task<bool> DeleteProductAsync(int productId)
        {
            var product = await _unitOfWork.ProductRepo.GetByIdAsync(productId);
            if (product == null) { return false; }
            await _unitOfWork.ProductRepo.Delete(productId);
            await _unitOfWork.SaveChangesAsync();
   
            return true;
        }

        public async Task<Product> GetProductByIdAsync(int id)
        {
            return await _unitOfWork.ProductRepo.GetByIdAsync(id);
        }

        public async Task <IEnumerable<ProductDetailsDto>> GetAllProductsWithDetailsAsync()
        {
            //available and not available products
            //done gets the product details by productId(prod, attributes and variants)
            var products = await _unitOfWork.ProductRepo.GetAllWithVariantsAndAttributesAsync();
            if (products == null || !products.Any())
            {
                _logger.LogWarning("No available products found.");
                return Enumerable.Empty<ProductDetailsDto>();
            }
            _logger.LogInformation($"Found {products.Count()} available products.");
            return _mapper.Map<IEnumerable<ProductDetailsDto>>(products);
        }

      
        public async Task<IEnumerable<ProductsUIDto>> GetProductsBySellerIdAsync(int sellerId, string role)
        {
            var products = await _unitOfWork.ProductRepo.GetProductsBySellerId(sellerId);
            if (products == null || !products.Any())
            {
                _logger.LogWarning($"No products found for seller with ID {sellerId}.");
                return Enumerable.Empty<ProductsUIDto>();
            }
            _logger.LogInformation($"Found {products.Count()} products for seller with ID {sellerId}.");
            if (role.ToLower() == "seller" || role.ToLower() == "admin")
            {
                return products.Select(p => _mapper.Map<ProductsUIDto>(p)).ToList();
            }
            else
            {
                // For non-seller and non-admin roles, filter out unavailable products
                var availableProducts = products.Where(p => p.IsAvailable && p.ApprovalStatus.ToLower()=="approved"&&p.Seller.IsVerified.ToLower()=="authorized");
                return availableProducts.Select(p => _mapper.Map<ProductsUIDto>(p)).ToList();
            }

        }







        //seller & customer & admin
        public async Task<ProductDetailsDto> GetProductDetailsAsync(int productId, string role)
        {
            //done gets the product details by productId(prod, attributes and variants)
            // available and not available products
            var product = await _unitOfWork.ProductRepo.GetWithVariantsAndAttributesAsync(productId);
            if (product == null)
            {
                _logger.LogWarning($"Product with ID {productId} not found.");
                return null; 
            }
            if(!product.IsAvailable && role.ToLower() == "Customer" && product.ApprovalStatus.ToLower() == "rejected" &&
                product.ApprovalStatus.ToLower() == "pending"&&product.Seller.IsVerified.ToLower() == "authorized")
            {
                _logger.LogWarning($"Product with ID {productId} is not available for non-admin users.");
                return null; // or throw an exception if preferred
            }

            return _mapper.Map<ProductDetailsDto>(product);
        }

        public async Task<IEnumerable<ProductsUIDto>> GetAllProductsAsync()
        {
            //done gets the available products
            var products = await _unitOfWork.ProductRepo.GetAllAsync();
            if (products == null || !products.Any())
            {
                _logger.LogWarning("No available products found.");
                return Enumerable.Empty<ProductsUIDto>();
            }
            _logger.LogInformation($"Found {products.Count()} available products.");
            return _mapper.Map<IEnumerable<ProductsUIDto>>(products);
        }




        public async Task<PagedResult<ProductsUIDto>> GetProductsByCategoriesAsync(string role,ProductFilterRequestDto productFilterRequestDto
                                                                                   ,int pageNumber = 1,     
                                                                                    int pageSize = 20)

        {

            if (productFilterRequestDto == null || !productFilterRequestDto.CategoryIds.Any())
            {
                _logger.LogWarning("GetProductsByCategoriesAsync called with null or empty categoryIds. Empty product List is returned");
                return new PagedResult<ProductsUIDto>(null, 0, pageNumber, pageSize);
            }
            var allCategoryIds = productFilterRequestDto.CategoryIds.Distinct().ToList();
            _logger.LogInformation($"GetProductsByCategoriesAsync called with {allCategoryIds.Count} category IDs");

            var additionalCategoryIds = new List<int>();

            foreach (var categoryId in allCategoryIds)
            {
                additionalCategoryIds.Add(categoryId); // include given category
                var descendants = await _unitOfWork.CategoryRepo.GetDescendantCategoryIdsAsync(categoryId);
                additionalCategoryIds.AddRange(descendants);
            }

            allCategoryIds.AddRange(additionalCategoryIds);
            allCategoryIds = allCategoryIds.Distinct().ToList();

            allCategoryIds = allCategoryIds.Distinct().ToList();

            var pagedProducts =  await _unitOfWork.ProductRepo.GetProductsByCategoryIdsAsync(allCategoryIds,
                                                                                productFilterRequestDto.AttributeFilters,
                                                                                productFilterRequestDto.MinPrice,
                                                                                productFilterRequestDto.MaxPrice,
                                                                                pageNumber,
                                                                                pageSize);
            if (pagedProducts == null || !pagedProducts.Items.Any())
            {
                _logger.LogWarning("No products found for the given categories.");
                return new PagedResult<ProductsUIDto>(null, 0,pageNumber,pageSize); 
            }

            _logger.LogInformation($"Found {pagedProducts.TotalCount} products for the given categories.");

            IEnumerable<Product> filteredProducts = pagedProducts.Items;

            if (role != "Admin" && role != "Seller")
            {
                // For non-seller and non-admin roles, filter out unavailable products
                _logger.LogInformation($"Filtering products for role: {role}. Only available products will be returned.");
                filteredProducts = filteredProducts.Where(p => p.IsAvailable&&p.ApprovalStatus.ToLower() == "approved" && p.Seller.IsVerified.ToLower() == "authorized");
            }
            return new PagedResult<ProductsUIDto>
            {
                Items = filteredProducts.Select(p => _mapper.Map<ProductsUIDto>(p)).ToList(),
                TotalCount = pagedProducts.TotalCount,
                CurrentPage = pageNumber,
                PageSize = pageSize
            };

        }



        public async Task<IEnumerable<ProductsUIDto>> SearchProductsAsync(string keyword)
        {
            var products = await _unitOfWork.ProductRepo.SearchAsync(keyword);
            if (products == null || !products.Any())
            {
                _logger.LogWarning($"No products found matching the keyword '{keyword}'.");
                return Enumerable.Empty<ProductsUIDto>();
            }
            _logger.LogInformation($"Found {products.Count()} products matching the keyword '{keyword}'.");
             products = products.Where(p=>p.IsAvailable).ToList();
            return _mapper.Map<IEnumerable<ProductsUIDto>>(products);
        }





        public async Task<int> CreateProductAsync(AddProductDto request)
        {

            if (request == null)
                throw new ArgumentNullException(nameof(request));

       
            var product = _mapper.Map<Product>(request);

            product.StockQuantity = request.StockQuantity;
            product.ApprovalStatus = "pending";
            product.CreatedAt = DateTime.UtcNow;
            product.UpdatedAt = DateTime.UtcNow;

            if (request.MainImageUrl != null && _fileService.IsValidImage(request.MainImageUrl))
            {
                product.MainImageUrl = await _fileService.SaveFileAsync(request.MainImageUrl,"products");
            }
            else
            {
                throw new Exception("Invalid main image URL");
            }

            foreach (var image in request.AdditionalImageUrls)
            {
                if (!_fileService.IsValidImage(image))
                    continue;

                var imageUrl = await _fileService.SaveFileAsync(image, "products");
                product.ProductImages.Add(new ProductImage { ImageUrl = imageUrl });
            }

            var categoryAttributes = await _unitOfWork.ProductAttributeRepo
                .GetAttributesByCategoryIdAsync(request.CategoryId);
            if (request.Variants != null || request.Variants.Any())
            {
                foreach (var attr in request.Attributes)
            {
                var attributeInDb = categoryAttributes
                    .FirstOrDefault(a => a.Name == attr.AttributeName);

                if (attributeInDb == null)
                    throw new Exception($"Attribute '{attr.AttributeName}' not found in category {request.CategoryId}");

                foreach (var value in attr.Values)
                {
                    product.productAttributeValues.Add(new ProductAttributeValue
                    {
                        AttributeId = attributeInDb.AttributeId, 
                        Value = value
                    });
                }
            }

            }

            if (request.Variants != null || request.Variants.Any())
            {
                product.ProductVariants = new List<ProductVariant>(); ;


                    foreach (var variantDto in request.Variants)
                {
                    var variant = _mapper.Map<ProductVariant>(variantDto);
                
                    if(variantDto.VariantImageUrl != null && _fileService.IsValidImage(variantDto.VariantImageUrl))
                    {
                        variant.VariantImageUrl = await _fileService.SaveFileAsync(variantDto.VariantImageUrl, "products");
                    }
                    else
                    {
                        throw new Exception("Invalid variant image URL");
                    }




                    product.ProductVariants.Add(variant);
                }
            }

            // Save everything
            await _unitOfWork.ProductRepo.AddAsync(product);
            await _unitOfWork.SaveChangesAsync();

            return product.ProductId;
        }



        public async Task ActivateProductAsync(int productId)
        {
            await _unitOfWork.ProductRepo.Activate(productId);
            await _unitOfWork.SaveChangesAsync();

        }
        public async Task DeactivateProductAsync(int productId)
        {
            await _unitOfWork.ProductRepo.Deactivate(productId);
            await _unitOfWork.SaveChangesAsync();
        }


        public async Task<ProductVariantDto> FindVariantAsync(int productId, FindVariantRequestDto request)
        {
            var product = await _unitOfWork.ProductRepo.GetWithVariantsAndAttributesAsync(productId);

            if (product == null)
                throw new KeyNotFoundException("Product not found");

            var variant = product.ProductVariants
                .FirstOrDefault(v =>
                    request.SelectedAttributes.All(sa =>
                        v.Attributes.Any(a =>
                            a.AttributeName == sa.AttributeName &&
                            a.AttributeValue == sa.AttributeValue))
                );

            if (variant == null)
                throw new KeyNotFoundException("No matching variant found");

            return _mapper.Map<ProductVariantDto>(variant);
        }










        public async Task<AttributeOptionsResponseDto> GetAttributeOptionsAsync(int productId, AttributeOptionsRequestDto request)
        {
            var product = await _unitOfWork.ProductRepo.GetWithVariantsAndAttributesAsync(productId);

            if (product == null)
                throw new KeyNotFoundException("Product not found");

            // Find variants matching current selection
            var matchingVariants = product.ProductVariants
                .Where(v => request.SelectedAttributes
                    .All(sa => v.Attributes
                        .Any(a => a.AttributeName == sa.AttributeName && a.AttributeValue == sa.AttributeValue)))
                .ToList();

            // Find remaining attributes and their valid values
            var allAttributes = product.productAttributeValues
                .Select(pav => pav.ProductAttribute.Name)
                .Distinct();

            var selectedNames = request.SelectedAttributes
                .Select(a => a.AttributeName);

            var remainingAttributes = allAttributes
                .Except(selectedNames);

            var nextOptions = remainingAttributes.Select(attrName => new AttributeOptionDto
            {
                AttributeName = attrName,
                ValidValues = matchingVariants
                    .SelectMany(v => v.Attributes)
                    .Where(a => a.AttributeName == attrName)
                    .Select(a => a.AttributeValue)
                    .Distinct()
                    .ToList()
            }).ToList();

            return new AttributeOptionsResponseDto { NextOptions = nextOptions };
        }




        public async Task UpdateProductAsync(UpdateProductDto request) // Changed return type to void or bool, as ID is already known
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // 1. Retrieve the existing product from the database
            // Use Include() to load related entities (images, attributes, variants, variant attributes)
            // This is CRUCIAL for tracking changes and updates.
            var existingProduct = await _unitOfWork.ProductRepo
                                    .GetWithVariantsAndAttributesAsync(request.ProductId); // Assuming this method exists and loads all related data

            if (existingProduct == null)
                throw new Exception($"Product with ID {request.ProductId} not found.");

            // 2. Update scalar properties of the existing product
            existingProduct.Name = request.Name;
            existingProduct.Description = request.Description;
            existingProduct.BasePrice = request.BasePrice;
            existingProduct.CategoryId = request.CategoryId; // Update category if allowed
            existingProduct.ApprovalStatus = "pending"; // Reset approval status on update
            existingProduct.UpdatedAt = DateTime.UtcNow;
            existingProduct.StockQuantity = request.StockQuantity;
            // 3. Handle Main Image Update
            if (request.MainImageUrl != null) // A new file was uploaded
            {
                if (!_fileService.IsValidImage(request.MainImageUrl))
                    throw new Exception("Invalid main image file provided.");

                // Optionally, delete the old main image file if it exists
                if (!string.IsNullOrEmpty(existingProduct.MainImageUrl))
                {
                    _fileService.DeleteFile(existingProduct.MainImageUrl); // Assuming DeleteFile method takes a URL/path
                }
                existingProduct.MainImageUrl = await _fileService.SaveFileAsync(request.MainImageUrl, "products");
            }
            // else: If request.MainImageUrl is null, it means no new file was uploaded for the main image.
            // The existingProduct.MainImageUrl will retain its value, effectively preserving the old image.

            // 4. Handle Additional Images
            // A common strategy for collection updates: clear existing and re-add from DTO.
            // This assumes your DTO sends ALL current additional images (both old retained and new ones).
            // If your frontend only sends NEW files, this logic needs adjustment (e.g., compare lists).
            if (request.AdditionalImageUrls != null && request.AdditionalImageUrls.Count > 0)
            {
                // Clear existing additional images from the product
                existingProduct.ProductImages.Clear();

                foreach (var imageFile in request.AdditionalImageUrls)
                {
                    if (!_fileService.IsValidImage(imageFile))
                        continue; // Skip invalid files, or throw an error based on your policy

                    var imageUrl = await _fileService.SaveFileAsync(imageFile, "products");
                    existingProduct.ProductImages.Add(new ProductImage { ImageUrl = imageUrl, ProductId = existingProduct.ProductId });
                }
            }
            // else: If request.AdditionalImageUrls is null, it means no new files were uploaded.
            // The existing images are cleared above, so this would effectively delete all additional images.
            // If you want to preserve existing ones when nothing new is uploaded, you need a different DTO strategy (e.g., list of string URLs for existing, list of IFormFile for new).
            // For simplicity, this assumes the frontend resubmits all desired additional images (new and retained).


            // 5. Handle Product Attributes (Root level attributes)
            // Similar to additional images, a common approach for 1:Many relationships is to delete existing and re-add.
            // This assumes your frontend sends ALL required product attributes, even if unchanged.
            if (request.Attributes != null && request.Attributes.Count > 0)
            {
                existingProduct.productAttributeValues.Clear(); // Clear existing product attribute values

                // Fetch category attributes to validate against
                var categoryAttributes = await _unitOfWork.ProductAttributeRepo
                                                .GetAttributesByCategoryIdAsync(request.CategoryId);

                foreach (var attrDto in request.Attributes)
                {
                    var attributeInDb = categoryAttributes.FirstOrDefault(a => a.AttributeId == attrDto.AttributeId); // Match by AttributeId

                    if (attributeInDb == null)
                        throw new Exception($"Attribute with ID '{attrDto.AttributeId}' not valid for category {request.CategoryId}");

                    foreach (var value in attrDto.Values)
                    {
                        existingProduct.productAttributeValues.Add(new ProductAttributeValue
                        {
                            ProductId = existingProduct.ProductId,
                            AttributeId = attributeInDb.AttributeId,
                            Value = value
                        });
                    }
                }
            }


            // 6. Handle Product Variants
            // This is the most complex part due to nested collections (variant attributes)
            // Strategy: Compare existing variants with requested variants
            var existingVariantIds = existingProduct.ProductVariants.Select(v => v.VariantId).ToList();
            var requestedVariantIds = request.Variants?.Select(v => v.VariantId).ToList() ?? new List<int>();

            // Variants to remove (exist in DB but not in request)
            var variantsToRemove = existingProduct.ProductVariants
                                    .Where(ev => !requestedVariantIds.Contains(ev.VariantId))
                                    .ToList();
            foreach (var variant in variantsToRemove)
            {
                // Optionally delete variant image file here
                if (!string.IsNullOrEmpty(variant.VariantImageUrl))
                {
                    _fileService.DeleteFile(variant.VariantImageUrl);
                }
                _unitOfWork.VariantRepo.Delete(variant.VariantId); // Assuming a generic Delete method
            }

            // Variants to add or update
            if (request.Variants != null)
            {
                foreach (var variantDto in request.Variants)
                {
                    ProductVariant variantToProcess;

                    if (variantDto.VariantId > 0 && existingVariantIds.Contains(variantDto.VariantId))
                    {
                        // Existing variant: Find and update
                        variantToProcess = existingProduct.ProductVariants.First(ev => ev.VariantId == variantDto.VariantId);
                        _mapper.Map(variantDto, variantToProcess); // Map DTO to existing entity to update scalar properties
                    }
                    else
                    {
                        // New variant: Create and add
                        variantToProcess = _mapper.Map<ProductVariant>(variantDto);
                        variantToProcess.ProductId = existingProduct.ProductId; // Link to parent product
                        existingProduct.ProductVariants.Add(variantToProcess); // Add to EF Core's tracking
                    }

                    // Handle Variant Image
                    if (variantDto.VariantImageUrl != null) // New file uploaded for this variant
                    {
                        if (!_fileService.IsValidImage(variantDto.VariantImageUrl))
                            throw new Exception($"Invalid image file for variant '{variantDto.VariantName}'.");

                        // Delete old variant image if it exists and a new one is provided
                        if (!string.IsNullOrEmpty(variantToProcess.VariantImageUrl))
                        {
                            _fileService.DeleteFile(variantToProcess.VariantImageUrl);
                        }
                        variantToProcess.VariantImageUrl = await _fileService.SaveFileAsync(variantDto.VariantImageUrl, "products");
                    }
                    // else if variantDto.VariantImageUrl is null and variantToProcess.VariantImageUrl already has a value,
                    // it implies the existing image should be kept, which is handled by simply not re-assigning.
                    // If variantDto.VariantImageUrl is an empty string and you want to remove the image, you need specific logic.
                    // Current logic: if null, keep existing. If it's an IFormFile, replace.

                    // Handle Variant Attributes
                    var existingVariantAttributeIds = variantToProcess.Attributes.Select(va => va.VariantAttributeId).ToList();
                    var requestedVariantAttributeIds = variantDto.Attributes?.Select(va => va.AttributeId).ToList() ?? new List<int>();

                    // Remove variant attributes
                    var variantAttrsToRemove = variantToProcess.Attributes
                                                .Where(eva => !requestedVariantAttributeIds.Contains(eva.VariantAttributeId))
                                                .ToList();
                    foreach (var va in variantAttrsToRemove)
                    {
                        _unitOfWork.VariantAttributeRepo.Delete(va.VariantAttributeId); // Assuming this repo is for VariantAttributeValue
                    }

                    // Add or update variant attributes
                    if (variantDto.Attributes != null)
                    {
                        foreach (var varAttrDto in variantDto.Attributes)
                        {
                            VariantAttribute variantAttrToProcess;

                            if (existingVariantAttributeIds.Contains(varAttrDto.AttributeId))
                            {
                                // Update existing variant attribute
                                variantAttrToProcess = variantToProcess.Attributes
                                                        .First(eva => eva.VariantAttributeId == varAttrDto.AttributeId);
                                variantAttrToProcess.AttributeValue = varAttrDto.AttributeValue;
                            }
                            else
                            {
                                // Add new variant attribute
                                variantAttrToProcess = new VariantAttribute
                                {
                                    VariantAttributeId = varAttrDto.AttributeId,
                                    AttributeValue = varAttrDto.AttributeValue,
                                    VariantId = variantToProcess.VariantId // Link to parent variant
                                };
                                variantToProcess.Attributes.Add(variantAttrToProcess);
                            }
                        }
                    }
                }
            }

            // 8. Mark product as modified and save changes
            _unitOfWork.ProductRepo.Update(existingProduct); // Assuming a generic Update method
            await _unitOfWork.SaveChangesAsync();
        }








    }
}





