using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;
using FlowerShop.Models.Entities;
using MySql.Data.EntityFramework;

namespace FlowerShop.DAL
{
    [DbConfigurationType(typeof(MySqlEFConfiguration))]
    public class FlowerShopDbContext : DbContext
    {
        static FlowerShopDbContext()
        {
            // Chỉ đăng ký initializer 1 lần cho cả app domain (bản cũ gọi trong constructor
            // nên mỗi request đều SetInitializer lại -> tốn vô ích khi tải cao).
            Database.SetInitializer(new FlowerShopDbInitializer());
        }

        public FlowerShopDbContext() : base("FlowerShopConnectionString")
        {
            ConfigureForHighThroughput();
        }

        public FlowerShopDbContext(string connectionStringName) : base(connectionStringName)
        {
            ConfigureForHighThroughput();
        }

        /// <summary>
        /// Tắt lazy loading + proxy: mọi truy vấn phải khai báo rõ Include/projection.
        /// Đây là biện pháp chống N+1 quan trọng nhất - bản cũ bật lazy loading nên trang
        /// danh sách 12 sản phẩm có thể sinh 13+ query.
        /// </summary>
        private void ConfigureForHighThroughput()
        {
            Configuration.LazyLoadingEnabled = false;
            Configuration.ProxyCreationEnabled = false;
            Configuration.AutoDetectChangesEnabled = true;
            Configuration.ValidateOnSaveEnabled = false;
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Occasion> Occasions { get; set; }
        public DbSet<FlowerType> FlowerTypes { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<AddonItem> AddonItems { get; set; }
        public DbSet<GreetingCard> GreetingCards { get; set; }
        public DbSet<Voucher> Vouchers { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<OrderAddon> OrderAddons { get; set; }
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

            modelBuilder.Entity<OrderDetail>()
                .HasRequired(od => od.Order)
                .WithMany(o => o.OrderDetails)
                .HasForeignKey(od => od.OrderId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<OrderAddon>()
                .HasRequired(oa => oa.Order)
                .WithMany(o => o.OrderAddons)
                .HasForeignKey(oa => oa.OrderId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<PaymentTransaction>()
                .HasRequired(pt => pt.Order)
                .WithMany(o => o.PaymentTransactions)
                .HasForeignKey(pt => pt.OrderId)
                .WillCascadeOnDelete(true);

            // Review không được xoá dây chuyền Product -> người dùng có thể xoá sản phẩm,
            // nhưng phải kiểm soát bằng nghiệp vụ (IsActive) thay vì xoá cứng.
            modelBuilder.Entity<Review>()
                .HasRequired(r => r.Product)
                .WithMany(p => p.Reviews)
                .HasForeignKey(r => r.ProductId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<ProductImage>()
                .HasRequired(i => i.Product)
                .WithMany(p => p.Images)
                .HasForeignKey(i => i.ProductId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<PasswordResetToken>()
                .HasRequired(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .WillCascadeOnDelete(true);

            // Chỉ mục được tạo bằng SQL script (xem DAL/DbIndexProvisioner + file dump)
            // để giống 100% giữa môi trường Laragon import tay và môi trường auto-create.
        }

        /// <summary>
        /// Truy vấn chỉ đọc: bỏ tracking để giảm bộ nhớ và tăng tốc khi tải cao.
        /// </summary>
        public IQueryable<T> ReadOnly<T>() where T : class
        {
            return Set<T>().AsNoTracking();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
