using Jumia_Api.Domain.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Jumia_Api.Infrastructure.Presistence.Context
{
    public class JumiaDbContext : IdentityDbContext<AppUser>
    {
        public JumiaDbContext(DbContextOptions<JumiaDbContext> options) : base(options)
        {
        }
        public virtual DbSet<Chat> Chats { get; set; }
        public virtual DbSet<ChatMessage> ChatMessages { get; set; }

        public virtual DbSet<Address> Addresses { get; set; }

        public virtual DbSet<Admin> Admins { get; set; }
     
        public virtual DbSet<Affiliate> Affiliates { get; set; }

        public virtual DbSet<AffiliateCommission> AffiliateCommissions { get; set; }

        public virtual DbSet<AffiliateSellerRelationship> AffiliateSellerRelationships { get; set; }

        public virtual DbSet<AffiliateWithdrawal> AffiliateWithdrawals { get; set; }

        public virtual DbSet<Cart> Carts { get; set; }

        public virtual DbSet<CartItem> CartItems { get; set; }

        public virtual DbSet<Category> Categories { get; set; }

        public virtual DbSet<Coupon> Coupons { get; set; }

        public virtual DbSet<Customer> Customers { get; set; }

        public virtual DbSet<HelpfulRating> HelpfulRatings { get; set; }

        public virtual DbSet<Order> Orders { get; set; }

        public virtual DbSet<OrderItem> OrderItems { get; set; }

        public virtual DbSet<Product> Products { get; set; }

        public virtual DbSet<ProductAttribute> ProductAttributes { get; set; }

        public virtual DbSet<ProductAttributeValue> ProductAttributeValues { get; set; }

        public virtual DbSet<ProductImage> ProductImages { get; set; }

        public virtual DbSet<ProductVariant> ProductVariants { get; set; }

        public virtual DbSet<Rating> Ratings { get; set; }

        public virtual DbSet<ReviewImage> ReviewImages { get; set; }

        public virtual DbSet<Seller> Sellers { get; set; }

        public virtual DbSet<SubOrder> SubOrders { get; set; }

        public virtual DbSet<UserCoupon> UserCoupons { get; set; }
 
        public virtual DbSet<VariantAttribute> VariantAttributes { get; set; }

        public virtual DbSet<Wishlist> Wishlists { get; set; }

        public virtual DbSet<WishlistItem> WishlistItems { get; set; }

        public DbSet<CampaignJobRequest> CampaignJobRequests { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SubOrder>()
                .HasOne(s => s.Order)
                .WithMany(o => o.SubOrders)
                .HasForeignKey(s => s.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Rating>()
                    .Property(r => r.IsVerified)
                    .HasDefaultValue("pending");


            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.SubOrder)
                .WithMany(s => s.OrderItems)
                .HasForeignKey(oi => oi.SubOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Chat>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
                entity.Property(e => e.UserName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.UserEmail).IsRequired().HasMaxLength(255);
                entity.Property(e => e.AdminId).HasMaxLength(450);
                entity.Property(e => e.AdminName).HasMaxLength(100);
                entity.Property(e => e.Status).HasConversion<string>();

                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.AdminId);
                entity.HasIndex(e => e.Status);
            });

            // ChatMessage configuration
            modelBuilder.Entity<ChatMessage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SenderId).IsRequired().HasMaxLength(450);
                entity.Property(e => e.SenderName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Message).IsRequired().HasMaxLength(2000);
                entity.Property(e => e.Type).HasConversion<string>();

                entity.HasOne(e => e.Chat)
                    .WithMany(c => c.Messages)
                    .HasForeignKey(e => e.ChatId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.ChatId);
                entity.HasIndex(e => e.SentAt);
            });
            // Chat configuration
            base.OnModelCreating(modelBuilder);
        }
    }
}
