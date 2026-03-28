using AIUI.Models; // Chat modelimizi kullanabilmek için
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;

namespace AIUI.Data
{
    // EF Core'un veritabanı özelliklerini kullanmak için DbContext sınıfından miras alıyoruz
    public class AppDbContext : DbContext
    {
        // SQLite içindeki 'Chats' tablomuzu temsil eden koleksiyon
        public DbSet<Chat> Chats { get; set; }

        // Veritabanı bağlantı ayarlarını yaptığımız özel metot
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Veritabanı dosyasının (.db) nereye kaydedileceğini belirliyoruz.
            // WebView2 çerezlerini kaydettiğimiz güvenli AppData klasörünü kullanmak en sağlıklı yöntemdir.
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string dbFolder = Path.Combine(appDataPath, "AIUI_Data");

            // Eğer klasör bilgisayarda yoksa, hata vermemesi için oluşturuyoruz
            if (!Directory.Exists(dbFolder))
            {
                Directory.CreateDirectory(dbFolder);
            }

            // Dosyanın tam yolunu belirliyoruz
            string dbPath = Path.Combine(dbFolder, "AiChats.db");

            // EF Core'a "SQLite kullan ve dosyayı bu yola kaydet" komutunu veriyoruz
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }
    }
}