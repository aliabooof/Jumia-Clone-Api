using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumia_Api.Application.Dtos.OrderDtos
{
    public class SubOrderDTO
    {
        public int ID { get; set; }
        public int OrderId { get; set; }
        public int SellerId { get; set; }
        public decimal Subtotal { get; set; }
        public string Status { get; set; } = "pending";
        public DateTime StatusUpdatedAt { get; set; } = DateTime.UtcNow;
        public string TrackingNumber { get; set; }
        public string ShippingProvider { get; set; }
        public string ReturnReason { get; set; }

        public DateTime? ReturnRequestedAt { get; set; }

        public DateTime? ReturnApprovedAt { get; set; }

        public DateTime? ReturnReceivedAt { get; set; }

        [Column(TypeName = "decimal(10, 2)")]
        public decimal? RefundAmount { get; set; }

        public DateTime? RefundedAt { get; set; }
        //list of order items
        public List<OrderItemDTO> OrderItems { get; set; } = new List<OrderItemDTO>();
    }
}
