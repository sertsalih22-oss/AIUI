using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIUI_0._1.Models
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
        public string Category { get; set; }

        // Eklenme tarihi
        public DateTime AddedDate { get; set; } = DateTime.Now;

        public void OpenChatInBrowser(string url)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true // Bu ayar, URL'yi sistemin varsayılan tarayıcısında açmasını sağlar
            });
        }
    }


}
