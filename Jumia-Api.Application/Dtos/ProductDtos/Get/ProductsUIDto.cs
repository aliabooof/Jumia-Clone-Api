using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumia_Api.Application.Dtos.ProductDtos.Get
{
    public class ProductsUIDto
    {
        public int ProductId { get; set; }
        public string BusinessName { get; set; }
        public string BusinessLogo { get; set; }

        public int SellerId { get; set; }
        public string Name { get; set; }
        public decimal BasePrice { get; set; }
        public float DiscountPercentage { get; set; } 
        public string ImageUrl { get; set; }
        public bool IsAvailable { get; set; }
        public string ApprovalStatus { get; set; }
        public int StockQuantity { get; set; }
        public List<ProductVariantDto> Variants { get; set; }

    }
}
