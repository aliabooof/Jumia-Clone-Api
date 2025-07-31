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
        Task<IEnumerable<SellerInfo>> GetAll();

        Task<bool> IsVerified(int sellerId);
        Task<bool> ToggleBlock(int sellerId);
        Task<AuthResult> RegisterAsync(CreateSellerDto dto);

        Task<IEnumerable<SellerInfo>> GetSellerById(int sellerId);
    }
}
