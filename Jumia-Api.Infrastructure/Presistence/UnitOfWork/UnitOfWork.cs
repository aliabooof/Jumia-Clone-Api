using Jumia_Api.Domain.Interfaces.Repositories;
using Jumia_Api.Domain.Interfaces.UnitOfWork;
using Jumia_Api.Domain.Models;
using Jumia_Api.Infrastructure.Presistence.Context;
using Jumia_Api.Infrastructure.Presistence.Repositories;
using Microsoft.AspNetCore.Identity;

namespace Jumia_Api.Infrastructure.Presistence.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly JumiaDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private ICategoryRepo? _categoryRepo;
        private IProductRepo? _productRepo;
        private IProductAttributeRepo? _productAttributeRepo;

        private IOrderRepository? _orderRepository;
        private IProductAttributeValueRepo? _productAttributeValueRepo;
        private IAddressRepo? _addressRepo;
        private IVariantRepo _variantRepo;
        private ICartRepo? _cartRepo;
        private ICartItemRepo? _cartItemRepo;
        private ICustomerRepo? _customerRepo;
        private IWishlistItemRepo? _wishlistItemRepo;
        private IWishlistRepo? _wishlistRepo;
        private ISellerRepo? _sellerRepo;   
        private IVariantAttributeRepo? _variantAttributeRepo;
        private IRatingRepo? _ratingRepo;
        private IOrderItemRepo? _orderItemRepo;

        private IsuborderRepo? _subOrderRepo;
        private ICampaignJobRequestRepo? _campaignJobRequestRepo;   

        private readonly Dictionary<Type, object> _repositories = new();
        private ICouponRepo? _couponRepo;



        public UnitOfWork(JumiaDbContext context)
        {
            _context = context;

        }

        public IProductRepo ProductRepo => _productRepo ??= new ProductRepo(_context);

        public ICategoryRepo CategoryRepo => _categoryRepo ?? new CategoryRepository(_context);

        public ICouponRepo CouponRepo => _couponRepo ?? new CouponRepository(_context);

        public IUserCouponRepo UserCouponRepo => new UserCouponRepository(_context);
        public IsuborderRepo SubOrderRepo => _subOrderRepo ?? new SuborderRepo(_context);


        public IProductAttributeRepo ProductAttributeRepo => _productAttributeRepo ?? new ProductAttributeRepo(_context);



        public IAddressRepo AddressRepo => _addressRepo?? new AddressRepo(_context);

        public ICartRepo CartRepo => _cartRepo ?? new CartRepo(_context);

        public ICartItemRepo CartItemRepo => _cartItemRepo ?? new CartItemRepo(_context);

        public ICustomerRepo CustomerRepo => _customerRepo ?? new CustomerRepo(_context);


        public IOrderRepository OrderRepo => _orderRepository ?? new OrderRepository(_context);

        public IRatingRepo RatingRepo => _ratingRepo ?? new RatingRepo(_context);

        public IWishlistItemRepo WishlistItemRepo => _wishlistItemRepo ?? new WishlistItemRepo(_context);
        public IWishlistRepo WishlistRepo => _wishlistRepo ?? new WishlistRepo(_context);

        public IVariantRepo VariantRepo => _variantRepo ?? new VariantRepo(_context);

        public IProductAttributeValueRepo ProductAttributeValueRepo => _productAttributeValueRepo ?? new ProductAttributeValueRepo(_context);

        public IVariantAttributeRepo VariantAttributeRepo => _variantAttributeRepo ?? new VariantAttributeRepo(_context);

        public ISellerRepo SellerRepo => _sellerRepo ?? new SellerRepo(_context);

        public IOrderItemRepo OrderItemRepo => _orderItemRepo ?? new OrderItemRepo(_context);

        public ICampaignJobRequestRepo CampaignJobRequestRepo => _campaignJobRequestRepo ?? new CampaignJobRequestRepo(_context);

        public void Dispose()
        {
            _context.Dispose();
        }

        public IGenericRepo<T> Repository<T>() where T : class
        {
            if (_repositories.TryGetValue(typeof(T), out var repo))
                return (IGenericRepo<T>)repo;

            var newRepo = new GenericRepo<T>(_context);
            _repositories.Add(typeof(T), newRepo);
            return newRepo;
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task<int> SaveChangesAsync(CancellationToken token)
        {
            return await _context.SaveChangesAsync(token);
        }
    }
}