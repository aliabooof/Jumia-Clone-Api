using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Jumia_Api.Application.Dtos.SellerDtos
{
    public class CreateSellerDto
    {
        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string OtpCode { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public string ConfirmPassword { get; set; }

        [Required]
        [StringLength(50)]

        public string FirstName { get; set; }

        [Required]
        [StringLength(50)]

        public string LastName { get; set; }

        [Required]
        [DataType(DataType.Date)]

        public DateTime BirthDate { get; set; }

        [Required]
        [RegularExpression("^(male|female|other)$", ErrorMessage = "Gender must be male, female, or other")]

        public string Gender { get; set; }

        public string BusinessDescription { get; set; }

        [MaxLength(255)]
        public string BusinessLogo { get; set; }

        [Required]

        public IFormFile Image { get; set; }

        public string Address { get; set; }
    }
}
