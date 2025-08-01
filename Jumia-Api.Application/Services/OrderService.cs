using AutoMapper;
using EllipticCurve.Utils;
using Jumia_Api.Application.Common.Results;
using Jumia_Api.Application.Dtos.OrderDtos;
using Jumia_Api.Application.Interfaces;
using Jumia_Api.Domain.Interfaces.UnitOfWork;
using Jumia_Api.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumia_Api.Application.Services
{
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public OrderService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<OrderCreationResult> CreateOrderAsync(CreateOrderDTO orderDto)
        {

            // 1. Collect all unique ProductIds and VariationIds
            var productIds = new List<int>();
            foreach (var subOrder in orderDto.SubOrders)
            {
                foreach (var orderItem in subOrder.OrderItems)
                {
                    productIds.Add(orderItem.ProductId);
                }
            }
            productIds = productIds.Distinct().ToList();

            // 2. Fetch products and their variants
            var productsInOrder = await _unitOfWork.ProductRepo.GetbyIdsWithVariantsAndAttributesAsync(productIds);
            var productMap = productsInOrder.ToDictionary(p => p.ProductId);

            // 3. Perform stock validation for each item in the order and update relevant stock
            foreach (var subOrder in orderDto.SubOrders)
            {
                foreach (var item in subOrder.OrderItems)
                {
                    if (!productMap.TryGetValue(item.ProductId, out var product))
                    {
                        return OrderCreationResult.FailureResult(
                            $"Product with ID {item.ProductId} not found for SubOrder ID {subOrder.ID}.",
                            new Dictionary<string, object> { { "ProductId", item.ProductId } }
                        );
                    }

                    if (item.VariationId.HasValue) // This item refers to a specific product variant
                    {
                        var variant = product.ProductVariants.FirstOrDefault(v => v.VariantId == item.VariationId.Value);

                        if (variant == null)
                        {
                            return OrderCreationResult.FailureResult(
                                $"Variant with ID {item.VariationId.Value} for Product ID {item.ProductId} not found for SubOrder ID {subOrder.ID}.",
                                new Dictionary<string, object> { { "ProductId", item.ProductId }, { "VariantId", item.VariationId.Value } }
                            );
                        }

                        if (variant.StockQuantity < item.Quantity)
                        {
                            return OrderCreationResult.FailureResult(
                                $"Insufficient stock for product '{product.Name}' variant '{variant.VariantName}'. Available: {variant.StockQuantity}, Requested: {item.Quantity} in SubOrder ID {subOrder.ID}.",
                                new Dictionary<string, object>
                                {
                            { "ProductId", item.ProductId },
                            { "VariantId", item.VariationId.Value },
                            { "RequestedQuantity", item.Quantity },
                            { "AvailableQuantity", variant.StockQuantity },
                            { "ProductName", product.Name },
                            { "VariantName", variant.VariantName }
                                }
                            );
                        }

                        // Decrement stock for the specific variant
                        variant.StockQuantity -= item.Quantity;
                        product.StockQuantity -= item.Quantity; // Keep this based on your current decision
                    }
                    else // This item refers to a base product (without variants)
                    {
                        // Ensure that if a product has variants, a VariantId *must* be specified.
                        if (product.ProductVariants != null && product.ProductVariants.Any())
                        {
                            return OrderCreationResult.FailureResult(
                                $"Product '{product.Name}' has variants, but no VariationId was specified for order item ProductId {item.ProductId} in SubOrder ID {subOrder.ID}. A specific variant must be selected.",
                                new Dictionary<string, object> { { "ProductId", item.ProductId } }
                            );
                        }

                        if (product.StockQuantity < item.Quantity)
                        {
                            return OrderCreationResult.FailureResult(
                                $"Insufficient stock for product '{product.Name}'. Available: {product.StockQuantity}, Requested: {item.Quantity} in SubOrder ID {subOrder.ID}.",
                                new Dictionary<string, object>
                                {
                            { "ProductId", item.ProductId },
                            { "RequestedQuantity", item.Quantity },
                            { "AvailableQuantity", product.StockQuantity },
                            { "ProductName", product.Name }
                                }
                            );
                        }

                        // Decrement stock for the base product
                        product.StockQuantity -= item.Quantity;
                    }
                }
            }

            // If all stock checks pass and decrements are applied, proceed with order creation
            var order = _mapper.Map<Order>(orderDto);
            order.CreatedAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;
            order.PaymentStatus = "pending";
          

            // Set CreatedAt and UpdatedAt for SubOrders and OrderItems
            //foreach (var subOrder in order.SubOrders)
            //{
            //    subOrder.CreatedAt = DateTime.UtcNow;
            //    subOrder.UpdatedAt = DateTime.UtcNow;
            //    foreach (var orderItem in subOrder.OrderItems)
            //    {
            //        orderItem.CreatedAt = DateTime.UtcNow;
            //        orderItem.UpdatedAt = DateTime.UtcNow;
            //    }
            //}

            await _unitOfWork.OrderRepo.AddAsync(order);
            await _unitOfWork.SaveChangesAsync();

            // Return success result with the created order DTO
            return OrderCreationResult.SuccessResult(_mapper.Map<OrderDTO>(order));
        }

        public async Task<bool> DeleteOrderAsync(int id)
        {
            var order = await _unitOfWork.OrderRepo.GetByIdAsync(id);
            if (order == null)
                return false;

            await _unitOfWork.OrderRepo.Delete(id);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<OrderDTO>> GetAllOrdersAsync()
        {
            var orders = await _unitOfWork.OrderRepo.GetAllWithDetailsAsync();
            return _mapper.Map<IEnumerable<OrderDTO>>(orders);
        }

        public async Task<OrderDTO> GetOrderByIdAsync(int id)
        {
            var order = await _unitOfWork.OrderRepo.GetWithDetailsAsync(id);
            
            return _mapper.Map<OrderDTO>(order);
        }

        public async Task<IEnumerable<OrderDTO>> GetOrdersByCustomerIdAsync(int customerId)
        {
            var orders = await _unitOfWork.OrderRepo.GetByCustomerIdAsync(customerId);
            return _mapper.Map<IEnumerable<OrderDTO>>(orders);
        }

        public async Task<OrderDTO> UpdateOrderAsync(int id, UpdateOrderDTO orderDto)
        {
            var existingOrder = await _unitOfWork.OrderRepo.GetByIdAsync(id);
            if (existingOrder == null)
                return null;

            _mapper.Map(orderDto, existingOrder);
            existingOrder.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.OrderRepo.Update(existingOrder);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<OrderDTO>(existingOrder);
        }
        public async Task<bool> CancelOrderAsync(int id, string cancellationReason = null)
        {
            var order = await _unitOfWork.OrderRepo.GetByIdAsync(id);

            if (order == null)
                return false;

            if (order.Status == "cancelled")
                return false;
            await CancelOrderTransactionAsync(id);

            //if (order.Status == "shipped" || order.Status == "delivered")
            //    return false;
            var success = await _unitOfWork.OrderRepo.CancelOrderAsync(id, cancellationReason);
            if (!success)
                return false;
            

            await _unitOfWork.SaveChangesAsync();
            return true;
        }
        public async Task<IEnumerable<SubOrderDTO>> GetSubOrdersByOrderIdAsync(int orderId)
        {
            var subOrders = await _unitOfWork.SubOrderRepo.GetSubOrdersByOrderIdAsync(orderId);
            return _mapper.Map<IEnumerable<SubOrderDTO>>(subOrders);
        }
        public async Task<IEnumerable<SubOrderDTO>> GetSubOrdersBySellerIdAsync(int sellerId)
        {
            var subOrders = await _unitOfWork.SubOrderRepo.GetSubOrdersBySellerIdAsync(sellerId);
            return _mapper.Map<IEnumerable<SubOrderDTO>>(subOrders);
        }

        public async Task<bool> CancelOrderTransactionAsync(int orderId)
        {
            try
            {
                // 1. Get the order with all its relevant details (suborders, order items, products, variants)
                var order = await _unitOfWork.OrderRepo.GetWithDetailsAsync(orderId);

                if (order == null)
                {
                    Console.WriteLine($"Order {orderId} not found for cancellation.");
                    return false; // Order not found, nothing to do
                }

                Console.WriteLine($"Attempting to cancel Order {orderId}: restoring stock and deleting order.");

                // 2. Restore stock for each item in the order
                foreach (var subOrder in order.SubOrders)
                {
                    foreach (var orderItem in subOrder.OrderItems)
                    {
                      
                        var product = await _unitOfWork.ProductRepo.GetWithVariantsAndAttributesAsync(orderItem.ProductId);
                        if (product == null)
                        {
                            Console.WriteLine($"WARNING: Product {orderItem.ProductId} not found for OrderItem {orderItem.OrderItemId} of Order {orderId}. Cannot restore stock.");
                            continue; // Skip this item, but try to restore others
                        }

                        if (orderItem.variationId.HasValue)
                        {
                            var variant = product.ProductVariants.FirstOrDefault(v => v.VariantId == orderItem.variationId.Value);
                            if (variant != null)
                            {
                                variant.StockQuantity += orderItem.Quantity;
                                Console.WriteLine($"Restored {orderItem.Quantity} to variant {variant.VariantId} of product {product.ProductId} (Order {orderId}). New stock: {variant.StockQuantity}");
                            }
                            else
                            {
                                Console.WriteLine($"WARNING: Variant {orderItem.variationId.Value} not found for Product {product.ProductId} (Order {orderId}). Cannot restore stock.");
                            }
                        }
                        else
                        {
                            // It's a non-variant product
                            product.StockQuantity += orderItem.Quantity;
                            Console.WriteLine($"Restored {orderItem.Quantity} to product {product.ProductId} (Order {orderId}). New stock: {product.StockQuantity}");
                        }

                        // IMPORTANT: Update the denormalized Product.StockQuantity if it's based on variant sum
                        if (product.ProductVariants != null && product.ProductVariants.Any())
                        {
                            product.StockQuantity = product.ProductVariants.Sum(v => v.StockQuantity);
                            Console.WriteLine($"Aggregated product {product.ProductId} stock updated to {product.StockQuantity} (Order {orderId}) after variant restoration.");
                        }
                    }
                }

                // 3. Delete the order itself
                

                // 4. Save all changes (stock updates + order deletion) in a single transaction
                await _unitOfWork.SaveChangesAsync();
                Console.WriteLine($"Order {orderId} successfully deleted and stock restored.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error canceling order {orderId} and restoring stock: {ex.Message}");
                // Log the full exception details in a production environment
                return false;
            }
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, string status)
        {
            var order =  await _unitOfWork.OrderRepo.GetByIdAsync(orderId);
            if (order == null)
            {
                return false;
            }
            var success = await _unitOfWork.OrderRepo.UpdateOrderStatus(orderId, status);
            if (!success)
            {
                return false;
            }

            await _unitOfWork.SaveChangesAsync();
            return true;

        }
    }

}
