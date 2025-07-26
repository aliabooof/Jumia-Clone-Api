using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumia_Api.Application.Dtos.AuthDtos
{
    public class PasswordSetupDto
    {
        [Required,EmailAddress]
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

        [Required]
        [StringLength(200)]

        public string Address { get; set; }
    }
}
