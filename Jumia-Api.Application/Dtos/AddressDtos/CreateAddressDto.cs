using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumia_Api.Application.Dtos.AddressDtos
{
    public class CreateAddressDto
    {
       
        public string StreetAddress { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
        public string PhoneNumber { get; set; }
        public bool IsDefault { get; set; } = false;
        public string AddressName { get; set; } = "Home";
    }
}
