using Microsoft.AspNetCore.Http;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumia_Api.Application.Dtos.ProductDtos.Post
{
   public  class AddProductVariantDto
    {
        public int VariantId { get; set; }
        public string VariantName { get; set; } = "";
        public decimal Price { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public int StockQuantity { get; set; }
        public string SKU { get; set; } = "";
       
        public IFormFile VariantImageUrl { get; set; } 
        public bool IsDefault { get; set; }
        public bool IsAvailable { get; set; }
        public List<VariantAttributeDto> Attributes { get; set; } = new();
    }
}
