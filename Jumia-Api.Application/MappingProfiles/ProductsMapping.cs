using AutoMapper;
using Jumia_Api.Application.Dtos.ProductDtos;
using Jumia_Api.Application.Dtos.ProductDtos.Get;
using Jumia_Api.Application.Dtos.ProductDtos.Post;
using Jumia_Api.Domain.Models;

namespace Jumia_Api.Application.MappingProfiles
{
    public class ProductsMapping:Profile
    {
        
       public ProductsMapping() {

           
            CreateMap<AddProductDto, Product>()
      .ForMember(dest => dest.ProductImages, opt => opt.Ignore()) 
      .ForMember(dest => dest.productAttributeValues,
          opt => opt.MapFrom(src =>
              src.Attributes.SelectMany(a => a.Values.Select(v =>
                  new ProductAttributeValue
                  {
                      AttributeId = a.AttributeId,
                      Value = v
                  }))))
      .ForMember(dest => dest.ProductVariants,
          opt => opt.MapFrom(src => src.Variants))
      .ForMember(dest => dest.MainImageUrl, opt => opt.Ignore()) 
      .ForMember(dest => dest.ProductId, opt => opt.Ignore())
      .ForMember(dest => dest.Seller, opt => opt.Ignore())
      .ForMember(dest => dest.Category, opt => opt.Ignore());
            CreateMap<AddProductVariantDto, ProductVariant>()
    .ForMember(dest => dest.VariantImageUrl, opt => opt.Ignore()) 
    .ForMember(dest => dest.VariantId, opt => opt.Ignore());
            
            
            CreateMap<UpdateProductDto, Product>()
      .ForMember(dest => dest.ProductImages, opt => opt.Ignore())
      .ForMember(dest => dest.productAttributeValues,
          opt => opt.MapFrom(src =>
              src.Attributes.SelectMany(a => a.Values.Select(v =>
                  new ProductAttributeValue
                  {
                      AttributeId = a.AttributeId,
                      Value = v
                  }))))
      .ForMember(dest => dest.ProductVariants,
          opt => opt.MapFrom(src => src.Variants))
      .ForMember(dest => dest.MainImageUrl, opt => opt.Ignore())
      .ForMember(dest => dest.ProductId, opt => opt.MapFrom(src => src.ProductId))
      .ForMember(dest => dest.Seller, opt => opt.Ignore())
      .ForMember(dest => dest.Category, opt => opt.Ignore());
            CreateMap<AddProductVariantDto, ProductVariant>()
    .ForMember(dest => dest.VariantImageUrl, opt => opt.Ignore())
    .ForMember(dest => dest.VariantId, opt => opt.Ignore());

            CreateMap<Product, ProductDetailsDto>()
                .ForMember(dest=>dest.MainImageUrl,opt=>opt.MapFrom(src=>src.MainImageUrl))
                .ForMember(dest => dest.BusinessName, opt => opt.MapFrom(src => src.Seller.BusinessName))
                .ForMember(dest => dest.BusinessDescription, opt => opt.MapFrom(src => src.Seller.BusinessDescription))
                .ForMember(dest => dest.BusinessLogo, opt => opt.MapFrom(src => src.Seller.BusinessLogo))
                .ForMember(dest=>dest.DiscountPercentage, opt =>
                    opt.MapFrom(src => src.ProductVariants != null && src.ProductVariants.Any()
? src.ProductVariants.Average(v => v.DiscountPercentage)
: src.DiscountPercentage))
                .ForMember(dest => dest.AdditionalImageUrls, opt =>
                    opt.MapFrom(src => src.ProductImages.Select(pi => pi.ImageUrl)))
                .ForMember(dest => dest.Attributes, opt =>
                    opt.MapFrom(src =>
                        src.productAttributeValues
                           .GroupBy(pav => pav.ProductAttribute.Name)
                           .Select(g => new ProductAttributeDto
                           {    AttributeId = g.First().ProductAttribute.AttributeId, 
                               AttributeName = g.Key,
                               Values = g.Select(v => v.Value).Distinct().ToList()
                           })))
                .ForMember(dest => dest.Variants, opt =>
                    opt.MapFrom(src => src.ProductVariants));


            //get
            CreateMap<Product, ProductsUIDto>()
                .ForMember(dest => dest.Variants, opt => opt.MapFrom(src => src.ProductVariants))
                .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src => src.MainImageUrl))
                .ForMember(dest => dest.BusinessName, opt => opt.MapFrom(src => src.Seller.BusinessName))
                .ForMember(dest => dest.BusinessLogo, opt => opt.MapFrom(src => src.Seller.BusinessLogo))
                .ForMember(dest => dest.DiscountPercentage, opt =>
                    opt.MapFrom(src =>
                     src.ProductVariants != null && src.ProductVariants.Any()
                ? src.ProductVariants.Average(v => v.DiscountPercentage)
                : src.DiscountPercentage));


            //post
            CreateMap<ProductVariantDto, ProductVariant>()
                .ForMember(dest => dest.Attributes, opt => opt.MapFrom(src => src.Attributes))
                .ForMember(dest => dest.VariantId, opt => opt.Ignore())   
                .ForMember(dest => dest.ProductId, opt => opt.Ignore())  
                .ForMember(dest => dest.Product, opt => opt.Ignore()).ReverseMap();   
            //get

            //post
            CreateMap<VariantAttributeDto, VariantAttribute>()
                .ForMember(dest => dest.VariantAttributeId, opt => opt.Ignore()) 
                .ForMember(dest => dest.VariantId, opt => opt.Ignore())         
                .ForMember(dest => dest.ProductVariant, opt => opt.Ignore());    

            //get
            CreateMap<VariantAttribute, VariantAttributeDto>()
          .ForMember(dest => dest.AttributeId, opt =>
              opt.MapFrom(src => src.VariantAttributeId))
          .ForMember(dest => dest.AttributeName, opt =>
              opt.MapFrom(src => src.AttributeName))
          .ForMember(dest => dest.AttributeValue, opt =>
              opt.MapFrom(src => src.AttributeValue));
        }

    }
}
