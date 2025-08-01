using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumia_Api.Domain.Models
{
    public class OrderItem
    {
        [Key]
        public int OrderItemId { get; set; }

        [Required]
        [ForeignKey("SubOrder")]
        public int SubOrderId { get; set; }

        [Required]
        [ForeignKey("Product")]
        public int ProductId { get; set; }

        [Required]
        public int Quantity { get; set; }

        
        [Required]
        [Column(TypeName = "decimal(10, 2)")]
        public decimal PriceAtPurchase { get; set; }
        public string MainImageUrl { get; set; } 

        [Required]
        [Column(TypeName = "decimal(10, 2)")]
        public decimal TotalPrice { get; set; }

        [ForeignKey("ProductVariant")]
        public int? variationId { get; set; }  

        // Navigation properties
        public SubOrder SubOrder { get; set; }
        public Product Product { get; set; }
        public ProductVariant ProductVariant { get; set; }
    }
}
