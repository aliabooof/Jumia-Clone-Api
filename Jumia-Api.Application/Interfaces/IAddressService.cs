using Jumia_Api.Application.Dtos.AddressDtos;
using Jumia_Api.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumia_Api.Application.Interfaces
{
    public interface IAddressService
    {

        // get all addresses of all users
        public Task<IEnumerable<AddressDto>> GetAllAddressesOfAllUsers();


        // get all addresses for a specific user by their user ID
        public Task<IEnumerable<AddressDto>> GetAllAddressesByUserId(string userId);


        // get a specific address by its ID
        public Task<AddressDto> GetAddressById(int addressId);


        // add a new address
        public Task<AddressDto> AddNewAddress(CreateAddressDto address, string userId);


        // update an existing address
        public Task<bool> UpdateAddress(int addressId, UpdateAddressDto dto);


        // delete an address by its ID
        public Task<bool> DeleteAddress(int addressId);

    }
}
