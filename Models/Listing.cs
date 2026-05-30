using System.ComponentModel.DataAnnotations;

namespace SukruUyarBackend.Models
{
    // Sahibinden.com'dan çekeceğimiz ilanların ana şablonu
    public class Listing
    {
        [Key]
        public int Id { get; set; } // Veritabanındaki otomatik artan benzersiz numara

        [Required]
        public string SahibindenId { get; set; } = string.Empty; // Sahibinden ilan numarası (Örn: 123456)

        [Required]
        public string Title { get; set; } = string.Empty; // İlan başlığı

        public string Description { get; set; } = string.Empty; // İlan açıklaması

        public string Category { get; set; } = string.Empty; // Daire, Arsa, İş Yeri vb.

        [Required]
        public string SahibindenUrl { get; set; } = string.Empty; // İlanın orijinal linki

        public bool IsActive { get; set; } = true; // İlan yayında mı, yayından kalktı mı?

        public DateTime CreatedAt { get; set; } = DateTime.Now; // Bizim sistemimize eklenme tarihi

        // İlişkisel Veritabanı Kuralı: Bir ilanın birden fazla resmi olabilir (One-to-Many)
        public List<ListingImage> Images { get; set; } = new List<ListingImage>();
    }

    // İlan resimlerini tutacağımız alt şablon
    public class ListingImage
    {
        [Key]
        public int Id { get; set; }

        public int ListingId { get; set; } // Bu resmin hangi ilana ait olduğunu belirtir (Foreign Key)

        [Required]
        public string ImageUrl { get; set; } = string.Empty; // Resmin internetteki linki
    }
}