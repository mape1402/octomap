using Microsoft.EntityFrameworkCore;
using OctoMap.Samples.Basic.Models;

namespace OctoMap.Samples.Basic
{
    /// <summary>
    /// Provides the Entity Framework sample database context.
    /// </summary>
    public sealed class SampleSalesDbContext : DbContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SampleSalesDbContext"/> class.
        /// </summary>
        /// <param name="options">The context options.</param>
        public SampleSalesDbContext(DbContextOptions<SampleSalesDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Gets the product set used by the projection sample.
        /// </summary>
        public DbSet<Product> Products { get; set; }

        /// <inheritdoc/>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>().HasKey(product => product.Sku);
        }
    }
}
