using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumia_Api.Domain.Models
{
    public class SubOrder
    {
        [Key]
        public int SubOrderId { get; set; }

        [Required]
        [ForeignKey("Order")]
        public int OrderId { get; set; }

        [Required]
        [ForeignKey("Seller")]
        public int SellerId { get; set; }

        [Required]
        [Column(TypeName = "decimal(10, 2)")]
        public decimal Subtotal { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "pending";

        public DateTime StatusUpdatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        public string TrackingNumber { get; set; }

        [MaxLength(100)]
        public string ShippingProvider { get; set; }

        public string ReturnReason { get; set; }

        public DateTime? ReturnRequestedAt { get; set; }

        public DateTime? ReturnApprovedAt { get; set; }

        public DateTime? ReturnReceivedAt { get; set; }

        [Column(TypeName = "decimal(10, 2)")]
        public decimal? RefundAmount { get; set; }

        public DateTime? RefundedAt { get; set; }

        // Navigation properties
        public Order Order { get; set; }
        public ICollection<OrderItem> OrderItems { get; set; }


        public Seller Seller { get; set; }
    }
}
