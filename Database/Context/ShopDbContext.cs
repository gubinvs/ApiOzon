using Microsoft.EntityFrameworkCore;
using ApiOzon.Core;

namespace ApiOzon
{
    public class ShopDbContext : DbContext
    {
        public ShopDbContext(
            DbContextOptions<ShopDbContext> options)
            : base(options)
        {
        }

        public DbSet<GoodsTableDb> GoodsTable { get; set; } = null!;

        public DbSet<WarehouseDb> Warehouse { get; set; } = null!;

        public DbSet<SkuOzonDb> SkuOzon { get; set; } = null!;

        public DbSet<OzonDeliveryPointDb> OzonDeliveryPoints { get; set; } = null!;
        public DbSet<OzonDeliverySyncState> OzonDeliverySyncStates { get; set;}



        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GoodsTableDb>(pc =>
            {
                pc.HasKey(u => u.Id);
                pc.ToTable("goods_table");
            });

            modelBuilder.Entity<WarehouseDb>(pc =>
            {
                pc.HasKey(u => u.Id);
                pc.ToTable("warehouse");
            });

            modelBuilder.Entity<SkuOzonDb>(pc =>
            {
                pc.HasKey(u => u.Id);
                pc.ToTable("skuOzon");
            });

            modelBuilder.Entity<OzonDeliveryPointDb>(pc =>
            {
                pc.HasKey(u => u.Id);
                pc.ToTable("ozonDeliveryPoints");

            });
        }

        internal object Find(string email)
        {
            throw new NotImplementedException();
        }
    }
}