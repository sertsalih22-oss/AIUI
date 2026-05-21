using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreadBase.Models
{
    public class Chat
    {
        // SQLite veritabanında her sohbeti ayıracak benzersiz anahtar
        public int Id { get; set; }

        // Sohbetin başlığı (Örn: "C# ile SQLite Bağlantısı")
        public string Title { get; set; }

        // Gemini URL'si (Örn: "https://gemini.google.com/app/...")
        public string Url { get; set; }

        // Gruplama için kategori (Örn: "Yazılım", "Oyun Fikirleri", "İngilizce")
        public int CategoryId { get; set; }

        // Eklenme tarihi
        public DateTime AddedDate { get; set; } = DateTime.Now;
        // SİHİRLİ DOKUNUŞ:
        // JS'den gelen 'Category' metnini yakalamak için geçici bir property tanımlıyoruz.
        // [NotMapped] niteliği (attribute) EF Core'a diyor ki: 
        // "Bu alanı sadece bellekte kullan, sakın veritabanında böyle bir sütun oluşturmaya çalışma!"
        [NotMapped]
        public string Category { get; set; }
        public string AIType
        {
            get
            {
                if (string.IsNullOrEmpty(Url)) return "Bilinmeyen";

                // URL'in içindeki domain yapısını güvenli bir şekilde analiz ediyoruz
                if (Url.Contains("gemini.google.com")) return "Gemini";
                if (Url.Contains("chatgpt.com") || Url.Contains("chat.openai.com")) return "ChatGPT";
                if (Url.Contains("claude.ai")) return "Claude";

                return "Web AI"; // Gelecekte eklenebilecek diğer standart web modelleri için
            }
        }
    }


}
