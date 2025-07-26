using AutoMapper;
using EllipticCurve.Utils;
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

        public async Task<OrderDTO> CreateOrderAsync(CreateOrderDTO orderDto)
        {
            var order = _mapper.Map<Order>(orderDto);
            order.CreatedAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;
            order.PaymentStatus = "pending";

            await _unitOfWork.OrderRepo.AddAsync(order);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<OrderDTO>(order);
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
            var orders = await _unitOfWork.OrderRepo.GetWithDetailsAsync(id);
            var order = orders.FirstOrDefault();
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
    }
}
