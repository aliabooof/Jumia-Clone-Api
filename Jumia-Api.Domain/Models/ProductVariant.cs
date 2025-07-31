using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumia_Api.Domain.Models
{
    public class ProductVariant
    {
        [Key]
        public int VariantId { get; set; }

        [Required]
        [ForeignKey("Product")]
        public int ProductId { get; set; }

        [Required]
        [MaxLength(100)]
        public string VariantName { get; set; }
        [Timestamp]
        public byte[] RowVersion { get; set; }

        [Required]
        [Column(TypeName = "decimal(10, 2)")]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(5, 2)")]
        public decimal? DiscountPercentage { get; set; } = 0;

        [Required]
        public int StockQuantity { get; set; } = 0;

        [MaxLength(50)]
        public string SKU { get; set; }

        [MaxLength(255)]
        public string VariantImageUrl { get; set; }

        public bool IsDefault { get; set; } = false;

        public bool IsAvailable { get; set; } = true;

        // Navigation property
        public Product Product { get; set; }

        public ICollection<VariantAttribute> Attributes { get; set; }
    }
}
