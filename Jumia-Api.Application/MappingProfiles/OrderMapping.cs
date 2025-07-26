using AutoMapper;
using Jumia_Api.Application.Dtos.OrderDtos;
using Jumia_Api.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumia_Api.Application.MappingProfiles
{
    public class OrderMapping:Profile
    {
        public OrderMapping()
        {
            CreateMap<CreateOrderDTO, Order>()
                .ForMember(dest => dest.PaymentStatus, opt => opt.MapFrom(src => "pending"))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));

            CreateMap<UpdateOrderDTO, Order>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
                //.ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));

            CreateMap<Order, OrderDTO>();

            CreateMap<CancelOrderDTO, Order>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(_ => "cancelled"))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));

            CreateMap<SubOrder, SubOrderDTO>()
                .ForMember(dest=>dest.ID,opt=>opt.MapFrom(src => src.SubOrderId))
                .ReverseMap();

            CreateMap<OrderItem, OrderItemDTO>().ReverseMap();

            CreateMap<OrderDTO, Order>()
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore()) 
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));
        }
    }
}
