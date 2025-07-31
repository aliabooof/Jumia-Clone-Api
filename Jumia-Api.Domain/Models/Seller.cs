using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumia_Api.Domain.Models
{
    public class Seller
    {
        [Key]
        public int SellerId { get; set; }

        [Required]
        [ForeignKey("User")]
        public string UserId { get; set; }

        [Required]
        [MaxLength(255)]
        public string BusinessName { get; set; }

        public string BusinessDescription { get; set; }

        [MaxLength(255)]
        public string BusinessLogo { get; set; }

        public string IsVerified { get; set; } = "pending";

        public DateTime? VerifiedAt { get; set; }

        public double Rating { get; set; } = 0;

        public string ImageUrl { get; set; }

        // Navigation property
        public AppUser User { get; set; }
    }
}
