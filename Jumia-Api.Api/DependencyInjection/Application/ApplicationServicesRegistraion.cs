using Jumia_Api.Application.Interfaces;
using Jumia_Api.Application.MappingProfiles;
using Jumia_Api.Application.Services;


using Jumia_Api.Domain.Interfaces.Repositories;
using Jumia_Api.Domain.Interfaces.UnitOfWork;
using Jumia_Api.Infrastructure.External_Services;
using Jumia_Api.Infrastructure.Presistence.Context;
using Jumia_Api.Infrastructure.Presistence.Repositories;
using Jumia_Api.Infrastructure.Presistence.UnitOfWork;
using Jumia_Api.Services.Implementation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using X.Paymob.CashIn;

namespace Jumia_Api.Api.DependencyInjection.Application
{
    public static class ApplicationServicesRegistraion
    {
        public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<ICouponService, CouponService>();
            services.AddScoped<IUserCouponService, UserCouponService>();

            services.AddScoped<IOrderService, OrderService>();

            //services.AddScoped<IChatRepository, ChatRepository>();
            services.AddScoped<IChatService, ChatService>();
            // Scan all assemblies
            services.AddAutoMapper(cfg => { },typeof(ProductsMapping).Assembly);
            services.AddAutoMapper(cfg => { }, typeof(UserMapping).Assembly);
            services.AddAutoMapper(cfg => { }, typeof(OrderMapping).Assembly);
            services.AddAutoMapper(cfg => { }, typeof(WishlistMapping).Assembly);
            services.AddAutoMapper(cfg => { }, typeof(CouponMapping).Assembly);


            services.AddScoped<IAuthService, AuthService>();


            services.AddMemoryCache();
            services.AddScoped<IOtpService, OtpService>();
            
            services.AddScoped<IUserService, UserService>();

            services.AddScoped<IAddressService, AddressService>();
            services.AddScoped<IAddressRepo, AddressRepo>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            services.AddScoped<ICartService, CartService>();


            services.AddScoped<IFileService, FileService>();

            services.AddPaymobCashIn(conf =>
            {
                conf.ApiKey = configuration["Paymob:ApiKey"];
                conf.Hmac = configuration["Paymob:HmacKey"];
            });
            

            services.AddHttpClient<IPaymentService,PaymentService>();
            services.AddScoped<IRatingService, RatingService>();
            

            services.AddScoped<IWishlistService, WishlistService>();
            services.AddScoped<ISellerService,SellerService>();








            return services;

        }
    }
}
