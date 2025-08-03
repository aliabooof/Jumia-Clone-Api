using Jumia_Api.Domain.Interfaces.Repositories;
using Jumia_Api.Domain.Interfaces.UnitOfWork;
using Jumia_Api.Infrastructure.Presistence.Context;
using Jumia_Api.Infrastructure.Presistence.Repositories;
using Jumia_Api.Infrastructure.Presistence.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace Jumia_Api.Api.DependencyInjection.Domain
{
    public static class DomainServicesRegistraion
    {
        public static IServiceCollection AddDomain(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IProductRepo, ProductRepo>();
            services.AddScoped<ICategoryRepo, CategoryRepository>();
            services.AddScoped(typeof(IGenericRepo<>), typeof(GenericRepo<>));
            services.AddScoped<IProductAttributeRepo, ProductAttributeRepo>();

            services.AddScoped<ICouponRepo, CouponRepository>();
            services.AddScoped<IUserCouponRepo, UserCouponRepository>();
            services.AddScoped<IUserCouponRepo, UserCouponRepository>();
            services.AddScoped<ICustomerRepo, CustomerRepo>();

            services.AddScoped<ICartItemRepo, CartItemRepo>();

            return services;

        }
    }
}
