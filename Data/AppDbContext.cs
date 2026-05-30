using Microsoft.EntityFrameworkCore;
using SukruUyarBackend.Models;

namespace SukruUyarBackend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Veritabanımızda oluşacak tabloları tanımlıyoruz
        public DbSet<Listing> Listings { get; set; }
        public DbSet<ListingImage> ListingImages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Bir ilan silindiğinde ona ait fotoğrafların da otomatik silinmesi kuralı (Cascade)
            modelBuilder.Entity<ListingImage>()
                .HasOne<Listing>()
                .WithMany(l => l.Images)
                .HasForeignKey(img => img.ListingId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}