using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumia_Api.Application.Dtos.ProductDtos.Get
{
    public class ProductDetailsDto
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        
        public int SellerId { get; set; }
        public string BusinessLogo { get; set; }
        public string BusinessName { get; set; }
        public string BusinessDescription { get; set; }
        public int CategoryId { get; set; }
        public string Description { get; set; }
        public decimal BasePrice { get; set; }
        public float DiscountPercentage { get; set; }
        public int StockQuantity { get; set; }
        public bool IsAvailable { get; set; }
        public string MainImageUrl { get; set; }
        public List<string> AdditionalImageUrls { get; set; }
        public List<ProductAttributeDto> Attributes { get; set; }
        public List<ProductVariantDto> Variants { get; set; }
    }
}
