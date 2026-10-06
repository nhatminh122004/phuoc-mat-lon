using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using FlowerShop.Models.Entities;
using MySql.Data.EntityFramework;

namespace FlowerShop.DAL
{
    [DbConfigurationType(typeof(MySqlEFConfiguration))]
    public class FlowerShopDbContext : DbContext
    {
        public FlowerShopDbContext() : base("FlowerShopConnectionString")
        {
            // Tự động khởi tạo database và seed dữ liệu nếu chưa có
            Database.SetInitializer(new FlowerShopDbInitializer());
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

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

            // Cấu hình quan hệ Cascade Delete hoặc Restrict
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
        }
    }
}
