using AutoMapper;
using Jumia_Api.Application.Dtos.CategoryDtos;
using Jumia_Api.Application.Dtos.CouponDtos;
using Jumia_Api.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumia_Api.Application.MappingProfiles
{
    public class CouponMapping : Profile
    {
        public CouponMapping() 
        {

            CreateMap<CreateCouponDto, Coupon>().ReverseMap();

            CreateMap<Coupon, CouponDto>();

            CreateMap<ApplyCouponDto, Coupon>();

            CreateMap<UserCoupon, UserCouponDto>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.CustomerId))
                .ForMember(dest => dest.CouponCode, opt => opt.MapFrom(src => src.Coupon.Code))
                .ForMember(dest => dest.DiscountAmount, opt => opt.MapFrom(src => src.Coupon.DiscountAmount))
                .ForMember(dest => dest.DiscountType, opt => opt.MapFrom(src => src.Coupon.DiscountType))
                .ForMember(dest => dest.StartDate, opt => opt.MapFrom(src => src.Coupon.StartDate))
                .ForMember(dest => dest.EndDate, opt => opt.MapFrom(src => src.Coupon.EndDate))
                .ForMember(dest => dest.CouponDescription, opt => opt.MapFrom(src => src.Coupon.Description))
                .ForMember(dest => dest.UsedAt, opt => opt.MapFrom(src => src.UsedAt.HasValue ? src.UsedAt.Value : (DateTime?)null));


            CreateMap<UserCouponActionDto, UserCoupon>()
                .ForMember(dest => dest.CustomerId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.CouponId, opt => opt.MapFrom(src => src.CouponId));

            CreateMap<UserCoupon, UserCouponActionDto>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.CustomerId));





        }

    }
}
