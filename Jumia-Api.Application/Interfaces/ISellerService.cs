using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jumia_Api.Api.Contracts.Results;
using Jumia_Api.Application.Dtos.AuthDtos;
using Jumia_Api.Application.Dtos.SellerDtos;

namespace Jumia_Api.Application.Interfaces
{
    public interface ISellerService
    {
        Task<AuthResult> RegisterAsync(CreateSellerDto dto);
    }
}
