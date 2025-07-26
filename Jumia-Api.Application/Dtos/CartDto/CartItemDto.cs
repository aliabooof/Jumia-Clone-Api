using Jumia_Api.Domain.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jumia_Api.Application.Dtos.ProductDtos;
using Jumia_Api.Application.Dtos.ProductDtos.Get;

namespace Jumia_Api.Application.Dtos.CartDto
{
   
        public class CartItemDto
        {
            public int CartItemId { get; set; }
            public int ProductId { get; set; }
            public string ProductName { get; set; }
            public string MainImageUrl { get; set; }

            public int? VariationId { get; set; } // null if product has no variant
            public string? VariantName { get; set; }
            public string? VariantImageUrl { get; set; }

            public decimal UnitPrice { get; set; } // price for product or variant
            public decimal DiscountPercentage { get; set; }
            public decimal FinalPrice => UnitPrice;

            public int Quantity { get; set; }
            public decimal Subtotal => FinalPrice * Quantity;
        }


    
}
