using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumia_Api.Application.Dtos.SellerDtos
{
    public class SellerInfo
    {
        public int SellerId { get; set; }

        public string SellerName { get; set; }
        public string Email { get; set; }
        public string UserId { get; set; }
        public string BusinessName { get; set; }
        public string? ImageUrl { get; set; }
        public string? BusinessDescription { get; set; }
        public string? BusinessLogo { get; set; }
        public string IsVerified { get; set; } = "pending";
        public DateTime? VerifiedAt { get; set; }
        public double? Rating { get; set; }

        // NEW
        public int TotalProductsSold { get; set; }
        public decimal TotalAmountSold { get; set; }
    }

}
