using Jumia_Api.Domain.Interfaces.Repositories;
using Jumia_Api.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumia_Api.Domain.Interfaces.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {

        IProductAttributeRepo ProductAttributeRepo { get; }
        IProductRepo ProductRepo { get; }
        IOrderItemRepo OrderItemRepo { get; }
        IAddressRepo AddressRepo { get; }

        ICartRepo CartRepo { get; }
        IGenericRepo<T> Repository<T>() where T : class;
        ICategoryRepo CategoryRepo { get; }

        ICouponRepo CouponRepo { get; }

        IUserCouponRepo UserCouponRepo { get; }
        IsuborderRepo SubOrderRepo { get; }



        IProductAttributeValueRepo ProductAttributeValueRepo { get; }
        ICartItemRepo CartItemRepo { get; }
        ICustomerRepo CustomerRepo { get; }
        ISellerRepo SellerRepo { get; }
        IOrderRepository OrderRepo { get; }
        IVariantAttributeRepo VariantAttributeRepo { get; }
        IVariantRepo VariantRepo { get; }
        IRatingRepo RatingRepo { get; }


        IWishlistRepo WishlistRepo { get; }
        IWishlistItemRepo WishlistItemRepo { get; }
        ICampaignJobRequestRepo CampaignJobRequestRepo { get; }

        Task<int> SaveChangesAsync();
        Task<int> SaveChangesAsync(CancellationToken token);


    }
}
