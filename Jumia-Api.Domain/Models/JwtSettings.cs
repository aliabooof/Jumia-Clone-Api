using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumia_Api.Domain.Models
{
    public class JwtSettings
    {
        public string Key { get; set; } 
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public int DurationInDays { get; set; }

    }
}
