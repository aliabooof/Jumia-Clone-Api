using AutoMapper;
using Jumia_Api.Application.Dtos.CartDto;
using Jumia_Api.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumia_Api.Application.MappingProfiles
{
    public class CartMapping : Profile
    {
        public CartMapping()
        {
            CreateMap<Cart, CartDto>()
                .ForMember(dest => dest.TotalPrice,
                           opt => opt.MapFrom(src => src.CartItems.Sum(i => i.PriceAtAddition * i.Quantity)))
                .ForMember(dest => dest.TotalQuantity,
                           opt => opt.MapFrom(src => src.CartItems.Sum(i => i.Quantity)));

            CreateMap<CartItem, CartItemDto>()
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product.Name))
                .ForMember(dest => dest.MainImageUrl, opt => opt.MapFrom(src => src.Product.MainImageUrl))
                .ForMember(dest => dest.VariantName, opt => opt.MapFrom(src => src.ProductVariant != null ? src.ProductVariant.VariantName : null))
                .ForMember(dest => dest.VariantImageUrl, opt => opt.MapFrom(src => src.ProductVariant != null ? src.ProductVariant.VariantImageUrl : null))
                .ForMember(dest => dest.UnitPrice, opt => opt.MapFrom(src => src.PriceAtAddition))
                .ForMember(dest => dest.DiscountPercentage, opt => opt.MapFrom(src => src.ProductVariant != null ? src.ProductVariant.DiscountPercentage : src.Product.DiscountPercentage));
        
        }
    }

}
