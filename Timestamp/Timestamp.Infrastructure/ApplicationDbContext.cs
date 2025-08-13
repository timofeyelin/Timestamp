using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Timestamp.Application.Interfaces;
using Timestamp.Domain.Models;


namespace Timestamp.Infrastructure
{
    public class ApplicationDbContext : DbContext, IApplicationDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) :
            base(options)
        { }

        public DbSet<Values> Values { get; set; }
        public DbSet<Results> Results { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Results>(entity =>
            {
                entity.HasIndex(r => r.FileName).IsUnique();
                entity.Property(r => r.FileName).IsRequired().HasMaxLength(255);
                entity.Property(r => r.DeltaTime).HasColumnType("decimal(18,6)");
                entity.Property(r => r.AverageExecutionTime).HasColumnType("decimal(18,6)");
                entity.Property(r => r.AverageValue).HasColumnType("decimal(18,6)");
                entity.Property(r => r.MedianValue).HasColumnType("decimal(18,6)");
                entity.Property(r => r.MaxValue).HasColumnType("decimal(18,6)");
                entity.Property(r => r.MinValue).HasColumnType("decimal(18,6)");
            });

            modelBuilder.Entity<Values>(entity =>
            {
                entity.Property(v => v.Date).IsRequired();
                entity.Property(v => v.ExecutionTime).HasColumnType("decimal(18,6)");
                entity.Property(v => v.Value).HasColumnType("decimal(18,6)");
                entity.HasOne(v => v.Results)
                      .WithMany(r => r.ValuesList)
                      .HasForeignKey(v => v.ResultsId);
            });
        }
    }

}
