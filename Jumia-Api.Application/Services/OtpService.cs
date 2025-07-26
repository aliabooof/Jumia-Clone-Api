using Jumia_Api.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumia_Api.Application.Services
{
    public class OtpService : IOtpService
    {
        private readonly IMemoryCache _cache;

        public OtpService(IMemoryCache cache)
        {
            _cache = cache;
        }
        public string GenerateOtp(string email)
        {
            var otp = new Random().Next(1000, 9999).ToString();
            _cache.Set(email, otp, TimeSpan.FromMinutes(5));
            return otp;
        }

        public void RemoveOtp(string email)
        {
            _cache.Remove(email);
        }

        public bool ValidateOtp(string email, string otp)
        {
            if(_cache.TryGetValue(email, out string storedOtp))
            {
                return storedOtp == otp;
            }
            return false;

        }
    }
}
