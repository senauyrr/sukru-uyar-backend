using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SukruUyarBackend.Data;
using SukruUyarBackend.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace SukruUyarBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // Bu istasyonun adresi: api/listings olacak
    public class ListingsController : ControllerBase
    {
        private readonly AppDbContext _context;

        // Veritabanı köprümüzü (DbContext) içeriye enjekte ediyoruz
        public ListingsController(AppDbContext context)
        {
            _context = context;
        }

        // 1. TÜM İLANLARI GETİR (Görselleriyle Birlikte)
        // Adres: GET api/listings
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Listing>>> GetListings()
        {
            return await _context.Listings
                .Include(l => l.Images) // İlişkili resimleri de veritabanından çek getir
                .OrderByDescending(l => l.CreatedAt) // En yeni ilan en üstte dursun
                .ToListAsync();
        }

        // 2. TEK BİR İLANIN DETAYINI GETİR
        // Adres: GET api/listings/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Listing>> GetListing(int id)
        {
            var listing = await _context.Listings
                .Include(l => l.Images)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (listing == null)
            {
                return NotFound(new { message = "İlan bulunamadı!" });
            }

            return listing;
        }

        // 3. YENİ İLAN EKLE (Otomasyon botumuz ilanları buraya fırlatacak)
        // Adres: POST api/listings
        [HttpPost]
        public async Task<ActionResult<Listing>> PostListing(Listing listing)
        {
            // Eğer aynı sahibinden ID'sine sahip ilan zaten varsa tekrar ekleme
            var existingListing = await _context.Listings
                .FirstOrDefaultAsync(l => l.SahibindenId == listing.SahibindenId);

            if (existingListing != null)
            {
                return BadRequest(new { message = "Bu ilan zaten sistemde kayıtlı!" });
            }

            _context.Listings.Add(listing);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetListing), new { id = listing.Id }, listing);
        }

        // 4. İLAN SİL
        // Adres: DELETE api/listings/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteListing(int id)
        {
            var listing = await _context.Listings.FindAsync(id);
            if (listing == null)
            {
                return NotFound(new { message = "Silinmek istenen ilan bulunamadı!" });
            }

            _context.Listings.Remove(listing);
            await _context.SaveChangesAsync(); // Cascade kuralı yüzünden resimleri de otomatik silinecek

            return Ok(new { message = "İlan ve bağlı tüm fotoğrafları başarıyla silindi." });
        }

        // 🤖 5. SAHİBİNDEN'DEN İLANLARI ÇEKEN VE VERİTABANINA DİREKT KAYDEDEN METOT
        // Adres: POST api/listings/fetch-sahibinden
        [HttpPost("fetch-sahibinden")]
        public async Task<IActionResult> FetchFromSahibinden()
        {
            using var httpClient = new HttpClient();
            // Kendimizi gerçek bir tarayıcı gibi tanıtıyoruz 🕵️
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            
            try
            {
                // Babanın gerçek sahibinden mağaza linki
                string url = "https://sukruuyar.sahibinden.com/";
                string html = await httpClient.GetStringAsync(url);
                
                // Regex ile HTML içinden başlıkları ve linkleri tereyağından kıl çeker gibi ayıklıyoruz
                var matches = System.Text.RegularExpressions.Regex.Matches(html, @"class=""classifiedTitle""[^>]*>.*?href=""([^""]+)"">([^<]+)", System.Text.RegularExpressions.RegexOptions.Singleline);
                
                int yeniEklenenSayaci = 0;

                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    var ilanLink = "https://sahibinden.com" + match.Groups[1].Value.Trim();
                    var ilanBaslik = match.Groups[2].Value.Trim();
                    
                    // Linkin sonundaki ilan ID'sini Regex ile yakalıyoruz (Örn: /ilan/emlak-konut-satilik-123456/detay -> 123456)
                    var idMatch = System.Text.RegularExpressions.Regex.Match(ilanLink, @"-(\d+)/?detay");
                    string sahibindenId = idMatch.Success ? idMatch.Groups[1].Value : Guid.NewGuid().ToString().Substring(0, 8);

                    // 🛡️ KONTROL: Bu ilan veritabanında zaten var mı? Çift kayıt olmasın!
                    var varMi = await _context.Listings.AnyAsync(l => l.SahibindenId == sahibindenId);
                    
                    if (!varMi)
                    {
                        // 🏙️ Yeni İlan Nesnesini Oluşturuyoruz
                        var yeniIlan = new Listing
                        {
                            SahibindenId = sahibindenId,
                            Title = ilanBaslik,
                            SahibindenUrl = ilanLink,
                            Description = $"{ilanBaslik} - Şükrü Uyar Gayrimenkul güvencesiyle portföyümüzdedir.",
                            Category = ilanBaslik.Contains("Kiralık") ? "Kiralık Daire" : "Satılık Daire", // Başlığa bakıp kategoriyi seziyoruz
                            IsActive = true,
                            CreatedAt = DateTime.Now,
                            Images = new List<ListingImage>
                            {
                                // Arayüzde ilanlar boş kalmasın diye şık bir konut resmi bağlıyoruz varsayılan olarak
                                new ListingImage { ImageUrl = "https://images.unsplash.com/photo-1564013799919-ab600027ffc6" }
                            }
                        };

                        _context.Listings.Add(yeniIlan);
                        yeniEklenenSayaci++;
                    }
                }

                // Eğer yeni ilanlar bulduysak hepsini tek seferde veritabanına mühürlüyoruz 🔑
                if (yeniEklenenSayaci > 0)
                {
                    await _context.SaveChangesAsync();
                }
                
                return Ok(new { message = $"Sahibinden başarıyla tarandı! {matches.Count} ilan incelendi, {yeniEklenenSayaci} adet yeni ilan sisteme kaydedildi!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = $"İlanlar çekilirken bir sorun oluştu: {ex.Message}" });
            }
        }
    } // ListingsController sınıfının kapanışı
} // Namespace kapanışı