using Microsoft.EntityFrameworkCore;


namespace ApiOzon
{
    public class ShopDbContext : DbContext
    {
        public ShopDbContext(DbContextOptions<ShopDbContext> options) : base(options){}

        public DbSet<GoodsTableDb> GoodsTable { get; set; } = null!;
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GoodsTableDb>((pc =>
            {
                pc.HasKey(u => u.Id);
                pc.ToTable("goods_table");
            }));
        }
        internal object Find(string email)
        {
            throw new NotImplementedException();
        }
    }
}