using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumia_Api.Application.Dtos.RatingDtos
{
    public class RatingInfoDto
    {
        public int RatingId { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }=string.Empty;

        public string ProductName { get; set; } = string.Empty;
        public int Stars { get; set; }
        public string Comment { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsVerifiedPurchase { get; set; }

        public string IsAccepted { get; set; }
        public int HelpfulCount { get; set; }
    }
}
