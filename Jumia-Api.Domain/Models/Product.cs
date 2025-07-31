using Jumia_Api.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumia_Api.Domain.Models
{
    public class Product
    {
        [Key]
        public int ProductId { get; set; }

        [Required]
        [ForeignKey("Seller")]
        public int SellerId { get; set; }

        [Required]
        [ForeignKey("Category")]
        public int CategoryId { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; }

        [Required]
        public string Description { get; set; }
        [Timestamp]
        public byte[] RowVersion { get; set; }

        [Required]
        [Column(TypeName = "decimal(10, 2)")]
        public decimal BasePrice { get; set; }

        [Column(TypeName = "decimal(5, 2)")]
        public decimal DiscountPercentage { get; set; } = 0;

        public bool IsAvailable { get; set; } = true;

        [Required]
        [MaxLength(10)]
        [RegularExpression("pending|approved|rejected")]
        public string ApprovalStatus { get; set; } = "pending";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public int StockQuantity { get; set; } 

        [Required]
        [MaxLength(255)]
        public string MainImageUrl { get; set; }

        public double AverageRating { get; set; } = 0;

        // Navigation properties
        public Seller Seller { get; set; }
        public Category Category { get; set; }

        public ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();
        public ICollection<ProductVariant> ProductVariants { get; set; } = new List<ProductVariant>();

        public ICollection<ProductAttributeValue> productAttributeValues { get; set; } = new List<ProductAttributeValue>();


    }





}
