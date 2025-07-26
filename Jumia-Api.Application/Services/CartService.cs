using AutoMapper;
using Jumia_Api.Application.Dtos.CartDto;
using Jumia_Api.Application.Interfaces;
using Jumia_Api.Domain.Interfaces.Repositories;
using Jumia_Api.Domain.Interfaces.UnitOfWork;
using Jumia_Api.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumia_Api.Application.Services
{
    public class CartService : ICartService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CartService(IUnitOfWork unitOfWork, IProductRepo productRepo, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<CartDto> GetCartAsync(int customerId)
        {
            var cart = await _unitOfWork.CartRepo.GetCustomerCartAsync(customerId);

            if (cart is null)
            {
                // If no cart exists, create a new one
                cart = new Cart { CustomerId = customerId };
                await _unitOfWork.CartRepo.AddAsync(cart);
                await _unitOfWork.SaveChangesAsync();
            }

            bool cartModified = false;

            foreach (var cartItem in cart.CartItems.ToList())
            {
                var product = await _unitOfWork.ProductRepo.GetByIdAsync(cartItem.ProductId);

                // Remove if product is unavailable
                if (product is null || !product.IsAvailable)
                {
                    await _unitOfWork.CartItemRepo.Delete(cartItem.CartItemId);
                    cart.CartItems.Remove(cartItem); // Remove from in-memory list
                    cartModified = true;
                    continue;
                }

                int availableStock = product.StockQuantity;

                if (cartItem.VariationId.HasValue)
                {
                    var variant = product.ProductVariants.FirstOrDefault(v => v.VariantId == cartItem.VariationId);

                    // Remove if variant is unavailable
                    if (variant is null || !variant.IsAvailable)
                    {
                        await _unitOfWork.CartItemRepo.Delete(cartItem.CartItemId);
                        cart.CartItems.Remove(cartItem);
                        cartModified = true;
                        continue;
                    }

                    availableStock = variant.StockQuantity;
                }

                // Cap quantity to available stock
                if (cartItem.Quantity > availableStock)
                {
                    cartItem.Quantity = availableStock;
                    cartModified = true;

                    // Optionally remove if stock is zero
                    if (availableStock <= 0)
                    {
                        await _unitOfWork.CartItemRepo.Delete(cartItem.CartItemId);
                        cart.CartItems.Remove(cartItem);
                    }
                }
            }

            if (cartModified)
                await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<CartDto>(cart);
        }


        public async Task AddItemsAsync(int customerId, List<AddToCartDto> dtos)
        {
            var cart = await _unitOfWork.CartRepo.GetCustomerCartAsync(customerId)
                       ?? new Cart { CustomerId = customerId };

            var productIds = dtos.Select(x => x.ProductId).Distinct().ToList();
            var products = await _unitOfWork.ProductRepo.GetbyIdsWithVariantsAndAttributesAsync(productIds);

            foreach (var dto in dtos)
            {
                var product = products.FirstOrDefault(p => p.ProductId == dto.ProductId);
                if (product == null || !product.IsAvailable)
                    throw new Exception($"Product {dto.ProductId} not available.");

                if (dto.VariantId.HasValue)
                {
                    var variant = product.ProductVariants.FirstOrDefault(v => v.VariantId == dto.VariantId);
                    if (variant == null || !variant.IsAvailable)
                        throw new Exception($"Variant {dto.VariantId} not available.");
                    if (variant.StockQuantity < dto.Quantity)
                        throw new Exception($"Insufficient stock for variant {dto.VariantId}.");
                }
                else
                {
                    if (product.StockQuantity < dto.Quantity)
                        throw new Exception($"Insufficient stock for product {dto.ProductId}.");
                }

                var existingItem = cart.CartItems.FirstOrDefault(ci =>
                    ci.ProductId == dto.ProductId && ci.VariationId == dto.VariantId);

                if (existingItem != null)
                {
                    existingItem.Quantity += dto.Quantity;
                }
                else
                {
                    var variant = dto.VariantId.HasValue
                            ? product.ProductVariants.FirstOrDefault(v => v.VariantId == dto.VariantId) : null;
                    var newItem = new CartItem
                    {
                        ProductId = dto.ProductId,
                        VariationId = dto.VariantId,
                        Quantity = dto.Quantity,
                        PriceAtAddition = variant != null
                                    ? (variant.Price - (variant.Price * (variant.DiscountPercentage ?? 0) / 100))
                                    : product.BasePrice
                    };

                    cart.CartItems.Add(newItem);
                }
            }

            if (cart.CartId == 0)
                await _unitOfWork.CartRepo.AddAsync(cart);
            else
                _unitOfWork.CartRepo.Update(cart);

            await _unitOfWork.SaveChangesAsync();
        }


        public async Task<CartItemDto?> UpdateItemQuantityAsync(int customerId, int cartItemId, int quantity)
        {
            var item = await _unitOfWork.CartItemRepo.GetByIdAsync(cartItemId);
            if (item is null)
                return null; // Item not found

            var product = await _unitOfWork.ProductRepo.GetWithVariantsAndAttributesAsync(item.ProductId);
            if (product is null || !product.IsAvailable)
            {
                await _unitOfWork.CartItemRepo.Delete(item.CartItemId);
                await _unitOfWork.SaveChangesAsync();
                return null; // Product no longer available
            }

            int availableStock = product.StockQuantity;

            if (item.VariationId.HasValue)
            {
                var variant = product.ProductVariants.FirstOrDefault(v => v.VariantId == item.VariationId.Value);

                if (variant is null || !variant.IsAvailable)
                {
                    await _unitOfWork.CartItemRepo.Delete(item.CartItemId);
                    await _unitOfWork.SaveChangesAsync();
                    return null; // Variant no longer available
                }

                availableStock = variant.StockQuantity;
            }

            // Increment the quantity and cap to stock
            var newQuantity = item.Quantity + quantity;

            if (newQuantity <= 0)
            {
                await _unitOfWork.CartItemRepo.Delete(item.CartItemId);
                await _unitOfWork.SaveChangesAsync();
                return null; // Remove item if quantity is zero or less
            }

            item.Quantity = Math.Min(newQuantity, availableStock);

            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<CartItemDto>(item);
        }




        public async Task RemoveItemAsync(int customerId, int cartItemId)
        {
            await _unitOfWork.CartItemRepo.Delete(cartItemId);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task ClearCartAsync(int customerId)
        {
            await _unitOfWork.CartRepo.ClearCartAsync(customerId);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
