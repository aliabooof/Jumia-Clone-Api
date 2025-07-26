
﻿using Jumia_Api.Application.Dtos.AuthDtos;

﻿using Jumia_Api.Application.Dtos.UserDtos;

using Jumia_Api.Domain.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumia_Api.Application.Interfaces
{
    public interface IUserService
    {

        Task<UserProfileDto> GetUserProfileAsync(string userId);
        Task UpdateUserProfileAsync(string userId, UpdateUserDto updateDto);

        Task<bool> UserExistsAsync(string email);

        Task<IdentityResult> CreateUserAsync(string email, string password, string firstName, string lastName, DateTime birthDate, string gender, string address);
        Task<AppUser> FindByEmailAsync(string email);
        Task<bool> CheckPasswordAsync(AppUser user, string password);

        Task<AppUser> GetUserByIdAsync(string userId);
        Task<IdentityResult> UpdateUserAsync(AppUser user);
        public Task<string?> GetUserRoleAsync(AppUser user);
        public Task AddUserToRoleAsync(AppUser user, string role);


    }
}
